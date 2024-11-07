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
    using System.Diagnostics;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Security.AccessControl;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Timers;
    using ExtensionMethods;

    /// <summary>
    /// A high performance thread safe Logger interface for your application providing a single global
    /// logger and/or multiple loggers.
    /// </summary>
    /// <remarks>
    /// 
    /// The Logger interface utilises a Producer/Consumer pattern based on the BlockedCollection to
    /// provide full thread safe logging. 
    /// 
    /// Programmatically, user code creates a Log Message. Minimal processing is done on the message
    /// using the ILogger interface methods. The LogMessage <code>QueuedLogMessage</code> is then
    /// placed on the FIFO queue. The queue is then serviced by a single consumer per logger instance.
    /// 
    /// Use of the Logger is as simple as calling any one of the LogMessage or Write methods.
    /// 
    /// Application implementation:
    /// 
    ///  Class LogBase is wrapped into class Logger thus creating a singleton instance of this class.
    ///  This makes the standard interface simple as shown below.
    ///  
    /// Simplest Usage:
    /// <code>
    ///     using CsTool.Logger
    ///         Logger.Write("Hello World");
    ///         Logger.Write(LogPriority.Fatal,"Goodbye");
    ///         Logger.Write(LogPriority.Info, "Hello {0}, MyLogger has been initialised", "World");
    ///         string world = "world";
    ///         Logger.Write($"Hello {world}");
    /// 
    ///     optional tweaks (available in an alternate code stream):
    ///         Logger.IsWarningLogFileEnabled = true;  // Create Warning log file (Warnings and Errors)
    ///         Logger.IsErrorLogFileEnabled = true;  // Create ErrorProcessing log file (Errors only)
    ///         Logger.IsInfoLogFileEnabled = false;    // Disable primary log file (would contain all messages)
    ///         Logger.FileNamePrepend = "My Log File"; // A different name for he log files
    /// </code>
    /// 
    /// Multiple Logger interface:
    /// <code>
    ///     using CsTool.Logger
    ///     LogBase logger1 = new LogBase("Logger1");
    ///     LogBase logger2 = new LogBase("Logger2");
    ///     logger1.Write("Hello World");
    ///     logger2.Write("Hello World");
    /// </code>
    /// 
    /// Additional independent logging streams may be created in your application simply by making a new instance
    /// of BaseLogger and changing the FilePrepend text before using the logger instance.
    /// 
    /// Thanks to: https://stackoverflow.com/questions/2954900/simple-multithread-safe-log-class
    /// </summary>
    public partial class LogBase : ILogBase
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Initialisation

        /// <summary>
        /// Indicates that this Logger instance has been initialised
        /// </summary>
        private bool IsInitialised { get; set; } = false;

        /// <summary>
        /// Inidcates that the ProcessExit event has been registered.
        /// </summary>
        private static bool IsProcessExitRegistered { get; set; } = false;

        /// <summary>
        /// Indicates that the UnhandledException event has been registered.
        /// </summary>
        private static bool IsExceptionHandlerRegistered { get; set; } = false;

        /// <summary>
        /// Thread safe lock object for all parameters being updated by the logger.
        /// </summary>
        private static readonly object padLockProperties = new object();

        /// <summary>
        /// Thread safe lock object for all messages being logged.
        /// </summary>
        private static readonly object padLockLogMessage = new object();

        public LogBase()
        {
            lock (padLockProperties)
            {
                // Ensure all unhandled exceptions are logged
                if (!IsExceptionHandlerRegistered)
                    AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(LogUnhandledException);
                IsExceptionHandlerRegistered = true;

                // Subscribe to the AppDomain.ProcessExit event. most important step to ensure all messages are saved on exit.
                if (!IsProcessExitRegistered)
                {
                    AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                    IsProcessExitRegistered = true;
                }

                InitialiseLog();
            }
        }

        /// <summary>
        /// Initialise an instance of LogBase with the parameters provided. These override file parameters.
        /// Name format: {FilePrepend}_{UserName}_{DateTimeFormat}.log
        /// </summary>
        /// <param name="newFilePrependText">The text string to append the date to in order to create a valid file name.
        /// If date stamping is disabled this is the file name excluding extension.</param>
        /// <param name="enableUserName">If true, the user name is appended to the file name.</param>
        /// <param name="fileNameDateTime">The date format to append to the file name.</param>
        public LogBase(string newFilePrependText, bool enableUserName, string fileNameDateTime)
        {
            lock (padLockProperties)
            {
                InitialiseLog();
                IsUserNameAppended = enableUserName;
                if (newFilePrependText != null)
                    FilePrepend = newFilePrependText;
                if (fileNameDateTime != null)
                    FileNameDateFormat = fileNameDateTime;
            }
        }

        /// <summary>
        /// Initialise the message queue and various settings for the logger.
        /// </summary>
        public void InitialiseLog()
        {
            if (IsInitialised)
            {
                Log.WriteDebug(LogPriority.Debug, "InitialiseLog: Logger already initialised");
                return;
            }

            //
            // Create the message FIFO queue.
            //
            bc = new BlockingCollection<QueuedMessage>(MaximumLogQueueSize);

            //
            // Logger messages may now be queued.
            //

            //
            // Load logger settings which includes identifying the log file path.
            //
            LoadAppDefaults(this, "1.0.0");

            //
            // Create the log file name
            //
            //LogFileName = CreateLogFileName();

            //
            // Now validate or set the log file path
            //
            SetLogDirectory(LogFilePath);

            //
            // Backup old log files, this will rename the existing file unless append is enabled.
            // but the new log file is not opened until a message is ready to be consumed.
            //
            if (isLogFileFirstOpen && !isAppendFileEnabled)
            {
                BackupLogFiles();
                CountLoggedMessages = 0;
            }

            //
            // Monitor/process the logging queue
            //
            ConsumeMessages();

            //
            // Setup a timed file flush action. This should not really be needed.
            //
            timer = new System.Timers.Timer(2000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;

            IsInitialised = true;
        }

        /// <summary>
        /// Destructor for class <code>Logger</code>. Using AppDomain.CurrentDomain.ProcessExit event ensures that the log file is closed.
        /// </summary>
        /// <param name="sender">Not used</param>
        /// <param name="e">not used</param>
        private void OnProcessExit(object sender, EventArgs e)
        {
            Dispose(true);
        }


        /// <summary>
        /// Log unhandled exceptions to the log file.
        /// </summary>
        /// <param name="sender">Sender</param>
        /// <param name="e">Exception object</param>
        private void LogUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception exception = e.ExceptionObject as Exception;
            if ( IsInitialised )
                Logger.Write(exception, "LogUnhandledException");
            else
                Log.Write(exception, "LogUnhandledException");
        }

        /// <summary>
        /// Class is not sealed, so we include Dispose(disposed).
        /// On disposal ensure that no message are left in the queue, process them if any exist
        /// </summary>
        /// <param name="disposed">Boolean to indicate finalisation</param>
        protected virtual void Dispose(bool disposed)
        {
            if (isShutDownActive)
            {
                // Should not get here but, just in case wait until the message queue bc is empty as another task is emptying it
                Log.Write(LogPriority.ErrorCritical, "Dispose: We should not get here, waiting for queue to empty...");
                if ( bc != null ) while (bc.Count > 0) Thread.Sleep(100);
                return;
            }
            lock (padLockProperties)
            {
                isShutDownActive = true;
                isLogFileFirstOpen = false;
                if (disposed)
                {
                    if (bc != null)
                    {
                        // Give time for the existing queue to be processed including any last minute logs from class destructors.
                        // Catching this is not guaranteed.
                        // NOTE: Reduce this time if "Logging stopped" is not completed in the log file.
                        //Thread.Sleep(100);

                        // Get rid of managed resources
                        // Prevent new log messages from being added
                        bc.CompleteAdding();
                        // Flush the queue (the Factory Task has been killed by now)
                        foreach (QueuedMessage p in bc.GetConsumingEnumerable())
                        {
                            LogQueuedMessage(p);
                        }
                    }
                    if (streamWriter == null || !streamWriter.BaseStream.CanWrite)
                        CreateNewLogFile();
                    streamWriter.Write(
                        "\n============================== Logging stopped : {0:yyyy-MMM-dd ddd HH:mm:ss} : Shutdown requested =====================",
                        DateTimeOffset.Now);
                    CloseAndFlush();
                    if (timer != null)
                        timer.Dispose();
                    if (bc != null)
                    {
                        bc.Dispose();
                        bc = null;
                    }
                }
            }
        }

        /// <summary>
        /// The timer for periodic file flushes.
        /// </summary>
        private System.Timers.Timer timer;

        /// <summary>
        /// Raw timer counter to indicate when a flush was last performed.
        /// </summary>
        private UInt64 TimerLastCount;

        /// <summary>
        /// Indicates that a file flush is due. This is used to prevent multiple flushes and allowing queuing of flushes
        /// in the message queue.
        /// </summary>
        private bool IsSyncDue { get; set; }


        /// <summary>
        /// Background timer action to flush the log file to disk if it has been written to since
        /// the last flush.
        /// TODO Not sure if this is relevant anymore.
        /// </summary>
        /// <param name="source">NA</param>
        /// <param name="e">NA</param>
        private void OnTimedEvent(Object source, ElapsedEventArgs e)
        {
            UInt64 thisCount = CountLoggedMessagesTotal;
            //Logger.Write($"OnTimedEvent({e.SignalTime:HH:mm:ss.fff}) {CountLoggedMessages}");
            if (TimerLastCount != thisCount)
            {
                TimerLastCount = thisCount;
                if (streamWriter != null && !IsSyncDue)
                {
                    IsSyncDue = true;
                    Logger.Instance.LogCommand(LogCommandAction.Flush);
                }
            }
        }
        #endregion Initialisation

        //
        // -----------------------------------------------------------------------------------------
        //

        #region Consumer

        /// <summary>
        /// Wait for queued messages and process them until the queue is finalised
        /// </summary>
        /// <remarks>TODO Consider using a Token to work with Dispose(), no problems observed as is.</remarks>
        private void ConsumeMessages()
        {
            var task = Task.Factory.StartNew(() =>
            {
                foreach (QueuedMessage p in bc.GetConsumingEnumerable())
                {
                    LogQueuedMessage(p);
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning | TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
        }

        #endregion Consumer

        //
        // -----------------------------------------------------------------------------------------
        //

        #region FileMethods

        /// <summary>
        /// Close the log file if it is open.
        /// </summary>
        /// <param name="closeReason">A non empty string will generate a log entry indicating the log file was closed.</param>
        public void CloseAndFlush(int maxWaitTime = 0, string closeReason = null)
        {
            lock (padLockFileObjects)
            {
                // Carefully and quietly close the writer regardless of its state.
                try
                {
                    if (streamWriter != null )
                    {
                        if ( maxWaitTime > 0 && bc.Count > 0 )
                        {
                            int wait = Math.Max(200,Math.Min(20, maxWaitTime / 10));
                            int totalWait = 0;
                            Log.Write("CloseAndFlush: Waiting for {0} messages to be flushed", bc.Count);
                            while (bc.Count > 0 && totalWait < maxWaitTime)
                            {
                                Thread.Sleep(wait);
                                totalWait += wait;
                            }
                            if (bc.Count > 0)
                            {
                                Log.Write("CloseAndFlush: Unexpected {0} messages still pending for flush, not waiting...", bc.Count);
                            }
                        }

                        streamWriter.Flush();
                        streamWriter.Dispose();
                        streamWriter = null;
                    }
                    if ( _outputStream != null )
                    {
                        _outputStream.Flush();
                        _outputStream.Dispose();
                        _outputStream = null;
                    }
                }
                catch
                {
                    // On Application Exit, Close sometimes gets called too late. DotNet has already closed the underlying stream
                }
            }
        }

        /// <summary>
        /// Opens the current log file with the default log file editor in a separate process.
        /// </summary>
        /// <remarks>File association works for NETFRAMEWORK but not CORE</remarks>
        public void DisplayLogFile()
        {
            try
            {
                if (!File.Exists(FullLogFileName))
                    Logger.Write(LogPriority.Info, "DisplayLogFile called before any logging has occurred");
                CloseAndFlush(1000);
                Process.Start(FullLogFileName);
            }
            catch ( Exception exception ) {
                Logger.Write(exception, "DisplayLogFile not supported");
            }
        }

        /// <summary>
        /// Generate the log file name excluding path
        /// </summary>
        /// <returns>The name of the log file</returns>
        private string CreateLogFileName()
        {
            string name;
            try
            {
                string prependName = FilePrepend;
                if (IsUserNameAppended)
                    prependName += "_" + Environment.UserName;

                if (!string.IsNullOrWhiteSpace(FileNameDateFormat))
                {
                    string dateText = DateTimeOffset.Now.ToString(FileNameDateFormat);
                    name = string.Format("{0}_{1}{2}", prependName, dateText, LogFileExtension);
                }
                else
                {
                    name = string.Format("{0}{1}", prependName, LogFileExtension);
                }
            }
            catch ( Exception exception )
            {
                // Default to last name used
                Logger.LogExceptionMessage(LogPriority.ErrorProcessing,exception,"Failed to generate new log file name, using previous name");
                name = logFileName;
            }
            return name;
        }

        /// <summary>
        /// Close the existing log file and open a new log file. Where backup files are enabled, this will increment the backup log files. Older
        /// log files will be deleted as necessary. Use this method from your application to keep the size and number of log files to a manageable
        /// level. To increase or decrease the number of backup files set <code>countOldFilesToKeep</code> to a suitable number, default = 10.
        /// TODO: The way in which the log directory is determined needs a review. Currently the
        /// method CreateNewLogFile() calls LogUtilities.GetCorrectPath( "Logs" )
        /// </summary>
        /// <param name="newFileReason">Optional string describing why the file was closed or re opened.</param>
        private void CreateNewLogFile( string newFileReason = null)
        {
            lock (padLockFileObjects)
            {
                this.CloseAndFlush(0, newFileReason);

                LogFileName = CreateLogFileName();

                if (isLogFileFirstOpen && !isAppendFileEnabled)
                {
                    BackupLogFiles();
                }
                FileMode fileMode = FileMode.OpenOrCreate;

                //bool append = (isShutDownActive || isAppendFileEnabled ) && CountOldLogFilesToKeep > 0;
                //if (append) 
                    fileMode = FileMode.Append;

                _outputStream = System.IO.File.Open(FullLogFileName, fileMode, FileAccess.Write, FileShare.Read);

                IsFileNameChangePending = false;
                FullLogFileNameOpen = FullLogFileName;

                // Parameter reassignment.
                _encoding = _encoding ?? new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

                streamWriter = new StreamWriter(_outputStream, _encoding);

                CountLoggedMessages = 0;

                // If the queue is full on sudden shutdown, messages are lost.
                // Suppressing does help with processing about 1500 messages. Suggestions welcome for improvement.
                //GC.SuppressFinalize(streamWriter);
                //GC.SuppressFinalize(LogWriter);

                // Rarely used
                if ( (!String.IsNullOrWhiteSpace(newFileReason) || isLogFileFirstOpen ) && !isShutDownActive)
                {
                    DateTimeOffset date = DateTimeOffset.Now;
                    streamWriter.Write(
                        "\n============================== Logging started : {0:yyyy-MMM-dd ddd HH:mm:ss} : {1} =====================\n\n",
                        date, newFileReason);
                    streamWriter.Flush();
                }
                isLogFileFirstOpen = false;
            }
        }

        /// <summary>
        /// Set the base directory for all logging. The default location is {StartupPath}\Logs.
        /// </summary>
        /// <param name="preferredPath">The preferred path for the log files. If not provided the startup path is used.</param>
        /// <returns>The path to the log files</returns>
        /// <remarks>The application may be started from different locations, thus we check
        /// to ensure that the startup path is not windows read only application paths
        /// or special reserved paths, or visual studio build paths.
        /// The order of preference for the writeable log path is as follows:
        /// - Exclude all Windows programme paths
        /// - Exclude Visual Studio bin paths
        /// - Application startup path (initial working directory), but not the application/DLL path
        /// - User's temp path
        /// - User's desktop
        ///
        /// Use of IsPathReserved(path) also confirms that the path is writeable so that the
        /// sub folder <code>Logs<code> may be created.
        /// </remarks>
        public string SetLogDirectory(string preferredPath = null)
        {
            if (string.IsNullOrWhiteSpace(preferredPath))
            {
                preferredPath = LogUtilities.MyStartupPath + @"\Logs";
            }
            try
            {
                preferredPath = LogUtilities.GetWriteablePath(preferredPath);

                if (!Directory.Exists(preferredPath))
                {
                    Directory.CreateDirectory(preferredPath);
                    Log.WriteDebug(LogPriority.Debug, "Created path {0}", preferredPath);

                }
            }
            catch (Exception exception)
            {
                Log.Write(exception, "SetLogDirectory: Fundamental path issue: " + preferredPath);
            }
            finally
            {
                LogFilePath = preferredPath;
            }
            return LogFilePath;
        }

#endregion FileMethods
    }
}