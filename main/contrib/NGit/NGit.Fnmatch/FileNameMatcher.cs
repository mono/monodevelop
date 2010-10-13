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
using NGit.Errors;
using NGit.Fnmatch;
using Sharpen;

namespace NGit.Fnmatch
{
	/// <summary>This class can be used to match filenames against fnmatch like patterns.
	/// 	</summary>
	/// <remarks>
	/// This class can be used to match filenames against fnmatch like patterns. It
	/// is not thread save.
	/// <p>
	/// Supported are the wildcard characters * and ? and groups with:
	/// <ul>
	/// <li> characters e.g. [abc]</li>
	/// <li> ranges e.g. [a-z]</li>
	/// <li> the following character classes
	/// <ul>
	/// <li>[:alnum:]</li>
	/// <li>[:alpha:]</li>
	/// <li>[:blank:]</li>
	/// <li>[:cntrl:]</li>
	/// <li>[:digit:]</li>
	/// <li>[:graph:]</li>
	/// <li>[:lower:]</li>
	/// <li>[:print:]</li>
	/// <li>[:punct:]</li>
	/// <li>[:space:]</li>
	/// <li>[:upper:]</li>
	/// <li>[:word:]</li>
	/// <li>[:xdigit:]</li>
	/// </ul>
	/// e. g. [[:xdigit:]] </li>
	/// </ul>
	/// </p>
	/// </remarks>
	public class FileNameMatcher
	{
		internal static readonly IList<Head> EMPTY_HEAD_LIST = Sharpen.Collections.EmptyList<Head>
			();

		private static readonly Sharpen.Pattern characterClassStartPattern = Sharpen.Pattern
			.Compile("\\[[.:=]");

		private IList<Head> headsStartValue;

		private IList<Head> heads;

		/// <summary>
		/// {
		/// <see cref="ExtendStringToMatchByOneCharacter(char)">ExtendStringToMatchByOneCharacter(char)
		/// 	</see>
		/// needs a list for the
		/// new heads, allocating a new array would be bad for the performance, as
		/// the method gets called very often.
		/// </summary>
		private IList<Head> listForLocalUseage;

		/// <param name="headsStartValue">must be a list which will never be modified.</param>
		private FileNameMatcher(IList<Head> headsStartValue) : this(headsStartValue, headsStartValue
			)
		{
		}

		/// <param name="headsStartValue">must be a list which will never be modified.</param>
		/// <param name="heads">
		/// a list which will be cloned and then used as current head
		/// list.
		/// </param>
		private FileNameMatcher(IList<Head> headsStartValue, IList<Head> heads)
		{
			this.headsStartValue = headsStartValue;
			this.heads = new AList<Head>(heads.Count);
			Sharpen.Collections.AddAll(this.heads, heads);
			this.listForLocalUseage = new AList<Head>(heads.Count);
		}

		/// <param name="patternString">must contain a pattern which fnmatch would accept.</param>
		/// <param name="invalidWildgetCharacter">
		/// if this parameter isn't null then this character will not
		/// match at wildcards(* and ? are wildcards).
		/// </param>
		/// <exception cref="NGit.Errors.InvalidPatternException">if the patternString contains a invalid fnmatch pattern.
		/// 	</exception>
		public FileNameMatcher(string patternString, char? invalidWildgetCharacter) : this
			(CreateHeadsStartValues(patternString, invalidWildgetCharacter))
		{
		}

		/// <summary>
		/// A Copy Constructor which creates a new
		/// <see cref="FileNameMatcher">FileNameMatcher</see>
		/// with the
		/// same state and reset point like <code>other</code>.
		/// </summary>
		/// <param name="other">
		/// another
		/// <see cref="FileNameMatcher">FileNameMatcher</see>
		/// instance.
		/// </param>
		public FileNameMatcher(NGit.Fnmatch.FileNameMatcher other) : this(other.headsStartValue
			, other.heads)
		{
		}

