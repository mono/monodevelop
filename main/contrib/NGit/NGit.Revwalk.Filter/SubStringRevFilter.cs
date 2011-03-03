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

using NGit.Revwalk;
using NGit.Revwalk.Filter;
using NGit.Util;
using Sharpen;

namespace NGit.Revwalk.Filter
{
	/// <summary>Abstract filter that searches text using only substring search.</summary>
	/// <remarks>Abstract filter that searches text using only substring search.</remarks>
	public abstract class SubStringRevFilter : RevFilter
	{
		/// <summary>Can this string be safely handled by a substring filter?</summary>
		/// <param name="pattern">the pattern text proposed by the user.</param>
		/// <returns>
		/// true if a substring filter can perform this pattern match; false
		/// if
		/// <see cref="PatternMatchRevFilter">PatternMatchRevFilter</see>
		/// must be used instead.
		/// </returns>
		public static bool Safe(string pattern)
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
					{
						return false;
					}
				}
			}
			return true;
		}

		private readonly RawSubStringPattern pattern;

		/// <summary>Construct a new matching filter.</summary>
		/// <remarks>Construct a new matching filter.</remarks>
		/// <param name="patternText">
		/// text to locate. This should be a safe string as described by
		/// the
		/// <see cref="Safe(string)">Safe(string)</see>
		/// as regular expression meta
		/// characters are treated as literals.
		/// </param>
		protected internal SubStringRevFilter(string patternText)
		{
			pattern = new RawSubStringPattern(patternText);
		}

		/// <exception cref="NGit.Errors.MissingObjectException"></exception>
		/// <exception cref="NGit.Errors.IncorrectObjectTypeException"></exception>
		/// <exception cref="System.IO.IOException"></exception>
		public override bool Include(RevWalk walker, RevCommit cmit)
		{
			return pattern.Match(Text(cmit)) >= 0;
		}

		public override bool RequiresCommitBody()
		{
			return true;
		}

		/// <summary>Obtain the raw text to match against.</summary>
		/// <remarks>Obtain the raw text to match against.</remarks>
		/// <param name="cmit">current commit being evaluated.</param>
		/// <returns>sequence for the commit's content that we need to match on.</returns>
		protected internal abstract RawCharSequence Text(RevCommit cmit);

		public override RevFilter Clone()
		{
			return this;
		}

		// Typically we are actually thread-safe.
		public override string ToString()
		{
			return base.ToString() + "(\"" + pattern.Pattern() + "\")";
		}
	}
}
