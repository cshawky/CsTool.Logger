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
    /// Silent implementation that does nothing.
    /// Useful for: unit tests, CI/CD pipelines, headless/service applications.
    /// </summary>
    public class SilentMessageBoxService : IMessageBoxService
    {
        private readonly bool defaultConfirmationResponse;

        /// <summary>
        /// Initialize silent message box service.
        /// </summary>
        /// <param name="defaultConfirmationResponse">Default response for confirmation dialogs (true = Yes, false = No)</param>
        public SilentMessageBoxService(bool defaultConfirmationResponse = false)
        {
            this.defaultConfirmationResponse = defaultConfirmationResponse;
        }

        /// <summary>
        /// Silently logs that a message would have been shown (no actual display).
        /// </summary>
        public void Show(string message, string title)
        {
            // No-op: message is already logged by LogBase before calling this
        }

        /// <summary>
        /// Returns the configured default response without user interaction.
        /// </summary>
        public bool ShowConfirmation(string message, string title)
        {
            return defaultConfirmationResponse;
        }
    }
}
