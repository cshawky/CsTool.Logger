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
    using System.IO;
    using System.Text;
    using ExtensionMethods;
    using Model;

    /// <summary>
    /// The Thread Safe Logger interface for your application.
    /// </summary>
    /// <remarks>Refer to <code>LogBase</code> for a better explanation
    /// 
    /// <code>LogBaseProperties</code> may be tailored at start up by the application defaults xml file. Thus initial default
    /// values are set in class <code>AppDefaults</code> and not here.
    /// </remarks>
    [ModelSettingsClass]
    public partial class LogBase : ILogBase
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region ModelSettings Properties

        [ModelSettingsProperty]
        public bool IsConsoleLoggingEnabled { get; set; } = false;

        /// <summary>
        /// Maximum number of queued messages before messages are lost or logging is blocked.
        /// TODO: No point allowing this setting to change because the queue is already created
        /// before it can be changed.
        /// </summary>
        //[ModelSettingsProperty]
        private int MaximumLogQueueSize { get; set; } = 100000;

        /// <summary>
        /// When disabled, the logging will wait until the buffer is no longer full.
        /// When enable, the log message is lost.
        /// </summary>
        [ModelSettingsProperty]
        public bool IsLoseMessageOnBufferFull { get; set; } = false;

        /// <summary>
        /// The maximum number of logged messages before the file is closed and a new file open.
        /// TODO - implement a maximum file size solution.
        /// </summary>
        [ModelSettingsProperty]
        public uint CountLoggedMessagesMaximum { get; set; } = 100000;


        /// <summary>
        /// The default number of old log files to keep. Depending on the application a new file is created each time the programme
        /// is started or when the log file is Closed, Flushed or created.
        /// </summary>
        [ModelSettingsProperty]
        public uint CountOldLogFilesToKeep { get; set; } = 20;

        /// <summary>
        /// Specify a date format to include in the file name. Examples:
        ///     "yyyy-MM-dd"
        ///     "yyyy-MM"
        /// </summary>
        [ModelSettingsProperty]
        public string FileNameDateFilter
        {
            get => fileNameDateFilter;
            set
            {
                if (fileNameDateFilter != value)
                {
                    fileNameDateFilter = value;
                    LogFileName = CreateLogFileName();
                }
            }
        }

        /// <summary>
        /// Absolute  directory to put the log files.
        /// <code>SetLogDirectory</code> ensures the path is writeable and not "reserved".
        /// </summary>
        [ModelSettingsPropertyWithSubstitutions]
        public string LogFilePath
        {
            get
            {
                if (logFilePath == null)
                {
                    lock (padLockProperties)
                    {
                        SetLogDirectory();
                    }
                }
                return logFilePath;
            }
            private set
            {
                if (logFilePath != value)
                {
                    lock (padLockProperties)
                    {
                        logFilePath = value;
                    }
                }
            }
        }

        /// <summary>
        /// The threshold debug priority that will enable log messages to be written.
        /// The lower the integer value of DebugThresholdDefault, the fewer debug messages will be displayed.
        /// Setting the LogThresholdMaxLevel to LogPriority.Never (a very big number) will disable all logging.
        /// Usage="LogFatal LogImportantInfo LogCritical LogError LogWarning LogInfo LogDebug LogVerbose" 
        /// </summary>
        /// <remarks >
        /// The supported debug levels are defined in class DebugThresholdLevel. To change the debug level
        /// enabled, set MyLogger.Logger.DebugThresholdDefault to the desired DebugThresholdLevel.
        ///
        /// For the programmer, when calling MyLogger.Logger.Write(priority,message) set the priority
        /// number according to the importance of your log message. A value of zero will always log, values
        /// greater than zero will only be logged if the value matches or is less than the setting in DebugThresholdDefault.
        /// </remarks>
        [ModelSettingsProperty]
        public DebugThresholdLevel LogThresholdMaxLevel { get; set; } = DebugThresholdLevel.LogInfo;

#if TEST_NEW_MODEL_SETTINGS
        /// <summary>   
        /// Development testing of the new ModelSettings feature making use of Attributes to automatically add properties
        /// to the settings file.
        /// </summary>
        [ModelSettingsInstance]
        public SampleModelSettings SampleSettings { get; set; } = new SampleModelSettings();
