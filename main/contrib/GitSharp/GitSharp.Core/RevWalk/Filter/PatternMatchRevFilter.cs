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

using System.Text.RegularExpressions;
using System;
using System.Text;
using GitSharp.Core.Util;

namespace GitSharp.Core.RevWalk.Filter
{
	/// <summary>
	/// Abstract filter that searches text using extended regular expressions.
	/// </summary>
	public abstract class PatternMatchRevFilter : RevFilter
	{
		///	<summary>
		/// Encode a string pattern for faster matching on byte arrays.
		///	<para />
		///	Force the characters to our funny UTF-8 only convention that we use on
		///	raw buffers. This avoids needing to perform character set decodes on the
		///	individual commit buffers.
		///	</summary>
		///	<param name="patternText">
		///	original pattern string supplied by the user or the
		///	application.
		/// </param>
		///	<returns>
		/// Same pattern, but re-encoded to match our funny raw UTF-8
		///	character sequence <seealso cref="RawCharSequence"/>.
		/// </returns>
		private static string forceToRaw(string patternText) // [henon] I believe, such recoding is not necessary in C#, is it?
		{
		    byte[] b = Constants.encode(patternText);
			var needle = new StringBuilder(b.Length);
			for (int i = 0; i < b.Length; i++)
			{
				needle.Append((char)(b[i] & 0xff));
			}
			return needle.ToString();
		}

		private readonly string _patternText;
		private readonly Regex _compiledPattern;

		///	<summary>
		/// Construct a new pattern matching filter.
		///	</summary>
		///	<param name="pattern">
		///	Text of the pattern. Callers may want to surround their
		///	pattern with ".*" on either end to allow matching in the
		///	middle of the string.
		/// </param>
		///	<param name="innerString">
		///	Should .* be wrapped around the pattern of ^ and $ are
		///	missing? Most users will want this set.
		/// </param>
		///	<param name="rawEncoding">
		///	should <seealso cref="forceToRaw(String)"/> be applied to the pattern
		///	before compiling it?
		/// </param>
		///	<param name="flags">
		///	flags from <seealso cref="Pattern"/> to control how matching performs. </param>
		internal PatternMatchRevFilter(string pattern, bool innerString, bool rawEncoding, RegexOptions flags)
		{
			if (pattern.Length == 0)
			{
				throw new ArgumentException("Cannot match on empty string.");
			}

			_patternText = pattern;

			if (innerString)
			{
				if (!pattern.StartsWith("^") && !pattern.StartsWith(".*"))
				{
					pattern = ".*" + pattern;
				}
				if (!pattern.EndsWith("$") && !pattern.EndsWith(".*"))
				{
					pattern = pattern + ".*";
				}
			}
			string p = rawEncoding ? forceToRaw(pattern) : pattern;
			_compiledPattern = new Regex(p, flags); //.matcher("");
		}

		///	<summary>
		/// Get the pattern this filter uses.
		///	</summary>
		///	<returns>
		/// The pattern this filter is applying to candidate strings.
		/// </returns>
		protected string Pattern
		{
			get { return _patternText; }
		}

		public override bool include(RevWalk walker, RevCommit cmit)
		{
			return _compiledPattern.IsMatch(text(cmit));
		}

		///	<summary>
		/// Obtain the raw text to match against.
		///	</summary>
		///	<param name="cmit">Current commit being evaluated.</param>
		///	<returns>
		/// Sequence for the commit's content that we need to match on.
		/// </returns>
		protected abstract string text(RevCommit cmit); // [henon] changed returntype from CharSequence to string! we have no equivalent for CharSequences in C# and Regex works with strings only.

		public override string ToString()
		{
			return base.ToString() + "(\"" + _patternText + "\")";
		}
	}
}