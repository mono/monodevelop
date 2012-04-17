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
using System.Collections.Generic;
using System.IO;
using System.Text;
using NGit;
using NGit.Diff;
using NGit.Internal;
using NGit.Patch;
using NGit.Util;
using Sharpen;

namespace NGit.Patch
{
	/// <summary>Patch header describing an action for a single file path.</summary>
	/// <remarks>Patch header describing an action for a single file path.</remarks>
	public class FileHeader : DiffEntry
	{
		private static readonly byte[] OLD_MODE = Constants.EncodeASCII("old mode ");

		private static readonly byte[] NEW_MODE = Constants.EncodeASCII("new mode ");

		internal static readonly byte[] DELETED_FILE_MODE = Constants.EncodeASCII("deleted file mode "
			);

		internal static readonly byte[] NEW_FILE_MODE = Constants.EncodeASCII("new file mode "
			);

		private static readonly byte[] COPY_FROM = Constants.EncodeASCII("copy from ");

		private static readonly byte[] COPY_TO = Constants.EncodeASCII("copy to ");

		private static readonly byte[] RENAME_OLD = Constants.EncodeASCII("rename old ");

		private static readonly byte[] RENAME_NEW = Constants.EncodeASCII("rename new ");

		private static readonly byte[] RENAME_FROM = Constants.EncodeASCII("rename from "
			);

		private static readonly byte[] RENAME_TO = Constants.EncodeASCII("rename to ");

		private static readonly byte[] SIMILARITY_INDEX = Constants.EncodeASCII("similarity index "
			);

		private static readonly byte[] DISSIMILARITY_INDEX = Constants.EncodeASCII("dissimilarity index "
			);

		internal static readonly byte[] INDEX = Constants.EncodeASCII("index ");

		internal static readonly byte[] OLD_NAME = Constants.EncodeASCII("--- ");

		internal static readonly byte[] NEW_NAME = Constants.EncodeASCII("+++ ");

		/// <summary>Type of patch used by this file.</summary>
		/// <remarks>Type of patch used by this file.</remarks>
		public enum PatchType
		{
			UNIFIED,
			BINARY,
			GIT_BINARY
		}

		/// <summary>Buffer holding the patch data for this file.</summary>
		/// <remarks>Buffer holding the patch data for this file.</remarks>
		internal readonly byte[] buf;

		/// <summary>
		/// Offset within
		/// <see cref="buf">buf</see>
		/// to the "diff ..." line.
		/// </summary>
		internal readonly int startOffset;

		/// <summary>
		/// Position 1 past the end of this file within
		/// <see cref="buf">buf</see>
		/// .
		/// </summary>
		internal int endOffset;

		/// <summary>Type of patch used to modify this file</summary>
		internal FileHeader.PatchType patchType;

		/// <summary>The hunks of this file</summary>
		private IList<HunkHeader> hunks;

		/// <summary>
		/// If
		/// <see cref="patchType">patchType</see>
		/// is
		/// <see cref="PatchType.GIT_BINARY">PatchType.GIT_BINARY</see>
		/// , the new image
		/// </summary>
		internal BinaryHunk forwardBinaryHunk;

		/// <summary>
		/// If
		/// <see cref="patchType">patchType</see>
		/// is
		/// <see cref="PatchType.GIT_BINARY">PatchType.GIT_BINARY</see>
		/// , the old image
		/// </summary>
		internal BinaryHunk reverseBinaryHunk;

		/// <summary>Constructs a new FileHeader</summary>
		/// <param name="headerLines">buffer holding the diff header for this file</param>
		/// <param name="edits">the edits for this file</param>
		/// <param name="type">the type of patch used to modify this file</param>
		public FileHeader(byte[] headerLines, EditList edits, FileHeader.PatchType type) : 
			this(headerLines, 0)
		{
			endOffset = headerLines.Length;
			int ptr = ParseGitFileName(NGit.Patch.Patch.DIFF_GIT.Length, headerLines.Length);
			ParseGitHeaders(ptr, headerLines.Length);
			this.patchType = type;
			AddHunk(new HunkHeader(this, edits));
		}

		internal FileHeader(byte[] b, int offset)
		{
			buf = b;
			startOffset = offset;
			changeType = DiffEntry.ChangeType.MODIFY;
			// unless otherwise designated
			patchType = FileHeader.PatchType.UNIFIED;
		}

		internal virtual int GetParentCount()
		{
			return 1;
		}

