// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2020 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
// Please refer to LICENCE.txt in this project folder.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------
namespace CsTool.Logger
{
    using System;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Collections.Concurrent;

    /// <summary>
    /// Lightweight container for messages queued to the logger (optimised for performance).
    /// Designed to minimise allocations, reduce writable state and allow better JIT inlining.
    /// Supports an internal object pool to reuse instances and avoid allocations under high load.
    /// Implements IDisposable so callers can use a using() pattern which will return the instance
    /// to the pool automatically.
    /// </summary>
    /// <remarks>
    /// GitHub Copilot optimisations from QueuedMessage:
    /// Step 1: Optimise class QueuedMessageOptimised for maximum performance
    /// •	Marked the class sealed.
    /// •	Centralized initialization in a private constructor and applied MethodImplOptions.AggressiveInlining.
    /// •	Made properties immutable where possible. (then reversed for pooling)
    /// •	Captured ThreadId at construction.
    /// •	Used Math.Min for safe RawDataLength calculation.
    /// Step 2: please add an object pool to reuse instances and avoid allocations
    /// •	Introduced a ConcurrentBag<QueuedMessageOptimised> pool with a maxPoolSize.
    /// •	Added static Rent(...) to obtain and initialize instances from the pool.
    /// •	Centralized initialization in an Initialize(...) method and used in constructor
    ///     and Rent(LogPriority, DateTimeOffset?, string, object[], LogCommandAction, Exception, bool, byte[], int).
    /// •	Return() clears references (e.g., Args, RawData) to avoid holding large objects.
    /// •	You should update call sites to use QueuedMessageOptimised.Rent(...) and 
    ///     call .Return() after the instance is no longer needed.
	/// •	Classes should be used with object pool, primarily as structs are always copied. 
	///		TODO Maybe test to confirm as the logging message queue itself may still benefit from struct references.
    /// •	If you want, I can add an IDisposable pattern to make Return more ergonomic.
    /// Step 3: Add IDisposable pattern to QueuedMessageOptimised to automate returning to pool
    /// •	added IDisposable to QueuedMessageOptimised
    /// •	Implemented Dispose() to call Return() and made Return() idempotent using an Interlocked flag.
    /// •	Rent() resets the returned flag when pulling from the pool.
    /// •	Return() clears references and returns the instance to the pool if under the pool cap.
    /// •	Now callers can use using (var msg = QueuedMessageOptimised.Rent(...)) { ... } to automatically return instances.
    /// Step 4: Ask to optimize again: optimize QueuedMessageOptimised for best performance
    /// •	I updated QueuedMessageOptimised to a more performance-focused implementation:
    /// •	Replaced auto-properties with fields to reduce indirection.
    /// •	Kept a concurrent object pool with Rent(LogPriority, DateTimeOffset?, string, object[], LogCommandAction, 
    ///         Exception, bool, byte[], int)/Return() and a max cap.
    /// •	Added AggressiveInlining to hot methods.
    /// •	Implemented IDisposable.Dispose() to return instances automatically.
    /// Usage Notes:
    /// •	Prefer 
    ///         var m = QueuedMessageOptimised.Rent(...); 
    ///         try { ... } 
    ///         finally { m.Return(); } 
    ///     or 
    ///         using (var m = QueuedMessageOptimised.Rent(...)) { ... }.
    /// •	Ensure you don't keep pooled instances alive beyond intended use; Return() clears references.
    /// Step5: Not done here but the queue itself could be optimised to store pooled instances only.
    /// 
    /// </remarks>
    public sealed class QueuedMessageOptimised : IDisposable
    {
        // -----------------------------------------------------------------------------------------
        #region Fields

        // Use fields rather than auto-properties to reduce indirection and method call overhead.
        internal LogPriority LPriority;              // Priority of log message
        internal string Msg;                         // Message
        internal object[] Args;                      // Message string arguments
        internal DateTimeOffset LDate;               // The date/time of the message
        internal LogCommandAction Command;          // Queue command, default is Log.
        internal bool IsException;                   // True if message is an exception message
        internal Exception LogException;             // Optional Exception

        // Captured at initialization time
        internal int ThreadId;

        // Mutable optional fields (kept mutable because callers set these after construction or via Initialize)
        internal byte[] RawData;
        internal int RawDataLength;

        // internal state for pooling / disposal
        private int returnedFlag; // 0 - in use, 1 - returned

        #endregion Fields

        // -----------------------------------------------------------------------------------------
        #region Pooling

        // Simple concurrent pool
        private static readonly ConcurrentBag<QueuedMessageOptimised> pool = new ConcurrentBag<QueuedMessageOptimised>();
        private static int poolCount = 0;
        private static int maxPoolSize = 1024; // cap the pool to avoid unbounded memory use

        public static int CurrentPoolCount => poolCount;
        public static int MaxPoolSize => maxPoolSize;

