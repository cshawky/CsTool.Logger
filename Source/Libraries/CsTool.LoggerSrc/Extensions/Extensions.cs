// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2020 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
// Please refer to LICENCE.txt in this project folder.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

/// <summary>
/// CsTool.Extensions namespace is tied to the new Logger interface and will supersede CsTool.ExtensionMethods.
/// Use these extensions instead of CsTool.ExtensionMethods
/// </summary>
namespace CsTool.Extensions
{
    using System;

    /// <summary>
    /// Helpful string extensions, kept due to historical reasons. There may nbow be a more efficient way to do this...
    /// </summary>
    public static class MyStringExtensions
    {
      public static bool IsNullOrEmpty(this string str)
        {
            return str == null || str == String.Empty || str[0] == '\0';
        }

        /// <summary>
        /// Inspect the string and return true if it is null or contains only white space.
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <returns>True if the string is null or contains white space only.</returns>
        public static bool IsNullOrWhiteSpace(this string str)
        {
            if (str == null) return true;
            int len = str.Length;
            if (len == 0) return true;
            if (str[0] == '\0') return true;
            for (int i = 0; i < len; i++)
            {
                if (str[i] == ' ') continue;
                if (str[i] == '\t') continue;
                if (str[i] == 0x09) continue;
                return false;
            }
            return true;
        }
    }
}
