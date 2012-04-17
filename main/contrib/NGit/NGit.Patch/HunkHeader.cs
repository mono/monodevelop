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

using System.Text;
using NGit;
using NGit.Diff;
using NGit.Internal;
using NGit.Patch;
using NGit.Util;
using Sharpen;

namespace NGit.Patch
{
	/// <summary>Hunk header describing the layout of a single block of lines</summary>
	public class HunkHeader
	{
		/// <summary>Details about an old image of the file.</summary>
		/// <remarks>Details about an old image of the file.</remarks>
		public abstract class OldImage
		{
			/// <summary>First line number the hunk starts on in this file.</summary>
			/// <remarks>First line number the hunk starts on in this file.</remarks>
			internal int startLine;

			/// <summary>Total number of lines this hunk covers in this file.</summary>
			/// <remarks>Total number of lines this hunk covers in this file.</remarks>
			internal int lineCount;

			/// <summary>Number of lines deleted by the post-image from this file.</summary>
			/// <remarks>Number of lines deleted by the post-image from this file.</remarks>
			internal int nDeleted;

			/// <summary>Number of lines added by the post-image not in this file.</summary>
			/// <remarks>Number of lines added by the post-image not in this file.</remarks>
			internal int nAdded;

			/// <returns>first line number the hunk starts on in this file.</returns>
			public virtual int GetStartLine()
			{
				return startLine;
			}

			/// <returns>total number of lines this hunk covers in this file.</returns>
			public virtual int GetLineCount()
			{
				return lineCount;
			}

			/// <returns>number of lines deleted by the post-image from this file.</returns>
			public virtual int GetLinesDeleted()
			{
				return nDeleted;
			}

			/// <returns>number of lines added by the post-image not in this file.</returns>
			public virtual int GetLinesAdded()
			{
				return nAdded;
			}

			/// <returns>object id of the pre-image file.</returns>
			public abstract AbbreviatedObjectId GetId();
		}

		internal readonly FileHeader file;

		/// <summary>
		/// Offset within
		/// <see cref="file">file</see>
		/// .buf to the "@@ -" line.
		/// </summary>
		internal readonly int startOffset;

		/// <summary>
		/// Position 1 past the end of this hunk within
		/// <see cref="file">file</see>
		/// 's buf.
		/// </summary>
		internal int endOffset;

		private readonly HunkHeader.OldImage old;

		/// <summary>First line number in the post-image file where the hunk starts</summary>
		internal int newStartLine;

		/// <summary>Total number of post-image lines this hunk covers (context + inserted)</summary>
		internal int newLineCount;

		/// <summary>Total number of lines of context appearing in this hunk</summary>
		internal int nContext;

		private EditList editList;

		internal HunkHeader(FileHeader fh, int offset) : this(fh, offset, new _OldImage_122
			(fh))
		{
		}

		private sealed class _OldImage_122 : HunkHeader.OldImage
		{
			public _OldImage_122(FileHeader fh)
			{
				this.fh = fh;
			}

			public override AbbreviatedObjectId GetId()
			{
				return fh.GetOldId();
			}

			private readonly FileHeader fh;
		}

		internal HunkHeader(FileHeader fh, int offset, HunkHeader.OldImage oi)
		{
			file = fh;
			startOffset = offset;
			old = oi;
		}

		internal HunkHeader(FileHeader fh, EditList editList) : this(fh, fh.buf.Length)
		{
			this.editList = editList;
			endOffset = startOffset;
			nContext = 0;
			if (editList.IsEmpty())
			{
				newStartLine = 0;
				newLineCount = 0;
			}
			else
			{
				newStartLine = editList[0].GetBeginB();
				Edit last = editList[editList.Count - 1];
				newLineCount = last.GetEndB() - newStartLine;
			}
		}

		/// <returns>header for the file this hunk applies to</returns>
		public virtual FileHeader GetFileHeader()
		{
			return file;
		}

		/// <returns>the byte array holding this hunk's patch script.</returns>
		public virtual byte[] GetBuffer()
		{
			return file.buf;
		}

		/// <returns>
		/// offset the start of this hunk in
		/// <see cref="GetBuffer()">GetBuffer()</see>
		/// .
		/// </returns>
		public virtual int GetStartOffset()
		{
			return startOffset;
		}

		/// <returns>
		/// offset one past the end of the hunk in
		/// <see cref="GetBuffer()">GetBuffer()</see>
		/// .
		/// </returns>
		public virtual int GetEndOffset()
		{
			return endOffset;
		}

