/*
This code is derived from jgit (http://eclipse.org/jgit).
Copyright owners are documented in jgit's IP log.

This program and the accompanying materials are made available
under the terms of the Eclipse Distribution License v1.0 which
accompanies this distribution, is reproduced below, and is
available at http://www.eclipse.org/org/documents/edl-v10.php

All rights reserved.

Redistribution and use in source and binary forms, with or
without modification, are permitted provided that the following
conditions are met:

- Redistributions of source code must retain the above copyright
  notice, this list of conditions and the following disclaimer.

- Redistributions in binary form must reproduce the above
  copyright notice, this list of conditions and the following
  disclaimer in the documentation and/or other materials provided
  with the distribution.

- Neither the name of the Eclipse Foundation, Inc. nor the
  names of its contributors may be used to endorse or promote
  products derived from this software without specific prior
  written permission.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND
CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES,
INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL,
SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER
CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF
ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/

using System;
using System.Text;
using NGit;
using NGit.Errors;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>Misc.</summary>
	/// <remarks>Misc. constants used throughout JGit.</remarks>
	public sealed class Constants
	{
		/// <summary>Hash function used natively by Git for all objects.</summary>
		/// <remarks>Hash function used natively by Git for all objects.</remarks>
		private static readonly string HASH_FUNCTION = "SHA-1";

		/// <summary>A Git object hash is 160 bits, i.e.</summary>
		/// <remarks>
		/// A Git object hash is 160 bits, i.e. 20 bytes.
		/// <p>
		/// Changing this assumption is not going to be as easy as changing this
		/// declaration.
		/// </remarks>
		public const int OBJECT_ID_LENGTH = 20;

		/// <summary>
		/// A Git object can be expressed as a 40 character string of hexadecimal
		/// digits.
		/// </summary>
		/// <remarks>
		/// A Git object can be expressed as a 40 character string of hexadecimal
		/// digits.
		/// </remarks>
		/// <seealso cref="OBJECT_ID_LENGTH">OBJECT_ID_LENGTH</seealso>
		public const int OBJECT_ID_STRING_LENGTH = OBJECT_ID_LENGTH * 2;

		/// <summary>Special name for the "HEAD" symbolic-ref.</summary>
		/// <remarks>Special name for the "HEAD" symbolic-ref.</remarks>
		public static readonly string HEAD = "HEAD";

		/// <summary>Special name for the "FETCH_HEAD" symbolic-ref.</summary>
		/// <remarks>Special name for the "FETCH_HEAD" symbolic-ref.</remarks>
		public static readonly string FETCH_HEAD = "FETCH_HEAD";

		/// <summary>Text string that identifies an object as a commit.</summary>
		/// <remarks>
		/// Text string that identifies an object as a commit.
		/// <p>
		/// Commits connect trees into a string of project histories, where each
		/// commit is an assertion that the best way to continue is to use this other
		/// tree (set of files).
		/// </remarks>
		public static readonly string TYPE_COMMIT = "commit";

		/// <summary>Text string that identifies an object as a blob.</summary>
		/// <remarks>
		/// Text string that identifies an object as a blob.
		/// <p>
		/// Blobs store whole file revisions. They are used for any user file, as
		/// well as for symlinks. Blobs form the bulk of any project's storage space.
		/// </remarks>
		public static readonly string TYPE_BLOB = "blob";

		/// <summary>Text string that identifies an object as a tree.</summary>
		/// <remarks>
		/// Text string that identifies an object as a tree.
		/// <p>
		/// Trees attach object ids (hashes) to names and file modes. The normal use
		/// for a tree is to store a version of a directory and its contents.
		/// </remarks>
		public static readonly string TYPE_TREE = "tree";

		/// <summary>Text string that identifies an object as an annotated tag.</summary>
		/// <remarks>
		/// Text string that identifies an object as an annotated tag.
		/// <p>
		/// Annotated tags store a pointer to any other object, and an additional
		/// message. It is most commonly used to record a stable release of the
		/// project.
		/// </remarks>
		public static readonly string TYPE_TAG = "tag";

		private static readonly byte[] ENCODED_TYPE_COMMIT = EncodeASCII(TYPE_COMMIT);

		private static readonly byte[] ENCODED_TYPE_BLOB = EncodeASCII(TYPE_BLOB);

		private static readonly byte[] ENCODED_TYPE_TREE = EncodeASCII(TYPE_TREE);

		private static readonly byte[] ENCODED_TYPE_TAG = EncodeASCII(TYPE_TAG);

		/// <summary>An unknown or invalid object type code.</summary>
		/// <remarks>An unknown or invalid object type code.</remarks>
		public const int OBJ_BAD = -1;

		/// <summary>In-pack object type: extended types.</summary>
		/// <remarks>
		/// In-pack object type: extended types.
		/// <p>
		/// This header code is reserved for future expansion. It is currently
		/// undefined/unsupported.
		/// </remarks>
		public const int OBJ_EXT = 0;

		/// <summary>In-pack object type: commit.</summary>
		/// <remarks>
		/// In-pack object type: commit.
		/// <p>
		/// Indicates the associated object is a commit.
		/// <p>
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </remarks>
		/// <seealso cref="TYPE_COMMIT">TYPE_COMMIT</seealso>
		public const int OBJ_COMMIT = 1;

		/// <summary>In-pack object type: tree.</summary>
		/// <remarks>
		/// In-pack object type: tree.
		/// <p>
		/// Indicates the associated object is a tree.
		/// <p>
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </remarks>
		/// <seealso cref="TYPE_BLOB">TYPE_BLOB</seealso>
		public const int OBJ_TREE = 2;

		/// <summary>In-pack object type: blob.</summary>
		/// <remarks>
		/// In-pack object type: blob.
		/// <p>
		/// Indicates the associated object is a blob.
		/// <p>
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </remarks>
		/// <seealso cref="TYPE_BLOB">TYPE_BLOB</seealso>
		public const int OBJ_BLOB = 3;

		/// <summary>In-pack object type: annotated tag.</summary>
		/// <remarks>
		/// In-pack object type: annotated tag.
		/// <p>
		/// Indicates the associated object is an annotated tag.
		/// <p>
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </remarks>
		/// <seealso cref="TYPE_TAG">TYPE_TAG</seealso>
		public const int OBJ_TAG = 4;

		/// <summary>In-pack object type: reserved for future use.</summary>
		/// <remarks>In-pack object type: reserved for future use.</remarks>
		public const int OBJ_TYPE_5 = 5;

		/// <summary>
		/// In-pack object type: offset delta
		/// <p>
		/// Objects stored with this type actually have a different type which must
		/// be obtained from their delta base object.
		/// </summary>
		/// <remarks>
		/// In-pack object type: offset delta
		/// <p>
		/// Objects stored with this type actually have a different type which must
		/// be obtained from their delta base object. Delta objects store only the
		/// changes needed to apply to the base object in order to recover the
		/// original object.
		/// <p>
		/// An offset delta uses a negative offset from the start of this object to
		/// refer to its delta base. The base object must exist in this packfile
		/// (even in the case of a thin pack).
		/// <p>
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </remarks>
		public const int OBJ_OFS_DELTA = 6;

		/// <summary>
		/// In-pack object type: reference delta
		/// <p>
		/// Objects stored with this type actually have a different type which must
		/// be obtained from their delta base object.
		/// </summary>
		/// <remarks>
		/// In-pack object type: reference delta
		/// <p>
		/// Objects stored with this type actually have a different type which must
		/// be obtained from their delta base object. Delta objects store only the
		/// changes needed to apply to the base object in order to recover the
		/// original object.
		/// <p>
		/// A reference delta uses a full object id (hash) to reference the delta
		/// base. The base object is allowed to be omitted from the packfile, but
		/// only in the case of a thin pack being transferred over the network.
		/// <p>
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </remarks>
		public const int OBJ_REF_DELTA = 7;

		/// <summary>
		/// Pack file signature that occurs at file header - identifies file as Git
		/// packfile formatted.
		/// </summary>
		/// <remarks>
		/// Pack file signature that occurs at file header - identifies file as Git
		/// packfile formatted.
		/// <p>
		/// <b>This constant is fixed and is defined by the Git packfile format.</b>
		/// </remarks>
		public static readonly byte[] PACK_SIGNATURE = new byte[] { (byte)('P'), (byte)('A'
			), (byte)('C'), (byte)('K') };

		/// <summary>Native character encoding for commit messages, file names...</summary>
		/// <remarks>Native character encoding for commit messages, file names...</remarks>
		public static readonly string CHARACTER_ENCODING = "UTF-8";

		/// <summary>Native character encoding for commit messages, file names...</summary>
		/// <remarks>Native character encoding for commit messages, file names...</remarks>
		public static readonly Encoding CHARSET;

		/// <summary>Default main branch name</summary>
		public static readonly string MASTER = "master";

		/// <summary>Prefix for branch refs</summary>
		public static readonly string R_HEADS = "refs/heads/";

		/// <summary>Prefix for remotes refs</summary>
		public static readonly string R_REMOTES = "refs/remotes/";

		/// <summary>Prefix for tag refs</summary>
		public static readonly string R_TAGS = "refs/tags/";

		/// <summary>Prefix for any ref</summary>
		public static readonly string R_REFS = "refs/";

		/// <summary>Logs folder name</summary>
		public static readonly string LOGS = "logs";

		/// <summary>Info refs folder</summary>
		public static readonly string INFO_REFS = "info/refs";

		/// <summary>Packed refs file</summary>
		public static readonly string PACKED_REFS = "packed-refs";

		/// <summary>The environment variable that contains the system user name</summary>
		public static readonly string OS_USER_NAME_KEY = "user.name";

		/// <summary>The environment variable that contains the author's name</summary>
		public static readonly string GIT_AUTHOR_NAME_KEY = "GIT_AUTHOR_NAME";

		/// <summary>The environment variable that contains the author's email</summary>
		public static readonly string GIT_AUTHOR_EMAIL_KEY = "GIT_AUTHOR_EMAIL";

		/// <summary>The environment variable that contains the commiter's name</summary>
		public static readonly string GIT_COMMITTER_NAME_KEY = "GIT_COMMITTER_NAME";

		/// <summary>The environment variable that contains the commiter's email</summary>
		public static readonly string GIT_COMMITTER_EMAIL_KEY = "GIT_COMMITTER_EMAIL";

		/// <summary>
		/// The environment variable that limits how close to the root of the file
		/// systems JGit will traverse when looking for a repository root.
		/// </summary>
		/// <remarks>
		/// The environment variable that limits how close to the root of the file
		/// systems JGit will traverse when looking for a repository root.
		/// </remarks>
		public static readonly string GIT_CEILING_DIRECTORIES_KEY = "GIT_CEILING_DIRECTORIES";

		/// <summary>
		/// The environment variable that tells us which directory is the ".git"
		/// directory
		/// </summary>
		public static readonly string GIT_DIR_KEY = "GIT_DIR";

		/// <summary>
		/// The environment variable that tells us which directory is the working
		/// directory.
		/// </summary>
		/// <remarks>
		/// The environment variable that tells us which directory is the working
		/// directory.
		/// </remarks>
		public static readonly string GIT_WORK_TREE_KEY = "GIT_WORK_TREE";

		/// <summary>The environment variable that tells us which file holds the Git index.</summary>
		/// <remarks>The environment variable that tells us which file holds the Git index.</remarks>
		public static readonly string GIT_INDEX_FILE_KEY = "GIT_INDEX_FILE";

		/// <summary>The environment variable that tells us where objects are stored</summary>
		public static readonly string GIT_OBJECT_DIRECTORY_KEY = "GIT_OBJECT_DIRECTORY";

		/// <summary>
		/// The environment variable that tells us where to look for objects, besides
		/// the default objects directory.
		/// </summary>
		/// <remarks>
		/// The environment variable that tells us where to look for objects, besides
		/// the default objects directory.
		/// </remarks>
		public static readonly string GIT_ALTERNATE_OBJECT_DIRECTORIES_KEY = "GIT_ALTERNATE_OBJECT_DIRECTORIES";

		/// <summary>Default value for the user name if no other information is available</summary>
		public static readonly string UNKNOWN_USER_DEFAULT = "unknown-user";

		/// <summary>Beginning of the common "Signed-off-by: " commit message line</summary>
		public static readonly string SIGNED_OFF_BY_TAG = "Signed-off-by: ";

		/// <summary>A gitignore file name</summary>
		public static readonly string GITIGNORE_FILENAME = ".gitignore";

		/// <summary>Default remote name used by clone, push and fetch operations</summary>
		public static readonly string DEFAULT_REMOTE_NAME = "origin";

		/// <summary>Default name for the Git repository directory</summary>
		public static readonly string DOT_GIT = ".git";

		/// <summary>A bare repository typically ends with this string</summary>
		public static readonly string DOT_GIT_EXT = ".git";

		/// <summary>Name of the ignore file</summary>
		public static readonly string DOT_GIT_IGNORE = ".gitignore";

		/// <summary>Create a new digest function for objects.</summary>
		/// <remarks>Create a new digest function for objects.</remarks>
		/// <returns>a new digest object.</returns>
		/// <exception cref="Sharpen.RuntimeException">
		/// this Java virtual machine does not support the required hash
		/// function. Very unlikely given that JGit uses a hash function
		/// that is in the Java reference specification.
		/// </exception>
		public static MessageDigest NewMessageDigest()
		{
			try
			{
				return MessageDigest.GetInstance(HASH_FUNCTION);
			}
			catch (NoSuchAlgorithmException nsae)
			{
				throw new RuntimeException(MessageFormat.Format(JGitText.Get().requiredHashFunctionNotAvailable
					, HASH_FUNCTION), nsae);
			}
		}

		/// <summary>Convert an OBJ_* type constant to a TYPE_* type constant.</summary>
		/// <remarks>Convert an OBJ_* type constant to a TYPE_* type constant.</remarks>
		/// <param name="typeCode">the type code, from a pack representation.</param>
		/// <returns>the canonical string name of this type.</returns>
		public static string TypeString(int typeCode)
		{
			switch (typeCode)
			{
				case OBJ_COMMIT:
				{
					return TYPE_COMMIT;
				}

				case OBJ_TREE:
				{
					return TYPE_TREE;
				}

				case OBJ_BLOB:
				{
					return TYPE_BLOB;
				}

				case OBJ_TAG:
				{
					return TYPE_TAG;
				}

				default:
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().badObjectType, typeCode
						));
				}
			}
		}

		/// <summary>Convert an OBJ_* type constant to an ASCII encoded string constant.</summary>
		/// <remarks>
		/// Convert an OBJ_* type constant to an ASCII encoded string constant.
		/// <p>
		/// The ASCII encoded string is often the canonical representation of
		/// the type within a loose object header, or within a tag header.
		/// </remarks>
		/// <param name="typeCode">the type code, from a pack representation.</param>
		/// <returns>the canonical ASCII encoded name of this type.</returns>
		public static byte[] EncodedTypeString(int typeCode)
		{
			switch (typeCode)
			{
				case OBJ_COMMIT:
				{
					return ENCODED_TYPE_COMMIT;
				}

				case OBJ_TREE:
				{
					return ENCODED_TYPE_TREE;
				}

				case OBJ_BLOB:
				{
					return ENCODED_TYPE_BLOB;
				}

				case OBJ_TAG:
				{
					return ENCODED_TYPE_TAG;
				}

				default:
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().badObjectType, typeCode
						));
				}
			}
		}

		/// <summary>Parse an encoded type string into a type constant.</summary>
		/// <remarks>Parse an encoded type string into a type constant.</remarks>
		/// <param name="id">
		/// object id this type string came from; may be null if that is
		/// not known at the time the parse is occurring.
		/// </param>
		/// <param name="typeString">string version of the type code.</param>
		/// <param name="endMark">
		/// character immediately following the type string. Usually ' '
		/// (space) or '\n' (line feed).
		/// </param>
		/// <param name="offset">
		/// position within <code>typeString</code> where the parse
		/// should start. Updated with the new position (just past
		/// <code>endMark</code> when the parse is successful.
		/// </param>
		/// <returns>
		/// a type code constant (one of
		/// <see cref="OBJ_BLOB">OBJ_BLOB</see>
		/// ,
		/// <see cref="OBJ_COMMIT">OBJ_COMMIT</see>
		/// ,
		/// <see cref="OBJ_TAG">OBJ_TAG</see>
		/// ,
		/// <see cref="OBJ_TREE">OBJ_TREE</see>
		/// .
		/// </returns>
		/// <exception cref="NGit.Errors.CorruptObjectException">there is no valid type identified by <code>typeString</code>.
		/// 	</exception>
		public static int DecodeTypeString(AnyObjectId id, byte[] typeString, byte endMark
			, MutableInteger offset)
		{
			try
			{
				int position = offset.value;
				switch (typeString[position])
				{
					case (byte)('b'):
					{
						if (typeString[position + 1] != 'l' || typeString[position + 2] != 'o' || typeString
							[position + 3] != 'b' || typeString[position + 4] != endMark)
						{
							throw new CorruptObjectException(id, JGitText.Get().corruptObjectInvalidType);
						}
						offset.value = position + 5;
						return NGit.Constants.OBJ_BLOB;
					}

					case (byte)('c'):
					{
						if (typeString[position + 1] != 'o' || typeString[position + 2] != 'm' || typeString
							[position + 3] != 'm' || typeString[position + 4] != 'i' || typeString[position 
							+ 5] != 't' || typeString[position + 6] != endMark)
						{
							throw new CorruptObjectException(id, JGitText.Get().corruptObjectInvalidType);
						}
						offset.value = position + 7;
						return NGit.Constants.OBJ_COMMIT;
					}

					case (byte)('t'):
					{
						switch (typeString[position + 1])
						{
							case (byte)('a'):
							{
								if (typeString[position + 2] != 'g' || typeString[position + 3] != endMark)
								{
									throw new CorruptObjectException(id, JGitText.Get().corruptObjectInvalidType);
								}
								offset.value = position + 4;
								return NGit.Constants.OBJ_TAG;
							}

							case (byte)('r'):
							{
								if (typeString[position + 2] != 'e' || typeString[position + 3] != 'e' || typeString
									[position + 4] != endMark)
								{
									throw new CorruptObjectException(id, JGitText.Get().corruptObjectInvalidType);
								}
								offset.value = position + 5;
								return NGit.Constants.OBJ_TREE;
							}

							default:
							{
								throw new CorruptObjectException(id, JGitText.Get().corruptObjectInvalidType);
							}
						}
						goto default;
					}

					default:
					{
						throw new CorruptObjectException(id, JGitText.Get().corruptObjectInvalidType);
					}
				}
			}
			catch (IndexOutOfRangeException)
			{
				throw new CorruptObjectException(id, JGitText.Get().corruptObjectInvalidType);
			}
		}

		/// <summary>Convert an integer into its decimal representation.</summary>
		/// <remarks>Convert an integer into its decimal representation.</remarks>
		/// <param name="s">the integer to convert.</param>
		/// <returns>
		/// a decimal representation of the input integer. The returned array
		/// is the smallest array that will hold the value.
		/// </returns>
		public static byte[] EncodeASCII(long s)
		{
			return EncodeASCII(System.Convert.ToString(s));
		}

		/// <summary>Convert a string to US-ASCII encoding.</summary>
		/// <remarks>Convert a string to US-ASCII encoding.</remarks>
		/// <param name="s">
		/// the string to convert. Must not contain any characters over
		/// 127 (outside of 7-bit ASCII).
		/// </param>
		/// <returns>
		/// a byte array of the same length as the input string, holding the
		/// same characters, in the same order.
		/// </returns>
		/// <exception cref="System.ArgumentException">
		/// the input string contains one or more characters outside of
		/// the 7-bit ASCII character space.
		/// </exception>
		public static byte[] EncodeASCII(string s)
		{
			byte[] r = new byte[s.Length];
			for (int k = r.Length - 1; k >= 0; k--)
			{
				char c = s[k];
				if (c > 127)
				{
					throw new ArgumentException(MessageFormat.Format(JGitText.Get().notASCIIString, s
						));
				}
				r[k] = unchecked((byte)c);
			}
			return r;
		}

		/// <summary>Convert a string to a byte array in the standard character encoding.</summary>
		/// <remarks>Convert a string to a byte array in the standard character encoding.</remarks>
		/// <param name="str">the string to convert. May contain any Unicode characters.</param>
		/// <returns>
		/// a byte array representing the requested string, encoded using the
		/// default character encoding (UTF-8).
		/// </returns>
		/// <seealso cref="CHARACTER_ENCODING">CHARACTER_ENCODING</seealso>
		public static byte[] Encode(string str)
		{
			ByteBuffer bb = NGit.Constants.CHARSET.Encode(str);
			int len = bb.Limit();
			if (bb.HasArray() && bb.ArrayOffset() == 0)
			{
				byte[] arr = ((byte[])bb.Array());
				if (arr.Length == len)
				{
					return arr;
				}
			}
			byte[] arr_1 = new byte[len];
			bb.Get(arr_1);
			return arr_1;
		}

		static Constants()
		{
			if (OBJECT_ID_LENGTH != NewMessageDigest().GetDigestLength())
			{
				throw new LinkageError(JGitText.Get().incorrectOBJECT_ID_LENGTH);
			}
			CHARSET = Sharpen.Extensions.GetEncoding(CHARACTER_ENCODING);
		}

		/// <summary>name of the file containing the commit msg for a merge commit</summary>
		public static readonly string MERGE_MSG = "MERGE_MSG";

		/// <summary>name of the file containing the IDs of the parents of a merge commit</summary>
		public static readonly string MERGE_HEAD = "MERGE_HEAD";

		/// <summary>
		/// name of the ref ORIG_HEAD used by certain commands to store the original
		/// value of HEAD
		/// </summary>
		public static readonly string ORIG_HEAD = "ORIG_HEAD";

		/// <summary>objectid for the empty blob</summary>
		public static readonly ObjectId EMPTY_BLOB_ID = ObjectId.FromString("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391"
			);

		public Constants()
		{
		}
		// Hide the default constructor
	}
}
