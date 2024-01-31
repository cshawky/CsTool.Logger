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
    using System.Linq;
    using System.Diagnostics;
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
        #region Methods

        /// <summary>
        /// Log a command to the FIFO without arguments
        /// </summary>
        /// <param name="logCommand">The command to execute</param>
        public void LogCommand(LogCommandAction logCommand )
        {
            LogCommand(logCommand, null, null);
        }

        /// <summary>
        /// Log a command to the FIFO without arguments
        /// </summary>
        /// <param name="logCommand">The command to execute</param>
        /// <param name="commandArgs">Arguments specific to the command</param>
        public void LogCommand(LogCommandAction logCommand, params object[] commandArgs)
        {
            QueuedMessage p = new QueuedMessage(logCommand, commandArgs);
            bc.TryAdd(p, AddMessageTimeout);
        }

        /// <summary>
        /// Basic Information LogMessage method.
        /// </summary>
        /// <param name="messageFormat">Message string with formatting</param>
        /// <param name="args">Format params argument array</param>
        public virtual void Write(string messageFormat, params object[] args)
        {
            Write(LogPriority.Info, messageFormat, args);
        }

        /// <summary>
        /// Core LogMessage method.
        /// </summary>
        /// <param name="logPriority">Priority of the log entry</param>
        /// <param name="messageFormat">Message string with formatting</param>
        public virtual void Write(LogPriority logPriority, string messageFormat)
        {
            if ((Int32)logPriority > (Int32)LogThresholdMaxLevel) return;
            if (IsLoseMessageOnBufferFull && bc.Count() >= bc.BoundedCapacity) return;
            QueuedMessage p = new QueuedMessage(logPriority, DateTimeOffset.Now, messageFormat);
            TryAdd(p);
        }

        /// <summary>
        /// Core LogMessage method.
        /// </summary>
        /// <param name="logPriority">Priority of the log entry</param>
        /// <param name="messageFormat">Message string with formatting</param>
        /// <param name="args">Format params argument array</param>
        public virtual void Write(LogPriority logPriority, string messageFormat, params object[] args)
        {
            if ((Int32)logPriority > (Int32)LogThresholdMaxLevel) return;
            if (IsLoseMessageOnBufferFull  && bc.Count() >= bc.BoundedCapacity) return;

            DateTimeOffset date = DateTimeOffset.Now;
            QueuedMessage p;
            if (args.Length > 0)
            {
                p = new QueuedMessage(logPriority, date, messageFormat, args);
            }
            else
            {
                p = new QueuedMessage(logPriority, date, messageFormat);
            }
            TryAdd(p);
        }

        /// <summary>
        /// Core LogMessage method. Serilog compatible calling structure.
        /// </summary>
        /// <param name="logPriority">Priority of the log entry</param>
        /// <param name="messageFormat">Message string with formatting</param>
        /// <param name="args">Format params argument array</param>
        public virtual void Write(LogEventLevel logPriority, string messageFormat, params object[] args)
        {
            if ((Int32)logPriority > (Int32)LogThresholdMaxLevel) return;
            if (IsLoseMessageOnBufferFull && bc.Count() >= bc.BoundedCapacity) return;

            DateTimeOffset date = DateTimeOffset.Now;
            QueuedMessage p;
            bool hasArgs = !(args == null || args.Length == 0);
            if (hasArgs)
            {
                p = new QueuedMessage((LogPriority)logPriority, date, messageFormat, args);
            }
            else
            {
                p = new QueuedMessage((LogPriority)logPriority, date, messageFormat);
            }
            TryAdd(p);
        }

        /// <summary>
        /// Special logger interface to print out a table of NameValueCollection pairs
        /// if passed as {0}, or just as a second parameter
        /// </summary>
        /// <param name="logPriority">Log Priority</param>
        /// <param name="messageFormat">The message</param>
        /// <param name="parameters">The NameValueCollection</param>
        public virtual void Write(LogPriority logPriority, string messageFormat, NameValueCollection parameters)
        {
            if ((Int32)logPriority > (Int32)LogThresholdMaxLevel) return;
            if (IsLoseMessageOnBufferFull && bc.Count() >= bc.BoundedCapacity) return;

            DateTimeOffset date = DateTimeOffset.Now;

            string parameterString = "Name Value Pairs: ";
            if (parameters == null)
            {
                parameterString += "null";
            }
            else
            {
                foreach (string key in parameters)
                {
                    parameterString += "\r\n\t" + key + "(" + parameters[key] + ")";
                }
            }
            if (messageFormat.Contains("{0}"))
            {
                messageFormat = string.Format(messageFormat, parameterString);
            }
            else
            {
                Write(logPriority, messageFormat += parameterString);
            }

            QueuedMessage p = new QueuedMessage(logPriority, date, messageFormat, null);
            TryAdd(p);
        }

        /// <summary>
        /// Allow the calling application to construct a string equivalent to the exception log message.
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="messageFormat"></param>
        /// <returns></returns>
        public virtual string ConstructExceptionMessage(Exception exception, string messageFormat)
        {
            string errorMessage = System.String.Empty;
            if (exception == null)
            {
                errorMessage = "ConstructExceptionMessage: No Exception details were provided!";
                return errorMessage;
            }
            try
            {
                errorMessage = string.Concat(
                                messageFormat,
                                "\n**Exception: ", exception.Message,
                                "\n  Line: ", exception.Source,
                                "\n  StackTrace: ", exception.StackTrace);
            }
            catch /* ( Exception exception2 ) */
            {
                // Exception most likely related to LogMessage.
                //throw new ApplicationException( errorMessage, exception2 );
            }
            return errorMessage;
        }

        /// <summary>
        /// Log the exception message with priority Fatal.
        /// </summary>
        /// <param name="exception">The Exception raised</param>
        /// <param name="messageFormat">Formatted message string</param>
        /// <param name="args">Optional formatted string arguments</param>
        public virtual void Write(Exception exception)
        {
            Write(LogPriority.Fatal, exception, "" );
        }

        /// <summary>
        /// Log the exception message with priority Fatal.
        /// </summary>
        /// <param name="exception">The Exception raised</param>
        /// <param name="messageFormat">Formatted message string</param>
        /// <param name="args">Optional formatted string arguments</param>
        public virtual void Write(Exception exception, string messageFormat, params object[] args)
        {
            Write(LogPriority.Fatal, exception, messageFormat, args);
        }

        /// <summary>
        /// Log the exception
        /// </summary>
        /// <param name="logPriority">Log priority</param>
        /// <param name="exception">The Exception raised</param>
        /// <param name="messageFormat">Formatted message string</param>
        /// <param name="args">Optional formatted string arguments</param>
        public virtual void Write(LogPriority logPriority, Exception exception, string messageFormat, params object[] args)
        {
            //
            // Do not log if priority is not high enough
            //
            if ((Int32)logPriority > (Int32)LogThresholdMaxLevel) return;
            if (IsLoseMessageOnBufferFull && bc.Count() >= bc.BoundedCapacity) return;

            DateTimeOffset date = DateTimeOffset.Now;

            //
            // A form method for displaying some error info
            //
            string errorMessage = System.String.Empty;

            errorMessage = string.Concat(
                            messageFormat,
                            "\n**Exception: ", exception.Message,
                            "\n  Line: ", exception.Source,
                            "\n  StackTrace: ", exception.StackTrace);

            QueuedMessage p = new QueuedMessage(logPriority, date, exception, errorMessage, args);
            TryAdd(p);
        }

        /// <summary>
        /// Logs a message without any additional formatting or data. The raw message is appending with
        /// a line feed/carriage return, but no date/time or priority is printed in the log
        /// The entire message is indented
        /// </summary>
        /// <param name="logPriority"></param>
        /// <param name="rawmessage"></param>
        public virtual void WriteRaw(LogPriority logPriority, string rawmessage, params object[] args)
        {
            if ((Int32)logPriority > (Int32)LogThresholdMaxLevel) return;
            if (IsLoseMessageOnBufferFull && bc.Count() >= bc.BoundedCapacity) return;

            DateTimeOffset date = DateTimeOffset.Now;
            if (args.Length > 0)
            {
                try
                {
                    // Unfortunately we have to use try() for this method to assist the programmer debug logging messages which
                    // can be runtime configurable.
                    rawmessage = string.Format(rawmessage, args);
                }
                catch (Exception exception)
                {
                    rawmessage += ConstructExceptionMessage(exception, ": LogRawMessage formatting error");
                }
            }
            // Queue raw message without date stamp
            QueuedMessage p = new QueuedMessage(logPriority, DateTimeOffset.MinValue, rawmessage, args);
            TryAdd(p);
        }

        /// <summary>
        /// Log a message to the programme log file depending on the log priority provided.
        /// </summary>
        /// <param name="messsage">Debug message.</param>
        /// <param name="countAsError">If enabled, the message is counted as an error for statistical purposes.</param>
        /// <param name="ignoreExceptions">If enabled, this method will not attempt to log an internal exception that might occur whilst attempting to process the message.</param>

        public virtual void LogMessageWithStats(string message, bool countAsError = false, bool ignoreExceptions = false)
        {
            LogMessageWithStats(LogPriority.Info, message, countAsError, ignoreExceptions);
        }

        public virtual void LogMessageWithStats(LogPriority logPriority, string message, bool countAsError = false, bool ignoreExceptions = false)
        {
            if ((Int32)logPriority > (Int32)LogThresholdMaxLevel) return;
            if (IsLoseMessageOnBufferFull && bc.Count() >= bc.BoundedCapacity) return;

            DateTimeOffset date = DateTimeOffset.Now;
            QueuedMessage p = new QueuedMessage(logPriority, date, message, null);
            if (countAsError)
            {
                p.IsException = true;
            }
            TryAdd(p);
        }