		/// <returns>information about the old image mentioned in this hunk.</returns>
		public virtual HunkHeader.OldImage GetOldImage()
		{
			return old;
		}

		/// <returns>first line number in the post-image file where the hunk starts</returns>
		public virtual int GetNewStartLine()
		{
			return newStartLine;
		}

		/// <returns>Total number of post-image lines this hunk covers</returns>
		public virtual int GetNewLineCount()
		{
			return newLineCount;
		}

		/// <returns>total number of lines of context appearing in this hunk</returns>
		public virtual int GetLinesContext()
		{
			return nContext;
		}

		/// <returns>a list describing the content edits performed within the hunk.</returns>
		public virtual EditList ToEditList()
		{
			if (editList == null)
			{
				editList = new EditList();
				byte[] buf = file.buf;
				int c = RawParseUtils.NextLF(buf, startOffset);
				int oLine = old.startLine;
				int nLine = newStartLine;
				Edit @in = null;
				for (; c < endOffset; c = RawParseUtils.NextLF(buf, c))
				{
					switch (buf[c])
					{
						case (byte)(' '):
						case (byte)('\n'):
						{
							@in = null;
							oLine++;
							nLine++;
							continue;
							goto case (byte)('-');
						}

						case (byte)('-'):
						{
							if (@in == null)
							{
								@in = new Edit(oLine - 1, nLine - 1);
								editList.AddItem(@in);
							}
							oLine++;
							@in.ExtendA();
							continue;
							goto case (byte)('+');
						}

						case (byte)('+'):
						{
							if (@in == null)
							{
								@in = new Edit(oLine - 1, nLine - 1);
								editList.AddItem(@in);
							}
							nLine++;
							@in.ExtendB();
							continue;
							goto case (byte)('\\');
						}

						case (byte)('\\'):
						{
							// Matches "\ No newline at end of file"
							continue;
							goto default;
						}

						default:
						{
							goto SCAN_break;
							break;
						}
					}
SCAN_continue: ;
				}
SCAN_break: ;
			}
			return editList;
		}

		internal virtual void ParseHeader()
		{
			// Parse "@@ -236,9 +236,9 @@ protected boolean"
			//
			byte[] buf = file.buf;
			MutableInteger ptr = new MutableInteger();
			ptr.value = RawParseUtils.NextLF(buf, startOffset, ' ');
			old.startLine = -RawParseUtils.ParseBase10(buf, ptr.value, ptr);
			if (buf[ptr.value] == ',')
			{
				old.lineCount = RawParseUtils.ParseBase10(buf, ptr.value + 1, ptr);
			}
			else
			{
				old.lineCount = 1;
			}
			newStartLine = RawParseUtils.ParseBase10(buf, ptr.value + 1, ptr);
			if (buf[ptr.value] == ',')
			{
				newLineCount = RawParseUtils.ParseBase10(buf, ptr.value + 1, ptr);
			}
			else
			{
				newLineCount = 1;
			}
		}

