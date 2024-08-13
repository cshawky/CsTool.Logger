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
        /// Thread safe lock object for all parameters being updated by the logger.
        /// </summary>
        private static readonly object padLockProperties = new object();

        /// <summary>
        /// Thread safe lock object for all messages being logged.
        /// </summary>
        private static readonly object padLockLogMessage = new object();

        /// <summary>
        /// Initialise class. If no filename is provided full initialisation is deferred.
        /// Name format: {FilePrepend}_{UserName}_{DateTime}.log
        /// </summary>
        /// <param name="newFilePrependText">The text string to append the date to in order to create a valid file name.
        /// If date stamping is disabled this is the file name excluding extension.</param>
        /// <param name="enableUserName">If true, the user name is appended to the file name.</param>
        /// <param name="fileNameDateTime">The date format to append to the file name.</param>
        /// <remarks>
        /// TODO: Use of locks is a recent addition prior to adding the Lazy<> initialisation for cleaner start up.
        /// </remarks>
        public LogBase(string newFilePrependText = null, bool enableUserName = false, string fileNameDateTime = null)
        {
            lock (padLockProperties)
            {
                //
                // Select the initial log file location. It must be writeable and not a reserved folder.
                //
                LoadApplicationDefaults();

                //
                // Create the message FIFO queue
                //
                bc = new BlockingCollection<QueuedMessage>(MaximumLogQueueSize);

                //
                // Initialise the filename pre pended text since the pre pended text is provided.
                //
                if ( !newFilePrependText.IsNullOrWhiteSpace() )
                    filePrepend = newFilePrependText;

                enableUserNamePrepend = enableUserName;
                if (fileNameDateTime != null )
                    FileNameDateFilter = fileNameDateTime;

                //
                // Create the log file name
                //
                LogFileName = CreateLogFileName();

                //
                // Now validate or set the log file path
                //
                SetLogDirectory(LogFilePath);

                //
                // Backup old log files, this will rename the existing file unless append is enabled.
                // but the new log file is not opened until a message is ready to be consumed.
                //
                if ( isLogFileFirstOpen && !isAppendFileEnabled )
                {
                    BackupLogFiles();
                    CountLoggedMessages = 0;
                }
            }

            //
            // Monitor/process the logging queue
            //
            ConsumeMessages();

            //
            // Setup a timed file flush action
            //
            timer = new System.Timers.Timer(2000);
            timer.Elapsed += OnTimedEvent;
            timer.AutoReset = true;
            timer.Enabled = true;
        }

        /// <summary>
        /// Destructor for class <code>Logger</code>
        /// </summary>
        ~LogBase()
        {
            Dispose();
        }

        /// <summary>
        /// Dispose managed and unmanaged resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Class is not sealed, so we include Dispose(disposed).
        /// On disposal ensure that no message are left in the queue, process them if any exist
        /// </summary>
        /// <param name="disposed">Boolean to indicate finalisation</param>
        protected virtual void Dispose(bool disposed)
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

        private Object padLockShutdown = new Object();

        private System.Timers.Timer timer;
        private UInt64 TimerLastCount;
        private bool IsSyncDue { get; set; }


        /// <summary>
        /// Background timer action to flush the log file to disk if it has been written to since
        /// the last flush.
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
                    if (bc.Count == 0)
                    {
                        Logger.Instance.LogCommand(LogCommandAction.Flush);
                    }
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
        public void CloseAndFlush(string closeReason = null)
        {
            lock (padLockFileObjects)
            {
                // Carefully and quietly close the writer regardless of its state.
                try
                {
                    if (streamWriter != null )
                    {
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
                CloseAndFlush();
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
                if (EnableUserNamePrepend)
                    prependName += "_" + Environment.UserName;

                if (!FileNameDateFilter.IsNullOrWhiteSpace())
                {
                    string dateText = DateTimeOffset.Now.ToString(FileNameDateFilter);
                    name = string.Format("{0}_{1}{2}", prependName, dateText, LogFileExtension);
                }
                else
                {
                    name = string.Format("{0}{1}", prependName, LogFileExtension);
                }
            }
            catch ( Exception exception )
            {
                // Default to last name
                Logger.LogExceptionMessage(LogPriority.ErrorProcessing,exception,"Failed to generate new log file name, using previous name");
                name = LogFileName;
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
                this.CloseAndFlush(newFileReason);

                LogFileName = CreateLogFileName();

                if (isLogFileFirstOpen && !isAppendFileEnabled)
                {
                    BackupLogFiles();
                }
                FileMode fileMode = FileMode.OpenOrCreate;

                //bool append = (isShutDownActive || isAppendFileEnabled ) && CountOldLogFilesToKeep > 0;
                //if (append) 
                    fileMode = FileMode.Append;

                Stream _outputStream = System.IO.File.Open(FullLogFileName, fileMode, FileAccess.Write, FileShare.Read);

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
            if (preferredPath.IsNullOrWhiteSpace())
            {
                preferredPath = LogUtilities.MyStartupPath + @"\Logs";
            }
            try
            {
                preferredPath = LogUtilities.GetWriteablePath(preferredPath);

                if (!Directory.Exists(preferredPath))
                {
                    Directory.CreateDirectory(preferredPath);
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