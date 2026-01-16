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
    using System.Diagnostics;
    using System.IO;
    using System.Text;
    using ExtensionMethods;

    /// <summary>
    /// The most basic of loggers. Use this implementation as the log of last resort. It is not thread safe
    /// but should work when little else does. The log file is created in the user's %TEMP%\Logs\CsTool.Logger folder
    /// which should be writeable.
    /// </summary>
    /// <remarks>
    /// Keep this static class simple and avoid reliance on ANY OTHER class so that its initialisation is reliable
    /// </remarks>
    public static class Log
    {
        public static bool LogFileExists = false;

        public static string FullFilePath { get; set; } = Environment.GetEnvironmentVariable("TEMP") + @"\Logs\CsTool.Logger"; 

        private static string FileName { get; set; } = @"CsTool.Logger." + Process.GetCurrentProcess().ProcessName
            .Replace(".vshost", "").Replace("XDesProc", "MyApplication").Replace(".exe", "") + ".log";

        private static string fullFileName = FullFilePath + @"\" + FileName;
        public static string FullFileName
        {
            get => fullFileName;
            set
            {
                if (String.IsNullOrWhiteSpace(value) || value == FullFileName) return;
                fullFileName = value;
                FullFilePath = Path.GetDirectoryName(value);
                FileName = Path.GetFileName(value);
                isFirstRun = true;
            }
        }

        private static bool isFirstRun { get; set; } = true;

        /// <summary>
        /// Underlying basic file logger with one backup log file. Provided that the default logfile
        /// location is writeable, this interface should log. Else, the application continues.
        /// This logger is not intended for general use. Use Logger.Write() instead.
        /// </summary>
        /// <param name="logPriority">Log priority</param>
        /// <param name="formattedMessage">String formatted message</param>
        /// <param name="args">Arguments for the formatted message</param>
        private static void WriteIt(LogPriority logPriority, string messageFormat, params object[] args)
        {
            try
            {
                if (!Directory.Exists(FullFilePath))
                    Directory.CreateDirectory(FullFilePath);
                if (isFirstRun)
                {
                    try
                    {
                        string oldFile = FullFileName + ".old";
                        if (File.Exists(FullFileName))
                        {
                            if (File.Exists(oldFile)) File.Delete(oldFile);
                            File.Move(FullFileName, oldFile);
                        }
                    }
                    catch { }
                    string text = String.Format(
                        "============================== CsTool.Logger Internal Logger : {0:yyyy-MMM-dd ddd HH:mm:ss.fff}  =====================\n\n",
                        DateTimeOffset.Now);
                    File.AppendAllText(FullFileName, text);
                    isFirstRun = false;
                }
                DateTimeOffset date = DateTimeOffset.Now;
                if (args.Length > 0)
                {
                    messageFormat = string.Format(messageFormat, args);
                }
                string message = string.Format("{0:HH:mm:ss.fff}: {1}: {2}\n", date, logPriority.ToString(), messageFormat);
                File.AppendAllText(fullFileName, message);
                LogFileExists = true;
            }
            catch { }
            finally
            {

            }
        }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="formattedMessage">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        public static void Write(string messageFormat, params object[] args)
        {
            WriteIt(LogPriority.Info, messageFormat, args);
        }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="formattedMessage">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        public static void Write(LogPriority logPriority, string messageFormat, params object[] args)
        {
            WriteIt(logPriority, messageFormat, args);
        }
        /// <summary>
        /// Log an exception message
        /// </summary>
        /// <param name="exception">The exception</param>
        /// <param name="messageFormat">Unformatted string or formatted string with object arguments</param>
        /// <param name="args">Arguments for the formatted message</param>
        public static void Write(LogPriority logPriority, Exception exception, string messageFormat, params object[] args)
        {
            string errorMessage = string.Concat(
                            messageFormat,
                            "\n**Exception: ", exception.Message,
                            "\n  Line: ", exception.Source,
                            "\n  StackTrace: ", exception.StackTrace);

            WriteIt(logPriority, errorMessage, args);
        }

        /// <summary>
        /// Log of last resort: Log a message including the line number, member name, source filename.
        /// </summary>
        /// <param name="logPriority">Log priority</param>
        /// <param name="messageFormat">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        public static void WriteDebug(LogPriority logPriority, string messageFormat, params object[] args)
        {
#if DEBUG
            StackFrame frame = new StackFrame(2, true);
            string className = frame.GetMethod().DeclaringType.Name;
            string memberName = frame.GetMethod().Name;
            int lineNumber = frame.GetFileLineNumber();
            string fileName = frame.GetFileName();
            messageFormat = string.Format("{0}{1}{2}.{3}() Line {4}: {5}", messageFormat, LogBase.LogNewLine, className, memberName, lineNumber, fileName);
#endif
            WriteIt(logPriority, messageFormat, args);
        }

        /// <summary>
        /// Log of last resort: Log a message including the line number, member name, source filename.
        /// </summary>
        /// <param name="exception">The exception caught</param>
        /// <param name="messageFormat">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        public static void WriteDebug(LogPriority logPriority, Exception exception, string messageFormat, params object[] args)
        {
#if DEBUG
            StackFrame frame = new StackFrame(2, true);
            string className = frame.GetMethod().DeclaringType.Name;
            string memberName = frame.GetMethod().Name;
            int lineNumber = frame.GetFileLineNumber();
            string fileName = frame.GetFileName();
            messageFormat = string.Format("{0}{1}{2}.{3}() Line {4}: {5}", messageFormat, LogBase.LogNewLine, className, memberName, lineNumber, fileName);
#endif
            Write(logPriority, exception, messageFormat, args);
        }

        /// <summary>
        /// Delete the existing log file
        /// </summary>
        public static void DeleteLogFile()
        {
            File.Delete(FullFileName);
            isFirstRun = true;
            LogFileExists = false;
        }

        /// <summary>
        /// Return the current log file as a string. Usually for the purpose if importing into
        /// the main log file and this log is then deleted.
        /// </summary>
        /// <returns>The log file as a string</returns>
        public new static string ToString()
        {
            // return the log file as a string
            try
            {
                if (File.Exists(FullFileName))
                {
                    return File.ReadAllText(FullFileName, Encoding.UTF8);
                }
                else
                {
                    return string.Empty;
                }
            }
            catch (Exception exception)
            {
                return $"Failed to read log file: {exception.Message}";
            }
        }

        public static string ExtractLogContents()
        {
            string content = String.Empty;
            if (LogFileExists)
            {
                try
                {
                    content = ToString();
                    DeleteLogFile();
                    LogFileExists = false;
                }
                catch (Exception exception)
                {
                    Write(LogPriority.ErrorCritical, exception, $"ExtractLogContents: Failed to extract and delete the log of last resort: {FullFileName}");
                }
            }
            return content;
        }
    }
}
