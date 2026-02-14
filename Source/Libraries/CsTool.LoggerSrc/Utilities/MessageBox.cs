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

    public partial class Logger
    {
        //
        // Legacy CsTool.CoreUtilities interfaces using Windows Forms dialogue boxes.
        //
        /// <summary>
        /// Show a message in a dialogue box.
        /// </summary>
        /// <param name="simpleMessage">The message</param>
        /// <param name="title">Title of the dialogue box</param>
        public static void ShowMessage(string simpleMessage, string title) => Instance.ShowMessage(simpleMessage, title);
        /// <summary>
        /// Show a message in a dialogue box.
        /// </summary>
        /// <param name="simpleMessage">The message</param>
        /// <param name="title">Title of the dialogue box</param>
        public static void ShowMessage(string simpleMessage, string title, bool doNotLogMessage) => Instance.ShowMessage(simpleMessage, title, doNotLogMessage);
        public static void ShowExceptionMessage(LogPriority logPriority, Exception exception, string progressMessage) => Instance.ShowExceptionMessage(logPriority, exception, progressMessage);

        public static bool ShowConfirmationMessage(string message, string title, bool doNotLogMessage) => Instance.ShowConfirmationMessage(message, title, doNotLogMessage);

        /// <summary>
        /// Is the ShowMessagesEnabled flag set?
        /// </summary>
        public static bool IsShowMessagesEnabled => Instance.IsShowMessagesEnabled;

        /// <summary>
        /// Enable or disable the use of ShowMessage() if used in the code.
        /// </summary>
        public static bool IsShowMessagesEnabledByDefault { get => Instance.IsShowMessagesEnabledByDefault; set => Instance.IsShowMessagesEnabledByDefault = value; }

        public static int ShowMessagesDisabledCount => Instance.ShowMessagesDisabledCount;

        public static void DisableShowMessages() => Instance.DisableShowMessages();

        public static void EnableShowMessages() => Instance.RestoreShowMessages();

        public static void RestoreShowMessages() => Instance.RestoreShowMessages();

    }
}