		/// <returns>the byte array holding this file's patch script.</returns>
		public virtual byte[] GetBuffer()
		{
			return buf;
		}

		/// <returns>
		/// offset the start of this file's script in
		/// <see cref="GetBuffer()">GetBuffer()</see>
		/// .
		/// </returns>
		public virtual int GetStartOffset()
		{
			return startOffset;
		}

		/// <returns>offset one past the end of the file script.</returns>
		public virtual int GetEndOffset()
		{
			return endOffset;
		}

		/// <summary>Convert the patch script for this file into a string.</summary>
		/// <remarks>
		/// Convert the patch script for this file into a string.
		/// <p>
		/// The default character encoding (
		/// <see cref="NGit.Constants.CHARSET">NGit.Constants.CHARSET</see>
		/// ) is assumed for
		/// both the old and new files.
		/// </remarks>
		/// <returns>the patch script, as a Unicode string.</returns>
		public virtual string GetScriptText()
		{
			return GetScriptText(null, null);
		}

		/// <summary>Convert the patch script for this file into a string.</summary>
		/// <remarks>Convert the patch script for this file into a string.</remarks>
		/// <param name="oldCharset">hint character set to decode the old lines with.</param>
		/// <param name="newCharset">hint character set to decode the new lines with.</param>
		/// <returns>the patch script, as a Unicode string.</returns>
		public virtual string GetScriptText(Encoding oldCharset, Encoding newCharset)
		{
			return GetScriptText(new Encoding[] { oldCharset, newCharset });
		}

		internal virtual string GetScriptText(Encoding[] charsetGuess)
		{
			if (GetHunks().IsEmpty())
			{
				// If we have no hunks then we can safely assume the entire
				// patch is a binary style patch, or a meta-data only style
				// patch. Either way the encoding of the headers should be
				// strictly 7-bit US-ASCII and the body is either 7-bit ASCII
				// (due to the base 85 encoding used for a BinaryHunk) or is
				// arbitrary noise we have chosen to ignore and not understand
				// (e.g. the message "Binary files ... differ").
				//
				return RawParseUtils.ExtractBinaryString(buf, startOffset, endOffset);
			}
			if (charsetGuess != null && charsetGuess.Length != GetParentCount() + 1)
			{
				throw new ArgumentException(MessageFormat.Format(JGitText.Get().expectedCharacterEncodingGuesses
					, (GetParentCount() + 1)));
			}
			if (TrySimpleConversion(charsetGuess))
			{
				Encoding cs = charsetGuess != null ? charsetGuess[0] : null;
				if (cs == null)
				{
					cs = Constants.CHARSET;
				}
				try
				{
					return RawParseUtils.DecodeNoFallback(cs, buf, startOffset, endOffset);
				}
				catch (CharacterCodingException)
				{
				}
			}
			// Try the much slower, more-memory intensive version which
			// can handle a character set conversion patch.
			StringBuilder r = new StringBuilder(endOffset - startOffset);
			// Always treat the headers as US-ASCII; Git file names are encoded
			// in a C style escape if any character has the high-bit set.
			//
			int hdrEnd = GetHunks()[0].GetStartOffset();
			for (int ptr = startOffset; ptr < hdrEnd; )
			{
				int eol = Math.Min(hdrEnd, RawParseUtils.NextLF(buf, ptr));
				r.Append(RawParseUtils.ExtractBinaryString(buf, ptr, eol));
				ptr = eol;
			}
			string[] files = ExtractFileLines(charsetGuess);
			int[] offsets = new int[files.Length];
			foreach (HunkHeader h in GetHunks())
			{
				h.ExtractFileLines(r, files, offsets);
			}
			return r.ToString();
		}

		private static bool TrySimpleConversion(Encoding[] charsetGuess)
		{
			if (charsetGuess == null)
			{
				return true;
			}
			for (int i = 1; i < charsetGuess.Length; i++)
			{
				if (charsetGuess[i] != charsetGuess[0])
				{
					return false;
				}
			}
			return true;
		}

