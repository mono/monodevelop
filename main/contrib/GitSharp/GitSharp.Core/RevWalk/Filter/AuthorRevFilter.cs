/*
 * Copyright (C) 2008, Shawn O. Pearce <spearce@spearce.org>
 * Copyright (C) 2009, Henon <meinrad.recheis@gmail.com>
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
using System.Text.RegularExpressions;
using GitSharp.Core.Util;

namespace GitSharp.Core.RevWalk.Filter
{
	/// <summary>
	/// Matches only commits whose author name matches the pattern.
	/// </summary>
	public static class AuthorRevFilter
	{
		///	<summary>
		/// Create a new author filter.
		///	<para />
		///	An optimized substring search may be automatically selected if the
		///	pattern does not contain any regular expression meta-characters.
		///	<para />
		///	The search is performed using a case-insensitive comparison. The
		///	character encoding of the commit message itself is not respected. The
		///	filter matches on raw UTF-8 byte sequences.
		///	</summary>
		///	<param name="pattern">Regular expression pattern to match.</param>
		///	<returns>
		/// A new filter that matches the given expression against the author
		/// name and address of a commit.
		/// </returns>
		public static RevFilter create(string pattern)
		{
			if (string.IsNullOrEmpty(pattern))
			{
				throw new ArgumentNullException("pattern", "Cannot match on empty string.");
			}

			if (SubStringRevFilter.safe(pattern))
			{
				return new SubStringSearch(pattern);
			}

			return new PatternSearch(pattern);
		}

		private static string TextFor(RevCommit cmit)
		{
			byte[] raw = cmit.RawBuffer;
			int b = RawParseUtils.author(raw, 0);
			if (b < 0) return string.Empty;
			int e = RawParseUtils.nextLF(raw, b, (byte)'>');
			return Constants.CHARSET.GetString(raw, b, e);
		}

		#region Nested Types

		private class PatternSearch : PatternMatchRevFilter
		{
			public PatternSearch(string patternText):  base(patternText, true, true, RegexOptions.IgnoreCase)
			{
			}

			protected override string text(RevCommit cmit)
			{
				return TextFor(cmit);
			}

			public override RevFilter Clone()
			{
				return new PatternSearch(Pattern);
			}
		}

		private class SubStringSearch : SubStringRevFilter
		{
			internal SubStringSearch(string patternText) : base(patternText)
			{
			}

			protected override string Text(RevCommit cmit)
			{
				return TextFor(cmit);
			}
		}

		#endregion

	}
}