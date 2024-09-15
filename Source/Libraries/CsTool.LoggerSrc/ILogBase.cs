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
    /// The Thread Safe Logger interface for your application.
    /// Public LogMessage/Write methods (overridable).
    /// </summary>
    /// <remarks>Refer to <code>LogBase</code> for a better explanation
    /// 
    /// Simplest Usage:
    /// 
    ///     Logger.Write("Hello World");
    ///     Logger.Write(LogPriority.Fatal,"Goodbye");
    /// 
    /// Multiple Logger interface:
    /// 
    ///     LogBase logger1 = new LogBase("Logger1");
    ///     LogBase logger2 = new LogBase("Logger2");
    ///     logger1.Write("Hello World");
    ///     logger2.Write("Hello World");
    /// </remarks>
    public interface ILogBase : IDisposable
    {
        //
        // CsTool.CoreUtilities.Logger Interfaces. 
        //
        void CloseAndFlush(int waitTime = 0, string closeReason = null);
        void DisplayLogFile();
        bool IsLogPriorityEnabled(LogPriority priority);
        void Write(string messageFormat, params object[] args);
        void Write(Exception exception, string progressMessage, params object[] args);
        void Write(LogPriority logPriority, string messageFormat);
        void Write(LogPriority logPriority, string messageFormat, params object[] args);
        void Write(LogPriority logPriority, Exception exception, string progressMessage, params object[] args);
        void Write(LogPriority logPriority, string messageFormat, NameValueCollection parameters);
        string ConstructExceptionMessage(Exception exception, string progressMessage);
        void WriteRaw(LogPriority logPriority, string rawMessage, params object[] args);
        void LogMessageWithStats(string message, bool countAsError = false, bool ignoreExceptions = false);
        void LogMessageWithStats(LogPriority logPriority, string message, bool countAsError = false, bool ignoreExceptions = false);

        //
        // CsTool.Logger new interfaces to handle back end actions through the Async FIFO
        // such as log file rename (messages are flushed before rename occurs) that should be asynchronous re queue position.
        //
        void LogCommand(LogCommandAction logCommand);
        void LogCommand(LogCommandAction logCommand, params object[] args);

    }
}
