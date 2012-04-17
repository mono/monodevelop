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
using NGit.Internal;
using NGit.Patch;
using NGit.Util;
using Sharpen;

namespace NGit.Patch
{
	/// <summary>Hunk header for a hunk appearing in a "diff --cc" style patch.</summary>
	/// <remarks>Hunk header for a hunk appearing in a "diff --cc" style patch.</remarks>
	public class CombinedHunkHeader : HunkHeader
	{
		private abstract class CombinedOldImage : HunkHeader.OldImage
		{
			internal int nContext;
		}

		private CombinedHunkHeader.CombinedOldImage[] old;

		internal CombinedHunkHeader(CombinedFileHeader fh, int offset) : base(fh, offset, 
			null)
		{
			old = new CombinedHunkHeader.CombinedOldImage[fh.GetParentCount()];
			for (int i = 0; i < old.Length; i++)
			{
				int imagePos = i;
				old[i] = new _CombinedOldImage_70(fh, imagePos);
			}
		}

		private sealed class _CombinedOldImage_70 : CombinedHunkHeader.CombinedOldImage
		{
			public _CombinedOldImage_70(CombinedFileHeader fh, int imagePos)
			{
				this.fh = fh;
				this.imagePos = imagePos;
			}

			public override AbbreviatedObjectId GetId()
			{
				return fh.GetOldId(imagePos);
			}

			private readonly CombinedFileHeader fh;

			private readonly int imagePos;
		}

		public override FileHeader GetFileHeader()
		{
			return (CombinedFileHeader)base.GetFileHeader();
		}

		public override HunkHeader.OldImage GetOldImage()
		{
			return GetOldImage(0);
		}

		/// <summary>Get the OldImage data related to the nth ancestor</summary>
		/// <param name="nthParent">the ancestor to get the old image data of</param>
		/// <returns>image data of the requested ancestor.</returns>
		public virtual HunkHeader.OldImage GetOldImage(int nthParent)
		{
			return old[nthParent];
		}