		private string[] ExtractFileLines(Encoding[] csGuess)
		{
			TemporaryBuffer[] tmp = new TemporaryBuffer[GetParentCount() + 1];
			try
			{
				for (int i = 0; i < tmp.Length; i++)
				{
					tmp[i] = new TemporaryBuffer.LocalFile();
				}
				foreach (HunkHeader h in GetHunks())
				{
					h.ExtractFileLines(tmp);
				}
				string[] r = new string[tmp.Length];
				for (int i_1 = 0; i_1 < tmp.Length; i_1++)
				{
					Encoding cs = csGuess != null ? csGuess[i_1] : null;
					if (cs == null)
					{
						cs = Constants.CHARSET;
					}
					r[i_1] = RawParseUtils.Decode(cs, tmp[i_1].ToByteArray());
				}
				return r;
			}
			catch (IOException ioe)
			{
				throw new RuntimeException(JGitText.Get().cannotConvertScriptToText, ioe);
			}
			finally
			{
				foreach (TemporaryBuffer b in tmp)
				{
					if (b != null)
					{
						b.Destroy();
					}
				}
			}
		}

		/// <returns>style of patch used to modify this file</returns>
		public virtual FileHeader.PatchType GetPatchType()
		{
			return patchType;
		}

		/// <returns>true if this patch modifies metadata about a file</returns>
		public virtual bool HasMetaDataChanges()
		{
			return changeType != DiffEntry.ChangeType.MODIFY || newMode != oldMode;
		}

		/// <returns>hunks altering this file; in order of appearance in patch</returns>
		public virtual IList<HunkHeader> GetHunks()
		{
			if (hunks == null)
			{
				return Sharpen.Collections.EmptyList<HunkHeader>();
			}
			return hunks;
		}

		internal virtual void AddHunk(HunkHeader h)
		{
			if (h.GetFileHeader() != this)
			{
				throw new ArgumentException(JGitText.Get().hunkBelongsToAnotherFile);
			}
			if (hunks == null)
			{
				hunks = new AList<HunkHeader>();
			}
			hunks.AddItem(h);
		}

		internal virtual HunkHeader NewHunkHeader(int offset)
		{
			return new HunkHeader(this, offset);
		}

		/// <returns>
		/// if a
		/// <see cref="PatchType.GIT_BINARY">PatchType.GIT_BINARY</see>
		/// , the new-image delta/literal
		/// </returns>
		public virtual BinaryHunk GetForwardBinaryHunk()
		{
			return forwardBinaryHunk;
		}

		/// <returns>
		/// if a
		/// <see cref="PatchType.GIT_BINARY">PatchType.GIT_BINARY</see>
		/// , the old-image delta/literal
		/// </returns>
		public virtual BinaryHunk GetReverseBinaryHunk()
		{
			return reverseBinaryHunk;
		}

		/// <returns>a list describing the content edits performed on this file.</returns>
		public virtual EditList ToEditList()
		{
			EditList r = new EditList();
			foreach (HunkHeader hunk in hunks)
			{
				Sharpen.Collections.AddAll(r, hunk.ToEditList());
			}
			return r;
		}

		/// <summary>Parse a "diff --git" or "diff --cc" line.</summary>
		/// <remarks>Parse a "diff --git" or "diff --cc" line.</remarks>
		/// <param name="ptr">first character after the "diff --git " or "diff --cc " part.</param>
		/// <param name="end">one past the last position to parse.</param>
		/// <returns>first character after the LF at the end of the line; -1 on error.</returns>
		internal virtual int ParseGitFileName(int ptr, int end)
		{
			int eol = RawParseUtils.NextLF(buf, ptr);
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
			int aStart = RawParseUtils.NextLF(buf, ptr, '/');
			if (aStart >= eol)
			{
				return eol;
			}
			while (ptr < eol)
			{
				int sp = RawParseUtils.NextLF(buf, ptr, ' ');
				if (sp >= eol)
				{
					// We can't split the header, it isn't valid.
					// This may be OK if this is a rename patch.
					//
					return eol;
				}
				int bStart = RawParseUtils.NextLF(buf, sp, '/');
				if (bStart >= eol)
				{
					return eol;
				}
				// If buffer[aStart..sp - 1] = buffer[bStart..eol - 1]
				// we have a valid split.
				//
				if (Eq(aStart, sp - 1, bStart, eol - 1))
				{
					if (buf[bol] == '"')
					{
						// We're a double quoted name. The region better end
						// in a double quote too, and we need to decode the
						// characters before reading the name.
						//
						if (buf[sp - 2] != '"')
						{
							return eol;
						}
						oldPath = QuotedString.GIT_PATH.Dequote(buf, bol, sp - 1);
						oldPath = P1(oldPath);
					}
					else
					{
						oldPath = RawParseUtils.Decode(Constants.CHARSET, buf, aStart, sp - 1);
					}
					newPath = oldPath;
					return eol;
				}
				// This split wasn't correct. Move past the space and try
				// another split as the space must be part of the file name.
				//
				ptr = sp;
			}
			return eol;
		}

