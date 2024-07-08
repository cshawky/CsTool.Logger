// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2020 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
// Please refer to LICENCE.txt in this project folder.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

namespace CsTool.Logger
{
    using CsTool.Extensions;
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The Thread Safe Logger interface for your application.
    /// </summary>
    /// <remarks>Refer to <code>LogBase</code> for a better explanation</remarks>
    public partial class LogBase : ILogBase
    {
        //
        // -----------------------------------------------------------------------------------------
        //

        /// <summary>
        /// For debugging, wait for this number of milliseconds before logging the message
        /// </summary>
#if DEBUG2
        public int DebugMessageWriteDelay { get; set; } = 0;
#endif

        /// <summary>
        /// Core LogMessage method.
        /// </summary>
        /// <param name="p">The queued message</param>
        private void LogQueuedMessage(QueuedMessage p)
        {
#if DEBUG2
            if (DebugMessageWriteDelay > 0) Thread.Sleep(DebugMessageWriteDelay);
#endif
            // Record when message gets processed
            DateTimeOffset date = DateTimeOffset.Now;

#if DEBUGLOGGER2
            // Log trace for the logger consumer. Use sparingly. There is no log management for this backup logger.
            Log.Write("LogQueuedMessage: " + p.Command.ToString() + ": " + p.Msg);
#endif // DEBUGLOGGER2

            switch (p.Command)
            {
                case LogCommandAction.Log:
                    if (p.Args != null && p.Args.Length > 0)
                    {
                        // In case user runtime message formatting is invalid, catch it here and continue
                        try
                        {
                            p.Msg = string.Format(p.Msg, p.Args);
                        }
                        catch (Exception exception)
                        {
                            p.Msg += ConstructExceptionMessage(exception, "LogMessage formatting error");
                        }
                    }
                    try
                    {
                        // Merge exception information
                        if ( p.IsException )
                        {
                            p.Msg = string.Format("{0}\n**Exception: {1}\n  Line: {2}\n  Stack: {3}",
                                p.Msg, p.LogException.Message, p.LogException.Source, p.LogException.StackTrace);
                        }

                        // Pad multi line messages
                        p.Msg = p.Msg.Replace("\n", LogNewLine);

                        string message;
                        if (p.LDate > DateTimeOffset.MinValue)
                        {
#if DEBUGLOGGER
                            // The log includes the time of the log message and the time the message is logged
                            message = string.Format("{0:HH:mm:ss.fff}:{1:HH:mm:ss.fff}:{2}: {3}: {4}", 
                                p.LDate, date, p.ThreadId.ToString().PadLeft(3, ' '), p.LPriority, p.Msg);
#else
                            message = string.Format("{0:HH:mm:ss.fff}:{1}: {2}: {3}", 
                                p.LDate,  p.ThreadId.ToString().PadLeft(3, ' '), p.LPriority, p.Msg);
#endif
                        }
                        else
                        {
                            message = string.Format("{0}{1}", LogNewLine, p.Msg);
                        }

                        if (streamWriter == null || !streamWriter.BaseStream.CanWrite)
                            CreateNewLogFile();

                        streamWriter.WriteLine(message);
                        if (IsSyncDue)
                        {
                            streamWriter.Flush();
                            IsSyncDue = false;
                        }
                        IncrementMessageCount(p.LPriority);
                    }
                    catch (Exception exception)
                    {
                        //
                        // Should never get here but attempt to get a message logged, assuming some format parsing issue.
                        //
                        if (!p.IsException)
                            Write(LogPriority.Fatal, exception, null);
                    }
                    break;

                case LogCommandAction.Close:
                    CloseAndFlush();
                    break;

                case LogCommandAction.Backup:
                    BackupLogFiles();
                    break;

                case LogCommandAction.Flush:
                    IsSyncDue = false;
                    if (streamWriter != null)
                    {
                        streamWriter.Flush();
                        TimerLastCount = countLoggedMessagesTotal;
                    }
                    break;

                case LogCommandAction.Rename:
                    if (p.Args != null && p.Args.Length > 0)
                    {
                        var value = p.Args[0];
                        if (value?.GetType() == typeof(string))
                        {
                            // Skip this change if the name has changed again
                            if (filePrepend != value.ToString()) break;

                            LogFileName = CreateLogFileName();
                            // Skip if the name is the same as the open file name
                            if (FullLogFileName != FullLogFileNameOpen)
                            {
                                if (streamWriter != null)
                                    CloseAndFlush();
                                BackupLogFiles();
                                isLogFileFirstOpen = true;
                            }
                        }
                    }
                    break;

                default:
                    break;
            }
        }

        /// <summary>
        /// Closes the active logfile and then renames all log files as backup taking
        /// into account a log file rename may have been requested. i.e. if renamed
        /// then the new name is used to determine if backups are required.
        /// The new log file is not opened until needed later.
        /// </summary>
        private void BackupLogFiles()
        {
            CloseAndFlush();
            lock (padLockFileObjects)
            {
                // Grab the latest file name as we don't rename old log files if the name has changed
                string fileName = CreateLogFileName();
                string fullFileName = LogFilePath + @"\" + fileName;
                LogUtilities.BackupOldFiles(fullFileName, CountOldLogFilesToKeep, null, true);
            }
        }

        /// <summary>
        /// Increment message log counters
        /// </summary>
        public void IncrementMessageCount(LogPriority logPriority)
        {
            CountLoggedMessages++;
            CountLoggedMessagesTotal++;
            if (countLoggedMessages > CountLoggedMessagesMaximum)
            {
                BackupLogFiles();
                CountLoggedMessages = 0;
            }
            if ((int)logPriority > 0 && (int)logPriority <= (int)LogPriority.ErrorProcessing)
            {
                CountLoggedErrors++;
            }
        }
    }
}