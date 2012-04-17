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
using NGit;
using NGit.Errors;
using NGit.Internal;
using NGit.Util;
using Sharpen;

namespace NGit
{
	/// <summary>Verifies that an object is formatted correctly.</summary>
	/// <remarks>
	/// Verifies that an object is formatted correctly.
	/// <p>
	/// Verifications made by this class only check that the fields of an object are
	/// formatted correctly. The ObjectId checksum of the object is not verified, and
	/// connectivity links between objects are also not verified. Its assumed that
	/// the caller can provide both of these validations on its own.
	/// <p>
	/// Instances of this class are not thread safe, but they may be reused to
	/// perform multiple object validations.
	/// </remarks>
	public class ObjectChecker
	{
		/// <summary>Header "tree "</summary>
		public static readonly byte[] tree = Constants.EncodeASCII("tree ");

		/// <summary>Header "parent "</summary>
		public static readonly byte[] parent = Constants.EncodeASCII("parent ");

		/// <summary>Header "author "</summary>
		public static readonly byte[] author = Constants.EncodeASCII("author ");

		/// <summary>Header "committer "</summary>
		public static readonly byte[] committer = Constants.EncodeASCII("committer ");

		/// <summary>Header "encoding "</summary>
		public static readonly byte[] encoding = Constants.EncodeASCII("encoding ");

		/// <summary>Header "object "</summary>
		public static readonly byte[] @object = Constants.EncodeASCII("object ");

		/// <summary>Header "type "</summary>
		public static readonly byte[] type = Constants.EncodeASCII("type ");

		/// <summary>Header "tag "</summary>
		public static readonly byte[] tag = Constants.EncodeASCII("tag ");

		/// <summary>Header "tagger "</summary>
		public static readonly byte[] tagger = Constants.EncodeASCII("tagger ");

		private readonly MutableObjectId tempId = new MutableObjectId();

		private readonly MutableInteger ptrout = new MutableInteger();

		/// <summary>Check an object for parsing errors.</summary>
		/// <remarks>Check an object for parsing errors.</remarks>
		/// <param name="objType">
		/// type of the object. Must be a valid object type code in
		/// <see cref="Constants">Constants</see>
		/// .
		/// </param>
		/// <param name="raw">
		/// the raw data which comprises the object. This should be in the
		/// canonical format (that is the format used to generate the
		/// ObjectId of the object). The array is never modified.
		/// </param>
		/// <exception cref="NGit.Errors.CorruptObjectException">if an error is identified.</exception>
		public virtual void Check(int objType, byte[] raw)
		{
			switch (objType)
			{
				case Constants.OBJ_COMMIT:
				{
					CheckCommit(raw);
					break;
				}

				case Constants.OBJ_TAG:
				{
					CheckTag(raw);
					break;
				}

				case Constants.OBJ_TREE:
				{
					CheckTree(raw);
					break;
				}

				case Constants.OBJ_BLOB:
				{
					CheckBlob(raw);
					break;
				}

				default:
				{
					throw new CorruptObjectException(MessageFormat.Format(JGitText.Get().corruptObjectInvalidType2
						, objType));
				}
			}
		}

		private int Id(byte[] raw, int ptr)
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

		private int PersonIdent(byte[] raw, int ptr)
		{
			int emailB = RawParseUtils.NextLF(raw, ptr, '<');
			if (emailB == ptr || raw[emailB - 1] != '<')
			{
				return -1;
			}
			int emailE = RawParseUtils.NextLF(raw, emailB, '>');
			if (emailE == emailB || raw[emailE - 1] != '>')
			{
				return -1;
			}
			if (emailE == raw.Length || raw[emailE] != ' ')
			{
				return -1;
			}
			RawParseUtils.ParseBase10(raw, emailE + 1, ptrout);
			// when
			ptr = ptrout.value;
			if (emailE + 1 == ptr)
			{
				return -1;
			}
			if (ptr == raw.Length || raw[ptr] != ' ')
			{
				return -1;
			}
			RawParseUtils.ParseBase10(raw, ptr + 1, ptrout);
			// tz offset
			if (ptr + 1 == ptrout.value)
			{
				return -1;
			}
			return ptrout.value;
		}