		internal virtual int ParseGitHeaders(int ptr, int end)
		{
			while (ptr < end)
			{
				int eol = RawParseUtils.NextLF(buf, ptr);
				if (IsHunkHdr(buf, ptr, eol) >= 1)
				{
					// First hunk header; break out and parse them later.
					break;
				}
				else
				{
					if (RawParseUtils.Match(buf, ptr, OLD_NAME) >= 0)
					{
						ParseOldName(ptr, eol);
					}
					else
					{
						if (RawParseUtils.Match(buf, ptr, NEW_NAME) >= 0)
						{
							ParseNewName(ptr, eol);
						}
						else
						{
							if (RawParseUtils.Match(buf, ptr, OLD_MODE) >= 0)
							{
								oldMode = ParseFileMode(ptr + OLD_MODE.Length, eol);
							}
							else
							{
								if (RawParseUtils.Match(buf, ptr, NEW_MODE) >= 0)
								{
									newMode = ParseFileMode(ptr + NEW_MODE.Length, eol);
								}
								else
								{
									if (RawParseUtils.Match(buf, ptr, DELETED_FILE_MODE) >= 0)
									{
										oldMode = ParseFileMode(ptr + DELETED_FILE_MODE.Length, eol);
										newMode = FileMode.MISSING;
										changeType = DiffEntry.ChangeType.DELETE;
									}
									else
									{
										if (RawParseUtils.Match(buf, ptr, NEW_FILE_MODE) >= 0)
										{
											ParseNewFileMode(ptr, eol);
										}
										else
										{
											if (RawParseUtils.Match(buf, ptr, COPY_FROM) >= 0)
											{
												oldPath = ParseName(oldPath, ptr + COPY_FROM.Length, eol);
												changeType = DiffEntry.ChangeType.COPY;
											}
											else
											{
												if (RawParseUtils.Match(buf, ptr, COPY_TO) >= 0)
												{
													newPath = ParseName(newPath, ptr + COPY_TO.Length, eol);
													changeType = DiffEntry.ChangeType.COPY;
												}
												else
												{
													if (RawParseUtils.Match(buf, ptr, RENAME_OLD) >= 0)
													{
														oldPath = ParseName(oldPath, ptr + RENAME_OLD.Length, eol);
														changeType = DiffEntry.ChangeType.RENAME;
													}
													else
													{
														if (RawParseUtils.Match(buf, ptr, RENAME_NEW) >= 0)
														{
															newPath = ParseName(newPath, ptr + RENAME_NEW.Length, eol);
															changeType = DiffEntry.ChangeType.RENAME;
														}
														else
														{
															if (RawParseUtils.Match(buf, ptr, RENAME_FROM) >= 0)
															{
																oldPath = ParseName(oldPath, ptr + RENAME_FROM.Length, eol);
																changeType = DiffEntry.ChangeType.RENAME;
															}
															else
															{
																if (RawParseUtils.Match(buf, ptr, RENAME_TO) >= 0)
																{
																	newPath = ParseName(newPath, ptr + RENAME_TO.Length, eol);
																	changeType = DiffEntry.ChangeType.RENAME;
																}
																else
																{
																	if (RawParseUtils.Match(buf, ptr, SIMILARITY_INDEX) >= 0)
																	{
																		score = RawParseUtils.ParseBase10(buf, ptr + SIMILARITY_INDEX.Length, null);
																	}
																	else
																	{
																		if (RawParseUtils.Match(buf, ptr, DISSIMILARITY_INDEX) >= 0)
																		{
																			score = RawParseUtils.ParseBase10(buf, ptr + DISSIMILARITY_INDEX.Length, null);
																		}
																		else
																		{
																			if (RawParseUtils.Match(buf, ptr, INDEX) >= 0)
																			{
																				ParseIndexLine(ptr + INDEX.Length, eol);
																			}
																			else
																			{
																				// Probably an empty patch (stat dirty).
																				break;
																			}
																		}
																	}
																}
															}
														}
													}
												}
											}
										}
									}
								}
							}
						}
					}
				}
				ptr = eol;
			}
			return ptr;
		}

		internal virtual void ParseOldName(int ptr, int eol)
		{
			oldPath = P1(ParseName(oldPath, ptr + OLD_NAME.Length, eol));
			if (oldPath == DEV_NULL)
			{
				changeType = DiffEntry.ChangeType.ADD;
			}
		}

