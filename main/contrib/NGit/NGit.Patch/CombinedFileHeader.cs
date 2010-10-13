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

using System.Collections.Generic;
using System.Text;
using NGit;
using NGit.Diff;
using NGit.Patch;
using NGit.Util;
using Sharpen;

namespace NGit.Patch
{
	/// <summary>A file in the Git "diff --cc" or "diff --combined" format.</summary>
	/// <remarks>
	/// A file in the Git "diff --cc" or "diff --combined" format.
	/// <p>
	/// A combined diff shows an n-way comparison between two or more ancestors and
	/// the final revision. Its primary function is to perform code reviews on a
	/// merge which introduces changes not in any ancestor.
	/// </remarks>
	public class CombinedFileHeader : FileHeader
	{
		private static readonly byte[] MODE = Constants.EncodeASCII("mode ");

		private AbbreviatedObjectId[] oldIds;

		private FileMode[] oldModes;

		internal CombinedFileHeader(byte[] b, int offset) : base(b, offset)
		{
		}

		public override IList<HunkHeader> GetHunks()
		{
			return base.GetHunks();
		}

		/// <returns>number of ancestor revisions mentioned in this diff.</returns>
		internal override int GetParentCount()
		{
			return oldIds.Length;
		}

		/// <returns>get the file mode of the first parent.</returns>
		public override FileMode GetOldMode()
		{
			return GetOldMode(0);
		}

		/// <summary>Get the file mode of the nth ancestor</summary>
		/// <param name="nthParent">the ancestor to get the mode of</param>
		/// <returns>the mode of the requested ancestor.</returns>
		public virtual FileMode GetOldMode(int nthParent)
		{
			return oldModes[nthParent];
		}

		/// <returns>get the object id of the first parent.</returns>
		public override AbbreviatedObjectId GetOldId()
		{
			return GetOldId(0);
		}

		/// <summary>Get the ObjectId of the nth ancestor</summary>
		/// <param name="nthParent">the ancestor to get the object id of</param>
		/// <returns>the id of the requested ancestor.</returns>
		public virtual AbbreviatedObjectId GetOldId(int nthParent)
		{
			return oldIds[nthParent];
		}

		public override string GetScriptText(Encoding ocs, Encoding ncs)
		{
			Encoding[] cs = new Encoding[GetParentCount() + 1];
			Arrays.Fill(cs, ocs);
			cs[GetParentCount()] = ncs;
			return GetScriptText(cs);
		}

		/// <summary>Convert the patch script for this file into a string.</summary>
		/// <remarks>Convert the patch script for this file into a string.</remarks>
		/// <param name="charsetGuess">
		/// optional array to suggest the character set to use when
		/// decoding each file's line. If supplied the array must have a
		/// length of <code>
		/// <see cref="GetParentCount()">GetParentCount()</see>
		/// + 1</code>
		/// representing the old revision character sets and the new
		/// revision character set.
		/// </param>
		/// <returns>the patch script, as a Unicode string.</returns>
		internal override string GetScriptText(Encoding[] charsetGuess)
		{
			return base.GetScriptText(charsetGuess);
		}

		internal override int ParseGitHeaders(int ptr, int end)
		{
			while (ptr < end)
			{
				int eol = RawParseUtils.NextLF(buf, ptr);
				if (IsHunkHdr(buf, ptr, end) >= 1)
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
							if (RawParseUtils.Match(buf, ptr, INDEX) >= 0)
							{
								ParseIndexLine(ptr + INDEX.Length, eol);
							}
							else
							{
								if (RawParseUtils.Match(buf, ptr, MODE) >= 0)
								{
									ParseModeLine(ptr + MODE.Length, eol);
								}
								else
								{
									if (RawParseUtils.Match(buf, ptr, NEW_FILE_MODE) >= 0)
									{
										ParseNewFileMode(ptr, eol);
									}
									else
									{
										if (RawParseUtils.Match(buf, ptr, DELETED_FILE_MODE) >= 0)
										{
											ParseDeletedFileMode(ptr + DELETED_FILE_MODE.Length, eol);
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
				ptr = eol;
			}
			return ptr;
		}

		internal override void ParseIndexLine(int ptr, int eol)
		{
			// "index $asha1,$bsha1..$csha1"
			//
			IList<AbbreviatedObjectId> ids = new AList<AbbreviatedObjectId>();
			while (ptr < eol)
			{
				int comma = RawParseUtils.NextLF(buf, ptr, ',');
				if (eol <= comma)
				{
					break;
				}
				ids.AddItem(AbbreviatedObjectId.FromString(buf, ptr, comma - 1));
				ptr = comma;
			}
			oldIds = new AbbreviatedObjectId[ids.Count + 1];
			Sharpen.Collections.ToArray(ids, oldIds);
			int dot2 = RawParseUtils.NextLF(buf, ptr, '.');
			oldIds[ids.Count] = AbbreviatedObjectId.FromString(buf, ptr, dot2 - 1);
			newId = AbbreviatedObjectId.FromString(buf, dot2 + 1, eol - 1);
			oldModes = new FileMode[oldIds.Length];
		}

		internal override void ParseNewFileMode(int ptr, int eol)
		{
			for (int i = 0; i < oldModes.Length; i++)
			{
				oldModes[i] = FileMode.MISSING;
			}
			base.ParseNewFileMode(ptr, eol);
		}

		internal override HunkHeader NewHunkHeader(int offset)
		{
			return new CombinedHunkHeader(this, offset);
		}

		private void ParseModeLine(int ptr, int eol)
		{
			// "mode $amode,$bmode..$cmode"
			//
			int n = 0;
			while (ptr < eol)
			{
				int comma = RawParseUtils.NextLF(buf, ptr, ',');
				if (eol <= comma)
				{
					break;
				}
				oldModes[n++] = ParseFileMode(ptr, comma);
				ptr = comma;
			}
			int dot2 = RawParseUtils.NextLF(buf, ptr, '.');
			oldModes[n] = ParseFileMode(ptr, dot2);
			newMode = ParseFileMode(dot2 + 1, eol);
		}

		private void ParseDeletedFileMode(int ptr, int eol)
		{
			// "deleted file mode $amode,$bmode"
			//
			changeType = DiffEntry.ChangeType.DELETE;
			int n = 0;
			while (ptr < eol)
			{
				int comma = RawParseUtils.NextLF(buf, ptr, ',');
				if (eol <= comma)
				{
					break;
				}
				oldModes[n++] = ParseFileMode(ptr, comma);
				ptr = comma;
			}
			oldModes[n] = ParseFileMode(ptr, eol);
			newMode = FileMode.MISSING;
		}
	}
}
