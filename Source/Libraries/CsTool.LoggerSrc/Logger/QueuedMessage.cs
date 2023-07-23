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
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Data;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using CsTool.Extensions;

    public class QueuedMessage
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Properties

        internal LogPriority LPriority { get; set; }            // Priority of log
        internal string Msg { get; set; }                       // Message
        internal object[] Args { get; set; }                    // Message string arguments
        internal DateTimeOffset LDate { get; set; }             // The date/time of the message
        internal LogCommandAction Command { get; set; }         // Queue command, default is Log.
        internal bool IsException { get; set; }                 // True if message is an exception message
        internal Exception LogException { get; set; }           // Optional Exception 
        internal Int32 ThreadId { get; set; } = Thread.CurrentThread.ManagedThreadId;  // ManagedThreadId if the current thread

        #endregion Properties

        //
        // -----------------------------------------------------------------------------------------
        //
        #region Initialisation

        /// <summary>
        /// Initialisation for class <code>QueuedLogMessage</code>
        /// </summary>
        public QueuedMessage()
        {
            LPriority = LogPriority.Info;
            LDate = DateTimeOffset.Now;
            Msg = System.String.Empty;
        }

        public QueuedMessage(string logMsg)
        {
            LPriority = LogPriority.Info;
            LDate = DateTimeOffset.Now;
            Msg = logMsg;
        }

        public QueuedMessage(DateTimeOffset logDate, string logMsg)
        {
            LPriority = LogPriority.Info;
            LDate = logDate;
            Msg = logMsg;
        }

        public QueuedMessage(string logMsg, object[] logArgs)
        {
            LPriority = LogPriority.Info;
            LDate = DateTimeOffset.Now;
            Msg = logMsg;
            Args = logArgs;
        }

        public QueuedMessage(DateTimeOffset logDate, string logMsg, object[] logArgs)
        {
            LPriority = LogPriority.Info;
            LDate = logDate;
            Msg = logMsg;
            Args = logArgs;
        }

        public QueuedMessage(LogPriority logPriority, DateTimeOffset logDate, string logMsg)
        {
            LPriority = logPriority;
            LDate = logDate;
            Msg = logMsg;
        }

        public QueuedMessage(LogPriority logPriority, DateTimeOffset logDate, string logMsg, object[] logArgs)
        {
            LPriority = logPriority;
            LDate = logDate;
            Msg = logMsg;
            Args = logArgs;
        }

        public QueuedMessage(LogPriority logPriority, DateTimeOffset logDate, Exception exception, string logMsg, object[] logArgs)
        {
            LPriority = logPriority;
            LDate = logDate;
            Msg = logMsg;
            Args = logArgs;
            LogException = exception;
            IsException = true;
        }

        public QueuedMessage(LogCommandAction logCommand)
        {
            Command = logCommand;
        }

        public QueuedMessage(LogCommandAction logCommand, object[] logArgs)
        {
            Command = logCommand;
            Args = logArgs;
        }

        public QueuedMessage(LogCommandAction logCommand, string logMsg, object[] logArgs)
        {
            Command = logCommand;
            Args = logArgs;
            Msg = logMsg;
        }

        #endregion Initialisation

    }
}
