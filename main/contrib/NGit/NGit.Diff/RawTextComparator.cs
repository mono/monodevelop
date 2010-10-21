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
using NGit.Diff;
using NGit.Util;
using Sharpen;

namespace NGit.Diff
{
	/// <summary>
	/// Equivalence function for
	/// <see cref="RawText">RawText</see>
	/// .
	/// </summary>
	public abstract class RawTextComparator : SequenceComparator<RawText>
	{
		private sealed class _RawTextComparator_56 : RawTextComparator
		{
			public _RawTextComparator_56()
			{
			}

			public override bool Equals(RawText a, int ai, RawText b, int bi)
			{
				ai++;
				bi++;
				int @as = a.lines.Get(ai);
				int bs = b.lines.Get(bi);
				int ae = a.lines.Get(ai + 1);
				int be = b.lines.Get(bi + 1);
				if (ae - @as != be - bs)
				{
					return false;
				}
				while (@as < ae)
				{
					if (a.content[@as++] != b.content[bs++])
					{
						return false;
					}
				}
				return true;
			}

			protected internal override int HashRegion(byte[] raw, int ptr, int end)
			{
				int hash = 5381;
				for (; ptr < end; ptr++)
				{
					hash = ((hash << 5) + hash) + (raw[ptr] & unchecked((int)(0xff)));
				}
				return hash;
			}
		}

		/// <summary>No special treatment.</summary>
		/// <remarks>No special treatment.</remarks>
		public static readonly RawTextComparator DEFAULT = new _RawTextComparator_56();

		private sealed class _RawTextComparator_87 : RawTextComparator
		{
			public _RawTextComparator_87()
			{
			}

			public override bool Equals(RawText a, int ai, RawText b, int bi)
			{
				ai++;
				bi++;
				int @as = a.lines.Get(ai);
				int bs = b.lines.Get(bi);
				int ae = a.lines.Get(ai + 1);
				int be = b.lines.Get(bi + 1);
				ae = RawCharUtil.TrimTrailingWhitespace(a.content, @as, ae);
				be = RawCharUtil.TrimTrailingWhitespace(b.content, bs, be);
				while (@as < ae && bs < be)
				{
					byte ac = a.content[@as];
					byte bc = b.content[bs];
					while (@as < ae - 1 && RawCharUtil.IsWhitespace(ac))
					{
						@as++;
						ac = a.content[@as];
					}
					while (bs < be - 1 && RawCharUtil.IsWhitespace(bc))
					{
						bs++;
						bc = b.content[bs];
					}
					if (ac != bc)
					{
						return false;
					}
					@as++;
					bs++;
				}
				return @as == ae && bs == be;
			}

			protected internal override int HashRegion(byte[] raw, int ptr, int end)
			{
				int hash = 5381;
				for (; ptr < end; ptr++)
				{
					byte c = raw[ptr];
					if (!RawCharUtil.IsWhitespace(c))
					{
						hash = ((hash << 5) + hash) + (c & unchecked((int)(0xff)));
					}
				}
				return hash;
			}
		}

		/// <summary>Ignores all whitespace.</summary>
		/// <remarks>Ignores all whitespace.</remarks>
		public static readonly RawTextComparator WS_IGNORE_ALL = new _RawTextComparator_87
			();

		private sealed class _RawTextComparator_138 : RawTextComparator
		{
			public _RawTextComparator_138()
			{
			}

			public override bool Equals(RawText a, int ai, RawText b, int bi)
			{
				ai++;
				bi++;
				int @as = a.lines.Get(ai);
				int bs = b.lines.Get(bi);
				int ae = a.lines.Get(ai + 1);
				int be = b.lines.Get(bi + 1);
				@as = RawCharUtil.TrimLeadingWhitespace(a.content, @as, ae);
				bs = RawCharUtil.TrimLeadingWhitespace(b.content, bs, be);
				if (ae - @as != be - bs)
				{
					return false;
				}
				while (@as < ae)
				{
					if (a.content[@as++] != b.content[bs++])
					{
						return false;
					}
				}
				return true;
			}

			protected internal override int HashRegion(byte[] raw, int ptr, int end)
			{
				int hash = 5381;
				ptr = RawCharUtil.TrimLeadingWhitespace(raw, ptr, end);
				for (; ptr < end; ptr++)
				{
					hash = ((hash << 5) + hash) + (raw[ptr] & unchecked((int)(0xff)));
				}
				return hash;
			}
		}

		/// <summary>Ignores leading whitespace.</summary>
		/// <remarks>Ignores leading whitespace.</remarks>
		public static readonly RawTextComparator WS_IGNORE_LEADING = new _RawTextComparator_138
			();

		private sealed class _RawTextComparator_173 : RawTextComparator
		{
			public _RawTextComparator_173()
			{
			}

			public override bool Equals(RawText a, int ai, RawText b, int bi)
			{
				ai++;
				bi++;
				int @as = a.lines.Get(ai);
				int bs = b.lines.Get(bi);
				int ae = a.lines.Get(ai + 1);
				int be = b.lines.Get(bi + 1);
				ae = RawCharUtil.TrimTrailingWhitespace(a.content, @as, ae);
				be = RawCharUtil.TrimTrailingWhitespace(b.content, bs, be);
				if (ae - @as != be - bs)
				{
					return false;
				}
				while (@as < ae)
				{
					if (a.content[@as++] != b.content[bs++])
					{
						return false;
					}
				}
				return true;
			}

			protected internal override int HashRegion(byte[] raw, int ptr, int end)
			{
				int hash = 5381;
				end = RawCharUtil.TrimTrailingWhitespace(raw, ptr, end);
				for (; ptr < end; ptr++)
				{
					hash = ((hash << 5) + hash) + (raw[ptr] & unchecked((int)(0xff)));
				}
				return hash;
			}
		}

