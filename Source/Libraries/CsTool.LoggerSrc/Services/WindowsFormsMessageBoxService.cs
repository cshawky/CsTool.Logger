// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2020 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
// Please refer to LICENCE.txt in this project folder.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

#if NETFRAMEWORK
namespace CsTool.Logger
{
    using System.Windows.Forms;

    /// <summary>
    /// Windows Forms implementation of message box service.
    /// Available for: .NET Framework 4.8.1 and .NET 10 Windows targets.
    /// </summary>
    public class WindowsFormsMessageBoxService : IMessageBoxService
    {
        /// <summary>
        /// Display an informational message using Windows Forms MessageBox.
        /// </summary>
        public void Show(string message, string title)
        {
            MessageBox.Show(message, title);
        }

        /// <summary>
        /// Display a Yes/No confirmation dialog using Windows Forms MessageBox.
        /// </summary>
        public bool ShowConfirmation(string message, string title)
        {
            DialogResult result = MessageBox.Show(message, title, MessageBoxButtons.YesNo);
            return result == DialogResult.Yes;
        }
    }
}
#endif
