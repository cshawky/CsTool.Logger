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

    /// <summary>
    /// A Singleton instance for the Async/thread safe logger <code>ILogBase</code>.
    /// This provides an optional static entry point for logging without the need to
    /// separately instantiate and configure the logger before use.
    /// The underlying logger <code>LogBase</code> may be instantiated multiple times
    /// for independent logging streams.
    /// </summary>
    public class Logger
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Initialisation

        /// <summary>
        /// Explicit static constructor for class <code>LoggerSingleton</code> to tell C# compiler
        /// not to mark type as beforefieldinit .
        /// 
        /// Simplest Usage:
        /// 
        /// Logger.Write("Hello World");
        /// Logger.Write(LogPriority.Fatal,"Goodbye");
        /// 
        /// Multiple Logger interface:
        /// 
        /// LogBase logger1 = new LogBase("Logger1");
        /// LogBase logger2 = new LogBase("Logger2");
        /// logger1.Write("Hello World");
        /// logger2.Write("Hello World");
        /// 
        /// </summary>
        static Logger()
        {
        }

        /// <summary>
        /// Initialisation for class <code>LoggerSingleton</code>
        /// This initialiser is only public to allow override by LoggerWPF and should not be called directly.
        /// </summary>
        /// <remarks>
        /// The initialiser is not meant ot be called. It is public to allow this class to be inherited and extended.
        /// </remarks>
        public Logger()
        {
        }

        /// <summary>
        /// Destructor for class <code>LoggerSingleton</code>
        /// </summary>
        ~Logger()
        {
        }

        /// <summary>
        /// Singleton interface to the logger
        /// </summary>
        public static LogBase Instance { get; } = new LogBase();

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
        public static int LogQueueCount { get => Instance.LogQueueCount; }

        #endregion Read only Properties

        #region Tuneable Properties

        public static string FilePrepend { get => Instance.FilePrepend; set => Instance.FilePrepend = value; }

        public static string FileNameDateFilter { get => Instance.FileNameDateFilter; set => Instance.FileNameDateFilter = value; }

        //public static int MaximumLogQueueSize { get => Instance.MaximumLogQueueSize; set => Instance.MaximumLogQueueSize = value; }

        public static bool IsLoseMessageOnBufferFull { get => Instance.IsLoseMessageOnBufferFull; set => Instance.IsLoseMessageOnBufferFull = value; }

        public static int AddMessageTimeout { get => Instance.AddMessageTimeout; set => Instance.AddMessageTimeout = value; }

        public static uint CountLoggedMessagesMaximum { get => Instance.CountLoggedMessagesMaximum; set => Instance.CountLoggedMessagesMaximum = value; }

        public static uint CountOldLogFilesToKeep { get => Instance.CountOldLogFilesToKeep; set => Instance.CountOldLogFilesToKeep = value; }

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
        public static void CloseAndFlush() => Instance.CloseAndFlush();
        public static string ConstructExceptionMessage(Exception exception, string simpleMessage) => Instance.ConstructExceptionMessage(exception, simpleMessage);
        public static void DisplayLogFile() => Instance.DisplayLogFile();
        public static bool IsLogPriorityEnabled(LogPriority priority) => Instance.IsLogPriorityEnabled(priority);
        public static void LogCommand(LogCommandAction logCommand) => Instance.LogCommand(logCommand);
        public static void LogCommand(LogCommandAction logCommand, params object[] args) => Instance.LogCommand(logCommand, args);
        public static void Write(string messageTemplate) => Instance.Write(LogPriority.Info, messageTemplate);
        public static void Write(string messageTemplate, params object[] propertyValues) => Instance.Write(LogPriority.Info, messageTemplate, propertyValues);
        public static void Write(Exception exception, string messageTemplate) => Instance.Write(LogPriority.Fatal, exception, messageTemplate);
        public static void Write(LogPriority level, string messageTemplate) => Instance.Write(level, messageTemplate);
        public static void Write(LogPriority level, string messageTemplate, params object[] propertyValues) => Instance.Write(level, messageTemplate, propertyValues);
        public static void Write(LogPriority level, Exception exception, string messageTemplate) => Instance.Write(level, exception, messageTemplate);
        public static void Write(LogPriority logPriority, string messageFormat, NameValueCollection parameters) => Instance.Write(logPriority, messageFormat, parameters);
        public static void WriteRaw(LogPriority logPriority, string rawMessage, params object[] args) => Instance.WriteRaw(logPriority, rawMessage, args);

        //
        // A few NLog log compatible method names
        //
        public static void Info(string messageTemplate) => Instance.Write(LogEventLevel.Information, messageTemplate, null);
        public static void Info(string messageTemplate, params object[] propertyValues) => Instance.Write(LogEventLevel.Information, messageTemplate, propertyValues);
        public static void Debug(string messageTemplate) => Instance.Write(LogEventLevel.Debug, messageTemplate, null);
        public static void Debug(string messageTemplate, params object[] propertyValues) => Instance.Write(LogEventLevel.Debug, messageTemplate, propertyValues);

        //
        // Basic Serilog equivalent interfaces (some) to assist if migrating to/from Serilog
        //
        public static void Write(LogEventLevel level, string messageTemplate) => Instance.Write(level, messageTemplate, null);
        public static void Write(LogEventLevel level, string messageTemplate, params object[] propertyValues) => Instance.Write(level, messageTemplate, propertyValues);
        public static void Write(LogEventLevel level, Exception exception, string messageTemplate) => Instance.Write((LogPriority)level, exception, messageTemplate);

        //
        // CsTool.Logger interfaces compatible with older/legacy CsTool.CoreUtilities.MyLogger Interfaces
        //
        public static void LogExceptionMessage(LogPriority logPriority, Exception exception, string progressMessage, params object[] args) => Instance.Write(logPriority, exception, progressMessage, args);
        public static void LogMessage(string messageFormat, params object[] args) => Instance.Write(LogPriority.Info, messageFormat, args);

        public static void LogMessage(LogPriority logPriority, string simpleMessage) => Instance.Write(logPriority, simpleMessage);
        public static void LogMessage(LogPriority logPriority, string messageFormat, params object[] args) => Instance.Write(logPriority, messageFormat, args);

        public static void LogMessage(LogPriority logPriority, string messageFormat, NameValueCollection parameters) => Instance.Write(logPriority, messageFormat, parameters);


        public static void LogRawMessage(LogPriority logPriority, string rawMessage, params object[] args) => Instance.WriteRaw(logPriority, rawMessage, args);

        //
        // Legacy CsTool.CoreUtilities interfaces. Do not use with new applications.
        //
        public static void LogMessageWithStats(string message, bool countAsError = false, bool ignoreExceptions = false) => Instance.LogMessageWithStats(message, countAsError, ignoreExceptions);

        public static void LogMessageWithStats(LogPriority logPriority, string message, bool countAsError = false, bool ignoreExceptions = false) => Instance.LogMessageWithStats(logPriority, message, countAsError, ignoreExceptions);

        #endregion Methods
    }
}
