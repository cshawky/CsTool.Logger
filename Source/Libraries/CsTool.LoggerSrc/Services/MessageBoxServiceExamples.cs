namespace CsTool.Logger
{

    // Example: How to use and customize the message box service
    using CsTool.Logger;
    using System.Collections.Generic;

    public class ExampleUsageClass
    {
        // This method demonstrates various ways to use and customize the message box service
        public void ExampleUsage()
        {
            // ═══════════════════════════════════════════════════════════════════════════
            // Option 1: Use the auto-detected default (recommended for most scenarios)
            // ═══════════════════════════════════════════════════════════════════════════

            Logger.Write("Hello World");  // Works automatically with platform-appropriate UI


            // ═══════════════════════════════════════════════════════════════════════════
            // Option 2: Explicitly set the service at application startup
            // ═══════════════════════════════════════════════════════════════════════════
#if NETFRAMEWORK
            // For Windows applications (uses Windows.Forms):
            LogBase.MessageBoxService = new WindowsFormsMessageBoxService();
#endif
            // For console applications:
            LogBase.MessageBoxService = new ConsoleMessageBoxService();

            // For automated tests or headless services:
            LogBase.MessageBoxService = new SilentMessageBoxService(defaultConfirmationResponse: true);

        }
    }
    // ═══════════════════════════════════════════════════════════════════════════
    // Option 3: Create custom implementation for your UI framework
    // ═══════════════════════════════════════════════════════════════════════════

    // Example: Avalonia UI implementation
#if AVALONIA
    public class AvaloniaMessageBoxService : IMessageBoxService
    {
        private Window _mainWindow;

        public AvaloniaMessageBoxService(Window mainWindow)
        {
            _mainWindow = mainWindow;
        }

        public void Show(string message, string title)
        {
            var messageBox = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxStandardWindow(title, message);
            messageBox.ShowDialog(_mainWindow).Wait();
        }

        public bool ShowConfirmation(string message, string title)
        {
            var messageBox = MessageBox.Avalonia.MessageBoxManager
                .GetMessageBoxStandardWindow(
                    title, 
                    message,
                    MessageBox.Avalonia.Enums.ButtonEnum.YesNo
                );
            var result = messageBox.ShowDialog(_mainWindow).Result;
            return result == MessageBox.Avalonia.Enums.ButtonResult.Yes;
        }

        // Then set it at startup:
        private void ExampleUsageAVALONIA()
        {
            LogBase.MessageBoxService = new AvaloniaMessageBoxService(mainWindow);
        }
    }
#endif


#if MAUI
    // Example: .NET MAUI implementation
    public class MauiMessageBoxService : IMessageBoxService
    {
        public async void Show(string message, string title)
        {
            await Application.Current.MainPage.DisplayAlert(title, message, "OK");
        }

        public async bool ShowConfirmation(string message, string title)
        {
            return await Application.Current.MainPage.DisplayAlert(title, message, "Yes", "No");
        }

        // Then set it in MauiProgram.cs or App.xaml.cs:
        private void ExampleUsageMAUI()
        {
            LogBase.MessageBoxService = new MauiMessageBoxService();
        }
    }

#endif


    // ═══════════════════════════════════════════════════════════════════════════
    // Option 4: Unit Testing - Mock the service
    // ═══════════════════════════════════════════════════════════════════════════

    public class TestMessageBoxService : IMessageBoxService
    {
        private static readonly List<(string message, string title)> list = new List<(string message, string title)>();

        public List<(string message, string title)> ShownMessages { get; } = list;
        public bool ConfirmationResponse { get; set; } = true;

        public void Show(string message, string title)
        {
            ShownMessages.Add((message, title));
        }

        public bool ShowConfirmation(string message, string title)
        {
            ShownMessages.Add((message, title));
            return ConfirmationResponse;
        }
    }

    public class UnitTestClass
    {
        // In your unit tests:
        //[TestMethod]
        public void TestLoggingWithMessageBox()
        {
            var testService = new TestMessageBoxService();
            LogBase.MessageBoxService = testService;

            // Your test code that triggers Logger.ShowMessage()...
            //   commented out for compilation as this is not actually a test module
            //Assert.AreEqual(1, testService.ShownMessages.Count);
            //Assert.AreEqual("Expected Message", testService.ShownMessages[0].message);
        }
    }
}