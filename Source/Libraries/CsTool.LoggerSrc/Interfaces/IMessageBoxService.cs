// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2020 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
// Please refer to LICENCE.txt in this project folder.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

namespace CsTool.Logger
{
    /// <summary>
    /// Cross-platform abstraction for displaying message dialogs.
    /// </summary>
    public interface IMessageBoxService
    {
        /// <summary>
        /// Display an informational message.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The dialog title</param>
        void Show(string message, string title);

        /// <summary>
        /// Display a confirmation dialog with Yes/No options.
        /// </summary>
        /// <param name="message">The message to display</param>
        /// <param name="title">The dialog title</param>
        /// <returns>True if Yes was selected, False otherwise</returns>
        bool ShowConfirmation(string message, string title);
    }

    /// <summary>
    /// Result of a message box interaction.
    /// </summary>
    public enum MessageBoxResult
    {
        None = 0,
        OK = 1,
        Cancel = 2,
        Yes = 6,
        No = 7
    }
}
