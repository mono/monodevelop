/*
 * Copyright (C) 2007, Dave Watson <dwatson@mimvista.com>
 * Copyright (C) 2008, Robin Rosenberg <robin.rosenberg@dewire.com>
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2008, Kevin Thompson <kevin.thompson@theautomaters.com>
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

using System.Text;
using System;
using GitSharp.Core.Util;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util.JavaHelper;

namespace GitSharp.Core
{
    public static class Constants
    {
        /// <summary>
        /// Hash function used natively by Git for all objects.
        /// </summary>
        private const string HASH_FUNCTION = "SHA-1";

        /// <summary>
        /// A Git object hash is 160 bits, i.e. 20 bytes.
        /// <para>
        /// Changing this assumption is not going to be as easy as changing this declaration.
        /// </para>
        /// </summary>
        public const int OBJECT_ID_LENGTH = 20;

        /// <summary>
        /// A Git object can be expressed as a 40 character string of hexadecimal digits. <see cref="OBJECT_ID_LENGTH"/>
        /// </summary>
        public const int OBJECT_ID_STRING_LENGTH = OBJECT_ID_LENGTH * 2;

        /// <summary>
        /// Special name for the "HEAD" symbolic-ref.
        /// </summary>
        public const string HEAD = "HEAD";

        /// <summary>
        /// Text string that identifies an object as a commit.
        /// <para />
        /// Commits connect trees into a string of project histories, where each
        /// commit is an assertion that the best way to continue is to use this other
        /// tree (set of files).
        /// </summary>
        public const string TYPE_COMMIT = "commit";

        /// <summary>
        /// Text string that identifies an object as a blob.
        /// <para />
        /// Blobs store whole file revisions. They are used for any user file, as
        /// well as for symlinks. Blobs form the bulk of any project's storage space.
        /// </summary>
        public const string TYPE_BLOB = "blob";

        /// <summary>
        /// Text string that identifies an object as a tree.
        /// <para />
        /// Trees attach object ids (hashes) to names and file modes. The normal use
        /// for a tree is to store a version of a directory and its contents.
        /// </summary>
        public const string TYPE_TREE = "tree";

        /// <summary>
        /// Text string that identifies an object as an annotated tag.
        /// <para />
        /// Annotated tags store a pointer to any other object, and an additional
        /// message. It is most commonly used to record a stable release of the
        /// project.
        /// </summary>
        public const string TYPE_TAG = "tag";

        public static readonly byte[] EncodedTypeCommit = encodeASCII(TYPE_COMMIT);
        public static readonly byte[] EncodedTypeBlob = encodeASCII(TYPE_BLOB);
        public static readonly byte[] EncodedTypeTree = encodeASCII(TYPE_TREE);
        public static readonly byte[] EncodedTypeTag = encodeASCII(TYPE_TAG);

        /// <summary>
        /// An unknown or invalid object type code.
        /// </summary>
        public const int OBJ_BAD = -1;

        /// <summary>
        /// In-pack object type: extended types.
        /// <para />
        /// This header code is reserved for future expansion. It is currently
        /// undefined/unsupported.
        /// </summary>
        public const int OBJ_EXT = 0;

        /// <summary>
        /// In-pack object type: commit.
        /// <para />
        /// Indicates the associated object is a commit.
        /// <para />
        /// <b>This constant is fixed and is defined by the Git packfile format.</b>
        /// <seealso cref="TYPE_COMMIT"/>
        /// </summary>
        public const int OBJ_COMMIT = 1;

        /// <summary>
        /// In-pack object type: tree.
        /// <para />
        /// Indicates the associated object is a tree.
        /// <para />
        /// <b>This constant is fixed and is defined by the Git packfile format.</b>
        /// </summary>
        /// <seealso cref="TYPE_BLOB"/>
        public const int OBJ_TREE = 2;

        /// <summary>
        /// In-pack object type: blob.
        /// <para />
        /// Indicates the associated object is a blob.
        /// <para />
        /// <b>This constant is fixed and is defined by the Git packfile format.</b>
        /// </summary>
        /// <seealso cref="TYPE_BLOB"/>
        public const int OBJ_BLOB = 3;

        /// <summary>
        /// In-pack object type: annotated tag.
        /// <para />
        /// Indicates the associated object is an annotated tag.
        /// <para />
        /// <b>This constant is fixed and is defined by the Git packfile format.</b>
        /// </summary>
        /// <seealso cref="TYPE_TAG"/>
        public const int OBJ_TAG = 4;

        /// <summary>
        /// In-pack object type: reserved for future use.
        /// </summary>
        public const int OBJ_TYPE_5 = 5;

        /// <summary>
        /// In-pack object type: offset delta
        /// <para />
        /// Objects stored with this type actually have a different type which must
        /// be obtained from their delta base object. Delta objects store only the
        /// changes needed to apply to the base object in order to recover the
        /// original object.
        /// <para />
        /// An offset delta uses a negative offset from the start of this object to
        /// refer to its delta base. The base object must exist in this packfile
        /// (even in the case of a thin pack).
        /// <para />
        /// <b>This constant is fixed and is defined by the Git packfile format.</b>
        /// </summary>
        public const int OBJ_OFS_DELTA = 6;

        /// <summary>
        /// In-pack object type: reference delta
        /// <para />
        /// Objects stored with this type actually have a different type which must
        /// be obtained from their delta base object. Delta objects store only the
        /// changes needed to apply to the base object in order to recover the
        /// original object.
        /// <para />
        /// A reference delta uses a full object id (hash) to reference the delta
        /// base. The base object is allowed to be omitted from the packfile, but
        /// only in the case of a thin pack being transferred over the network.
        /// <para />
        /// <b>This constant is fixed and is defined by the Git packfile format.</b>
        /// </summary>
        public const int OBJ_REF_DELTA = 7;

        /// <summary>
        /// Pack file signature that occurs at file header - identifies file as Git
        /// packfile formatted.
        /// <para />
        /// <b>This constant is fixed and is defined by the Git packfile format.</b>
        /// </summary>
        public static readonly byte[] PACK_SIGNATURE = { (byte)'P', (byte)'A', (byte)'C', (byte)'K' };

        /// <summary>
        /// Native character encoding for commit messages, file names...
        /// </summary>
        public static readonly Encoding CHARSET = Charset.forName("UTF-8");

        /// <summary>
        /// Default main branch name
        /// </summary>
        public const string MASTER = "master";

        /// <summary>
        /// Prefix for branch refs
        /// </summary>
        public const string R_HEADS = "refs/heads/";

        /// <summary>
        /// Prefix for remotes refs
        /// </summary>
        public const string R_REMOTES = "refs/remotes/";

        /// <summary>
        /// Prefix for tag refs
        /// </summary>
        public const string R_TAGS = "refs/tags/";

        /// <summary>
        /// Prefix for any ref
        /// </summary>
        public const string R_REFS = "refs/";

        /// <summary>
        /// Logs folder name
        /// </summary>
        public const string LOGS = "logs";

        /// <summary>
        /// Info refs folder
        /// </summary>
        public const string INFO_REFS = "info/refs";

        /// <summary>
        /// Packed refs file
        /// </summary>
        public const string PACKED_REFS = "packed-refs";

        /// <summary>
        /// The environment variable that contains the system user name
        /// </summary>
        public const string OS_USER_NAME_KEY = "user.name";

        /// <summary>
        /// The environment variable that contains the author's name
        /// </summary>
        public const string GIT_AUTHOR_NAME_KEY = "GIT_AUTHOR_NAME";

        /// <summary>
        /// The environment variable that contains the author's email
        /// </summary>
        public const string GIT_AUTHOR_EMAIL_KEY = "GIT_AUTHOR_EMAIL";

        /// <summary>
        /// The environment variable that contains the commiter's name
        /// </summary>
        public const string GIT_COMMITTER_NAME_KEY = "GIT_COMMITTER_NAME";

        /// <summary>
        /// The environment variable that contains the commiter's email
        /// </summary>
        public const string GIT_COMMITTER_EMAIL_KEY = "GIT_COMMITTER_EMAIL";

        /// <summary>
        /// The environment variable that limits how close to the root of the file systems JGit will traverse when looking for a repository root.
        /// </summary>
        public const string GIT_CEILING_DIRECTORIES_KEY = "GIT_CEILING_DIRECTORIES";

        /// <summary>
        /// The environment variable that tells us which directory is the ".git" directory
        /// </summary>
        public const string GIT_DIR_KEY = "GIT_DIR";

        /// <summary>
        /// The environment variable that tells us which directory is the working directory.
        /// </summary>
        public const string GIT_WORK_TREE_KEY = "GIT_WORK_TREE";

        /// <summary>
        /// The environment variable that tells us which file holds the Git index.
        /// </summary>
        public const string GIT_INDEX_KEY = "GIT_INDEX";

        /// <summary>
        /// The environment variable that tells us where objects are stored
        /// </summary>
        public const string GIT_OBJECT_DIRECTORY_KEY = "GIT_OBJECT_DIRECTORY";

        /// <summary>
        /// The environment variable that tells us where to look for objects, besides the default objects directory.
        /// </summary>
        public const string GIT_ALTERNATE_OBJECT_DIRECTORIES_KEY = "GIT_ALTERNATE_OBJECT_DIRECTORIES";

        /// <summary>
        /// Default value for the user name if no other information is available
        /// </summary>
        public const string UNKNOWN_USER_DEFAULT = "unknown-user";

        /// <summary>
        /// Beginning of the common "Signed-off-by: " commit message line
        /// </summary>
        public const string SIGNED_OFF_BY_TAG = "Signed-off-by: ";

        /// <summary>
        /// Default remote name used by clone, push and fetch operations
        /// </summary>
        public const string DEFAULT_REMOTE_NAME = "origin";

        /// <summary>
        /// Default name for the Git repository directory
        /// </summary>
        public const string DOT_GIT = ".git";

        /// <summary>
        /// A bare repository typically ends with this string
        /// </summary>
        public const string DOT_GIT_EXT = ".git";

        /// <summary>
        /// A gitignore file name
        /// </summary>
        public const string GITIGNORE_FILENAME = ".gitignore";

        /// <summary>
        /// Create a new digest function for objects.
        /// </summary>
        /// <returns>A new digest object.</returns>
        public static MessageDigest newMessageDigest()
        {
            return MessageDigest.getInstance(HASH_FUNCTION);
        }

        /// <summary>
        /// Convert an OBJ_* type constant to a TYPE_* type constant.
        /// </summary>
        /// <param name="typeCode">
        /// typeCode the type code, from a pack representation.
        /// </param>
        /// <returns>The canonical string name of this type.</returns>
        public static string typeString(int typeCode)
        {
            switch (typeCode)
            {
                case OBJ_COMMIT:
                    return TYPE_COMMIT;
                case OBJ_TREE:
                    return TYPE_TREE;
                case OBJ_BLOB:
                    return TYPE_BLOB;
                case OBJ_TAG:
                    return TYPE_TAG;
                default:
                    throw new ArgumentException("Bad object type: " + typeCode);
            }
        }

        /// <summary>
        /// Convert an OBJ_* type constant to an ASCII encoded string constant.
        /// <para />
        /// The ASCII encoded string is often the canonical representation of
        /// the type within a loose object header, or within a tag header.
        /// </summary>
        /// <param name="typeCode">
        /// typeCode the type code, from a pack representation.
        /// </param>
        /// <returns>
        /// The canonical ASCII encoded name of this type.
        /// </returns>
        public static byte[] encodedTypeString(int typeCode)
        {
            switch (typeCode)
            {
                case OBJ_COMMIT:
                    return EncodedTypeCommit;
                case OBJ_TREE:
                    return EncodedTypeTree;
                case OBJ_BLOB:
                    return EncodedTypeBlob;
                case OBJ_TAG:
                    return EncodedTypeTag;
                default:
                    throw new ArgumentException("Bad object type: " + typeCode);
            }
        }

        /// <summary>
        /// Parse an encoded type string into a type constant.
        /// </summary>
        /// <param name="id">
        /// <see cref="ObjectId" /> this type string came from; may be null if 
        /// that is not known at the time the parse is occurring.
        /// </param>
        /// <param name="typeString">string version of the type code.</param>
        /// <param name="endMark">
        /// Character immediately following the type string. Usually ' '
        /// (space) or '\n' (line feed).
        /// </param>
        /// <param name="offset">
        /// Position within <paramref name="typeString"/> where the parse
        /// should start. Updated with the new position (just past
        /// <paramref name="endMark"/> when the parse is successful).
        /// </param>
        /// <returns>
        /// A type code constant (one of <see cref="OBJ_BLOB"/>,
        /// <see cref="OBJ_COMMIT"/>, <see cref="OBJ_TAG"/>, <see cref="OBJ_TREE"/>
        /// </returns>
        /// <exception cref="CorruptObjectException"></exception>
        public static int decodeTypeString(AnyObjectId id, byte[] typeString, byte endMark, MutableInteger offset)
        {
            try
            {
                int position = offset.value;
                switch (typeString[position])
                {
                    case (byte)'b':
                        if (typeString[position + 1] != (byte)'l'
                            || typeString[position + 2] != (byte)'o'
                            || typeString[position + 3] != (byte)'b'
                            || typeString[position + 4] != endMark)
                        {
                            throw new CorruptObjectException(id, "invalid type");
                        }
                        offset.value = position + 5;
                        return OBJ_BLOB;

                    case (byte)'c':
                        if (typeString[position + 1] != (byte)'o'
                                || typeString[position + 2] != (byte)'m'
                                || typeString[position + 3] != (byte)'m'
                                || typeString[position + 4] != (byte)'i'
                                || typeString[position + 5] != (byte)'t'
                                || typeString[position + 6] != endMark)
                        {
                            throw new CorruptObjectException(id, "invalid type");
                        }
                        offset.value = position + 7;
                        return OBJ_COMMIT;

                    case (byte)'t':
                        switch (typeString[position + 1])
                        {
                            case (byte)'a':
                                if (typeString[position + 2] != (byte)'g'
                                    || typeString[position + 3] != endMark)
                                    throw new CorruptObjectException(id, "invalid type");
                                offset.value = position + 4;
                                return OBJ_TAG;

                            case (byte)'r':
                                if (typeString[position + 2] != (byte)'e'
                                        || typeString[position + 3] != (byte)'e'
                                        || typeString[position + 4] != endMark)
                                    throw new CorruptObjectException(id, "invalid type");
                                offset.value = position + 5;
                                return OBJ_TREE;

                            default:
                                throw new CorruptObjectException(id, "invalid type");
                        }

                    default:
                        throw new CorruptObjectException(id, "invalid type");
                }
            }
            catch (IndexOutOfRangeException)
            {
                throw new CorruptObjectException(id, "invalid type");
            }
        }

        /// <summary>
        /// Convert an integer into its decimal representation.
        /// </summary>
        /// <param name="s">the integer to convert.</param>
        /// <returns>
        /// Decimal representation of the input integer. The returned array
        /// is the smallest array that will hold the value.
        /// </returns>
        public static byte[] encodeASCII(long s)
        {
            return encodeASCII(Convert.ToString(s));
        }

        /// <summary>
        /// Convert a string to US-ASCII encoding.       
        /// </summary>
        /// <param name="s">
        /// The string to convert. Must not contain any characters over
        /// 127 (outside of 7-bit ASCII).
        /// </param>
        /// <returns>
        /// A byte array of the same Length as the input string, holding the
        /// same characters, in the same order.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// The input string contains one or more characters outside of
        /// the 7-bit ASCII character space.
        /// </exception>
        public static byte[] encodeASCII(string s)
        {
            var r = new byte[s.Length];
            for (int k = r.Length - 1; k >= 0; k--)
            {
                char c = s[k];
                if (c > 127)
                {
                    throw new ArgumentException("Not ASCII string: " + s);
                }
                r[k] = (byte)c;
            }
            return r;
        }

        /// <summary>
        /// Convert a string to a byte array in UTF-8 character encoding.
        /// </summary>
        /// <param name="str">
        /// The string to convert. May contain any Unicode characters.
        /// </param>
        /// <returns>
        /// A byte array representing the requested string, encoded using the
        /// default character encoding (UTF-8).
        /// </returns>
        public static byte[] encode(string str)
        {
            return CHARSET.GetBytes(str);
        }
    }
}