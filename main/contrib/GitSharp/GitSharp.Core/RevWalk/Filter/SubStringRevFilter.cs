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

namespace GitSharp.Core.RevWalk.Filter
{
	/// <summary>
	/// Abstract filter that searches text using only substring search.
	/// </summary>
	public abstract class SubStringRevFilter : RevFilter
	{
		///	<summary>
		/// Can this string be safely handled by a substring filter?
		///	</summary>
		///	<param name="pattern">
		///	the pattern text proposed by the user.
		/// </param>
		///	<returns>
		/// True if a substring filter can perform this pattern match; false
		/// if <seealso cref="PatternMatchRevFilter"/> must be used instead.
		/// </returns>
		public static bool safe(string pattern)
		{
			for (int i = 0; i < pattern.Length; i++)
			{
				char c = pattern[i];
				switch (c)
				{
					case '.':
					case '?':
					case '*':
					case '+':
					case '{':
					case '}':
					case '(':
					case ')':
					case '[':
					case ']':
					case '\\':
						return false;
				}
			}
			return true;
		}

		private readonly string _patternText;
		private readonly Regex _pattern;

		///	<summary>
		/// Construct a new matching filter.
		///	</summary>
		///	<param name="patternText">
		///	text to locate. This should be a safe string as described by
		///	the <seealso cref="safe(string)"/> as regular expression meta
		///	characters are treated as literals.
		/// </param>
		internal SubStringRevFilter(string patternText)
		{
			if (string.IsNullOrEmpty(patternText))
			{
				throw new ArgumentNullException("patternText");
			}

			_patternText = patternText;
			_pattern = new Regex(patternText);
		}

		public override bool include(RevWalk walker, RevCommit cmit)
		{
			return _pattern.IsMatch(Text(cmit));
		}

		///	<summary>
		/// Obtain the raw text to match against.
		///	</summary>
		///	<param name="cmit">Current commit being evaluated.</param>
		///	<returns>
		/// Sequence for the commit's content that we need to match on.
		/// </returns>
		protected abstract string Text(RevCommit cmit);

		public override RevFilter Clone()
		{
			return this; // Typically we are actually thread-safe.
		}

		public override string ToString()
		{
			return base.ToString() + "(\"" + _patternText + "\")";
		}
	}
}