		internal virtual int ParseBody(NGit.Patch.Patch script, int end)
		{
			byte[] buf = file.buf;
			int c = RawParseUtils.NextLF(buf, startOffset);
			int last = c;
			old.nDeleted = 0;
			old.nAdded = 0;
			for (; c < end; last = c, c = RawParseUtils.NextLF(buf, c))
			{
				switch (buf[c])
				{
					case (byte)(' '):
					case (byte)('\n'):
					{
						nContext++;
						continue;
						goto case (byte)('-');
					}

					case (byte)('-'):
					{
						old.nDeleted++;
						continue;
						goto case (byte)('+');
					}

					case (byte)('+'):
					{
						old.nAdded++;
						continue;
						goto case (byte)('\\');
					}

					case (byte)('\\'):
					{
						// Matches "\ No newline at end of file"
						continue;
						goto default;
					}

					default:
					{
						goto SCAN_break;
						break;
					}
				}
SCAN_continue: ;
			}
SCAN_break: ;
			if (last < end && nContext + old.nDeleted - 1 == old.lineCount && nContext + old.
				nAdded == newLineCount && RawParseUtils.Match(buf, last, NGit.Patch.Patch.SIG_FOOTER
				) >= 0)
			{
				// This is an extremely common occurrence of "corruption".
				// Users add footers with their signatures after this mark,
				// and git diff adds the git executable version number.
				// Let it slide; the hunk otherwise looked sound.
				//
				old.nDeleted--;
				return last;
			}
			if (nContext + old.nDeleted < old.lineCount)
			{
				int missingCount = old.lineCount - (nContext + old.nDeleted);
				script.Error(buf, startOffset, MessageFormat.Format(JGitText.Get().truncatedHunkOldLinesMissing
					, missingCount));
			}
			else
			{
				if (nContext + old.nAdded < newLineCount)
				{
					int missingCount = newLineCount - (nContext + old.nAdded);
					script.Error(buf, startOffset, MessageFormat.Format(JGitText.Get().truncatedHunkNewLinesMissing
						, missingCount));
				}
				else
				{
					if (nContext + old.nDeleted > old.lineCount || nContext + old.nAdded > newLineCount)
					{
						string oldcnt = old.lineCount + ":" + newLineCount;
						string newcnt = (nContext + old.nDeleted) + ":" + (nContext + old.nAdded);
						script.Warn(buf, startOffset, MessageFormat.Format(JGitText.Get().hunkHeaderDoesNotMatchBodyLineCountOf
							, oldcnt, newcnt));
					}
				}
			}
			return c;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal virtual void ExtractFileLines(OutputStream[] @out)
		{
			byte[] buf = file.buf;
			int ptr = startOffset;
			int eol = RawParseUtils.NextLF(buf, ptr);
			if (endOffset <= eol)
			{
				return;
			}
			// Treat the hunk header as though it were from the ancestor,
			// as it may have a function header appearing after it which
			// was copied out of the ancestor file.
			//
			@out[0].Write(buf, ptr, eol - ptr);
			for (ptr = eol; ptr < endOffset; ptr = eol)
			{
				eol = RawParseUtils.NextLF(buf, ptr);
				switch (buf[ptr])
				{
					case (byte)(' '):
					case (byte)('\n'):
					case (byte)('\\'):
					{
						@out[0].Write(buf, ptr, eol - ptr);
						@out[1].Write(buf, ptr, eol - ptr);
						break;
					}

					case (byte)('-'):
					{
						@out[0].Write(buf, ptr, eol - ptr);
						break;
					}

					case (byte)('+'):
					{
						@out[1].Write(buf, ptr, eol - ptr);
						break;
					}

					default:
					{
						goto SCAN_break;
						break;
					}
				}
SCAN_continue: ;
			}
SCAN_break: ;
		}

		internal virtual void ExtractFileLines(StringBuilder sb, string[] text, int[] offsets
			)
		{
			byte[] buf = file.buf;
			int ptr = startOffset;
			int eol = RawParseUtils.NextLF(buf, ptr);
			if (endOffset <= eol)
			{
				return;
			}
			CopyLine(sb, text, offsets, 0);
			for (ptr = eol; ptr < endOffset; ptr = eol)
			{
				eol = RawParseUtils.NextLF(buf, ptr);
				switch (buf[ptr])
				{
					case (byte)(' '):
					case (byte)('\n'):
					case (byte)('\\'):
					{
						CopyLine(sb, text, offsets, 0);
						SkipLine(text, offsets, 1);
						break;
					}

					case (byte)('-'):
					{
						CopyLine(sb, text, offsets, 0);
						break;
					}

					case (byte)('+'):
					{
						CopyLine(sb, text, offsets, 1);
						break;
					}

					default:
					{
						goto SCAN_break;
						break;
					}
				}
SCAN_continue: ;
			}
SCAN_break: ;
		}

		internal virtual void CopyLine(StringBuilder sb, string[] text, int[] offsets, int
			 fileIdx)
		{
			string s = text[fileIdx];
			int start = offsets[fileIdx];
			int end = s.IndexOf('\n', start);
			if (end < 0)
			{
				end = s.Length;
			}
			else
			{
				end++;
			}
			sb.AppendRange(s, start, end);
			offsets[fileIdx] = end;
		}

		internal virtual void SkipLine(string[] text, int[] offsets, int fileIdx)
		{
			string s = text[fileIdx];
			int end = s.IndexOf('\n', offsets[fileIdx]);
			offsets[fileIdx] = end < 0 ? s.Length : end + 1;
		}

		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			buf.Append("HunkHeader[");
			buf.Append(GetOldImage().GetStartLine());
			buf.Append(',');
			buf.Append(GetOldImage().GetLineCount());
			buf.Append("->");
			buf.Append(GetNewStartLine()).Append(',').Append(GetNewLineCount());
			buf.Append(']');
			return buf.ToString();
		}
	}
}