		/// <summary>Check a commit for errors.</summary>
		/// <remarks>Check a commit for errors.</remarks>
		/// <param name="raw">the commit data. The array is never modified.</param>
		/// <exception cref="NGit.Errors.CorruptObjectException">if any error was detected.</exception>
		public virtual void CheckCommit(byte[] raw)
		{
			int ptr = 0;
			if ((ptr = RawParseUtils.Match(raw, ptr, tree)) < 0)
			{
				throw new CorruptObjectException("no tree header");
			}
			if ((ptr = Id(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid tree");
			}
			while (RawParseUtils.Match(raw, ptr, parent) >= 0)
			{
				ptr += parent.Length;
				if ((ptr = Id(raw, ptr)) < 0 || raw[ptr++] != '\n')
				{
					throw new CorruptObjectException("invalid parent");
				}
			}
			if ((ptr = RawParseUtils.Match(raw, ptr, author)) < 0)
			{
				throw new CorruptObjectException("no author");
			}
			if ((ptr = PersonIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid author");
			}
			if ((ptr = RawParseUtils.Match(raw, ptr, committer)) < 0)
			{
				throw new CorruptObjectException("no committer");
			}
			if ((ptr = PersonIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid committer");
			}
		}

		/// <summary>Check an annotated tag for errors.</summary>
		/// <remarks>Check an annotated tag for errors.</remarks>
		/// <param name="raw">the tag data. The array is never modified.</param>
		/// <exception cref="NGit.Errors.CorruptObjectException">if any error was detected.</exception>
		public virtual void CheckTag(byte[] raw)
		{
			int ptr = 0;
			if ((ptr = RawParseUtils.Match(raw, ptr, @object)) < 0)
			{
				throw new CorruptObjectException("no object header");
			}
			if ((ptr = Id(raw, ptr)) < 0 || raw[ptr++] != '\n')
			{
				throw new CorruptObjectException("invalid object");
			}
			if ((ptr = RawParseUtils.Match(raw, ptr, type)) < 0)
			{
				throw new CorruptObjectException("no type header");
			}
			ptr = RawParseUtils.NextLF(raw, ptr);
			if ((ptr = RawParseUtils.Match(raw, ptr, tag)) < 0)
			{
				throw new CorruptObjectException("no tag header");
			}
			ptr = RawParseUtils.NextLF(raw, ptr);
			if ((ptr = RawParseUtils.Match(raw, ptr, tagger)) > 0)
			{
				if ((ptr = PersonIdent(raw, ptr)) < 0 || raw[ptr++] != '\n')
				{
					throw new CorruptObjectException("invalid tagger");
				}
			}
		}

		private static int LastPathChar(int mode)
		{
			return FileMode.TREE.Equals(mode) ? '/' : '\0';
		}

		private static int PathCompare(byte[] raw, int aPos, int aEnd, int aMode, int bPos
			, int bEnd, int bMode)
		{
			while (aPos < aEnd && bPos < bEnd)
			{
				int cmp = (raw[aPos++] & unchecked((int)(0xff))) - (raw[bPos++] & unchecked((int)
					(0xff)));
				if (cmp != 0)
				{
					return cmp;
				}
			}
			if (aPos < aEnd)
			{
				return (raw[aPos] & unchecked((int)(0xff))) - LastPathChar(bMode);
			}
			if (bPos < bEnd)
			{
				return LastPathChar(aMode) - (raw[bPos] & unchecked((int)(0xff)));
			}
			return 0;
		}

		private static bool DuplicateName(byte[] raw, int thisNamePos, int thisNameEnd)
		{
			int sz = raw.Length;
			int nextPtr = thisNameEnd + 1 + Constants.OBJECT_ID_LENGTH;
			for (; ; )
			{
				int nextMode = 0;
				for (; ; )
				{
					if (nextPtr >= sz)
					{
						return false;
					}
					byte c = raw[nextPtr++];
					if (' ' == c)
					{
						break;
					}
					nextMode <<= 3;
					nextMode += c - '0';
				}
				int nextNamePos = nextPtr;
				for (; ; )
				{
					if (nextPtr == sz)
					{
						return false;
					}
					byte c = raw[nextPtr++];
					if (c == 0)
					{
						break;
					}
				}
				if (nextNamePos + 1 == nextPtr)
				{
					return false;
				}
				int cmp = PathCompare(raw, thisNamePos, thisNameEnd, FileMode.TREE.GetBits(), nextNamePos
					, nextPtr - 1, nextMode);
				if (cmp < 0)
				{
					return false;
				}
				else
				{
					if (cmp == 0)
					{
						return true;
					}
				}
				nextPtr += Constants.OBJECT_ID_LENGTH;
			}
		}

		/// <summary>Check a canonical formatted tree for errors.</summary>
		/// <remarks>Check a canonical formatted tree for errors.</remarks>
		/// <param name="raw">the raw tree data. The array is never modified.</param>
		/// <exception cref="NGit.Errors.CorruptObjectException">if any error was detected.</exception>
		public virtual void CheckTree(byte[] raw)
		{
			int sz = raw.Length;
			int ptr = 0;
			int lastNameB = 0;
			int lastNameE = 0;
			int lastMode = 0;
			while (ptr < sz)
			{
				int thisMode = 0;
				for (; ; )
				{
					if (ptr == sz)
					{
						throw new CorruptObjectException("truncated in mode");
					}
					byte c = raw[ptr++];
					if (' ' == c)
					{
						break;
					}
					if (((sbyte)c) < '0' || c > '7')
					{
						throw new CorruptObjectException("invalid mode character");
					}
					if (thisMode == 0 && c == '0')
					{
						throw new CorruptObjectException("mode starts with '0'");
					}
					thisMode <<= 3;
					thisMode += c - '0';
				}
				if (FileMode.FromBits(thisMode).GetObjectType() == Constants.OBJ_BAD)
				{
					throw new CorruptObjectException("invalid mode " + thisMode);
				}
				int thisNameB = ptr;
				for (; ; )
				{
					if (ptr == sz)
					{
						throw new CorruptObjectException("truncated in name");
					}
					byte c = raw[ptr++];
					if (c == 0)
					{
						break;
					}
					if (c == '/')
					{
						throw new CorruptObjectException("name contains '/'");
					}
				}
				if (thisNameB + 1 == ptr)
				{
					throw new CorruptObjectException("zero length name");
				}
				if (raw[thisNameB] == '.')
				{
					int nameLen = (ptr - 1) - thisNameB;
					if (nameLen == 1)
					{
						throw new CorruptObjectException("invalid name '.'");
					}
					if (nameLen == 2 && raw[thisNameB + 1] == '.')
					{
						throw new CorruptObjectException("invalid name '..'");
					}
				}
				if (DuplicateName(raw, thisNameB, ptr - 1))
				{
					throw new CorruptObjectException("duplicate entry names");
				}
				if (lastNameB != 0)
				{
					int cmp = PathCompare(raw, lastNameB, lastNameE, lastMode, thisNameB, ptr - 1, thisMode
						);
					if (cmp > 0)
					{
						throw new CorruptObjectException("incorrectly sorted");
					}
				}
				lastNameB = thisNameB;
				lastNameE = ptr - 1;
				lastMode = thisMode;
				ptr += Constants.OBJECT_ID_LENGTH;
				if (ptr > sz)
				{
					throw new CorruptObjectException("truncated in object id");
				}
			}
		}

		/// <summary>Check a blob for errors.</summary>
		/// <remarks>Check a blob for errors.</remarks>
		/// <param name="raw">the blob data. The array is never modified.</param>
		/// <exception cref="NGit.Errors.CorruptObjectException">if any error was detected.</exception>
		public virtual void CheckBlob(byte[] raw)
		{
		}
		// We can always assume the blob is valid.
	}
}
