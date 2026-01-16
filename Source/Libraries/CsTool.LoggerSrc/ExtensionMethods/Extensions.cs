// -------------------------------------------------------------------------------------------------------------------------
// <copyright>
// https://www.apache.org/licenses/LICENSE-2.0
// Copyright 2020 Chris Shawcross "cshawky", SHAWKY Electronics, Australia
// Please refer to LICENCE.txt in this project folder.
// </copyright>
// -------------------------------------------------------------------------------------------------------------------------

/// <summary>
/// TODO Consider migrating CsTool.ExtensionMethods to CsTool.Logger.ExtensionMethods
/// CsTool.Extensions namespace is tied to the new Logger interface and may supersede CsTool.ExtensionMethods.
/// Use these extensions instead of CsTool.ExtensionMethods.
/// CsTool.CoreUtilities has an expanded set of extensions in class <code>ExtensionMethods</code>.
/// </summary>
namespace CsTool.Logger.ExtensionMethods
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Helpful string extensions, kept due to historical reasons. There may nbow be a more efficient way to do this...
    /// </summary>
    public static class MyStringExtensions
    {
        /// <summary>
        /// Framework does not support Replace(String, String, StringComparison) as with .NET 10
        /// </summary>
        /// <param name="str">the string to search</param>
        /// <param name="search">The search string</param>
        /// <param name="replacement">The replacement string</param>
        /// <returns>The modified string</returns>
        public static string ReplaceIgnoreCase(this string str, string search, string replacement)
        {
            return Regex.Replace(str, Regex.Escape(search), replacement, RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Old library. Now supported in Framework
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty_Deprecated(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        /// <summary>
        /// Inspect the string and return true if it is null or contains only white space.
        /// Tabs, returns are considered white space. This differs slightly from String.IsNullOrWhiteSpace().
        /// </summary>
        /// <param name="str">The string to check</param>
        /// <returns>True if the string is null or contains white space only.</returns>
        public static bool IsNullOrWhiteSpace_Deprecated(this string str)
        {
            return string.IsNullOrWhiteSpace(str);
        }
    }
}
