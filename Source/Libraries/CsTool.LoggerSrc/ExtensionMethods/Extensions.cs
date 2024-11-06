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

    /// <summary>
    /// Helpful string extensions, kept due to historical reasons. There may nbow be a more efficient way to do this...
    /// </summary>
    public static class MyStringExtensions
    {
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
