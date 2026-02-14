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

    /// <summary>
    /// Console-based implementation of message box service.
    /// Cross-platform fallback for non-Windows environments or console applications.
    /// </summary>
    public class ConsoleMessageBoxService : IMessageBoxService
    {
        /// <summary>
        /// Display an informational message to the console.
        /// </summary>
        public void Show(string message, string title)
        {
            Console.WriteLine();
            Console.WriteLine($"═══ {title} ═══");
            Console.WriteLine(message);
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey(intercept: true);
            Console.WriteLine();
        }

        /// <summary>
        /// Display a Yes/No confirmation prompt in the console.
        /// </summary>
        public bool ShowConfirmation(string message, string title)
        {
            Console.WriteLine();
            Console.WriteLine($"═══ {title} ═══");
            Console.WriteLine(message);
            Console.Write("Continue? (Y/N): ");

            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(intercept: true);
                char response = char.ToUpperInvariant(key.KeyChar);

                if (response == 'Y')
                {
                    Console.WriteLine("Yes");
                    return true;
                }
                else if (response == 'N')
                {
                    Console.WriteLine("No");
                    return false;
                }
                // Invalid input, prompt again
            }
        }
    }
}