#endif // TEST_NEW_MODEL_SETTINGS
        #endregion ModelSettings Properties

        //
        // -----------------------------------------------------------------------------------------
        //
        #region PropertiesFile

        /// <summary>
        /// Thread lock for File objects
        /// </summary>
        private Object padLockFileObjects = new Object();

        /// <summary>
        /// Backend field for the <code>IsFileNameChangePending</code> property.
        /// </summary>
        private bool isFileNameChangePending = true;

        /// <summary>
        /// Indicates that the name format of path of the log file has changed and the action
        /// has not been completed.
        /// </summary>
        public bool IsFileNameChangePending
        {
            get => isFileNameChangePending;
            set
            {
                lock (padLockFileObjects)
                {
                    if (isFileNameChangePending != value)
                    {
                        isFileNameChangePending = value;
                        if ( isFileNameChangePending )
                            LogCommand(LogCommandAction.Rename, value);
                    }
                }
            }
        }

        /// <summary>
        /// Backend field for the <code>EnableUserNamePrepend</code> property.
        /// </summary>
        private bool enableUserNamePrepend;

        /// <summary>
        /// Enable the inclusion of the user name in the log file name. {FilePrepend}_{UserName}.log
        /// 
        /// </summary>
        public bool EnableUserNamePrepend
        {
            get
            {
                return enableUserNamePrepend;
            }
            set
            {
                if (enableUserNamePrepend != value)
                {
                    enableUserNamePrepend = value;
                    LogFileName = CreateLogFileName();
                }
            }
        }

        /// <summary>
        /// Backend field for the <code>FilePrepend</code> property.
        /// </summary>
        private string filePrepend;

        /// <summary>
        /// Define the pre pended portion of the log file. If an existing log file is open
        /// that file will be closed and a new file created. ALl messages will be flushed
        /// before closing. This is done asynchronously.
        /// </summary>
        public string FilePrepend
        {
            get
            {
                if (filePrepend == null)
                    FilePrepend = LogUtilities.MyProcessName;
                return filePrepend;
            }
            set
            {
                if (String.IsNullOrWhiteSpace(value))
                    return;
                lock (padLockFileObjects)
                {
                    if (filePrepend != value)
                    {
                        // The log file naming convention has changed. Close all files and initialise new log files
                        // Change default pre pended text
                        value = value.Replace(".vshost", "").Replace(".exe", "").Trim();
                        if (filePrepend != value)
                        {
                            filePrepend = value;
                            LogFileName = CreateLogFileName();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Backend field for the <code>LogFileName</code> property.
        /// </summary>
        private string logFileName;

        /// <summary>
        /// Underlying output stream (file) for the <code>streamWriter</code>
        /// </summary>
        private FileStream _outputStream;

        /// <summary>
        /// File stream writer
        /// </summary>
        /// <remarks>StreamWriter is not thread safe, so the Logger marshals all logging to a single thread.</remarks>
        private StreamWriter streamWriter;

        /// <summary>
        /// File encoding in case we need some encoding configured.
        /// </summary>
        public Encoding _encoding;

        #endregion PropertiesFile

        //
        // -----------------------------------------------------------------------------------------
        //
        #region Properties

        public int LogQueueCount
        {
            get { return bc.Count; }
        }

        /// <summary>
        /// Mask for extracting the LogType from LogPriority
        /// </summary>
        internal const int logTypeMask = 0x0000FFFF;

        /// <summary>
        /// Mask for extracting priority from LogPriority
        /// </summary>
        internal const int logPriorityMask = 0x7FFF0000;

        /// <summary>
        /// Allows for tidy indentation of multi line log messages from this class.
        /// </summary>
        public const string LogLineIndent = "                  ";

        /// <summary>
        /// Allows for tidy indentation of multi line log messages from this class.
        /// </summary>
        public const string LogNewLine = "\n" + LogLineIndent;
 
        /// <summary>
        /// Log file extension. Use lower case for Windows 8 and UEStudio compatibility.
        /// </summary>
        private string LogFileExtension { get; set; } = ".log";


        /// <summary>
        /// Backend field for <code>FileNameDateFilter</code> property.
        /// </summary>
        private string fileNameDateFilter;

        /// <summary>
        /// When enabled, LogExceptionMessage method calls are counted and stored in CoreUtilities Model.
        /// </summary>
        //public bool IsExceptionStatsEnabled = false;

#if DEBUG
        /// <summary>
        /// When True the BlockingCollection is bypassed and every message is logged from the calling thread.
        /// To support multiple threads, the action is blocking. This method is not recommended, and included
        /// primarily to gauge the performance impact of foreground/background logging.
        /// </summary>
        public bool IsSynchronousLogger = true;
#endif
        /// <summary>
        /// The log message FIFO
        /// </summary>
        private BlockingCollection<QueuedMessage> bc;

        /// <summary>
        /// Backend field for <code>CountLoggedMessages</code> property.
        /// </summary>
        private uint countLoggedMessages;

        /// <summary>
        /// Backend field for <code>CountLoggedErrors</code> property.
        /// </summary>
        private uint countLoggedErrors;

        /// <summary>
        /// Lock for the countLoggedMessages
        /// </summary>
        private static readonly object padLockCountLoggedErrors = new object();


        /// <summary>
        /// Lock for the countLoggedMessages
        /// </summary>
        private static readonly object padLockCountLoggedMessages = new object();

        /// <summary>
        /// Lock for the countLoggedMessagesTotal
        /// </summary>
        private static readonly object padLockCountLoggedMessagesTotal = new object();

        /// <summary>
        /// Backend field for <code>CountLoggedMessagesTotal</code> property.
        /// </summary>
        private ulong countLoggedMessagesTotal;

        /// <summary>
        /// Backend field for <code>CountLostMessagesTotal</code> property.
        /// </summary>
        private ulong countLostMessagesTotal;
        private static readonly object padLockCountLostMessagesTotal = new object();

        /// <summary>
        /// Absolute  directory to put the log files.
        /// </summary>
        private string logFilePath;

        /// <summary>
        /// True if the instance is deconstructing, disposing. Allows for
        /// last resort logging to be appended to the log files.
        /// </summary>
        private bool isShutDownActive = false;

        /// <summary>
        /// True if this is the first time the log file is opened for this application.
        /// </summary>
        /// <remarks>Assists with managing old log files. On first open, the existing files
        /// are always renamed.
        /// TODO: In combination with isShutdownActive, this check may be redundant but allows
        /// re-introduction of the isFileAppend feature to be added.</remarks>
        private bool isLogFileFirstOpen = true;

        /// <summary>
        /// Append to existing log file on startup is currently not supported.
        /// </summary>
        private const bool isAppendFileEnabled = false;

        /// <summary>
        /// The time in milliseconds to wait for the log message to be queued before
        /// returning to the calling thread.
        /// Default -1 wait indefinitely.
        /// </summary>
        /// <remarks>
        /// You should only need to set a value > 0 if your programme is susceptible
        /// to log message runaway. Messages that can't be queued will be lost.
        /// </remarks>
        public int AddMessageTimeout { get; set; } = 1000;

        /// <summary>
        /// Diagnostics, longest time to queue a message. Always <= AddMessageTimeout
        /// </summary>
        public double AddMessageMaxTime { get; set; }

        /// <summary>
        /// A count of the number of messages logged to the current open file.
        /// </summary>
        public uint CountLoggedMessages
        {
            get
            {
                return this.countLoggedMessages;
            }
            private set
            {
                lock (padLockCountLoggedMessages)
                {
                    // don't check if value has changed, just update it
                    countLoggedMessages = value;
                    if (value > CountLoggedMessagesMaximum)
                    {
                        BackupLogFiles();
                        countLoggedMessages = 0;
                    }
                }
            }
        }

        /// <summary>
        /// A count of the number of messages logged to the current open file.
        /// </summary>
        public uint CountLoggedErrors
        {
            get => this.countLoggedErrors;
            private set
            {
                lock (padLockCountLoggedErrors)
                {
                    countLoggedErrors = value;
                }
            }
        }

        /// <summary>
        /// A count of the total number of messages processed by this logger.
        /// </summary>
        public ulong CountLoggedMessagesTotal 
        { 
            get => countLoggedMessagesTotal;
            private set
            {
                lock (padLockCountLoggedMessagesTotal)
                {
                    countLoggedMessagesTotal = value;
                }
            }
        }

        /// <summary>
        /// A count of the total number of messages that could not be queued.
        /// </summary>
        public ulong CountLostMessagesTotal 
        {
            get => countLostMessagesTotal;
            private set
            {
                lock (padLockCountLostMessagesTotal)
                {
                    countLostMessagesTotal = value;
                }
            }
        }

        #endregion Properties

        //
        // -----------------------------------------------------------------------------------------
        //

        #region PropertiesExtended


        /// <summary>
        /// The name of the current log file excluding path. LogFileName should be generated
        /// by <code>CreateLogFileName()</code> whenever the file name parameters change.
        /// </summary>
        public string LogFileName
        {
            get 
            {
                if (logFileName == null)
                    logFileName = LogUtilities.MyProcessName + ".log";
                return logFileName; 
            }
            private set
            {
                if (logFileName != value)
                {
                    lock (padLockProperties)
                    {
                        logFileName = value;
                        IsFileNameChangePending = true;
                    }
                }
            }
        }

        /// <summary>
        /// Full log file name including path and extension.
        /// </summary>
        public string FullLogFileName
        {
            get => LogFilePath + "\\" + LogFileName;
        }

        /// <summary>
        /// Backend field for the <code>FullLogFileNameOpen</code> property.
        /// </summary>
        private string fullLogFileNameOpen = null;

        /// <summary>
        /// The full name and path of the log file that is currently open.
        /// </summary>
        public string FullLogFileNameOpen
        {
            get => fullLogFileNameOpen;
            private set
            {
                lock (padLockProperties)
                {
                    fullLogFileNameOpen = value;
                }
            }
        }

        /// <summary>
        /// Helper method to indicate that this LogPriority is currently active.
        /// Use this method to encapsulate code that only needs to be executed when
        /// logging for this priority is enabled.
        /// </summary>
        /// <param name="priority"></param>
        /// <returns>True if the LogPriority is currently enabled to log. False otherwise</returns>
        /// <remarks>
        /// ExampleControl usage:
        /// <code>
        /// if ( LogPriorityIsEnabled(LogPriority.Debug) )
        /// {
        ///     ...perform some calculations or construct data
        ///     {YourAppClass}.Logger.Write(LogPriority.Debug, message);
        /// }
        /// </code>
        /// </remarks>
        public bool IsLogPriorityEnabled(LogPriority priority)
        {
            if ((Int32)LogThresholdMaxLevel >= (Int32)priority)
                return true;
            return false;
        }

        #endregion PropertiesExtended
    }
}