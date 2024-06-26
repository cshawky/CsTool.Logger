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
    using System.IO;
    using System.Text;

    //
    // Should not be using Windows.Forms. Migrate to WPF or MAUI. Included for historic support.
    //
    using System.Windows.Forms;


    /// <summary>
    /// The Thread Safe Logger interface for your application.
    /// </summary>
    /// <remarks>Refer to <code>LogBase</code> for a better explanation</remarks>
    public partial class LogBase : ILogBase
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region ShowMessages

        /// <summary>
        /// Enable or disable the use of message dialogue boxes requested through LogMessage() and LogExceptionMessage().
        /// This option is normally enabled except when the programme is expected to continue operations without user
        /// intervention. Refer to <code>DefaultSettings.IsShowMessagesEnabled</code> for more information.
        /// </summary>
        /// <remarks>This property is available through any ViewModel that inherits <code>OnPropertyChangedViewModel</code></remarks>
        public bool IsShowMessagesEnabled
        {
            get
            {
                if (ShowMessagesDisabledCount > 0) return false;
                return IsShowMessagesEnabledByDefault;
            }
        }

        /// <summary>
        /// Backend field for the <code>IsSaveSettingsNeeded</code> property.
        /// </summary>
        private bool isShowMessagesEnabledByDefault;


        public bool IsShowMessagesEnabledByDefault
        {
            get
            {
                return isShowMessagesEnabledByDefault;
            }
            set
            {
                lock (disableShowMessagesLock)
                {
                    if (isShowMessagesEnabledByDefault != value)
                        isShowMessagesEnabledByDefault = value;
                }
            }
        }

        /// <summary>
        /// Incremented each time a request is made to temporarily disable the message displays.
        /// Decremented each time the request is cancelled (restore).
        /// </summary>
        /// <remarks>This property is available through any ViewModel that inherits <code>OnPropertyChangedViewModel</code></remarks>
        public int ShowMessagesDisabledCount { get; private set; }

        private Object disableShowMessagesLock = new Object();

        /// <summary>
        /// User interface for enabling or disabling the display of message boxes at runtime. The default setting for the underlying 
        /// property <code>Logger.IsShowMessagesEnabled</code> is controlled by the <code>DefaultSettings.xml</code> file.
        /// 
        /// Changing this property at runtime does not affect the default setting for application start up. That setting must be edited directly
        /// in the application <code>DefaultSettings.xml</code> file.
        /// 
        /// Normally, one would disable message boxes whilst the Path Scanner or other re-iterative modules are active.
        /// </summary>
        /// <remarks>
        /// To temporarily disable the display of message (for example when a background timed task executes) then
        /// use <code>DisableShowMessages</code> and <code>RestoreShowMessages</code>
        /// This property is available through any ViewModel that inherits <code>OnPropertyChangedViewModel</code></remarks>
        public void DisableShowMessages()
        {
            lock (disableShowMessagesLock)
            {
                ShowMessagesDisabledCount++;
            }
        }

        /// <summary>
        /// Restore (release) the temporary disable on ShowMessages.
        /// </summary>
        /// <remarks>This method is available through any ViewModel that inherits <code>OnPropertyChangedViewModel</code></remarks>
        public void RestoreShowMessages()
        {
            lock (disableShowMessagesLock)
            {
                if (ShowMessagesDisabledCount > 0)
                    ShowMessagesDisabledCount--;
            }
        }

        #endregion ShowMessages

        #region Message Dialogue Boxes
        /// <summary>
        /// Displays the modal message box asking a question for the user to answer Yes or No.
        /// The message should be clear such that the correct button is pressed. The details are logged.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="dontLogMessage"></param>
        /// <returns>True = "Yes", False = "No"</returns>
        public bool ShowConfirmationMessage(string message, string title, bool dontLogMessage)
        {
            // Show the message in a dialogue box
            bool result = false;
            DialogResult pressed = MessageBox.Show(message, title, MessageBoxButtons.YesNo);
            switch (pressed)
            {
                case DialogResult.No:
                case DialogResult.Cancel:
                case DialogResult.None:
                    result = false;
                    break;

                case DialogResult.OK:
                case DialogResult.Yes:
                    result = true;
                    break;
            }

            // By default all dialogue box messages are logged
            if (dontLogMessage == false)
            {
                // Log the message
                string logMessage = "User Confirmation Message: " + title + " : " + message
                    + "\nUser Confirmation Message Response: " + pressed + ", response treated as " + result + ".";
                Write(LogPriority.Always, logMessage);
            }

            return result;
        }

        /// <summary>
        /// Display the Exception in a popup dialogue box.
        /// </summary>
        /// <param name="logPriority"></param>
        /// <param name="exception"></param>
        /// <param name="progressMessage"></param>
        public void ShowExceptionMessage(LogPriority logPriority, Exception exception, string progressMessage)
        {
            //
            // Do not log if priority is not high enough
            //
            if ((Int32)logPriority > (Int32)LogThresholdMaxLevel) return;

            //
            // A form method for displaying some error info
            //
            string errorMessage = "";
            try
            {
                errorMessage = string.Concat(
                                progressMessage,
                                "\n**Exception: ", exception.Message,
                                "\n  Line: ", exception.Source,
                                "\n  StackTrace: ", exception.StackTrace);
            }
            catch (Exception exception2)
            {
                // Exception most likely related to LogMessage.
                throw new ApplicationException(errorMessage, exception2);
            }
            finally
            {
                ShowMessage(errorMessage, "Debug Exception Message", false);
            }
        }

        /// <summary>
        /// Display a message in a popup dialogue box.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        public void ShowMessage(string message, string title)
        {
            // Log and show the message in a dialogue box
            ShowMessage(message, title, false);
        }

        /// <summary>
        /// @TODO: Do Summary
        /// </summary>
        /// <param name="message"></param>
        /// <param name="title"></param>
        /// <param name="dontLogMessage"></param>
        public void ShowMessage(string message, string title, bool dontLogMessage)
        {
            // By default all dialogue box messages are logged
            if (!dontLogMessage || !IsShowMessagesEnabled)
            {
                // Log the message
                string logMessage = "User Message: " + title + " : " + message;
                Write(LogPriority.Always, logMessage);
            }

            // Show the message in a dialogue box
            if (IsShowMessagesEnabled)
                MessageBox.Show(message, title);
        }

        #endregion Message Dialogue Boxes
    }

}