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
using NGit.Util;
using Sharpen;

namespace NGit.Util
{
	/// <summary>Searches text using only substring search.</summary>
	/// <remarks>
	/// Searches text using only substring search.
	/// <p>
	/// Instances are thread-safe. Multiple concurrent threads may perform matches on
	/// different character sequences at the same time.
	/// </remarks>
	public class RawSubStringPattern
	{
		private readonly string needleString;

		private readonly byte[] needle;

		/// <summary>Construct a new substring pattern.</summary>
		/// <remarks>Construct a new substring pattern.</remarks>
		/// <param name="patternText">
		/// text to locate. This should be a literal string, as no
		/// meta-characters are supported by this implementation. The
		/// string may not be the empty string.
		/// </param>
		public RawSubStringPattern(string patternText)
		{
			if (patternText.Length == 0)
			{
				throw new ArgumentException(JGitText.Get().cannotMatchOnEmptyString);
			}
			needleString = patternText;
			byte[] b = Constants.Encode(patternText);
			needle = new byte[b.Length];
			for (int i = 0; i < b.Length; i++)
			{
				needle[i] = Lc(b[i]);
			}
		}

		/// <summary>Match a character sequence against this pattern.</summary>
		/// <remarks>Match a character sequence against this pattern.</remarks>
		/// <param name="rcs">
		/// the sequence to match. Must not be null but the length of the
		/// sequence is permitted to be 0.
		/// </param>
		/// <returns>
		/// offset within <code>rcs</code> of the first occurrence of this
		/// pattern; -1 if this pattern does not appear at any position of
		/// <code>rcs</code>.
		/// </returns>
		public virtual int Match(RawCharSequence rcs)
		{
			int needleLen = needle.Length;
			byte first = needle[0];
			byte[] text = rcs.buffer;
			int matchPos = rcs.startPtr;
			int maxPos = rcs.endPtr - needleLen;
			for (; matchPos < maxPos; matchPos++)
			{
				if (Neq(first, text[matchPos]))
				{
					while (++matchPos < maxPos && Neq(first, text[matchPos]))
					{
					}
					if (matchPos == maxPos)
					{
						return -1;
					}
				}
				int si = ++matchPos;
				for (int j = 1; j < needleLen; j++, si++)
				{
					if (Neq(needle[j], text[si]))
					{
						goto OUTER_continue;
					}
				}
				return matchPos - 1;
OUTER_continue: ;
			}
OUTER_break: ;
			return -1;
		}

		private static bool Neq(byte a, byte b)
		{
			return a != b && a != Lc(b);
		}

		private static byte Lc(byte q)
		{
			return unchecked((byte)StringUtils.ToLowerCase((char)(q & unchecked((int)(0xff)))
				));
		}

		/// <summary>Get the literal pattern string this instance searches for.</summary>
		/// <remarks>Get the literal pattern string this instance searches for.</remarks>
		/// <returns>the pattern string given to our constructor.</returns>
		public virtual string Pattern()
		{
			return needleString;
		}

		public override string ToString()
		{
			return Pattern();
		}
	}
}
