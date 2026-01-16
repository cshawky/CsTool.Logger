// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2025 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
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
    /// Internal logging allowing for use of the basic log file or standard file. Useful when
    /// methods are called before full logger initialisation and also used by the application.
    /// </summary>
    public partial class LogBase : ILogBase
    {
        /// <summary>
        /// Log of last resort: Log a message including the line number, member name, source filename.
        /// </summary>
        /// <param name="exception">The exception caught</param>
        /// <param name="messageFormat">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        internal static void SafeWrite(LogPriority logPriority, Exception exception, string messageFormat, params object[] args)
        {
            if (LogBase.IsFirstInitialised)
            {
                try
                {
                    Logger.Instance.Write(logPriority, exception, messageFormat, args);
                    return;
                }
                catch (Exception innerException)
                {
                    Log.Write(logPriority, innerException, "Logger.Instance.Write() failed in SafeWrite(). Original message:");
                    Log.Write(logPriority, exception, messageFormat, args);
                    return;
                }
            }
            Log.Write(logPriority, exception, messageFormat, args);
        }
        /// <summary>
        /// Log of last resort: Log a message including the line number, member name, source filename.
        /// </summary>
        /// <param name="logPriority">Log priority</param>
        /// <param name="messageFormat">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        internal static void SafeWrite(LogPriority logPriority, string messageFormat, params object[] args)
        {
            if (LogBase.IsFirstInitialised)
            {
                try
                {
                    Logger.Instance.Write(logPriority, messageFormat, args);
                    return;
                }
                catch (Exception innerException)
                {
                    Log.Write(logPriority, innerException, "Logger.Instance.Write() failed in SafeWrite(). Original message:");
                    Log.Write(logPriority, messageFormat, args);
                    return;
                }
            }
            Log.Write(logPriority, messageFormat, args);
        }

        /// <summary>
        /// Log of last resort: Log a message including the line number, member name, source filename.
        /// </summary>
        /// <param name="logPriority">Log priority</param>
        /// <param name="messageFormat">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        public static void SafeWriteDebug(LogPriority logPriority, string messageFormat, params object[] args)
        {
#if DEBUG
            StackFrame frame = new StackFrame(2, true);
            string className = frame.GetMethod().DeclaringType.Name;
            string memberName = frame.GetMethod().Name;
            int lineNumber = frame.GetFileLineNumber();
            string fileName = frame.GetFileName();
            messageFormat = string.Format("{0}{1}{2}.{3}() Line {4}: {5}", messageFormat, LogBase.LogNewLine, className, memberName, lineNumber, fileName);
#endif
            SafeWrite(logPriority, messageFormat, args);
        }

        /// <summary>
        /// Log of last resort: Log a message including the line number, member name, source filename.
        /// </summary>
        /// <param name="exception">The exception caught</param>
        /// <param name="messageFormat">String formatted message</param>
        /// <param name="args">formatted message arguments</param>
        public static void SafeWriteDebug(LogPriority logPriority, Exception exception, string messageFormat, params object[] args)
        {
#if DEBUG
            StackFrame frame = new StackFrame(2, true);
            string className = frame.GetMethod().DeclaringType.Name;
            string memberName = frame.GetMethod().Name;
            int lineNumber = frame.GetFileLineNumber();
            string fileName = frame.GetFileName();
            messageFormat = string.Format("{0}{1}{2}.{3}() Line {4}: {5}", messageFormat, LogBase.LogNewLine, className, memberName, lineNumber, fileName);
#endif
            SafeWrite(logPriority, exception, messageFormat, args);
        }
    }
}
