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

    /// <summary>
    /// The LogPriority provides the debug level where logging occurs and the type of log
    /// entry being made.
    /// </summary>
    /// <remarks>
    /// Logging will occur if the LogPriority of the message is less than LogThresholdMaxLevel.
    /// i.e. Lower LogPriority enumerations have higher priority.
    /// 
    /// The enumeration of LogPriority wraps the LogType into the lower word to categorise
    /// each LogPriority into separate files regardless of relative priority.
    /// </remarks>
    public enum LogPriority : Int32
    {
        // Lowest value
        Always = 0,             // Log requests with this priority will always log
        Fatal = 1,              // Programme cannot continue
        ImportantInfo = 2,      // Equivalent to Info except will be logged if LogPriority.ErrorCritical is enabled
        ErrorCritical = 3,      // Exceptions and bad processing errors likely to require code modification
        ErrorProcessing = 4,    // Normal processing/data errors
        Warning = 5,            // Data processing or warnings (high priority debug messages)
        Info = 6,               // General information
        Debug = 7,              // More detail on processing activities
        Verbose = 8            // Extensive detail on processing where used
        //,Never = 9999            // If requests were to use this level, it would never log
        // Highest value
    }

    /// <summary>
    /// Serilog compatible log priority. This enumeration allows the application to call CsTool.Logger using the Serilog
    /// LogEventLevel enumerations. The underlying enumerations map to LogPriority as expected by this interface.
    /// Restrict your log priorities to this list, if thinking of migrating to Serilog later.
    /// </summary>
    public enum LogEventLevel : Int32
    {
        Verbose = LogPriority.Verbose,
        Debug = LogPriority.Debug,
        Information = LogPriority.Info,
        Warning = LogPriority.Warning,
        Error = LogPriority.ErrorProcessing,
        Fatal = LogPriority.Fatal
    }

    /// <summary>
    /// NLog LegLevels for compatibility: TODO: implement compatible interfaces
    /// </summary>
    public enum LogLevel : Int32
    {
        Trace = LogPriority.Always,
        Debug = LogPriority.Debug,
        Info = LogPriority.Info,
        Warn = LogPriority.Warning,
        Error = LogPriority.ErrorProcessing,
        Fatal = LogPriority.Fatal
    }

    /// <summary>
    /// The LogCommand is a LogMessage extension concept. The idea is to allow the application to request actions
    /// on the log files asynchronously. For example, changing the file name is instant unless the request is
    /// queued. Please use it sparingly as it may be depreciated in a future release.
    /// </summary>
    public enum LogCommandAction : Int32
    {
        Log = 0,
        Close = 1,           // Request the current log files to be closed
        Backup = 2,          // Backup the log files
        Rename = 3,          // Rename the log files: FilePrepend
        Flush = 4            // Trigger log file flush
    }

    /// <summary>
    /// The valid values for LogThresholdMaxLevel. If <code>LogPriority < LogThreasholdMaxLevel</code> then the message is logged.
    /// </summary>
    /// <remarks>
    /// The user code calls a logging method with parameter <code>LogPriority</code>.
    /// The logging method compares LogPriority against LogThresholdMaxLevel. If <code>LogThresholdMaxLevel >= LogPriority</code> then
    /// logging occurs. For example, if <code>LogThresholdMaxLevel = LogNothing</code>, logging is disabled because LogPriority is always >= 0.
    /// </remarks>
    public enum DebugThresholdLevel : Int32
    {
        //LogNothing = (-1),
        // Lowest enumeration - most important
        LogFatal = LogPriority.Fatal,
        LogImportantInfo = LogPriority.ImportantInfo,
        LogCritical = LogPriority.ErrorCritical,
        LogError = LogPriority.ErrorProcessing,
        LogWarning = LogPriority.Warning ,
        LogInfo = LogPriority.Info,
        LogDebug = LogPriority.Debug,
        LogVerbose  = LogPriority.Verbose
        //,LogEverything = (LogPriority.Never - 1)
    }
}