		/// <exception cref="NGit.Errors.InvalidPatternException"></exception>
		private static IList<Head> CreateHeadsStartValues(string patternString, char? invalidWildgetCharacter
			)
		{
			IList<AbstractHead> allHeads = ParseHeads(patternString, invalidWildgetCharacter);
			IList<Head> nextHeadsSuggestion = new AList<Head>(2);
			nextHeadsSuggestion.AddItem(LastHead.INSTANCE);
			for (int i = allHeads.Count - 1; i >= 0; i--)
			{
				AbstractHead head = allHeads[i];
				// explanation:
				// a and * of the pattern "a*b"
				// need *b as newHeads
				// that's why * extends the list for it self and it's left neighbor.
				if (head.IsStar())
				{
					nextHeadsSuggestion.AddItem(head);
					head.SetNewHeads(nextHeadsSuggestion);
				}
				else
				{
					head.SetNewHeads(nextHeadsSuggestion);
					nextHeadsSuggestion = new AList<Head>(2);
					nextHeadsSuggestion.AddItem(head);
				}
			}
			return nextHeadsSuggestion;
		}

		/// <exception cref="NGit.Errors.InvalidPatternException"></exception>
		private static int FindGroupEnd(int indexOfStartBracket, string pattern)
		{
			int firstValidCharClassIndex = indexOfStartBracket + 1;
			int firstValidEndBracketIndex = indexOfStartBracket + 2;
			if (indexOfStartBracket + 1 >= pattern.Length)
			{
				throw new NoClosingBracketException(indexOfStartBracket, "[", "]", pattern);
			}
			if (pattern[firstValidCharClassIndex] == '!')
			{
				firstValidCharClassIndex++;
				firstValidEndBracketIndex++;
			}
			Matcher charClassStartMatcher = characterClassStartPattern.Matcher(pattern);
			int groupEnd = -1;
			while (groupEnd == -1)
			{
				int possibleGroupEnd = pattern.IndexOf(']', firstValidEndBracketIndex);
				if (possibleGroupEnd == -1)
				{
					throw new NoClosingBracketException(indexOfStartBracket, "[", "]", pattern);
				}
				bool foundCharClass = charClassStartMatcher.Find(firstValidCharClassIndex);
				if (foundCharClass && charClassStartMatcher.Start() < possibleGroupEnd)
				{
					string classStart = charClassStartMatcher.Group(0);
					string classEnd = classStart[1] + "]";
					int classStartIndex = charClassStartMatcher.Start();
					int classEndIndex = pattern.IndexOf(classEnd, classStartIndex + 2);
					if (classEndIndex == -1)
					{
						throw new NoClosingBracketException(classStartIndex, classStart, classEnd, pattern
							);
					}
					firstValidCharClassIndex = classEndIndex + 2;
					firstValidEndBracketIndex = firstValidCharClassIndex;
				}
				else
				{
					groupEnd = possibleGroupEnd;
				}
			}
			return groupEnd;
		}

		/// <exception cref="NGit.Errors.InvalidPatternException"></exception>
		private static IList<AbstractHead> ParseHeads(string pattern, char? invalidWildgetCharacter
			)
		{
			int currentIndex = 0;
			IList<AbstractHead> heads = new AList<AbstractHead>();
			while (currentIndex < pattern.Length)
			{
				int groupStart = pattern.IndexOf('[', currentIndex);
				if (groupStart == -1)
				{
					string patternPart = Sharpen.Runtime.Substring(pattern, currentIndex);
					Sharpen.Collections.AddAll(heads, CreateSimpleHeads(patternPart, invalidWildgetCharacter
						));
					currentIndex = pattern.Length;
				}
				else
				{
					string patternPart = Sharpen.Runtime.Substring(pattern, currentIndex, groupStart);
					Sharpen.Collections.AddAll(heads, CreateSimpleHeads(patternPart, invalidWildgetCharacter
						));
					int groupEnd = FindGroupEnd(groupStart, pattern);
					string groupPart = Sharpen.Runtime.Substring(pattern, groupStart + 1, groupEnd);
					heads.AddItem(new GroupHead(groupPart, pattern));
					currentIndex = groupEnd + 1;
				}
			}
			return heads;
		}

