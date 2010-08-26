/*
 * Copyright (C) 2008, Johannes E. Schindelin <johannes.schindelin@gmx.de>
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
using GitSharp.Core.Patch;
using GitSharp.Core.Util;

namespace GitSharp.Core.Diff
{
	/// <summary>
	/// Format an <seealso cref="EditList"/> as a Git style unified patch script.
	/// </summary>
	public class DiffFormatter
	{
		private static readonly byte[] NoNewLine = 
			Constants.encodeASCII("\\ No newline at end of file\n");

		private int _context;

		/// <summary>
		/// Create a new formatter with a default level of context.
		/// </summary>
		public DiffFormatter()
		{
			setContext(3);
		}

		/// <summary>
		/// Change the number of lines of context to display.
		///	</summary>
		///	<param name="lineCount">
		/// Number of lines of context to see before the first
		/// modification and After the last modification within a hunk of
		/// the modified file.
		/// </param>
		public void setContext(int lineCount)
		{
			if (lineCount < 0)
			{
				throw new ArgumentException("context must be >= 0");
			}

			_context = lineCount;
		}

		/// <summary>
		/// Format a patch script, reusing a previously parsed FileHeader.
		///	<para />
		///	This formatter is primarily useful for editing an existing patch script
		///	to increase or reduce the number of lines of context within the script.
		///	All header lines are reused as-is from the supplied FileHeader.
		/// </summary>
		/// <param name="out">stream to write the patch script out to.</param>
		/// <param name="head">existing file header containing the header lines to copy.</param>
		/// <param name="a">
		/// Text source for the pre-image version of the content. 
		/// This must match the content of <seealso cref="FileHeader.getOldId()"/>.
		/// </param>
		/// <param name="b">writing to the supplied stream failed.</param>
		public void format(Stream @out, FileHeader head, RawText a, RawText b)
		{
			if ( head == null)
			{
				throw new System.ArgumentNullException("head");
			}
			if ( @out == null)
			{
				throw new System.ArgumentNullException("out");
			}
			
			// Reuse the existing FileHeader as-is by blindly copying its
			// header lines, but avoiding its hunks. Instead we recreate
			// the hunks from the text instances we have been supplied.
			//
			int start = head.StartOffset;
			int end = head.EndOffset;

			if (!head.Hunks.isEmpty())
			{
				end = head.Hunks[0].StartOffset;
			}

			@out.Write(head.Buffer, start, end - start);

			FormatEdits(@out, a, b, head.ToEditList());
		}

        /// <summary>
        /// Formats a list of edits in unified diff format
        /// </summary>
        /// <param name="out">where the unified diff is written to</param>
        /// <param name="a">the text A which was compared</param>
        /// <param name="b">the text B which was compared</param>
        /// <param name="edits">some differences which have been calculated between A and B</param>
		public void FormatEdits(Stream @out, RawText a, RawText b, EditList edits)
		{
			for (int curIdx = 0; curIdx < edits.Count; /* */)
			{
				Edit curEdit = edits.get(curIdx);
				int endIdx = FindCombinedEnd(edits, curIdx);
				Edit endEdit = edits.get(endIdx);

				int aCur = Math.Max(0, curEdit.BeginA - _context);
				int bCur = Math.Max(0, curEdit.BeginB - _context);
				int aEnd = Math.Min(a.size(), endEdit.EndA + _context);
				int bEnd = Math.Min(b.size(), endEdit.EndB + _context);

				WriteHunkHeader(@out, aCur, aEnd, bCur, bEnd);

				while (aCur < aEnd || bCur < bEnd)
				{
					if (aCur < curEdit.BeginA || endIdx + 1 < curIdx)
					{
						WriteLine(@out, ' ', a, aCur);
						aCur++;
						bCur++;
					}
					else if (aCur < curEdit.EndA)
					{
						WriteLine(@out, '-', a, aCur++);

					}
					else if (bCur < curEdit.EndB)
					{
						WriteLine(@out, '+', b, bCur++);
					}

					if (End(curEdit, aCur, bCur) && ++curIdx < edits.Count)
					{
						curEdit = edits.get(curIdx);
					}
				}
			}
		}

		private static void WriteHunkHeader(Stream @out, int aCur, int aEnd, int bCur, int bEnd)
		{
			@out.WriteByte(Convert.ToByte('@'));
			@out.WriteByte(Convert.ToByte('@'));
			WriteRange(@out, '-', aCur + 1, aEnd - aCur);
			WriteRange(@out, '+', bCur + 1, bEnd - bCur);
			@out.WriteByte(Convert.ToByte(' '));
			@out.WriteByte(Convert.ToByte('@'));
			@out.WriteByte(Convert.ToByte('@'));
			@out.WriteByte(Convert.ToByte('\n'));
		}

		private static void WriteRange(Stream @out, char prefix, int begin, int cnt)
		{
			@out.WriteByte(Convert.ToByte(' '));
			@out.WriteByte(Convert.ToByte(prefix));

			switch (cnt)
			{
				case 0:
					// If the range is empty, its beginning number must be the
					// line just before the range, or 0 if the range is at the
					// start of the file stream. Here, begin is always 1 based,
					// so an empty file would produce "0,0".
					//
					WriteInteger(@out, begin - 1);
					@out.WriteByte(Convert.ToByte(','));
					@out.WriteByte(Convert.ToByte('0'));
					break;

				case 1:
					// If the range is exactly one line, produce only the number.
					//
                    WriteInteger(@out, begin);
					break;

				default:
                    WriteInteger(@out, begin);
                    @out.WriteByte(Convert.ToByte(','));
					WriteInteger(@out, cnt);
					break;
			}
		}

		private static void WriteInteger(Stream @out, int count)
		{
			var buffer = Constants.encodeASCII(count);
			@out.Write(buffer, 0, buffer.Length);
		}

		private static void WriteLine(Stream @out, char prefix, RawText text, int cur)
		{
			@out.WriteByte(Convert.ToByte(prefix));
			text.writeLine(@out, cur);
			@out.WriteByte(Convert.ToByte('\n'));
			if (cur + 1 == text.size() && text.isMissingNewlineAtEnd())
			{
				@out.Write(NoNewLine, 0, NoNewLine.Length);
			}
		}

		private int FindCombinedEnd(IList<Edit> edits, int i)
		{
			int end = i + 1;
			while (end < edits.Count && (CombineA(edits, end) || CombineB(edits, end)))
			{
				end++;
			}
			return end - 1;
		}

		private bool CombineA(IList<Edit> e, int i)
		{
			return e[i].BeginA - e[i - 1].EndA <= 2 * _context;
		}

		private bool CombineB(IList<Edit> e, int i)
		{
			return e[i].BeginB - e[i - 1].EndB <= 2 * _context;
		}

		private static bool End(Edit edit, int a, int b)
		{
			return edit.EndA <= a && edit.EndB <= b;
		}
	}
}