/*
 * Copyright (C) 2009, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or
 * without modification, are permitted provided that the following
 * conditions are met:
 *
 * - Redistributions of source code must retain the above copyright
 *   notice, this list of conditions and the following disclaimer.
 *
 * - Redistributions in binary form must reproduce the above
 *   copyright notice, this list of conditions and the following
 *   disclaimer in the documentation and/or other materials provided
 *   with the distribution.
 *
 * - Neither the name of the Git Development Community nor the
 *   names of its contributors may be used to endorse or promote
 *   products derived from this software without specific prior
 *   written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
 * CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
 * INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
 * SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 * NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
 * CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
 * ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Text;

namespace GitSharp.Core.Util
{
    /// <summary>
    /// Miscellaneous string comparison utility methods.
    /// </summary>
    public static class StringUtils
    {
        private static readonly char[] LC;

        static StringUtils()
        {
            LC = new char['Z' + 1];
            for (char c = '\0'; c < LC.Length; c++)
                LC[c] = c;
            for (char c = 'A'; c <= 'Z'; c++)
                LC[c] = (char)('a' + (c - 'A'));
        }

        /// <summary>
        /// Convert the input to lowercase.
        /// <para>
        /// This method does not honor the JVM locale, but instead always behaves as
        /// though it is in the US-ASCII locale. Only characters in the range 'A'
        /// through 'Z' are converted. All other characters are left as-is, even if
        /// they otherwise would have a lowercase character equivalent.
        /// </para>
        /// </summary>
        /// <param name="c">the input character.</param>
        /// <returns>lowercase version of the input.</returns>
        public static char toLowerCase(char c)
        {
            return c <= 'Z' ? LC[c] : c;
        }

        /// <summary>
        /// Convert the input string to lower case, according to the "C" locale.
        /// <para>
        /// This method does not honor the JVM locale, but instead always behaves as
        /// though it is in the US-ASCII locale. Only characters in the range 'A'
        /// through 'Z' are converted, all other characters are left as-is, even if
        /// they otherwise would have a lowercase character equivalent.
        /// </para>
        /// </summary>
        /// <param name="in">the input string. Must not be null.</param>
        /// <returns>a copy of the input string, After converting characters in the range 'A'..'Z' to 'a'..'z'.</returns>
        public static string toLowerCase(string @in)
        {
            StringBuilder r = new StringBuilder(@in.Length);
            for (int i = 0; i < @in.Length; i++)
                r.Append(toLowerCase(@in[i]));
            return r.ToString();
        }

        /// <summary>
        /// Test if two strings are equal, ignoring case.
        /// <para>
        /// This method does not honor the JVM locale, but instead always behaves as
        /// though it is in the US-ASCII locale.
        /// </para>
        /// </summary>
        /// <param name="a">first string to compare.</param>
        /// <param name="b">second string to compare.</param>
        /// <returns>true if a equals b</returns>
        public static bool equalsIgnoreCase(string a, string b)
        {
            if (a == b)
                return true;
            if (a.Length != b.Length)
                return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (toLowerCase(a[i]) != toLowerCase(b[i]))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// Parse a string as a standard Git boolean value.
        /// <para/>
        /// The terms {@code yes}, {@code true}, {@code 1}, {@code on} can all be
        /// used to mean {@code true}.
        /// <para/>
        /// The terms {@code no}, {@code false}, {@code 0}, {@code off} can all be
        /// used to mean {@code false}.
        /// <para/>
        /// Comparisons ignore case, via <see cref="equalsIgnoreCase"/>.
        /// </summary>
        /// <param name="stringValue">the string to parse.</param>
        /// <returns>the boolean interpretation of <paramref name="stringValue"/>.</returns>
        public static bool toBoolean(string stringValue)
        {
            if (stringValue == null)
                throw new ArgumentNullException("stringValue", "Expected boolean string value");

            if (equalsIgnoreCase("yes", stringValue)
                    || equalsIgnoreCase("true", stringValue)
                    || equalsIgnoreCase("1", stringValue)
                    || equalsIgnoreCase("on", stringValue))
            {
                return true;

            }
            else if (equalsIgnoreCase("no", stringValue)
                    || equalsIgnoreCase("false", stringValue)
                    || equalsIgnoreCase("0", stringValue)
                    || equalsIgnoreCase("off", stringValue))
            {
                return false;

            }
            else
            {
                throw new ArgumentException("Not a boolean: " + stringValue);
            }
        }

    }
}
