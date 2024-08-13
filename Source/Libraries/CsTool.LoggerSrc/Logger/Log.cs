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
    using ExtensionMethods;

    /// <summary>
    /// The most basic of loggers. Use this implementation as the log of last resort. It is not thread safe
    /// but should work when little else does. The log file is created in the user's %TEMP%\CsTool.Logger folder
    /// which should be writeable.
    /// </summary>
    /// <remarks>
    /// Keep this static class simple and avoid reliance on ANY OTHER class so that its initialisation is reliable
    /// </remarks>
    public static class Log
    {

        private static string FullFilePath { get; set; } = Environment.GetEnvironmentVariable("TEMP") + @"\CsTool.Logger"; 

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
        /// <param name="formattedMessage">String formatted message</param>
        /// <param name="args">Arguments for the formatted message</param>
        private static void WriteIt(string formattedMessage, params object[] args)
        {
            try
            {
                if (!Directory.Exists(FullFilePath))
                    Directory.CreateDirectory(FullFilePath);
                if (isFirstRun)
                {
                    string oldFile = FullFileName + ".old";
                    if (File.Exists(FullFileName))
                    {
                        if (File.Exists(oldFile)) File.Delete(oldFile);
                        File.Move(FullFileName, oldFile);
                    }
                    File.AppendAllText(FullFileName, "================== CsTool.Logger Internal Logger ===============================================\n");
                    isFirstRun = false;
                }
                DateTimeOffset date = DateTimeOffset.Now;
                if (args.Length > 0)
                {
                    // Unfortunately we have to use try() for this method to assist the programmer debug logging messages which
                    // can be runtime configurable.
                    formattedMessage = string.Format(formattedMessage, args);
                }
                string message = string.Format("{0:HH:mm:ss.fff}: {1}\n", date, formattedMessage);
                File.AppendAllText(fullFileName, message);
            }
            catch { }
        }

        /// <summary>
        /// Log a message
        /// </summary>
        /// <param name="formattedMessage">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        public static void Write(string formattedMessage, params object[] args)
        {
            WriteIt(formattedMessage, args);
        }

        /// <summary>
        /// Log an exception message
        /// </summary>
        /// <param name="exception">The exception</param>
        /// <param name="formattedMessage">Unformatted string or formatted string with object arguments</param>
        /// <param name="args">Arguments for the formatted message</param>
        public static void Write(Exception exception, string formattedMessage, params object[] args)
        {
            string errorMessage = string.Concat(
                            formattedMessage,
                            "\n**Exception: ", exception.Message,
                            "\n  Line: ", exception.Source,
                            "\n  StackTrace: ", exception.StackTrace);

            WriteIt(errorMessage, args);
        }
    }
}