		/// <summary>Ignores trailing whitespace.</summary>
		/// <remarks>Ignores trailing whitespace.</remarks>
		public static readonly RawTextComparator WS_IGNORE_TRAILING = new _RawTextComparator_173
			();

		private sealed class _RawTextComparator_208 : RawTextComparator
		{
			public _RawTextComparator_208()
			{
			}

			public override bool Equals(RawText a, int ai, RawText b, int bi)
			{
				ai++;
				bi++;
				int @as = a.lines.Get(ai);
				int bs = b.lines.Get(bi);
				int ae = a.lines.Get(ai + 1);
				int be = b.lines.Get(bi + 1);
				ae = RawCharUtil.TrimTrailingWhitespace(a.content, @as, ae);
				be = RawCharUtil.TrimTrailingWhitespace(b.content, bs, be);
				while (@as < ae && bs < be)
				{
					byte ac = a.content[@as];
					byte bc = b.content[bs];
					if (ac != bc)
					{
						return false;
					}
					if (RawCharUtil.IsWhitespace(ac))
					{
						@as = RawCharUtil.TrimLeadingWhitespace(a.content, @as, ae);
					}
					else
					{
						@as++;
					}
					if (RawCharUtil.IsWhitespace(bc))
					{
						bs = RawCharUtil.TrimLeadingWhitespace(b.content, bs, be);
					}
					else
					{
						bs++;
					}
				}
				return @as == ae && bs == be;
			}

			protected internal override int HashRegion(byte[] raw, int ptr, int end)
			{
				int hash = 5381;
				end = RawCharUtil.TrimTrailingWhitespace(raw, ptr, end);
				while (ptr < end)
				{
					byte c = raw[ptr];
					hash = ((hash << 5) + hash) + (c & unchecked((int)(0xff)));
					if (RawCharUtil.IsWhitespace(c))
					{
						ptr = RawCharUtil.TrimLeadingWhitespace(raw, ptr, end);
					}
					else
					{
						ptr++;
					}
				}
				return hash;
			}
		}

		/// <summary>Ignores whitespace occurring between non-whitespace characters.</summary>
		/// <remarks>Ignores whitespace occurring between non-whitespace characters.</remarks>
		public static readonly RawTextComparator WS_IGNORE_CHANGE = new _RawTextComparator_208
			();

		public override int Hash(RawText seq, int lno)
		{
			int begin = seq.lines.Get(lno + 1);
			int end = seq.lines.Get(lno + 2);
			return HashRegion(seq.content, begin, end);
		}

		public override Edit ReduceCommonStartEnd(RawText a, RawText b, Edit e)
		{
			// This is a faster exact match based form that tries to improve
			// performance for the common case of the header and trailer of
			// a text file not changing at all. After this fast path we use
			// the slower path based on the super class' using equals() to
			// allow for whitespace ignore modes to still work.
			if (e.beginA == e.endA || e.beginB == e.endB)
			{
				return e;
			}
			byte[] aRaw = a.content;
			byte[] bRaw = b.content;
			int aPtr = a.lines.Get(e.beginA + 1);
			int bPtr = a.lines.Get(e.beginB + 1);
			int aEnd = a.lines.Get(e.endA + 1);
			int bEnd = b.lines.Get(e.endB + 1);
			// This can never happen, but the JIT doesn't know that. If we
			// define this assertion before the tight while loops below it
			// should be able to skip the array bound checks on access.
			//
			if (aPtr < 0 || bPtr < 0 || aEnd > aRaw.Length || bEnd > bRaw.Length)
			{
				throw new IndexOutOfRangeException();
			}
			while (aPtr < aEnd && bPtr < bEnd && aRaw[aPtr] == bRaw[bPtr])
			{
				aPtr++;
				bPtr++;
			}
			while (aPtr < aEnd && bPtr < bEnd && aRaw[aEnd - 1] == bRaw[bEnd - 1])
			{
				aEnd--;
				bEnd--;
			}
			e.beginA = FindForwardLine(a.lines, e.beginA, aPtr);
			e.beginB = FindForwardLine(b.lines, e.beginB, bPtr);
			e.endA = FindReverseLine(a.lines, e.endA, aEnd);
			bool partialA = aEnd < a.lines.Get(e.endA + 1);
			if (partialA)
			{
				bEnd += a.lines.Get(e.endA + 1) - aEnd;
			}
			e.endB = FindReverseLine(b.lines, e.endB, bEnd);
			if (!partialA && bEnd < b.lines.Get(e.endB + 1))
			{
				e.endA++;
			}
			return base.ReduceCommonStartEnd(a, b, e);
		}

		private static int FindForwardLine(IntList lines, int idx, int ptr)
		{
			int end = lines.Size() - 2;
			while (idx < end && lines.Get(idx + 2) <= ptr)
			{
				idx++;
			}
			return idx;
		}

		private static int FindReverseLine(IntList lines, int idx, int ptr)
		{
			while (0 < idx && ptr <= lines.Get(idx))
			{
				idx--;
			}
			return idx;
		}

		/// <summary>Compute a hash code for a region.</summary>
		/// <remarks>Compute a hash code for a region.</remarks>
		/// <param name="raw">the raw file content.</param>
		/// <param name="ptr">first byte of the region to hash.</param>
		/// <param name="end">1 past the last byte of the region.</param>
		/// <returns>hash code for the region <code>[ptr, end)</code> of raw.</returns>
		protected internal abstract int HashRegion(byte[] raw, int ptr, int end);
	}
}