		internal virtual void ParseNewName(int ptr, int eol)
		{
			newPath = P1(ParseName(newPath, ptr + NEW_NAME.Length, eol));
			if (newPath == DEV_NULL)
			{
				changeType = DiffEntry.ChangeType.DELETE;
			}
		}

		internal virtual void ParseNewFileMode(int ptr, int eol)
		{
			oldMode = FileMode.MISSING;
			newMode = ParseFileMode(ptr + NEW_FILE_MODE.Length, eol);
			changeType = DiffEntry.ChangeType.ADD;
		}

		internal virtual int ParseTraditionalHeaders(int ptr, int end)
		{
			while (ptr < end)
			{
				int eol = RawParseUtils.NextLF(buf, ptr);
				if (IsHunkHdr(buf, ptr, eol) >= 1)
				{
					// First hunk header; break out and parse them later.
					break;
				}
				else
				{
					if (RawParseUtils.Match(buf, ptr, OLD_NAME) >= 0)
					{
						ParseOldName(ptr, eol);
					}
					else
					{
						if (RawParseUtils.Match(buf, ptr, NEW_NAME) >= 0)
						{
							ParseNewName(ptr, eol);
						}
						else
						{
							// Possibly an empty patch.
							break;
						}
					}
				}
				ptr = eol;
			}
			return ptr;
		}

		private string ParseName(string expect, int ptr, int end)
		{
			if (ptr == end)
			{
				return expect;
			}
			string r;
			if (buf[ptr] == '"')
			{
				// New style GNU diff format
				//
				r = QuotedString.GIT_PATH.Dequote(buf, ptr, end - 1);
			}
			else
			{
				// Older style GNU diff format, an optional tab ends the name.
				//
				int tab = end;
				while (ptr < tab && buf[tab - 1] != '\t')
				{
					tab--;
				}
				if (ptr == tab)
				{
					tab = end;
				}
				r = RawParseUtils.Decode(Constants.CHARSET, buf, ptr, tab - 1);
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
			return s > 0 ? Sharpen.Runtime.Substring(r, s + 1) : r;
		}

		internal virtual FileMode ParseFileMode(int ptr, int end)
		{
			int tmp = 0;
			while (ptr < end - 1)
			{
				tmp <<= 3;
				tmp += buf[ptr++] - '0';
			}
			return FileMode.FromBits(tmp);
		}

		internal virtual void ParseIndexLine(int ptr, int end)
		{
			// "index $asha1..$bsha1[ $mode]" where $asha1 and $bsha1
			// can be unique abbreviations
			//
			int dot2 = RawParseUtils.NextLF(buf, ptr, '.');
			int mode = RawParseUtils.NextLF(buf, dot2, ' ');
			oldId = AbbreviatedObjectId.FromString(buf, ptr, dot2 - 1);
			newId = AbbreviatedObjectId.FromString(buf, dot2 + 1, mode - 1);
			if (mode < end)
			{
				newMode = oldMode = ParseFileMode(mode, end);
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
				if (buf[aPtr++] != buf[bPtr++])
				{
					return false;
				}
			}
			return true;
		}

		/// <summary>Determine if this is a patch hunk header.</summary>
		/// <remarks>Determine if this is a patch hunk header.</remarks>
		/// <param name="buf">the buffer to scan</param>
		/// <param name="start">first position in the buffer to evaluate</param>
		/// <param name="end">
		/// last position to consider; usually the end of the buffer (
		/// <code>buf.length</code>) or the first position on the next
		/// line. This is only used to avoid very long runs of '@' from
		/// killing the scan loop.
		/// </param>
		/// <returns>
		/// the number of "ancestor revisions" in the hunk header. A
		/// traditional two-way diff ("@@ -...") returns 1; a combined diff
		/// for a 3 way-merge returns 3. If this is not a hunk header, 0 is
		/// returned instead.
		/// </returns>
		internal static int IsHunkHdr(byte[] buf, int start, int end)
		{
			int ptr = start;
			while (ptr < end && buf[ptr] == '@')
			{
				ptr++;
			}
			if (ptr - start < 2)
			{
				return 0;
			}
			if (ptr == end || buf[ptr++] != ' ')
			{
				return 0;
			}
			if (ptr == end || buf[ptr++] != '-')
			{
				return 0;
			}
			return (ptr - 3) - start;
		}
	}
}
