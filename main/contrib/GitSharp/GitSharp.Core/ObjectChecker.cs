/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
 * 
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
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core
{
    /// <summary>
    /// Verifies that an object is formatted correctly.
    /// <para />
    /// Verifications made by this class only check that the fields of an object are
    /// formatted correctly. The ObjectId checksum of the object is not verified, and
    /// connectivity links between objects are also not verified. Its assumed that
    /// the caller can provide both of these validations on its own.
    /// <para />
    /// Instances of this class are not thread safe, but they may be reused to
    /// perform multiple object validations.
    /// </summary>
    public class ObjectChecker
    {
        /** Header "tree " */
        public static byte[] tree = Constants.encodeASCII("tree ");

        /** Header "parent " */
        public static byte[] parent = Constants.encodeASCII("parent ");

        /** Header "author " */
        public static byte[] author = Constants.encodeASCII("author ");

        /** Header "committer " */
        public static byte[] committer = Constants.encodeASCII("committer ");

        /** Header "encoding " */
        public static byte[] encoding = Constants.encodeASCII("encoding ");

        /** Header "object " */
        public static byte[] @object = Constants.encodeASCII("object ");

        /** Header "type " */
        public static byte[] type = Constants.encodeASCII("type ");

        /** Header "tag " */
        public static byte[] tag = Constants.encodeASCII("tag ");

        /** Header "tagger " */
        public static byte[] tagger = Constants.encodeASCII("tagger ");


        private MutableObjectId tempId = new MutableObjectId();

        private MutableInteger ptrout = new MutableInteger();

        /// <summary>
        /// Check an object for parsing errors.
        /// </summary>
        /// <param name="objType">
        /// Type of the object. Must be a valid object type code in
        /// <see cref="Constants"/>.</param>
        /// <param name="raw">
        /// The raw data which comprises the object. This should be in the
        /// canonical format (that is the format used to generate the
        /// <see cref="ObjectId"/> of the object). The array is never modified.
        /// </param>
        /// <exception cref="CorruptObjectException">If any error is identified.</exception>
        public void check(int objType, byte[] raw)
        {
            switch (objType)
            {
                case Constants.OBJ_COMMIT:
                    checkCommit(raw);
                    break;

                case Constants.OBJ_TAG:
                    checkTag(raw);
                    break;

                case Constants.OBJ_TREE:
                    checkTree(raw);
                    break;

                case Constants.OBJ_BLOB:
                    checkBlob(raw);
                    break;

                default:
                    throw new CorruptObjectException("Invalid object type: " + objType);
            }
        }

        private int id(byte[] raw, int ptr)
        {
            try
            {
                tempId.FromString(raw, ptr);
                return ptr + Constants.OBJECT_ID_STRING_LENGTH;
            }
            catch (ArgumentException)
            {
                return -1;
            }
        }

        private int personIdent(byte[] raw, int ptr)
        {
            int emailB = RawParseUtils.nextLF(raw, ptr, (byte)'<');
            if (emailB == ptr || raw[emailB - 1] != '<')
                return -1;

            int emailE = RawParseUtils.nextLF(raw, emailB, (byte)'>');
            if (emailE == emailB || raw[emailE - 1] != '>')
                return -1;
            if (emailE == raw.Length || raw[emailE] != ' ')
                return -1;

            RawParseUtils.parseBase10(raw, emailE + 1, ptrout); // when
            ptr = ptrout.value;
            if (emailE + 1 == ptr)
                return -1;
            if (ptr == raw.Length || raw[ptr] != ' ')
                return -1;

            RawParseUtils.parseBase10(raw, ptr + 1, ptrout); // tz offset
            if (ptr + 1 == ptrout.value)
                return -1;
            return ptrout.value;
        }

        /// <summary>
        /// Check a commit for errors.
        /// </summary>
        /// <param name="raw">The commit data. The array is never modified.</param>
        /// <exception cref="CorruptObjectException">If any error was detected.</exception>
        public void checkCommit(byte[] raw)
        {
            int ptr = 0;

            if ((ptr = RawParseUtils.match(raw, ptr, tree)) < 0)
                throw new CorruptObjectException("no tree header");
            if ((ptr = id(raw, ptr)) < 0 || raw[ptr++] != '\n')
                throw new CorruptObjectException("invalid tree");

            while (RawParseUtils.match(raw, ptr, parent) >= 0)
            {
                ptr += parent.Length;
                if ((ptr = id(raw, ptr)) < 0 || raw[ptr++] != '\n')
                    throw new CorruptObjectException("invalid parent");
            }

            if ((ptr = RawParseUtils.match(raw, ptr, author)) < 0)
                throw new CorruptObjectException("no author");
            if ((ptr = personIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
                throw new CorruptObjectException("invalid author");

            if ((ptr = RawParseUtils.match(raw, ptr, committer)) < 0)
                throw new CorruptObjectException("no committer");
            if ((ptr = personIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
                throw new CorruptObjectException("invalid committer");
        }

        /// <summary>
        /// Check an annotated tag for errors.
        /// </summary>
        /// <param name="raw">The tag data. The array is never modified.</param>
        /// <exception cref="CorruptObjectException">If any error was detected.</exception>
        public void checkTag(byte[] raw)
        {
            int ptr = 0;

            if ((ptr = RawParseUtils.match(raw, ptr, @object)) < 0)
                throw new CorruptObjectException("no object header");
            if ((ptr = id(raw, ptr)) < 0 || raw[ptr++] != '\n')
                throw new CorruptObjectException("invalid object");

            if ((ptr = RawParseUtils.match(raw, ptr, type)) < 0)
                throw new CorruptObjectException("no type header");
            ptr = RawParseUtils.nextLF(raw, ptr);

            if ((ptr = RawParseUtils.match(raw, ptr, tag)) < 0)
                throw new CorruptObjectException("no tag header");
            ptr = RawParseUtils.nextLF(raw, ptr);

            if ((ptr = RawParseUtils.match(raw, ptr, tagger)) > 0)
            {
                if ((ptr = personIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
                    throw new CorruptObjectException("invalid tagger");
            }
        }

        private static int lastPathChar(int mode)
        {
            return FileMode.Tree.Equals(mode) ? '/' : '\0';
        }

        private static int pathCompare(byte[] raw, int aPos, int aEnd, int aMode, int bPos, int bEnd, int bMode)
        {
            while (aPos < aEnd && bPos < bEnd)
            {
                int cmp = (raw[aPos++] & 0xff) - (raw[bPos++] & 0xff);
                if (cmp != 0)
                    return cmp;
            }

            if (aPos < aEnd)
                return (raw[aPos] & 0xff) - lastPathChar(bMode);
            if (bPos < bEnd)
                return lastPathChar(aMode) - (raw[bPos] & 0xff);
            return 0;
        }

        private static bool duplicateName(byte[] raw, int thisNamePos, int thisNameEnd)
        {
            int sz = raw.Length;
            int nextPtr = thisNameEnd + 1 + Constants.OBJECT_ID_LENGTH;
            for (; ; )
            {
                int nextMode = 0;
                for (; ; )
                {
                    if (nextPtr >= sz)
                        return false;
                    byte c = raw[nextPtr++];
                    if (' ' == c)
                        break;
                    nextMode <<= 3;
                    nextMode += (c - (byte)'0');
                }

                int nextNamePos = nextPtr;
                for (; ; )
                {
                    if (nextPtr == sz)
                        return false;
                    byte c = raw[nextPtr++];
                    if (c == '\0')
                        break;
                }
                if (nextNamePos + 1 == nextPtr)
                    return false;

                int cmp = pathCompare(raw, thisNamePos, thisNameEnd, FileMode.Tree.Bits, nextNamePos, nextPtr - 1, nextMode);
                if (cmp < 0)
                    return false;
                else if (cmp == 0)
                    return true;

                nextPtr += Constants.OBJECT_ID_LENGTH;
            }
        }

        /// <summary>
        /// Check a canonical formatted tree for errors.
        /// </summary>
        /// <param name="raw">The raw tree data. The array is never modified.</param>
        /// <exception cref="CorruptObjectException">If any error was detected.</exception>
        public void checkTree(byte[] raw)
        {
            int sz = raw.Length;
            int ptr = 0;
            int lastNameB = 0, lastNameE = 0, lastMode = 0;

            while (ptr < sz)
            {
                int thisMode = 0;
                for (; ; )
                {
                    if (ptr == sz)
                        throw new CorruptObjectException("truncated in mode");
                    byte c = raw[ptr++];
                    if (' ' == c)
                        break;
                    if (c < '0' || c > '7')
                        throw new CorruptObjectException("invalid mode character");
                    if (thisMode == 0 && c == '0')
                        throw new CorruptObjectException("mode starts with '0'");
                    thisMode <<= 3;
                    thisMode += (c - (byte)'0');
                }

                if (FileMode.FromBits(thisMode).ObjectType == ObjectType.Bad)
                    throw new CorruptObjectException("invalid mode " + NB.DecimalToBase(thisMode, 8));

                int thisNameB = ptr;
                for (; ; )
                {
                    if (ptr == sz)
                        throw new CorruptObjectException("truncated in name");
                    byte c = raw[ptr++];
                    if (c == '\0')
                        break;
                    if (c == '/')
                        throw new CorruptObjectException("name contains '/'");
                }
                if (thisNameB + 1 == ptr)
                    throw new CorruptObjectException("zero length name");
                if (raw[thisNameB] == '.')
                {
                    int nameLen = (ptr - 1) - thisNameB;
                    if (nameLen == 1)
                        throw new CorruptObjectException("invalid name '.'");
                    if (nameLen == 2 && raw[thisNameB + 1] == '.')
                        throw new CorruptObjectException("invalid name '..'");
                }
                if (duplicateName(raw, thisNameB, ptr - 1))
                    throw new CorruptObjectException("duplicate entry names");

                if (lastNameB != 0)
                {
                    int cmp = pathCompare(raw, lastNameB, lastNameE,
                           lastMode, thisNameB, ptr - 1, thisMode);
                    if (cmp > 0)
                        throw new CorruptObjectException("incorrectly sorted");
                }

                lastNameB = thisNameB;
                lastNameE = ptr - 1;
                lastMode = thisMode;

                ptr += Constants.OBJECT_ID_LENGTH;
                if (ptr > sz)
                    throw new CorruptObjectException("truncated in object id");
            }
        }

        /// <summary>
        /// Check a blob for errors.
        /// </summary>
        /// <param name="raw">The blob data. The array is never modified.</param>
        /// <exception cref="CorruptObjectException">If any error was detected.</exception>
        public void checkBlob(byte[] raw)
        {
            // We can always assume the blob is valid.
        }
    }

}
