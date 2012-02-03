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
using System.Text;
using NGit;
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using Sharpen;

namespace NGit.Revwalk.Filter
{
	/// <summary>Abstract filter that searches text using extended regular expressions.</summary>
	/// <remarks>Abstract filter that searches text using extended regular expressions.</remarks>
	public abstract class PatternMatchRevFilter : RevFilter
	{
		/// <summary>Encode a string pattern for faster matching on byte arrays.</summary>
		/// <remarks>
		/// Encode a string pattern for faster matching on byte arrays.
		/// <p>
		/// Force the characters to our funny UTF-8 only convention that we use on
		/// raw buffers. This avoids needing to perform character set decodes on the
		/// individual commit buffers.
		/// </remarks>
		/// <param name="patternText">
		/// original pattern string supplied by the user or the
		/// application.
		/// </param>
		/// <returns>
		/// same pattern, but re-encoded to match our funny raw UTF-8
		/// character sequence
		/// <see cref="NGit.Util.RawCharSequence">NGit.Util.RawCharSequence</see>
		/// .
		/// </returns>
		protected internal static string ForceToRaw(string patternText)
		{
			byte[] b = Constants.Encode(patternText);
			StringBuilder needle = new StringBuilder(b.Length);
			for (int i = 0; i < b.Length; i++)
			{
				needle.Append((char)(b[i] & unchecked((int)(0xff))));
			}
			return needle.ToString();
		}

		private readonly string patternText;

		private readonly Matcher compiledPattern;

		/// <summary>Construct a new pattern matching filter.</summary>
		/// <remarks>Construct a new pattern matching filter.</remarks>
		/// <param name="pattern">
		/// text of the pattern. Callers may want to surround their
		/// pattern with ".*" on either end to allow matching in the
		/// middle of the string.
		/// </param>
		/// <param name="innerString">
		/// should .* be wrapped around the pattern of ^ and $ are
		/// missing? Most users will want this set.
		/// </param>
		/// <param name="rawEncoding">
		/// should
		/// <see cref="ForceToRaw(string)">ForceToRaw(string)</see>
		/// be applied to the pattern
		/// before compiling it?
		/// </param>
		/// <param name="flags">
		/// flags from
		/// <see cref="Sharpen.Pattern">Sharpen.Pattern</see>
		/// to control how matching performs.
		/// </param>
		protected internal PatternMatchRevFilter(string pattern, bool innerString, bool rawEncoding
			, int flags)
		{
			if (pattern.Length == 0)
			{
				throw new ArgumentException(JGitText.Get().cannotMatchOnEmptyString);
			}
			patternText = pattern;
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
			string p = rawEncoding ? ForceToRaw(pattern) : pattern;
			compiledPattern = Sharpen.Pattern.Compile(p, flags).Matcher(string.Empty);
		}

		/// <summary>Get the pattern this filter uses.</summary>
		/// <remarks>Get the pattern this filter uses.</remarks>
		/// <returns>the pattern this filter is applying to candidate strings.</returns>
		public virtual string Pattern()
		{
			return patternText;
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool Include(RevWalk walker, RevCommit cmit)
		{
			return compiledPattern.Reset(Text(cmit)).Matches();
		}

		public override bool RequiresCommitBody()
		{
			return true;
		}

		/// <summary>Obtain the raw text to match against.</summary>
		/// <remarks>Obtain the raw text to match against.</remarks>
		/// <param name="cmit">current commit being evaluated.</param>
		/// <returns>sequence for the commit's content that we need to match on.</returns>
		protected internal abstract CharSequence Text(RevCommit cmit);

		public override string ToString()
		{
			return base.ToString() + "(\"" + patternText + "\")";
		}
	}
}
