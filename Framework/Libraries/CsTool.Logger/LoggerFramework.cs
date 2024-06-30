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
        public static void ShowMessage(string simpleMessage, string title) => Instance.ShowMessage(simpleMessage, title);
        public static void ShowMessage(string simpleMessage, string title, bool doNotLogMessage) => Instance.ShowMessage(simpleMessage, title, doNotLogMessage);
        public static void ShowExceptionMessage(LogPriority logPriority, Exception exception, string progressMessage) => Instance.ShowExceptionMessage(logPriority, exception, progressMessage);

        public static bool ShowConfirmationMessage(string message, string title, bool doNotLogMessage) => Instance.ShowConfirmationMessage(message, title, doNotLogMessage);

        public static bool IsShowMessagesEnabled => Instance.IsShowMessagesEnabled;

        public static bool IsShowMessagesEnabledByDefault => Instance.IsShowMessagesEnabledByDefault;

        public static int ShowMessagesDisabledCount => Instance.ShowMessagesDisabledCount;

        public static void DisableShowMessages() => Instance.DisableShowMessages();

        public static void EnableShowMessages() => Instance.RestoreShowMessages();

        public static void RestoreShowMessages() => Instance.RestoreShowMessages();

    }
}