#if DEBUG
        public bool IsQueueAsyncEnabled { get; set; }

        private Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// TODO: This methods provides for additional diagnostics whilst testing and stressing the
        /// queue and logging. Testing determined a delay occurs when the queue is full.
        /// Delay ~ 40 milliseconds, when Count = 100000, being the queue limit.
        /// Even with this code the performance for insert is faster than Serilog for a 200000 messages pumped through.
        /// </summary>
        /// <param name="p">The message to queue</param>
        private void TryAdd(QueuedMessage p)
        {
            stopwatch.Restart();
            if (IsSynchronousLogger)
            {
                lock (padLockFileObjects)
                {
                    LogQueuedMessage(p);
                }
            }
            else
            {
                // Unfortunately adding in parallel isn't as efficient
                // even though the AddMessageMaxTime is smaller.
                if (IsQueueAsyncEnabled)
                {

                   Parallel.Invoke(() =>
                   {
                       if (!bc.TryAdd(p, AddMessageTimeout))
                       {
                           CountLostMessagesTotal++;
                       }
                   });
                }
                else
                {
                    if (!bc.TryAdd(p, AddMessageTimeout))
                    {
                        CountLostMessagesTotal++;
                    }
                }
            }
            stopwatch.Stop();
            var t = stopwatch.Elapsed.TotalSeconds;
            if (t > AddMessageMaxTime ) AddMessageMaxTime = t;
        }
#else
        private void TryAdd(QueuedMessage p)
        {
            if (!bc.TryAdd(p, AddMessageTimeout))
            {
                CountLostMessagesTotal++;
            }
        }
#endif


        #endregion Methods
    }
}
