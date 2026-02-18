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
    using System.Collections.Specialized;
    using System.Xml.Linq;
    using System.Runtime.CompilerServices;

#if DEBUG_LOGGER_EARLY_DESTRUCTION
    // Exploring using a wrapper class instance to encourage other class instance to be destroyed
    // before the logger is closed. This did not work as expect so the problem is elsewhere.
    // What is the problem: Placing a log message in a class destructor does not necessarily get logged
    // because that class destructor is being called after the log file and message queue are closed.
    // The late destructor is running as its late save action works.
    public class LogHandle
    {
        public LogBase Instance { get; } = Logger.Instance;

        public LogHandle()
        {
            Logger.Write(LogPriority.Verbose,"LogHandle");
        }

        ~LogHandle()
        {
            Logger.Write(LogPriority.Verbose, "~LogHandle");
        }
    }
#endif // DEBUG_LOGGER_EARLY_DESTRUCTION
    /// <summary>
    /// A Singleton instance for the Async/thread safe logger <code>ILogBase</code>.
    /// This provides a static entry point for logging without the need to
    /// separately instantiate and configure the logger before use.
    /// The underlying logger <code>LogBase</code> may be instantiated multiple times
    /// for independent logging streams.
    /// In theory (not validated), one may also extend the LogBase or Logger classes for your own needs.
    /// </summary>
    public sealed partial class Logger : IDisposable
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Initialisation

        /// <summary>
        /// Singleton interface to the logger.
        /// Simplest Usage, no initialisation is necessary:
        /// <code>
        /// Logger.Write("Hello World");
        /// Logger.Write(LogPriority.Verbose,"I'd really just like to say Gidday, sounds more Australian :)");
        /// Logger.Write(exception,"Oh no :( Possible cause: Par0({0}) Par1({1})", par0, par1);
        /// Logger.Write(LogPriority.Fatal,"Bugger Goodbye");
        /// </code>
        ///
        /// Multiple Logger interface: Each individual logger is created using new LogBase("LoggerName"):
        /// <code>
        /// LogBase logger1 = new LogBase("Logger1");
        /// LogBase logger2 = new LogBase("Logger2");
        /// logger1.Write("Hello World");
        /// logger2.Write("Hello World");
        /// </code>
        /// </summary>
        /// <remarks>
        /// --------------------------------------------------------------------------------------------------------------------------
        /// Reference information for the use of a Singleton Instance rather than a static class.
        /// --------------------------------------------------------------------------------------------------------------------------
        /// Updated Aug 2024 to take advantage of lazy initialisation over thread locking or simple static initialisation.
        /// Original source: https://www.dotnetperls.com (actual article no longer available) for reasons 
        /// why the singleton approach has been used. The author's code base has used Singleton for many years with much success
        /// except for the rare start up race condition. Code tweaks and the use of Lazy<> and locks seems to tidy this up nicely.
        /// 
        /// Part of the original dotnetperls article is still available at http://www.dotnetperls.com/static and other sections for 
        /// reasons why the singleton approach has been used.
        /// - "static readonly" thread safety
        /// - other resources indicate having a public property get is not needed
        ///
        /// CSharpInDepth also has an excellent article on the topic. CsTool original approach was fine, though inclusion of Lazy is an
        /// improvement tweak based on more recent implementation approaches. The aim here is for consistency across the code platform.
        /// 
        /// https://csharpindepth.com/Articles/Singleton (Fourth version, not quite lazy but thread-safe without using locks - yah)
        /// - instance can be public static readonly, no property needed. Super simples. Key is static readonly on Instance. Having
        /// a static private empty constructor is not necessary as we are aiming for Lazy instantiation.
        /// 
        /// So why do we need Lazy given all the information above? I have no idea, for now we have it and it works. We do not need
        /// a super efficient initialisation interface as it is once only.
        /// 
        /// Smarter Lazy: https://thecodeman.net/posts/how-to-use-singleton-in-multithreading
        /// Alternative popular double locking:
        ///      ** https://csharpindepth.com/Articles/Singleton
        ///         https://www.c-sharpcorner.com/article/singleton-design-pattern-in-c-sharp-part-one/
        ///         https://medium.com/@mitesh_shah/improvements-and-implementations-of-the-singleton-pattern-53365c2e19e
        ///         https://resulhsn.medium.com/better-implementation-of-singleton-pattern-in-net-167b3299b478
        ///
        /// We don't need everything and simplicity is the key. But from the csharpindepth article, it appears the safest and
        /// recommended approach is Method 6, or method 5. We are now using method 6 as per new developer advice. I like method 5
        /// and used method 4 in the past without issue. 
        /// 
        /// We want the Singleton Logger to be initialised early as we expect it to be available early and live late
        /// to get all pending logs written to file (a work in progress). I am not sure if the early initialisation is achieved.
        /// To assist when time permits, the raw log interface Log.Write is available for early logging.
        /// 
        /// The instance was originally accessed like this, but initialisation was not thread safe. Good for most applications.
        /// <code>
        ///     public static readonly LogBase Instance = new LogBase();
        /// </code>
        /// 
        /// But now the preference is to use built in thread safety through Lazy<> then apply locking mechanisms for the data
        /// only when necessary (improvements could be applied still).
        /// 
        /// <code>
        ///     private static readonly Lazy<LogBase> instance = new Lazy<LogBase>(() => new LogBase());
        ///     public static LogBase Instance { get => instance.Value; }
        /// </code>
        /// 
        /// The use of <code>sealed class Logger</code> is to prevent inheritance of the Singleton instance class. It's inclusion is questionable.
        /// 
        /// Testing Status Nov 2025: Concurrency issues cannot be produced. Initialisation seems to work fine though during debug trapping
        /// there can be an issue. Ideally we want the class to be initialised as early as possible.
        /// 
        /// Jan 2026: Discovered a case where Logger.Dispose() is being called and the queue destroyed before other classes have
        /// been destroyed. It looks like the AppDomain.CurrentDomain.ProcessExit signal is being received too early so this
        /// new feature aimed to ensure the file is closed nicely with a closing log message has its own problems.
        /// - we must then try going back to a destructor or as recommended (and not recommended) IDisposable to close the file.
        /// </remarks>

        private static readonly Lazy<LogBase> instance = new Lazy<LogBase>(() => new LogBase());
        private bool disposedValue;

        /// <summary>
        /// Accessor for the Singleton instance of the LogBase. Alternatively access the same singleton
        /// instance through static class <code>Logger</code> which provides a static interface using
        /// the more common logging naming conventions such as "Logger.Write()".
        /// </summary>
        public static LogBase Instance { get => instance.Value; }

        /// <summary>
        /// Original notes: Explicit static constructor for class <code>LoggerSingleton</code> to tell C# compiler
        /// not to mark type as beforefieldinit.
        /// </summary>
        /// <remarks>
        /// Simplest Usage, no initialisation is necessary:
        /// <code>
        /// Logger.Write("Hello World");
        /// Logger.Write(LogPriority.Fatal,"Goodbye");
        /// </code>
        ///
        /// Multiple Logger interface: Each individual logger is created using new LogBase("LoggerName"):
        /// <code>
        /// LogBase logger1 = new LogBase("Logger1");
        /// LogBase logger2 = new LogBase("Logger2");
        /// logger1.Write("Hello World");
        /// logger2.Write("Hello World");
        /// </code>
        /// </remarks>
        static Logger()
        {
        }

        /// <summary>
        /// Do not use me. Use new LogBase() instead: <code>LogBase logger1 = new LogBase("Logger1");</code>
        /// This initialiser is only public to allow override by LoggerWPF and should not be called directly.
        /// </summary>
        /// <remarks>
        /// The initialiser is not meant ot be called. It is public to allow this class to be inherited and extended.
        /// </remarks>
        public Logger()
        {
            throw new NotSupportedException("Do not use Logger class directly for instantiating individual loggers. Use class LogBase() instead.");
        }

        #endregion Initialisation

        //
        // -----------------------------------------------------------------------------------------
        //
        #region Read only Properties
        public static string FullLogFileName { get => Instance.FullLogFileName; }
        public static string LogFilePath { get => Instance.LogFilePath; }
        public static string LogFileName { get => Instance.LogFileName; }
        public static double AddMessageMaxTime { get => Instance.AddMessageMaxTime; }
        public static uint CountLoggedMessages { get => Instance.CountLoggedMessages; }
        public static ulong CountLoggedMessagesTotal { get => Instance.CountLoggedMessagesTotal; }
        public static ulong CountLostMessagesTotal { get => Instance.CountLostMessagesTotal; }
        public static uint CountLoggedErrors { get => Instance.CountLoggedErrors; }
        public static int LogQueueCount { get => Instance.LogQueueCount; }

        #endregion Read only Properties

        #region Tuneable Properties

        /// <summary>
        /// Specifies the left portion of the log file name. The default is the application name.
        /// e.g. {FilePrepend}_{UserName}_{FileNameDateFormat}.log
        /// </summary>
        public static string FilePrepend { get => Instance.FilePrepend; set => Instance.FilePrepend = value; }

        /// <summary>
        /// Specifies the date format for the log file name. The default is no date. Example "yyyy-MM-dd".
        /// </summary>
        public static string FileNameDateFormat { get => Instance.FileNameDateFormat; set => Instance.FileNameDateFormat = value; }

        //public static int MaximumLogQueueSize { get => Instance.MaximumLogQueueSize; set => Instance.MaximumLogQueueSize = value; }

        /// <summary>
        /// Enable Console Logging, only useful for Framework Console applications
        /// </summary>
        public static bool IsConsoleLoggingEnabled { get => Instance.IsConsoleLoggingEnabled; set => Instance.IsConsoleLoggingEnabled = value; }

        public static bool IsLoseMessageOnBufferFull { get => Instance.IsLoseMessageOnBufferFull; set => Instance.IsLoseMessageOnBufferFull = value; }

        /// <summary>
        /// Includes the user name in the log file name: {FilePrepend}_{UserName}_{FileNameDateFormat}.log
        /// </summary>
        public static bool IsUserNameAppended { get => Instance.IsUserNameAppended; set => Instance.IsUserNameAppended = value; }

        public static int AddMessageTimeout { get => Instance.AddMessageTimeout; set => Instance.AddMessageTimeout = value; }

        public static uint CountLoggedMessagesMaximum { get => Instance.CountLoggedMessagesMaximum; set => Instance.CountLoggedMessagesMaximum = value; }

        public static uint CountOldLogFilesToKeep { get => Instance.CountOldLogFilesToKeep; set => Instance.CountOldLogFilesToKeep = value; }

        /// <summary>
        /// The threshold debug priority that will enable log messages to be written.
        /// The lower the integer value of DebugThresholdDefault, the fewer debug messages will be displayed.
        /// Setting the LogThresholdMaxLevel to LogPriority.Never (a very big number) will disable all logging.
        /// </summary>
        public static DebugThresholdLevel LogThresholdMaxLevel { get => Instance.LogThresholdMaxLevel; set => Instance.LogThresholdMaxLevel = value; }

        #endregion Tuneable Properties
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Methods
        //
        // New CsTool.Logger interfaces to handle back end actions through the Async FIFO
        // such as log file rename (messages are flushed before rename occurs)
        //
        // These methods are the latest implementations: Refer to class iLogBase for minimum interfaces.
        //
        public static void Close() => Instance.CloseAndFlush();
        public static void CloseAndFlush(int maxWaitTime = 0, string closeReason = "") => Instance.CloseAndFlush(maxWaitTime, closeReason);
        public static string ConstructExceptionMessage(Exception exception, string simpleMessage) => Instance.ConstructExceptionMessage(exception, simpleMessage);
        public static void DisplayLogFile() => Instance.DisplayLogFile();
        public static bool IsLogPriorityEnabled(LogPriority level) => Instance.IsLogPriorityEnabled(level);
        public static void LogCommand(LogCommandAction logCommand) => Instance.LogCommand(logCommand);
        public static void LogCommand(LogCommandAction logCommand, params object[] args) => Instance.LogCommand(logCommand, args);
        public static bool LoadAppDefaults(object classInstance, string sectionName, string version, string fileName = "",
            bool createIfMissing = true, bool updateIfNeeded = true)
            => Instance.LoadAppDefaults(classInstance, sectionName, version, fileName, createIfMissing, updateIfNeeded);

        /// <summary>
        /// Set the base directory for all logging. The default location is {StartupPath}\Logs.
        /// </summary>
        public static void SetLogDirectory(string preferredPath) => Instance.SetLogDirectory(preferredPath);
        public static void Write(string messageFormat) => Instance.Write(LogPriority.Info, messageFormat);
        public static void Write(string messageFormat, params object[] propertyValues) => Instance.Write(LogPriority.Info, messageFormat, propertyValues);
        public static void Write(LogPriority level, string messageFormat) => Instance.Write(level, messageFormat);
        public static void Write(LogPriority level, string messageFormat, params object[] propertyValues) => Instance.Write(level, messageFormat, propertyValues);
        public static void Write(LogPriority level, string messageFormat, NameValueCollection parameters) => Instance.Write(level, messageFormat, parameters);
        public static void WriteRaw(string rawMessage, params object[] args) => Instance.WriteRaw(LogPriority.Info, rawMessage, args);
        public static void WriteRaw(LogPriority level, string rawMessage, params object[] args) => Instance.WriteRaw(level, rawMessage, args);
        public static void WriteHex(LogPriority level, byte[] byteArray, string rawMessage, params object[] args) => Instance.WriteHex(level, byteArray, rawMessage, args);
        public static void WriteHex(LogPriority level, byte[] byteArray, int maxBytes, string rawMessage, params object[] args) => Instance.WriteHex(level, byteArray, maxBytes, rawMessage, args);
        public static void Write(Exception exception) => Instance.Write(exception);
        public static void Write(Exception exception, string messageFormat) => Instance.Write(LogPriority.Fatal, exception, messageFormat);
        public static void Write(Exception exception, string messageFormat, params object[] propertyValues) => Instance.Write(LogPriority.Fatal, exception, messageFormat, propertyValues);
        public static void Write(LogPriority level, Exception exception, string messageFormat) => Instance.Write(level, exception, messageFormat);
        public static void Write(LogPriority level, Exception exception, string messageFormat, params object[] propertyValues) => Instance.Write(level, exception, messageFormat, propertyValues);

        /// <summary>
        /// Log the message and include the source file reference, method and line number.
        /// </summary>
        /// <param name="level"></param>
        /// <param name="messageFormat"></param>
        /// <param name="propertyValues"></param>
        public static void WriteDebug(LogPriority level, string messageFormat, params object[] propertyValues) => Instance.WriteDebug(level, messageFormat, propertyValues);

        //
        // A few NLog log compatible method names, mainly for some test comparison. TODO Implement more if needed.
        //
        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Info(string messageFormat) => Instance.Write(LogEventLevel.Information, messageFormat, null);
        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Info(string messageFormat, params object[] propertyValues) => Instance.Write(LogEventLevel.Information, messageFormat, propertyValues);
        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Debug(string messageFormat) => Instance.Write(LogEventLevel.Debug, messageFormat, null);
        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Debug(string messageFormat, params object[] propertyValues) => Instance.Write(LogEventLevel.Debug, messageFormat, propertyValues);

        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Log(LogLevel level, string messageFormat, params object[] propertyValues) => Instance.Write((LogPriority)level, messageFormat, propertyValues);

        //
        // Basic Serilog equivalent interfaces (some) to assist if migrating to/from Serilog.
        // Only methods using LogEventLevel as the priority are supported by Sirilog.
        /// <summary>
        /// Serilog compatible log method.
        /// </summary>
        public static void Write(LogEventLevel level, string messageFormat) => Instance.Write(level, messageFormat, null);
        /// <summary>
        /// Serilog compatible log method.
        /// </summary>
        public static void Write(LogEventLevel level, string messageFormat, params object[] propertyValues) => Instance.Write(level, messageFormat, propertyValues);
        /// <summary>
        /// Serilog compatible log method.
        /// </summary>
        public static void Write(LogEventLevel level, Exception exception, string messageFormat) => Instance.Write((LogPriority)level, exception, messageFormat);

        /// <summary>
        /// Legacy Interface for easy exception logging. Use Logger.Write(exception,...) instead.
        /// </summary>
        /// <param name="exception">The exception will be added to the log message</param>
        public static void LogExceptionMessage(Exception exception) => Instance.Write(LogPriority.ErrorProcessing, exception, "Exception");
 
        /// <summary>
        /// Legacy Interface for easy exception logging. Use Logger.Write(exception,...) instead.
        /// </summary>
        /// <param name="exception">The exception will be added to the log message</param>
        public static void LogExceptionMessage(LogPriority level, Exception exception, string progressMessage, params object[] args) => Instance.Write(level, exception, progressMessage, args);

        //
        // GitHub Copilot liked this format but it might have been guessing
        //
        public static void LogException(Exception exception) => Instance.Write(LogPriority.ErrorProcessing, exception, "Exception");
        public static void LogException(Exception exception, string messageFormat) => Instance.Write(LogPriority.Fatal, exception, messageFormat);
        public static void LogException(Exception exception, string messageFormat, params object[] propertyValues) => Instance.Write(LogPriority.Fatal, exception, messageFormat, propertyValues);
        public static void LogException(LogPriority level, Exception exception, string progressMessage, params object[] args) => Instance.Write(level, exception, progressMessage, args);

        //
        // CsTool.Logger interfaces compatible with older/legacy CsTool.CoreUtilities.MyLogger Interfaces
        //

        public static void LogMessage(string messageFormat, params object[] args) => Instance.Write(LogPriority.Info, messageFormat, args);

        public static void LogMessage(LogPriority level, string simpleMessage) => Instance.Write(level, simpleMessage);
        public static void LogMessage(LogPriority level, string messageFormat, params object[] args) => Instance.Write(level, messageFormat, args);

        public static void LogMessage(LogPriority level, string messageFormat, NameValueCollection parameters) => Instance.Write(level, messageFormat, parameters);


        public static void LogRawMessage(LogPriority level, string rawMessage, params object[] args) => Instance.WriteRaw(level, rawMessage, args);

        //
        // Legacy CsTool.CoreUtilities interfaces. Do not use with new applications.
        //
        public static void LogMessageWithStats(string message, bool countAsError = false, bool ignoreExceptions = false) => Instance.LogMessageWithStats(message, countAsError, ignoreExceptions);

        public static void LogMessageWithStats(LogPriority level, string message, bool countAsError = false, bool ignoreExceptions = false) => Instance.LogMessageWithStats(level, message, countAsError, ignoreExceptions);

        //
        // CsTool.Logger debugging. When logging is required prior to the Logger being fully initialised use SafeWrite
        // to ensure a message is logged.
        //
        internal static void SafeWrite(Exception exception, string messageFormat, params object[] propertyValues) => LogBase.SafeWrite(LogPriority.ErrorCritical, exception, messageFormat, propertyValues);
        public static void SafeWrite(LogPriority level, Exception exception, string messageFormat) => LogBase.SafeWrite(level, exception, messageFormat);
        public static void SafeWrite(LogPriority level, Exception exception, string messageFormat, params object[] propertyValues) => LogBase.SafeWrite(level, exception, messageFormat, propertyValues);

        public static void SafeWrite(LogPriority level, string messageFormat, params object[] propertyValues) => LogBase.SafeWrite(level, messageFormat, propertyValues);

        internal static void SafeWrite(string messageFormat, params object[] propertyValues) => LogBase.SafeWrite(LogPriority.Info, messageFormat, propertyValues);

        internal static void SafeWriteDebug(LogPriority level, string messageFormat, params object[] propertyValues) => LogBase.SafeWriteDebug(level, messageFormat, propertyValues);

        private void Dispose(bool disposing)
        {
            CsTool.Logger.Log.Write(LogPriority.Always, "!Logger.Dispose({0})...",disposing);
            if (!disposedValue)
            {
                if (disposing)
                {
                    Logger.Write(LogPriority.Debug, "!Logger Dispose(): Disposing...");
                    // TODO: dispose managed state (managed objects)
                    Instance.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                disposedValue = true;
            }
        }

        // // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~Logger()
        {
            CsTool.Logger.Log.Write(LogPriority.Always, "!Logger Destructor: Disposing...");
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
            CsTool.Logger.Log.Write(LogPriority.Debug, "!Logger Destructor: Disposed");
        }

        void IDisposable.Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion Methods

        //
        // -----------------------------------------------------------------------------------------
        //
        #region Configuration File Interface Methods

        #endregion Configuration File Interface Methods
    }
}