		internal override void ParseHeader()
		{
			// Parse "@@@ -55,12 -163,13 +163,15 @@@ protected boolean"
			//
			byte[] buf = file.buf;
			MutableInteger ptr = new MutableInteger();
			ptr.value = RawParseUtils.NextLF(buf, startOffset, ' ');
			for (int n = 0; n < old.Length; n++)
			{
				old[n].startLine = -RawParseUtils.ParseBase10(buf, ptr.value, ptr);
				if (buf[ptr.value] == ',')
				{
					old[n].lineCount = RawParseUtils.ParseBase10(buf, ptr.value + 1, ptr);
				}
				else
				{
					old[n].lineCount = 1;
				}
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

		internal override int ParseBody(NGit.Patch.Patch script, int end)
		{
			byte[] buf = file.buf;
			int c = RawParseUtils.NextLF(buf, startOffset);
			foreach (CombinedHunkHeader.CombinedOldImage o in old)
			{
				o.nDeleted = 0;
				o.nAdded = 0;
				o.nContext = 0;
			}
			nContext = 0;
			int nAdded = 0;
			for (int eol; c < end; c = eol)
			{
				eol = RawParseUtils.NextLF(buf, c);
				if (eol - c < old.Length + 1)
				{
					// Line isn't long enough to mention the state of each
					// ancestor. It must be the end of the hunk.
					goto SCAN_break;
				}
				switch (buf[c])
				{
					case (byte)(' '):
					case (byte)('-'):
					case (byte)('+'):
					{
						break;
					}

					default:
					{
						// Line can't possibly be part of this hunk; the first
						// ancestor information isn't recognizable.
						//
						goto SCAN_break;
						break;
					}
				}
				int localcontext = 0;
				for (int ancestor = 0; ancestor < old.Length; ancestor++)
				{
					switch (buf[c + ancestor])
					{
						case (byte)(' '):
						{
							localcontext++;
							old[ancestor].nContext++;
							continue;
							goto case (byte)('-');
						}

						case (byte)('-'):
						{
							old[ancestor].nDeleted++;
							continue;
							goto case (byte)('+');
						}

						case (byte)('+'):
						{
							old[ancestor].nAdded++;
							nAdded++;
							continue;
							goto default;
						}

						default:
						{
							goto SCAN_break;
							break;
						}
					}
				}
				if (localcontext == old.Length)
				{
					nContext++;
				}
SCAN_continue: ;
			}
SCAN_break: ;
			for (int ancestor_1 = 0; ancestor_1 < old.Length; ancestor_1++)
			{
				CombinedHunkHeader.CombinedOldImage o_1 = old[ancestor_1];
				int cmp = o_1.nContext + o_1.nDeleted;
				if (cmp < o_1.lineCount)
				{
					int missingCnt = o_1.lineCount - cmp;
					script.Error(buf, startOffset, MessageFormat.Format(JGitText.Get().truncatedHunkLinesMissingForAncestor
						, missingCnt, (ancestor_1 + 1)));
				}
			}
			if (nContext + nAdded < newLineCount)
			{
				int missingCount = newLineCount - (nContext + nAdded);
				script.Error(buf, startOffset, MessageFormat.Format(JGitText.Get().truncatedHunkNewLinesMissing
					, missingCount));
			}
			return c;
		}

		/// <exception cref="System.IO.IOException"></exception>
		internal override void ExtractFileLines(OutputStream[] @out)
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
				if (eol - ptr < old.Length + 1)
				{
					// Line isn't long enough to mention the state of each
					// ancestor. It must be the end of the hunk.
					goto SCAN_break;
				}
				switch (buf[ptr])
				{
					case (byte)(' '):
					case (byte)('-'):
					case (byte)('+'):
					{
						break;
					}

					default:
					{
						// Line can't possibly be part of this hunk; the first
						// ancestor information isn't recognizable.
						//
						goto SCAN_break;
						break;
					}
				}
				int delcnt = 0;
				for (int ancestor = 0; ancestor < old.Length; ancestor++)
				{
					switch (buf[ptr + ancestor])
					{
						case (byte)('-'):
						{
							delcnt++;
							@out[ancestor].Write(buf, ptr, eol - ptr);
							continue;
							goto case (byte)(' ');
						}

						case (byte)(' '):
						{
							@out[ancestor].Write(buf, ptr, eol - ptr);
							continue;
							goto case (byte)('+');
						}

						case (byte)('+'):
						{
							continue;
							goto default;
						}

						default:
						{
							goto SCAN_break;
							break;
						}
					}
				}
				if (delcnt < old.Length)
				{
					// This line appears in the new file if it wasn't deleted
					// relative to all ancestors.
					//
					@out[old.Length].Write(buf, ptr, eol - ptr);
				}
SCAN_continue: ;
			}
SCAN_break: ;
		}

		internal override void ExtractFileLines(StringBuilder sb, string[] text, int[] offsets
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
				if (eol - ptr < old.Length + 1)
				{
					// Line isn't long enough to mention the state of each
					// ancestor. It must be the end of the hunk.
					goto SCAN_break;
				}
				switch (buf[ptr])
				{
					case (byte)(' '):
					case (byte)('-'):
					case (byte)('+'):
					{
						break;
					}

					default:
					{
						// Line can't possibly be part of this hunk; the first
						// ancestor information isn't recognizable.
						//
						goto SCAN_break;
						break;
					}
				}
				bool copied = false;
				for (int ancestor = 0; ancestor < old.Length; ancestor++)
				{
					switch (buf[ptr + ancestor])
					{
						case (byte)(' '):
						case (byte)('-'):
						{
							if (copied)
							{
								SkipLine(text, offsets, ancestor);
							}
							else
							{
								CopyLine(sb, text, offsets, ancestor);
								copied = true;
							}
							continue;
							goto case (byte)('+');
						}

						case (byte)('+'):
						{
							continue;
							goto default;
						}

						default:
						{
							goto SCAN_break;
							break;
						}
					}
				}
				if (!copied)
				{
					// If none of the ancestors caused the copy then this line
					// must be new across the board, so it only appears in the
					// text of the new file.
					//
					CopyLine(sb, text, offsets, old.Length);
				}
SCAN_continue: ;
			}
SCAN_break: ;
		}
	}
}
