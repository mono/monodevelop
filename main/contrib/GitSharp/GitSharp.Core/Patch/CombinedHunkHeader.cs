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

using System.Collections.Generic;
using System.IO;
using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core.Patch
{
	/// <summary>
	/// Hunk header for a hunk appearing in a "diff --cc" style patch.
	/// </summary>
	public class CombinedHunkHeader : HunkHeader
	{
		private readonly List<CombinedOldImage> _old;

		public CombinedHunkHeader(FileHeader fh, int offset)
			: base(fh, offset, null)
		{
			int size = fh.ParentCount;
			_old = new List<CombinedOldImage>(size);
			for (int i = 0; i < size; i++)
			{
				_old.Add(new CombinedOldImage(fh, i));
			}
		}

		/// <summary>
		/// Gets the <see cref="OldImage"/> data related to the nth ancestor
		/// </summary>
		/// <param name="nthParent">The ancestor to get the old image data of</param>
		/// <returns>The image data of the requested ancestor.</returns>
		internal OldImage GetOldImage(int nthParent)
		{
			return _old[nthParent];
		}

        internal override OldImage OldImage
        {
            get
            {
                return GetOldImage(0);
            }
        }

		public override void parseHeader()
		{
			// Parse "@@@ -55,12 -163,13 +163,15 @@@ protected boolean"
			//
			byte[] buf = File.Buffer;
			var ptr = new MutableInteger
						{
							value = RawParseUtils.nextLF(buf, StartOffset, (byte)' ')
						};

			_old.ForEach(coi =>
							{
								coi.StartLine = -1 * RawParseUtils.parseBase10(Buffer, ptr.value, ptr);
								coi.LineCount = buf[ptr.value] == ',' ?
									RawParseUtils.parseBase10(buf, ptr.value + 1, ptr) : 1;
							});

			NewStartLine = RawParseUtils.parseBase10(buf, ptr.value + 1, ptr);
			NewLineCount = buf[ptr.value] == ',' ? RawParseUtils.parseBase10(buf, ptr.value + 1, ptr) : 1;
		}

		public override int parseBody(Patch script, int end)
		{
			byte[] buf = File.Buffer;
			int c = RawParseUtils.nextLF(buf, StartOffset);

			_old.ForEach(coi =>
			             	{
			             		coi.LinesAdded = 0;
			             		coi.LinesDeleted = 0;
			             		coi.LinesContext = 0;
			             	});

			LinesContext = 0;
			int nAdded = 0;

			for (int eol; c < end; c = eol)
			{
				eol = RawParseUtils.nextLF(buf, c);

				if (eol - c < _old.Count + 1)
				{
					// Line isn't long enough to mention the state of each
					// ancestor. It must be the end of the hunk.
					break;
				}

				bool break_scan = false;
				switch (buf[c])
				{
					case (byte)' ':
					case (byte)'-':
					case (byte)'+':
						break;

					default:
						// Line can't possibly be part of this hunk; the first
						// ancestor information isn't recognizable.
						//
						break_scan = true;
						break;
				}
				if (break_scan)
					break;

				int localcontext = 0;
				for (int ancestor = 0; ancestor < _old.Count; ancestor++)
				{
					switch (buf[c + ancestor])
					{
						case (byte)' ':
							localcontext++;
							_old[ancestor].LinesContext++;
							continue;

						case (byte)'-':
							_old[ancestor].LinesDeleted++;
							continue;

						case (byte)'+':
							_old[ancestor].LinesAdded++;
							nAdded++;
							continue;

						default:
							break_scan = true;
							break;
					}
					if (break_scan)
						break;
				}
				if (break_scan)
					break;

				if (localcontext == _old.Count)
				{
					LinesContext++;
				}
			}

			for (int ancestor = 0; ancestor < _old.Count; ancestor++)
			{
				CombinedOldImage o = _old[ancestor];
				int cmp = o.LinesContext + o.LinesDeleted;
				if (cmp < o.LineCount)
				{
					int missingCnt = o.LineCount - cmp;
					script.error(buf, StartOffset, "Truncated hunk, at least "
							+ missingCnt + " lines is missing for ancestor "
							+ (ancestor + 1));
				}
			}

			if (LinesContext + nAdded < NewLineCount)
			{
				int missingCount = NewLineCount - (LinesContext + nAdded);
				script.error(buf, StartOffset, "Truncated hunk, at least "
						+ missingCount + " new lines is missing");
			}

			return c;
		}

		public void extractFileLines(Stream[] outStream)
		{
			byte[] buf = File.Buffer;
			int ptr = StartOffset;
			int eol = RawParseUtils.nextLF(buf, ptr);
			if (EndOffset <= eol)
			{
				return;
			}

			// Treat the hunk header as though it were from the ancestor,
			// as it may have a function header appearing After it which
			// was copied out of the ancestor file.
			//
			outStream[0].Write(buf, ptr, eol - ptr);

			//SCAN: 
			for (ptr = eol; ptr < EndOffset; ptr = eol)
			{
				eol = RawParseUtils.nextLF(buf, ptr);

				if (eol - ptr < _old.Count + 1)
				{
					// Line isn't long enough to mention the state of each
					// ancestor. It must be the end of the hunk.
					break;
				}

				bool breakScan = false;
				switch (buf[ptr])
				{
					case (byte)' ':
					case (byte)'-':
					case (byte)'+':
						break;

					default:
						// Line can't possibly be part of this hunk; the first
						// ancestor information isn't recognizable.
						//
						breakScan = true;
						break;
				}
				if (breakScan)
					break;

				int delcnt = 0;
				for (int ancestor = 0; ancestor < _old.Count; ancestor++)
				{
					switch (buf[ptr + ancestor])
					{
						case (byte)'-':
							delcnt++;
							outStream[ancestor].Write(buf, ptr, eol - ptr);
							continue;

						case (byte)' ':
							outStream[ancestor].Write(buf, ptr, eol - ptr);
							continue;

						case (byte)'+':
							continue;

						default:
							breakScan = true;
							break;
					}

					if (breakScan)
					{
						break;
					}
				}

				if (breakScan)
					break;

				if (delcnt < _old.Count)
				{
					// This line appears in the new file if it wasn't deleted
					// relative to all ancestors.
					//
					outStream[_old.Count].Write(buf, ptr, eol - ptr);
				}
			}
		}

		public override void extractFileLines(StringBuilder sb, string[] text, int[] offsets)
		{
			byte[] buf = File.Buffer;
			int ptr = StartOffset;
			int eol = RawParseUtils.nextLF(buf, ptr);

			if (EndOffset <= eol)
			{
				return;
			}

			copyLine(sb, text, offsets, 0);

			for (ptr = eol; ptr < EndOffset; ptr = eol)
			{
				eol = RawParseUtils.nextLF(buf, ptr);

				if (eol - ptr < _old.Count + 1)
				{
					// Line isn't long enough to mention the state of each
					// ancestor. It must be the end of the hunk.
					break;
				}

				bool breakScan = false;
				switch (buf[ptr])
				{
					case (byte)' ':
					case (byte)'-':
					case (byte)'+':
						break;

					default:
						// Line can't possibly be part of this hunk; the first
						// ancestor information isn't recognizable.
						//
						breakScan = true;
						break;
				}
				if (breakScan)
				{
					break;
				}

				bool copied = false;
				for (int ancestor = 0; ancestor < _old.Count; ancestor++)
				{
					switch (buf[ptr + ancestor])
					{
						case (byte)' ':
						case (byte)'-':
							if (copied)
								skipLine(text, offsets, ancestor);
							else
							{
								copyLine(sb, text, offsets, ancestor);
								copied = true;
							}
							continue;

						case (byte)'+':
							continue;

						default:
							breakScan = true;
							break;
					}

					if (breakScan)
					{
						break;
					}
				}

				if (breakScan)
				{
					break;
				}

				if (!copied)
				{
					// If none of the ancestors caused the copy then this line
					// must be new across the board, so it only appears in the
					// text of the new file.
					//
					copyLine(sb, text, offsets, _old.Count);
				}
			}
		}
	}
}