		private static IList<AbstractHead> CreateSimpleHeads(string patternPart, char? invalidWildgetCharacter
			)
		{
			IList<AbstractHead> heads = new AList<AbstractHead>(patternPart.Length);
			for (int i = 0; i < patternPart.Length; i++)
			{
				char c = patternPart[i];
				switch (c)
				{
					case '*':
					{
						AbstractHead head = CreateWildCardHead(invalidWildgetCharacter, true);
						heads.AddItem(head);
						break;
					}

					case '?':
					{
						AbstractHead head = CreateWildCardHead(invalidWildgetCharacter, false);
						heads.AddItem(head);
						break;
					}

					default:
					{
						CharacterHead head_1 = new CharacterHead(c);
						heads.AddItem(head_1);
						break;
					}
				}
			}
			return heads;
		}

		private static AbstractHead CreateWildCardHead(char? invalidWildgetCharacter, bool
			 star)
		{
			if (invalidWildgetCharacter != null)
			{
				return new RestrictedWildCardHead(invalidWildgetCharacter.Value, star);
			}
			else
			{
				return new WildCardHead(star);
			}
		}

		private void ExtendStringToMatchByOneCharacter(char c)
		{
			IList<Head> newHeads = listForLocalUseage;
			newHeads.Clear();
			IList<Head> lastAddedHeads = null;
			for (int i = 0; i < heads.Count; i++)
			{
				Head head = heads[i];
				IList<Head> headsToAdd = head.GetNextHeads(c);
				// Why the next performance optimization isn't wrong:
				// Some times two heads return the very same list.
				// We save future effort if we don't add these heads again.
				// This is the case with the heads "a" and "*" of "a*b" which
				// both can return the list ["*","b"]
				if (headsToAdd != lastAddedHeads)
				{
					Sharpen.Collections.AddAll(newHeads, headsToAdd);
					lastAddedHeads = headsToAdd;
				}
			}
			listForLocalUseage = heads;
			heads = newHeads;
		}

		/// <param name="stringToMatch">
		/// extends the string which is matched against the patterns of
		/// this class.
		/// </param>
		public virtual void Append(string stringToMatch)
		{
			for (int i = 0; i < stringToMatch.Length; i++)
			{
				char c = stringToMatch[i];
				ExtendStringToMatchByOneCharacter(c);
			}
		}

		/// <summary>Resets this matcher to it's state right after construction.</summary>
		/// <remarks>Resets this matcher to it's state right after construction.</remarks>
		public virtual void Reset()
		{
			heads.Clear();
			Sharpen.Collections.AddAll(heads, headsStartValue);
		}

		/// <returns>
		/// a
		/// <see cref="FileNameMatcher">FileNameMatcher</see>
		/// instance which uses the same pattern
		/// like this matcher, but has the current state of this matcher as
		/// reset and start point.
		/// </returns>
		public virtual NGit.Fnmatch.FileNameMatcher CreateMatcherForSuffix()
		{
			IList<Head> copyOfHeads = new AList<Head>(heads.Count);
			Sharpen.Collections.AddAll(copyOfHeads, heads);
			return new NGit.Fnmatch.FileNameMatcher(copyOfHeads);
		}

		/// <returns>true, if the string currently being matched does match.</returns>
		public virtual bool IsMatch()
		{
			ListIterator<Head> headIterator = heads.ListIterator(heads.Count);
			while (headIterator.HasPrevious())
			{
				Head head = headIterator.Previous();
				if (head == LastHead.INSTANCE)
				{
					return true;
				}
			}
			return false;
		}

		/// <returns>
		/// false, if the string being matched will not match when the string
		/// gets extended.
		/// </returns>
		public virtual bool CanAppendMatch()
		{
			for (int i = 0; i < heads.Count; i++)
			{
				if (heads[i] != LastHead.INSTANCE)
				{
					return true;
				}
			}
			return false;
		}
	}
}
