/*
 * Copyright (C) 2008, Google Inc.
 * Copyright (C) 2009, Gil Ran <gilrun@gmail.com>
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
using System.Collections.Generic;
using System.IO;
using System.Text;
using GitSharp.Core.Diff;
using GitSharp.Core.Util;

namespace GitSharp.Core.Patch
{
	/// <summary>
	/// Patch header describing an action for a single file path.
	/// </summary>
	[Serializable]
	public class FileHeader
	{
		// Magical file name used for file adds or deletes.
		public const string DEV_NULL = "/dev/null";

		private static readonly byte[] OldModeString = Constants.encodeASCII("old mode ");

		private static readonly byte[] NewModeString = Constants.encodeASCII("new mode ");

		protected static readonly byte[] DeletedFileMode = Constants.encodeASCII("deleted file mode ");

		protected static readonly byte[] NewFileMode = Constants.encodeASCII("new file mode ");

		private static readonly byte[] CopyFrom = Constants.encodeASCII("copy from ");

		private static readonly byte[] CopyTo = Constants.encodeASCII("copy to ");

		private static readonly byte[] RenameOld = Constants.encodeASCII("rename old ");

		private static readonly byte[] RenameNew = Constants.encodeASCII("rename new ");

		private static readonly byte[] RenameFrom = Constants.encodeASCII("rename from ");

		private static readonly byte[] RenameTo = Constants.encodeASCII("rename to ");

		private static readonly byte[] SimilarityIndex = Constants.encodeASCII("similarity index ");

		private static readonly byte[] DissimilarityIndex = Constants.encodeASCII("dissimilarity index ");

		protected static readonly byte[] Index = Constants.encodeASCII("index ");

		public static readonly byte[] OLD_NAME = Constants.encodeASCII("--- ");

		public static readonly byte[] NEW_NAME = Constants.encodeASCII("+++ ");

		/// <summary>
		/// General type of change a single file-level patch describes.
		/// </summary>
		[Serializable]
		public enum ChangeTypeEnum
		{
			/// <summary>
			/// Add a new file to the project
			/// </summary>
			ADD,

			/// <summary>
			/// Modify an existing file in the project (content and/or mode)
			/// </summary>
			MODIFY,

			/// <summary>
			/// Delete an existing file from the project
			/// </summary>
			DELETE,

			/// <summary>
			/// Rename an existing file to a new location
			/// </summary>
			RENAME,

			/// <summary>
			/// Copy an existing file to a new location, keeping the original
			/// </summary>
			COPY
		}

		/// <summary>
		/// Type of patch used by this file.
		/// </summary>
		[Serializable]
		public enum PatchTypeEnum
		{
			/// <summary>
			/// A traditional unified diff style patch of a text file.
			/// </summary>
			UNIFIED,

			/// <summary>
			/// An empty patch with a message "Binary files ... differ"
			/// </summary>
			BINARY,

			/// <summary>
			/// A Git binary patch, holding pre and post image deltas
			/// </summary>
			GIT_BINARY
		}

		// File name of the old (pre-image).
		private string oldName;

		// File name of the new (post-image).
		private string newName;

		// Old mode of the file, if described by the patch, else null.
		private FileMode _oldMode;

		// New mode of the file, if described by the patch, else null.
		private FileMode _newMode;

		// Similarity score if ChangeType is a copy or rename.
		private int _score;

		// ObjectId listed on the index line for the old (pre-image)
		private AbbreviatedObjectId oldId;

		// The hunks of this file
		private List<HunkHeader> _hunks;

		public FileHeader(byte[] b, int offset)
		{
			Buffer = b;
			StartOffset = offset;
			ChangeType = ChangeTypeEnum.MODIFY; // unless otherwise designated
			PatchType = PatchTypeEnum.UNIFIED;
		}

		public virtual int ParentCount
		{
			get { return 1; }
		}

		protected AbbreviatedObjectId NewId { private get; set; }

		/// <summary>
		/// The byte array holding this file's patch script.
		/// </summary>
		/// <returns></returns>
		public byte[] Buffer { get; private set; }

		/// <summary>
		/// Offset the start of this file's script in <see cref="Buffer"/>
		/// </summary>
		public int StartOffset { get; private set; }

		/// <summary>
		/// Offset one past the end of the file script.
		/// </summary>
		/// <returns></returns>
		public int EndOffset { get; set; }

		public BinaryHunk ForwardBinaryHunk { get; set; }

		public BinaryHunk ReverseBinaryHunk { get; set; }

		public ChangeTypeEnum ChangeType { get; set; }

		public PatchTypeEnum PatchType { get; set; }

		/// <summary>
		/// Convert the patch script for this file into a string.
		/// <para />
		/// The default character encoding <see cref="Constants.CHARSET"/> is assumed for
		/// both the old and new files.
		/// </summary>
		/// <returns>
		/// The patch script, as a Unicode string.
		/// </returns>
		public string getScriptText()
		{
			return getScriptText(null, null);
		}

		/// <summary>
		///Convert the patch script for this file into a string.
		/// </summary>
		/// <param name="oldCharset">hint character set to decode the old lines with.</param>
		/// <param name="newCharset">hint character set to decode the new lines with.</param>
		/// <returns>the patch script, as a Unicode string.</returns>
		public virtual string getScriptText(Encoding oldCharset, Encoding newCharset)
		{
			return getScriptText(new[] { oldCharset, newCharset });
		}

		/// <summary>
		/// Convert the patch script for this file into a string.
		/// </summary>
		/// <param name="charsetGuess">
		/// optional array to suggest the character set to use when
		/// decoding each file's line. If supplied the array must have a
		/// length of <code><see cref="ParentCount"/> + 1</code>
		/// representing the old revision character sets and the new
		/// revision character set.
		/// </param>
		/// <returns>the patch script, as a Unicode string.</returns>
		public string getScriptText(Encoding[] charsetGuess)
		{
			if (Hunks.Count == 0)
			{
				// If we have no hunks then we can safely assume the entire
				// patch is a binary style patch, or a meta-data only style
				// patch. Either way the encoding of the headers should be
				// strictly 7-bit US-ASCII and the body is either 7-bit ASCII
				// (due to the base 85 encoding used for a BinaryHunk) or is
				// arbitrary noise we have chosen to ignore and not understand
				// (e.g. the message "Binary files ... differ").
				//
				return RawParseUtils.extractBinaryString(Buffer, StartOffset, EndOffset);
			}

			if (charsetGuess != null && charsetGuess.Length != ParentCount + 1)
				throw new ArgumentException("Expected "
						+ (ParentCount + 1) + " character encoding guesses");

			if (TrySimpleConversion(charsetGuess))
			{
				Encoding cs = (charsetGuess != null ? charsetGuess[0] : null) ?? Constants.CHARSET;

				try
				{
					return RawParseUtils.decodeNoFallback(cs, Buffer, StartOffset, EndOffset);
				}
				catch (EncoderFallbackException)
				{
					// Try the much slower, more-memory intensive version which
					// can handle a character set conversion patch.
				}
			}

			var r = new StringBuilder(EndOffset - StartOffset);

			// Always treat the headers as US-ASCII; Git file names are encoded
			// in a C style escape if any character has the high-bit set.
			//
			int hdrEnd = Hunks[0].StartOffset;
			for (int ptr = StartOffset; ptr < hdrEnd; )
			{
				int eol = Math.Min(hdrEnd, RawParseUtils.nextLF(Buffer, ptr));
				r.Append(RawParseUtils.extractBinaryString(Buffer, ptr, eol));
				ptr = eol;
			}

			string[] files = ExtractFileLines(charsetGuess);
			var offsets = new int[files.Length];
			foreach (HunkHeader h in Hunks)
			{
				h.extractFileLines(r, files, offsets);
			}

			return r.ToString();
		}

		private static bool TrySimpleConversion(Encoding[] charsetGuess)
		{
			if (charsetGuess == null) return true;

			for (int i = 1; i < charsetGuess.Length; i++)
			{
				if (charsetGuess[i] != charsetGuess[0]) return false;
			}
			return true;
		}

		private string[] ExtractFileLines(Encoding[] csGuess)
		{
			var tmp = new TemporaryBuffer[ParentCount + 1];
			try
			{
				for (int i = 0; i < tmp.Length; i++)
				{
					tmp[i] = new LocalFileBuffer();
				}

				foreach (HunkHeader h in Hunks)
				{
					h.extractFileLines(tmp);
				}

				var r = new String[tmp.Length];
				for (int i = 0; i < tmp.Length; i++)
				{
					Encoding cs = (csGuess != null ? csGuess[i] : null) ?? Constants.CHARSET;
					r[i] = RawParseUtils.decode(cs, tmp[i].ToArray());
				}

				return r;
			}
			catch (IOException ioe)
			{
				throw new Exception("Cannot convert script to text", ioe);
			}
			finally
			{
				foreach (TemporaryBuffer b in tmp)
				{
					if (b != null)
						b.destroy();
				}
			}
		}

		/// <summary>
		/// Get the old name associated with this file.
		/// <para />
		/// The meaning of the old name can differ depending on the semantic meaning
		/// of this patch:
		/// <ul>
		/// <li><i>file add</i>: always <code>/dev/null</code></li>
		/// <li><i>file modify</i>: always <see cref="NewName"/></li>
		/// <li><i>file delete</i>: always the file being deleted</li>
		/// <li><i>file copy</i>: source file the copy originates from</li>
		/// <li><i>file rename</i>: source file the rename originates from</li>
		/// </ul>
		/// </summary>
		/// <returns>Old name for this file.</returns>
		public string OldName
		{
			get { return oldName; }
		}

		/// <summary>
		/// Get the new name associated with this file.
		/// <para />
		/// The meaning of the new name can differ depending on the semantic meaning
		/// of this patch:
		/// <ul>
		/// <li><i>file add</i>: always the file being created</li>
		/// <li><i>file modify</i>: always <see cref="OldName"/></li>
		/// <li><i>file delete</i>: always <code>/dev/null</code></li>
		/// <li><i>file copy</i>: destination file the copy ends up at</li>
		/// <li><i>file rename</i>: destination file the rename ends up at</li>
		/// </ul>
		/// </summary>
		/// <returns></returns>
		public string NewName
		{
			get { return newName; }
		}

		/// <summary>
		/// The old file mode, if described in the patch
		/// </summary>
		public virtual FileMode GetOldMode()
		{
			return _oldMode;
		}

		/// <summary>
		/// The new file mode, if described in the patch
		/// </summary>
		public FileMode NewMode
		{
			get { return _newMode; }
			protected set { _newMode = value; }
		}

		/// <summary>
		/// The type of change this patch makes on <see cref="NewName"/>
		/// </summary>
		public ChangeTypeEnum getChangeType()
		{
			return ChangeType;
		}

		/// <summary>
		/// Returns similarity score between <see cref="OldName"/> and
		/// <see cref="NewName"/> if <see cref="getChangeType()"/> is
		/// <see cref="ChangeTypeEnum.COPY"/> or <see cref="ChangeTypeEnum.RENAME"/>.
		/// </summary>
		/// <returns></returns>
		public int getScore()
		{
			return _score;
		}

		/// <summary>
		/// Get the old object id from the <code>index</code>.
		/// </summary>
		/// <returns>
		/// The object id; null if there is no index line
		/// </returns>
		public virtual AbbreviatedObjectId getOldId()
		{
			return oldId;
		}

		/// <summary>
		/// Get the new object id from the <code>index</code>.
		/// </summary>
		/// <returns>
		/// The object id; null if there is no index line
		/// </returns>
		public AbbreviatedObjectId getNewId()
		{
			return NewId;
		}

		/// <summary>
		/// Style of patch used to modify this file
		/// </summary>
		/// <returns></returns>
		public PatchTypeEnum getPatchType()
		{
			return PatchType;
		}

		/// <summary>
		/// True if this patch modifies metadata about a file
		/// </summary>
		/// <returns></returns>
		public bool hasMetaDataChanges()
		{
			return ChangeType != ChangeTypeEnum.MODIFY || _newMode != _oldMode;
		}

		/// <summary>
		/// Gets the hunks altering this file; in order of appearance in patch
		/// </summary>
		/// <returns></returns>
		public List<HunkHeader> Hunks
		{
			get { return _hunks ?? (_hunks = new List<HunkHeader>()); }
		}

		public void addHunk(HunkHeader h)
		{
			if (h.File != this)
			{
				throw new ArgumentException("Hunk belongs to another file");
			}

			Hunks.Add(h);
		}

		public virtual HunkHeader newHunkHeader(int offset)
		{
			return new HunkHeader(this, offset);
		}

		/// <summary>
		/// If a <see cref="PatchTypeEnum.GIT_BINARY"/>, the new-image delta/literal
		/// </summary>
		/// <returns></returns>
		public BinaryHunk getForwardBinaryHunk()
		{
			return ForwardBinaryHunk;
		}

		/// <summary>
		/// If a <see cref="PatchTypeEnum.GIT_BINARY"/>, the old-image delta/literal
		/// </summary>
		/// <returns></returns>
		public BinaryHunk getReverseBinaryHunk()
		{
			return ReverseBinaryHunk;
		}

		/// <summary>
		/// Returns a list describing the content edits performed on this file.
		/// </summary>
		/// <returns></returns>
		public EditList ToEditList()
		{
			var r = new EditList();
			_hunks.ForEach(hunk => r.AddRange(hunk.ToEditList()));
			return r;
		}

		/// <summary>
		/// Parse a "diff --git" or "diff --cc" line.
		/// </summary>
		/// <param name="ptr">
		/// first character After the "diff --git " or "diff --cc " part.
		/// </param>
		/// <param name="end">
		/// one past the last position to parse.
		/// </param>
		/// <returns>
		/// first character After the LF at the end of the line; -1 on error.
		/// </returns>
		public int parseGitFileName(int ptr, int end)
		{
			int eol = RawParseUtils.nextLF(Buffer, ptr);
			int bol = ptr;
			if (eol >= end)
			{
				return -1;
			}

			// buffer[ptr..eol] looks like "a/foo b/foo\n". After the first
			// A regex to match this is "^[^/]+/(.*?) [^/+]+/\1\n$". There
			// is only one way to split the line such that text to the left
			// of the space matches the text to the right, excluding the part
			// before the first slash.
			//
			int aStart = RawParseUtils.nextLF(Buffer, ptr, (byte)'/');
			if (aStart >= eol)
				return eol;

			while (ptr < eol)
			{
				int sp = RawParseUtils.nextLF(Buffer, ptr, (byte)' ');
				if (sp >= eol)
				{
					// We can't split the header, it isn't valid.
					// This may be OK if this is a rename patch.
					//
					return eol;
				}
				int bStart = RawParseUtils.nextLF(Buffer, sp, (byte)'/');
				if (bStart >= eol)
					return eol;

				// If buffer[aStart..sp - 1] = buffer[bStart..eol - 1]
				// we have a valid split.
				//
				if (Eq(aStart, sp - 1, bStart, eol - 1))
				{
					if (Buffer[bol] == '"')
					{
						// We're a double quoted name. The region better end
						// in a double quote too, and we need to decode the
						// characters before reading the name.
						//
						if (Buffer[sp - 2] != '"')
						{
							return eol;
						}
						oldName = QuotedString.GitPathStyle.GIT_PATH.dequote(Buffer, bol, sp - 1);
						oldName = P1(oldName);
					}
					else
					{
						oldName = RawParseUtils.decode(Constants.CHARSET, Buffer, aStart, sp - 1);
					}
					newName = oldName;
					return eol;
				}

				// This split wasn't correct. Move past the space and try
				// another split as the space must be part of the file name.
				//
				ptr = sp;
			}

			return eol;
		}

		public virtual int parseGitHeaders(int ptr, int end)
		{
			while (ptr < end)
			{
				int eol = RawParseUtils.nextLF(Buffer, ptr);
				if (isHunkHdr(Buffer, ptr, eol) >= 1)
				{
					// First hunk header; break out and parse them later.
					break;
				}

				if (RawParseUtils.match(Buffer, ptr, OLD_NAME) >= 0)
				{
					ParseOldName(ptr, eol);
				}
				else if (RawParseUtils.match(Buffer, ptr, NEW_NAME) >= 0)
				{
					ParseNewName(ptr, eol);
				}
				else if (RawParseUtils.match(Buffer, ptr, OldModeString) >= 0)
				{
					_oldMode = ParseFileMode(ptr + OldModeString.Length, eol);
				}
				else if (RawParseUtils.match(Buffer, ptr, NewModeString) >= 0)
				{
					_newMode = ParseFileMode(ptr + NewModeString.Length, eol);
				}
				else if (RawParseUtils.match(Buffer, ptr, DeletedFileMode) >= 0)
				{
					_oldMode = ParseFileMode(ptr + DeletedFileMode.Length, eol);
					_newMode = FileMode.Missing;
					ChangeType = ChangeTypeEnum.DELETE;
				}
				else if (RawParseUtils.match(Buffer, ptr, NewFileMode) >= 0)
				{
					ParseNewFileMode(ptr, eol);
				}
				else if (RawParseUtils.match(Buffer, ptr, CopyFrom) >= 0)
				{
					oldName = ParseName(oldName, ptr + CopyFrom.Length, eol);
					ChangeType = ChangeTypeEnum.COPY;

				}
				else if (RawParseUtils.match(Buffer, ptr, CopyTo) >= 0)
				{
					newName = ParseName(newName, ptr + CopyTo.Length, eol);
					ChangeType = ChangeTypeEnum.COPY;
				}
				else if (RawParseUtils.match(Buffer, ptr, RenameOld) >= 0)
				{
					oldName = ParseName(oldName, ptr + RenameOld.Length, eol);
					ChangeType = ChangeTypeEnum.RENAME;
				}
				else if (RawParseUtils.match(Buffer, ptr, RenameNew) >= 0)
				{
					newName = ParseName(newName, ptr + RenameNew.Length, eol);
					ChangeType = ChangeTypeEnum.RENAME;
				}
				else if (RawParseUtils.match(Buffer, ptr, RenameFrom) >= 0)
				{
					oldName = ParseName(oldName, ptr + RenameFrom.Length, eol);
					ChangeType = ChangeTypeEnum.RENAME;
				}
				else if (RawParseUtils.match(Buffer, ptr, RenameTo) >= 0)
				{
					newName = ParseName(newName, ptr + RenameTo.Length, eol);
					ChangeType = ChangeTypeEnum.RENAME;
				}
				else if (RawParseUtils.match(Buffer, ptr, SimilarityIndex) >= 0)
				{
					_score = RawParseUtils.parseBase10(Buffer, ptr + SimilarityIndex.Length, null);
				}
				else if (RawParseUtils.match(Buffer, ptr, DissimilarityIndex) >= 0)
				{
					_score = RawParseUtils.parseBase10(Buffer, ptr + DissimilarityIndex.Length, null);
				}
				else if (RawParseUtils.match(Buffer, ptr, Index) >= 0)
				{
					ParseIndexLine(ptr + Index.Length, eol);
				}
				else
				{
					// Probably an empty patch (stat dirty).
					break;
				}

				ptr = eol;
			}

			return ptr;
		}

		protected void ParseOldName(int ptr, int eol)
		{
			oldName = P1(ParseName(oldName, ptr + OLD_NAME.Length, eol));
			if (oldName == DEV_NULL)
			{
				ChangeType = ChangeTypeEnum.ADD;
			}
		}

		protected void ParseNewName(int ptr, int eol)
		{
			newName = P1(ParseName(newName, ptr + NEW_NAME.Length, eol));
			if (newName == DEV_NULL)
			{
				ChangeType = ChangeTypeEnum.DELETE;
			}
		}

		protected virtual void ParseNewFileMode(int ptr, int eol)
		{
			_oldMode = FileMode.Missing;
			_newMode = ParseFileMode(ptr + NewFileMode.Length, eol);
			ChangeType = ChangeTypeEnum.ADD;
		}

		public int parseTraditionalHeaders(int ptr, int end)
		{
			while (ptr < end)
			{
				int eol = RawParseUtils.nextLF(Buffer, ptr);
				if (isHunkHdr(Buffer, ptr, eol) >= 1)
				{
					// First hunk header; break out and parse them later.
					break;
				}

				if (RawParseUtils.match(Buffer, ptr, OLD_NAME) >= 0)
				{
					ParseOldName(ptr, eol);
				}
				else if (RawParseUtils.match(Buffer, ptr, NEW_NAME) >= 0)
				{
					ParseNewName(ptr, eol);
				}
				else
				{
					// Possibly an empty patch.
					break;
				}

				ptr = eol;
			}

			return ptr;
		}

		private string ParseName(String expect, int ptr, int end)
		{
			if (ptr == end)
			{
				return expect;
			}

			string r;
			if (Buffer[ptr] == '"')
			{
				// New style GNU diff format
				//
				r = QuotedString.GitPathStyle.GIT_PATH.dequote(Buffer, ptr, end - 1);
			}
			else
			{
				// Older style GNU diff format, an optional tab ends the name.
				//
				int tab = end;
				while (ptr < tab && Buffer[tab - 1] != '\t')
				{
					tab--;
				}

				if (ptr == tab)
				{
					tab = end;
				}

				r = RawParseUtils.decode(Constants.CHARSET, Buffer, ptr, tab - 1);
			}

			if (r.Equals(DEV_NULL))
			{
				r = DEV_NULL;
			}

			return r;
		}

		private static string P1(string r)
		{
			int s = r.IndexOf('/');
			return s > 0 ? r.Substring(s + 1) : r;
		}

		protected FileMode ParseFileMode(int ptr, int end)
		{
			int tmp = 0;
			while (ptr < end - 1)
			{
				tmp <<= 3;
				tmp += Buffer[ptr++] - '0';
			}
			return FileMode.FromBits(tmp);
		}

		protected virtual void ParseIndexLine(int ptr, int end)
		{
			// "index $asha1..$bsha1[ $mode]" where $asha1 and $bsha1
			// can be unique abbreviations
			//
			int dot2 = RawParseUtils.nextLF(Buffer, ptr, (byte)'.');
			int mode = RawParseUtils.nextLF(Buffer, dot2, (byte)' ');

			oldId = AbbreviatedObjectId.FromString(Buffer, ptr, dot2 - 1);
			NewId = AbbreviatedObjectId.FromString(Buffer, dot2 + 1, mode - 1);

			if (mode < end)
			{
				_newMode = _oldMode = ParseFileMode(mode, end);
			}
		}

		private bool Eq(int aPtr, int aEnd, int bPtr, int bEnd)
		{
			if (aEnd - aPtr != bEnd - bPtr)
			{
				return false;
			}
			while (aPtr < aEnd)
			{
				if (Buffer[aPtr++] != Buffer[bPtr++])
				{
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Determine if this is a patch hunk header.
		/// </summary>
		/// <param name="buf">the buffer to scan</param>
		/// <param name="start">first position in the buffer to evaluate</param>
		/// <param name="end">
		/// last position to consider; usually the end of the buffer 
		/// (<code>buf.length</code>) or the first position on the next
		/// line. This is only used to avoid very long runs of '@' from
		/// killing the scan loop.
		/// </param>
		/// <returns>
		/// the number of "ancestor revisions" in the hunk header. A
		/// traditional two-way diff ("@@ -...") returns 1; a combined diff
		/// for a 3 way-merge returns 3. If this is not a hunk header, 0 is
		/// returned instead.
		/// </returns>
		public static int isHunkHdr(byte[] buf, int start, int end)
		{
			int ptr = start;
			while ((ptr < end) && (buf[ptr] == '@'))
			{
				ptr++;
			}

			if ((ptr - start) < 2)
				return 0;
			
			if ((ptr == end) || (buf[ptr++] != ' '))
				return 0;
			
			if ((ptr == end) || (buf[ptr++] != '-'))
				return 0;
			
			return (ptr - 3) - start;
		}
	}
}