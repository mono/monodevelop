/*
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
using GitSharp.Core.Util.JavaHelper;

namespace GitSharp.Core.Util
{
    public static class StringExtension
    {
        /// <summary>
        ///   Helper function to easily replace all occurences of the incompatible string.Substring method in ported java code
        /// </summary>
        /// <param name="longstring">
        ///   The string from which a part has to extracted.
        /// </param>
        /// <param name="beginIndex">
        ///   The beginning index, inclusive.
        /// </param>
        /// <param name="endIndex">
        ///   The ending index, exclusive.
        /// </param>
        /// <returns>
        ///   The specified substring.
        /// </returns>
        public static string Slice(this string longstring, int beginIndex, int endIndex)
        {
            #region Parameters Validation

            if (beginIndex > endIndex)
            {
                throw new ArgumentOutOfRangeException("beginIndex",
                                                      string.Format(
                                                          "beginIndex has to be less or equal than endIndex. Actual values were beginIndex={0} and endIndex={1}",
                                                          beginIndex, endIndex));
            }

            #endregion

            return longstring.Substring(beginIndex, endIndex - beginIndex);
        }

        public static byte[] getBytes(this string plainString, string encodingAlias)
        {
            Encoding encoder = Charset.forName(encodingAlias);

            try
            {
                return encoder.GetBytes(plainString);
            }
            catch (EncoderFallbackException e)
            {
                throw new EncoderFallbackException(
                    string.Format("A problem occured while encoding '{0}' using encoder '{1}'.", plainString,
                                  encoder.WebName), e);
            }
        }

        public static byte[] getBytes(this string plainString)
        {
            return plainString.getBytes("UTF-8");
        }

        /// <summary>
        /// Compares two strings lexicographically. (cf. http://java.sun.com/j2se/1.4.2/docs/api/java/lang/String.html#compareTo(java.lang.String))
        /// </summary>
        /// <param name="inputString">the reference string</param>
        /// <param name="stringToCompareTo">the string to be compared</param>
        /// <returns>the value 0 if the string to compared with is equal to this string; a value less than 0 if this string is lexicographically less than the string to compare with; and a value greater than 0 if this string is lexicographically greater than the string to compare with.</returns>
        public static int compareTo(this string inputString, string stringToCompareTo)
        {
            int aLength = inputString.Length;
            int anotherLength = stringToCompareTo.Length;

            for (int i = 0; i < Math.Min(aLength, anotherLength); i++)
            {
                char aChar = inputString[i];
                char anotherChar = stringToCompareTo[i];

                if (aChar == anotherChar)
                {
                    continue;
                }

                return aChar - anotherChar;
            }

            if (aLength != anotherLength)
            {
                return (aLength - anotherLength);
            }

            return 0;
        }
    }
}
