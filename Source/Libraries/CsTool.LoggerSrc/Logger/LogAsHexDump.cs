using CsTool.Logger;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace CsTool.Logger
{
    /// <summary>
    /// Extensions to the logger interface for hex dump logging. The aim at present is to
    /// create a hex dump similar to that shown via an editor such as Notepad++/UEStudio.
    /// Performance has not been measured though the code is adapted from a very good
    /// StackOverflow answer that suggests this approach is the fastest safe method.
    /// https://stackoverflow.com/questions/311165/how-do-you-convert-a-byte-array-to-a-hexadecimal-string-and-vice-versa/24343727#24343727
    /// CodesInChaos: https://stackoverflow.com/a/24343727/48700
    /// </summary>
    public partial class LogBase : ILogBase
    {
        //
        // -----------------------------------------------------------------------------------------
        //
        #region Methods

        #endregion Methods

        //
        // -----------------------------------------------------------------------------------------
        //
        // The following methods are adapted from CodesInChaos https://stackoverflow.com/a/24343727/48700
        #region Methods from CodesInChaos

        // Optimised lookup table for converting byte to hex string
        private static readonly uint[] _lookup32 = CreateLookup32();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint[] CreateLookup32()
        {
            var result = new uint[256];
            for (int i = 0; i < 256; i++)
            {
                string s = i.ToString("X2");
                result[i] = ((uint)s[0]) + ((uint)s[1] << 16);
            }
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string ByteArrayToHexViaLookup32(byte[] bytes)
        {
            return new string(ByteArrayToHexArrayViaLookup32(bytes));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char[] ByteArrayToHexArrayViaLookup32(byte[] bytes)
        {
            var lookup32 = _lookup32;
            char[] result = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                var val = lookup32[bytes[i]];
                result[2 * i] = (char)val;
                result[2 * i + 1] = (char)(val >> 16);
            }
            return result;
        }

        /// <summary>
        /// Formatted variant of <see cref="ByteArrayToHexArrayViaLookup32(byte[])"/>.
        /// Example using 48 characters split 32 per line
        ///     41 42 43 44 45 46 47 48 49 4A 4B 4C 4D 4E 4F 50  ABCDEFGHIJKLMNOP
        ///     41 42 43 44 45 46 47 48                          ABCDEFGH
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="maxBytes">The maximum number of bytes to use, 0 indicates all bytes. used to constrain fixed sized arrays</param>
        /// <returns>The formatted hex dump as a character array allowing for further processing</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static char[] ByteArrayToHexDumpViaLookup32(byte[] bytes, int maxBytes, int bytesPerLine = 32)
        {
            var lookup32 = _lookup32;
            int iMax = maxBytes > 0 && maxBytes < bytes.Length ? maxBytes : bytes.Length;
            int lines = (iMax + bytesPerLine - 1) / bytesPerLine; // Calculate number of lines needed
            char[] result = new char[(bytesPerLine * 3 + 2 + bytesPerLine + 1) * lines];
            //char[] result = new char[lines * (bytesPerLine * 4 + 3) - 1];
            int j = 0;
            for (int i = 0; i < iMax; i++)
            {
                if (i % bytesPerLine == 0 && i > 0)
                {
                    result[j++] = '\n'; // Add line feed after each line
                }
                var val = lookup32[bytes[i]];
                result[j++] = (char)val;
                result[j++] = (char)(val >> 16);
                result[j++] = ' ';
            }
            result[j++] = ' ';
            result[j++] = ' ';
            // Temporarily insert a line feed until we handle bytesPerLine
            result[j++] = '\n';
            for (int k = 0; k < iMax; k++)
            {
                if (k % bytesPerLine == 0 && k > 0)
                {
                    result[j++] = '\n'; // Add line feed after each line
                }
                char c = (char)bytes[k];
                // This could be optimised like _lookup32
                if (c > 31 && c < 127)
                {
                    result[j++] = (char)bytes[k];
                }
                else
                {
                    result[j++] = '.'; // Replace control characters with a dot
                }
            }
            while ( j < result.Length)
            {
                result[j++] = ' '; // Pad the array
            }
            return result;
        }
        #endregion Methods from CodesInChaos

    }
}
