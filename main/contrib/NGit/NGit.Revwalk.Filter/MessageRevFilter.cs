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
using NGit.Revwalk;
using NGit.Revwalk.Filter;
using NGit.Util;
using Sharpen;

namespace NGit.Revwalk.Filter
{
	/// <summary>Matches only commits whose message matches the pattern.</summary>
	/// <remarks>Matches only commits whose message matches the pattern.</remarks>
	public class MessageRevFilter
	{
		/// <summary>Create a message filter.</summary>
		/// <remarks>
		/// Create a message filter.
		/// <p>
		/// An optimized substring search may be automatically selected if the
		/// pattern does not contain any regular expression meta-characters.
		/// <p>
		/// The search is performed using a case-insensitive comparison. The
		/// character encoding of the commit message itself is not respected. The
		/// filter matches on raw UTF-8 byte sequences.
		/// </remarks>
		/// <param name="pattern">regular expression pattern to match.</param>
		/// <returns>
		/// a new filter that matches the given expression against the
		/// message body of the commit.
		/// </returns>
		public static RevFilter Create(string pattern)
		{
			if (pattern.Length == 0)
			{
				throw new ArgumentException(JGitText.Get().cannotMatchOnEmptyString);
			}
			if (SubStringRevFilter.Safe(pattern))
			{
				return new MessageRevFilter.SubStringSearch(pattern);
			}
			return new MessageRevFilter.PatternSearch(pattern);
		}

		public MessageRevFilter()
		{
		}

		// Don't permit us to be created.
		internal static RawCharSequence TextFor(RevCommit cmit)
		{
			byte[] raw = cmit.RawBuffer;
			int b = RawParseUtils.CommitMessage(raw, 0);
			if (b < 0)
			{
				return RawCharSequence.EMPTY;
			}
			return new RawCharSequence(raw, b, raw.Length);
		}

		private class PatternSearch : PatternMatchRevFilter
		{
			internal PatternSearch(string patternText) : base(patternText, true, true, Sharpen.Pattern
				.CASE_INSENSITIVE | Sharpen.Pattern.DOTALL)
			{
			}

			protected internal override CharSequence Text(RevCommit cmit)
			{
				return TextFor(cmit);
			}

			public override RevFilter Clone()
			{
				return new MessageRevFilter.PatternSearch(Pattern());
			}
		}

		private class SubStringSearch : SubStringRevFilter
		{
			protected internal SubStringSearch(string patternText) : base(patternText)
			{
			}

			protected internal override RawCharSequence Text(RevCommit cmit)
			{
				return TextFor(cmit);
			}
		}
	}
}
