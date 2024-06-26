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
        public static void ShowExceptionMessage(LogPriority logPriority, Exception exception, string progressMessage) => Instance.ShowExceptionMessage(logPriority, exception, progressMessage);

    }
}