        /// <summary>
        /// Rent an instance from the pool (or create a new one) and initialize it.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static QueuedMessageOptimised Rent(LogPriority priority = LogPriority.Info,
                                                 DateTimeOffset? date = null,
                                                 string msg = "",
                                                 object[] args = null,
                                                 LogCommandAction command = LogCommandAction.Log,
                                                 Exception exception = null,
                                                 bool isException = false,
                                                 byte[] rawData = null,
                                                 int rawDataLength = 0)
        {
            if (pool.TryTake(out var item))
            {
                Interlocked.Decrement(ref poolCount);
                // mark in-use
                Interlocked.Exchange(ref item.returnedFlag, 0);
                item.Initialize(priority, date ?? DateTimeOffset.Now, msg, args, command, exception, isException, rawData, rawDataLength <= 0 ? 0 : Math.Min((rawData == null) ? 0 : rawData.Length, rawDataLength));
                return item;
            }

            // not available in pool, create new
            return new QueuedMessageOptimised(priority, date ?? DateTimeOffset.Now, msg, args, command, exception, isException, rawData, rawDataLength <= 0 ? 0 : Math.Min((rawData == null) ? 0 : rawData.Length, rawDataLength));
        }

        /// <summary>
        /// Return the instance to the pool for reuse. The instance is reset.
        /// Safe to call multiple times; only the first call has effect.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Return()
        {
            // ensure only returned once
            if (Interlocked.Exchange(ref returnedFlag, 1) == 1)
                return;

            // reset mutable state to avoid holding onto large objects
            Msg = string.Empty;
            Args = null;
            LDate = DateTimeOffset.MinValue;
            Command = LogCommandAction.Log;
            IsException = false;
            LogException = null;
            ThreadId = 0;
            RawData = null;
            RawDataLength = 0;

            // push back to pool if under cap
            if (Interlocked.Increment(ref poolCount) <= maxPoolSize)
            {
                pool.Add(this);
            }
            else
            {
                // pool is full; drop the item and decrement count
                Interlocked.Decrement(ref poolCount);
            }
        }

        #endregion Pooling

        // -----------------------------------------------------------------------------------------
        #region Initialization

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Initialize(LogPriority priority, DateTimeOffset date, string msg, object[] args, LogCommandAction command, Exception exception, bool isException, byte[] rawData, int rawDataLength)
        {
            LPriority = priority;
            LDate = date;
            Msg = msg ?? string.Empty;
            Args = args;
            Command = command;
            LogException = exception;
            IsException = isException;
            ThreadId = Thread.CurrentThread.ManagedThreadId;
            RawData = rawData;
            RawDataLength = rawDataLength <= 0 ? 0 : rawDataLength;

            // mark as in-use
            Interlocked.Exchange(ref returnedFlag, 0);
        }

        // Private constructor used by Rent
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private QueuedMessageOptimised(LogPriority priority, DateTimeOffset date, string msg, object[] args, LogCommandAction command, Exception exception, bool isException, byte[] rawData, int rawDataLength)
        {
            Initialize(priority, date, msg, args, command, exception, isException, rawData, rawDataLength);
        }

        // Convenience overloads kept for compatibility but call into pooling constructor
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised()
            : this(LogPriority.Info, DateTimeOffset.Now, string.Empty, null, LogCommandAction.Log, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(string logMsg)
            : this(LogPriority.Info, DateTimeOffset.Now, logMsg, null, LogCommandAction.Log, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(DateTimeOffset logDate, string logMsg)
            : this(LogPriority.Info, logDate, logMsg, null, LogCommandAction.Log, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(string logMsg, object[] logArgs)
            : this(LogPriority.Info, DateTimeOffset.Now, logMsg, logArgs, LogCommandAction.Log, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(DateTimeOffset logDate, string logMsg, object[] logArgs)
            : this(LogPriority.Info, logDate, logMsg, logArgs, LogCommandAction.Log, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(LogPriority logPriority, DateTimeOffset logDate, string logMsg)
            : this(logPriority, logDate, logMsg, null, LogCommandAction.Log, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(LogPriority logPriority, DateTimeOffset logDate, string logMsg, object[] logArgs)
            : this(logPriority, logDate, logMsg, logArgs, LogCommandAction.Log, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(LogPriority logPriority, DateTimeOffset logDate, Exception exception, string logMsg, object[] logArgs)
            : this(logPriority, logDate, logMsg, logArgs, LogCommandAction.Log, exception, true, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(LogPriority logPriority, DateTimeOffset logDate, string logMsg, byte[] byteArray, int maxBytes)
            : this(logPriority, logDate, logMsg, null, LogCommandAction.Log, null, false, byteArray, (byteArray == null) ? 0 : Math.Min(byteArray.Length, maxBytes))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(LogPriority logPriority, DateTimeOffset logDate, string logMsg, byte[] byteArray, int maxBytes, object[] logArgs)
            : this(logPriority, logDate, logMsg, logArgs, LogCommandAction.Log, null, false, byteArray, (byteArray == null) ? 0 : Math.Min(byteArray.Length, maxBytes))
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(LogCommandAction logCommand)
            : this(LogPriority.Info, DateTimeOffset.Now, string.Empty, null, logCommand, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(LogCommandAction logCommand, object[] logArgs)
            : this(LogPriority.Info, DateTimeOffset.Now, string.Empty, logArgs, logCommand, null, false, null, 0)
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public QueuedMessageOptimised(LogCommandAction logCommand, string logMsg, object[] logArgs)
            : this(LogPriority.Info, DateTimeOffset.Now, logMsg, logArgs, logCommand, null, false, null, 0)
        {
        }

        #endregion Initialisation

        // -----------------------------------------------------------------------------------------
        #region IDisposable

        /// <summary>
        /// Dispose returns the instance to the pool. Safe to call multiple times.
        /// Using this allows callers to use a using(...) pattern.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Dispose()
        {
            Return();
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

    }
}
