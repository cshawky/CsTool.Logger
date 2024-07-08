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
    public partial class Logger
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Initialisation

        /// <summary>
        /// Explicit static constructor for class <code>LoggerSingleton</code> to tell C# compiler
        /// not to mark type as beforefieldinit .
        /// 
        /// Simplest Usage, no initialisation necessary:
        /// 
        /// Logger.Write("Hello World");
        /// Logger.Write(LogPriority.Fatal,"Goodbye");
        /// 
        /// Multiple Logger interface. Each individual logger is created using new LogBase("LoggerName"):
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
        public static uint CountLoggedErrors { get => Instance.CountLoggedErrors; }
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
        // TODO Put method and properties summaries here for intellicode
        //

        public static void Close() => Instance.CloseAndFlush();
        public static void CloseAndFlush() => Instance.CloseAndFlush();
        public static string ConstructExceptionMessage(Exception exception, string simpleMessage) => Instance.ConstructExceptionMessage(exception, simpleMessage);
        public static void DisplayLogFile() => Instance.DisplayLogFile();
        public static bool IsLogPriorityEnabled(LogPriority priority) => Instance.IsLogPriorityEnabled(priority);
        public static void LogCommand(LogCommandAction logCommand) => Instance.LogCommand(logCommand);
        public static void LogCommand(LogCommandAction logCommand, params object[] args) => Instance.LogCommand(logCommand, args);

        /// <summary>
        /// Set the base directory for all logging. The default location is {StartupPath}\Logs.
        /// </summary>
        public static void SetLogDirectory(string preferredPath) => Instance.SetLogDirectory(preferredPath);
        public static void Write(string messageTemplate) => Instance.Write(LogPriority.Info, messageTemplate);
        public static void Write(string messageTemplate, params object[] propertyValues) => Instance.Write(LogPriority.Info, messageTemplate, propertyValues);
        public static void Write(LogPriority level, string messageTemplate) => Instance.Write(level, messageTemplate);
        public static void Write(LogPriority level, string messageTemplate, params object[] propertyValues) => Instance.Write(level, messageTemplate, propertyValues);
        public static void Write(LogPriority logPriority, string messageFormat, NameValueCollection parameters) => Instance.Write(logPriority, messageFormat, parameters);
        public static void WriteRaw(LogPriority logPriority, string rawMessage, params object[] args) => Instance.WriteRaw(logPriority, rawMessage, args);
        public static void Write(Exception exception) => Instance.Write(exception);
        public static void Write(Exception exception, string messageTemplate) => Instance.Write(LogPriority.Fatal, exception, messageTemplate);
        public static void Write(Exception exception, string messageTemplate, params object[] propertyValues) => Instance.Write(LogPriority.Fatal, exception, messageTemplate, propertyValues);
        public static void Write(LogPriority level, Exception exception, string messageTemplate) => Instance.Write(level, exception, messageTemplate);
        public static void Write(LogPriority level, Exception exception, string messageTemplate, params object[] propertyValues) => Instance.Write(level, exception, messageTemplate, propertyValues);

        //
        // A few NLog log compatible method names, mainly for some test comparison. TODO Implement more if needed.
        //
        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Info(string messageTemplate) => Instance.Write(LogEventLevel.Information, messageTemplate, null);
        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Info(string messageTemplate, params object[] propertyValues) => Instance.Write(LogEventLevel.Information, messageTemplate, propertyValues);
        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Debug(string messageTemplate) => Instance.Write(LogEventLevel.Debug, messageTemplate, null);
        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Debug(string messageTemplate, params object[] propertyValues) => Instance.Write(LogEventLevel.Debug, messageTemplate, propertyValues);

        /// <summary>
        /// NLog compatible log method. This method is not recommended for new applications.
        /// </summary>
        public static void Log(LogLevel logLevel, string messageTemplate, params object[] propertyValues) => Instance.Write((LogPriority)logLevel, messageTemplate, propertyValues);

        //
        // Basic Serilog equivalent interfaces (some) to assist if migrating to/from Serilog.
        // Only methods using LogEventLevel as the priority are supported by Sirilog.
        /// <summary>
        /// Serilog compatible log method.
        /// </summary>
        public static void Write(LogEventLevel level, string messageTemplate) => Instance.Write(level, messageTemplate, null);
        /// <summary>
        /// Serilog compatible log method.
        /// </summary>
        public static void Write(LogEventLevel level, string messageTemplate, params object[] propertyValues) => Instance.Write(level, messageTemplate, propertyValues);
        /// <summary>
        /// Serilog compatible log method.
        /// </summary>
        public static void Write(LogEventLevel level, Exception exception, string messageTemplate) => Instance.Write((LogPriority)level, exception, messageTemplate);

        /// <summary>
        /// Legacy Interface for easy exception logging. Use Logger.Write(exception,...) instead.
        /// </summary>
        /// <param name="exception">The exception will be added to the log message</param>
        public static void LogExceptionMessage(Exception exception) => Instance.Write(LogPriority.ErrorProcessing, exception, "Exception");
 
        /// <summary>
        /// Legacy Interface for easy exception logging. Use Logger.Write(exception,...) instead.
        /// </summary>
        /// <param name="exception">The exception will be added to the log message</param>
        public static void LogExceptionMessage(LogPriority logPriority, Exception exception, string progressMessage, params object[] args) => Instance.Write(logPriority, exception, progressMessage, args);

        //
        // GitHub Copilot liked this format but it might have been guessing
        //
        public static void LogException(Exception exception) => Instance.Write(LogPriority.ErrorProcessing, exception, "Exception");
        public static void LogException(Exception exception, string messageTemplate) => Instance.Write(LogPriority.Fatal, exception, messageTemplate);
        public static void LogException(Exception exception, string messageTemplate, params object[] propertyValues) => Instance.Write(LogPriority.Fatal, exception, messageTemplate, propertyValues);
        public static void LogException(LogPriority logPriority, Exception exception, string progressMessage, params object[] args) => Instance.Write(logPriority, exception, progressMessage, args);

        //
        // CsTool.Logger interfaces compatible with older/legacy CsTool.CoreUtilities.MyLogger Interfaces
        //

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
