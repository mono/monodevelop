/*
 * Copyright (C) 2008, Florian Köberle <florianskarten@web.de>
 * Copyright (C) 2009, Adriano Machado <adriano.m.machado@hotmail.com>
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

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using GitSharp.Core.Exceptions;
using GitSharp.Core.Util;

namespace GitSharp.Core.FnMatch
{
	/// <summary>
	/// This class can be used to match filenames against fnmatch like patterns. 
	/// It is not thread save.
	/// <para />
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
	/// </summary>
	public sealed class FileNameMatcher
	{
		internal static readonly IList<IHead> EmptyHeadList = new List<IHead>();
		private static readonly IList<AbstractHead> EmptyAbstractHeadList = new List<AbstractHead>();

		private const string CharacterClassStartPattern = "\\[[.:=]";

		private readonly IList<IHead> _headsStartValue;
		private List<IHead> _heads;

		///	<summary>
		/// <seealso cref="ExtendStringToMatchByOneCharacter"/> needs a list for the
		/// new heads, allocating a new array would be bad for the performance, as
		/// the method gets called very often.
		///	</summary>
		private List<IHead> _listForLocalUseage;

		///	<summary>
		/// </summary>
		///	<param name="headsStartValue">
		///	Must be a list which will never be modified.
		/// </param>
		private FileNameMatcher(IList<IHead> headsStartValue)
			: this(headsStartValue, headsStartValue)
		{
		}

		///	<summary>
		/// </summary>
		///	<param name="headsStartValue">must be a list which will never be modified.</param>
		///	<param name="heads">a list which will be cloned and then used as current head list. </param>
		private FileNameMatcher(IList<IHead> headsStartValue, ICollection<IHead> heads)
		{
			_headsStartValue = headsStartValue;
			_heads = new List<IHead>(heads.Count);
			_heads.AddRange(heads);
			_listForLocalUseage = new List<IHead>(heads.Count);
		}

		/// <summary>
		/// </summary>
		///	<param name="patternString">must contain a pattern which fnmatch would accept.</param>
		/// <param name="invalidWildgetCharacter">
		/// if this parameter isn't null then this character will not
		/// match at wildcards(* and ? are wildcards). 
		/// </param>
		///	<exception cref="InvalidPatternException">
		/// if the patternString contains a invalid fnmatch pattern.
		/// </exception>
		public FileNameMatcher(string patternString, char? invalidWildgetCharacter)
			: this(CreateHeadsStartValues(patternString, invalidWildgetCharacter))
		{
		}

		///	<summary>
		/// A copy constructor which creates a new <seealso cref="FileNameMatcher"/> with the
		/// same state and Reset point like <code>other</code>.
		/// </summary>
		/// <param name="other">
		/// another <seealso cref="FileNameMatcher"/> instance.
		/// </param>
		public FileNameMatcher(FileNameMatcher other)
			: this(other._headsStartValue, other._heads)
		{
			if (other == null)
				throw new System.ArgumentNullException ("other");
		}

		private static IList<IHead> CreateHeadsStartValues(string patternString, char? invalidWildgetCharacter)
		{
			IList<AbstractHead> allHeads = ParseHeads(patternString, invalidWildgetCharacter);

			IList<IHead> nextHeadsSuggestion = new List<IHead>(2) { LastHead.Instance };
			for (int i = allHeads.Count - 1; i >= 0; i--)
			{
				AbstractHead head = allHeads[i];

				// explanation:
				// a and * of the pattern "a*b"
				// need *b as newHeads
				// that's why * extends the list for it self and it's left neighbor.
				if (head.IsStar)
				{
					nextHeadsSuggestion.Add(head);
					head.setNewHeads(nextHeadsSuggestion);
				}
				else
				{
					head.setNewHeads(nextHeadsSuggestion);
					nextHeadsSuggestion = new List<IHead>(2) { head };
				}
			}

			return nextHeadsSuggestion;
		}

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

			int groupEnd = -1;
			do
			{
				int possibleGroupEnd = pattern.IndexOf(']', firstValidEndBracketIndex);
				if (possibleGroupEnd == -1)
				{
					throw new NoClosingBracketException(indexOfStartBracket, "[", "]", pattern);
				}

				Match charClassStartMatch = Regex.Match(pattern.Substring(firstValidCharClassIndex), CharacterClassStartPattern);

				bool foundCharClass = charClassStartMatch.Success;
				int charClassStartMatchIndex = charClassStartMatch.Index + firstValidCharClassIndex;

				if (foundCharClass && charClassStartMatchIndex < possibleGroupEnd)
				{
					string classStart = charClassStartMatch.Groups[0].Value;
					string classEnd = classStart[1] + "]";

					int classStartIndex = charClassStartMatchIndex;
					int classEndIndex = pattern.IndexOf(classEnd, classStartIndex + 2);

					if (classEndIndex == -1)
					{
						throw new NoClosingBracketException(classStartIndex, classStart, classEnd, pattern);
					}

					firstValidCharClassIndex = classEndIndex + 2;
					firstValidEndBracketIndex = firstValidCharClassIndex;
				}
				else
				{
					groupEnd = possibleGroupEnd;
				}
			} while (groupEnd == -1);

			return groupEnd;
		}

		private static IList<AbstractHead> ParseHeads(string pattern, char? invalidWildgetCharacter)
		{
			if (string.IsNullOrEmpty(pattern))
			{
				return EmptyAbstractHeadList;
			}

			var heads = new List<AbstractHead>();
			int currentIndex = 0;

            while (currentIndex < pattern.Length)
			{
				int groupStart = pattern.IndexOf('[', currentIndex);
				if (groupStart == -1)
				{
					string patternPart = pattern.Substring(currentIndex);
					heads.AddRange(CreateSimpleHeads(patternPart, invalidWildgetCharacter));
					currentIndex = pattern.Length;
				}
				else
				{
					string patternPart = pattern.Slice(currentIndex, groupStart);
					heads.AddRange(CreateSimpleHeads(patternPart, invalidWildgetCharacter));

                    int groupEnd = FindGroupEnd(groupStart, pattern);
					string groupPart = pattern.Slice(groupStart + 1, groupEnd);
					heads.Add(new GroupHead(groupPart, pattern));
					currentIndex = groupEnd + 1;
				}
			}

			return heads;
		}

		private static IList<AbstractHead> CreateSimpleHeads(string patternPart, char? invalidWildgetCharacter)
		{
			IList<AbstractHead> heads = new List<AbstractHead>(patternPart.Length);

			for (int i = 0; i < patternPart.Length; i++)
			{
				AbstractHead head;

				char c = patternPart[i];
				switch (c)
				{
					case '*':
						head = CreateWildCardHead(invalidWildgetCharacter, true);
						heads.Add(head);
						break;

					case '?':
						head = CreateWildCardHead(invalidWildgetCharacter, false);
						heads.Add(head);
						break;

					default:
						head = new CharacterHead(c);
						heads.Add(head);
						break;
				}
			}

			return heads;
		}

		private static AbstractHead CreateWildCardHead(char? invalidWildgetCharacter, bool star)
		{
			if (invalidWildgetCharacter != null)
			{
				return new RestrictedWildCardHead(invalidWildgetCharacter.Value, star);
			}

			return new WildCardHead(star);
		}

		private void ExtendStringToMatchByOneCharacter(char c)
		{
			List<IHead> newHeads = _listForLocalUseage;
			newHeads.Clear();
			List<IHead> lastAddedHeads = null;
			for (int i = 0; i < _heads.Count; i++)
			{
				IHead head = _heads[i];
				IList<IHead> headsToAdd = head.GetNextHeads(c);

				// Why the next performance optimization isn't wrong:
				// Some times two heads return the very same list.
				// We save future effort if we don't add these heads again.
				// This is the case with the heads "a" and "*" of "a*b" which
				// both can return the list ["*","b"]
				if (headsToAdd != lastAddedHeads)
				{
					newHeads.AddRange(headsToAdd);
					lastAddedHeads = new List<IHead>(headsToAdd);
				}
			}

			_listForLocalUseage = _heads;
			_heads = newHeads;
		}

		///	<summary>
		/// </summary>
		///	<param name="stringToMatch">
		/// Extends the string which is matched against the patterns of this class.
		///  </param>
		public void Append(string stringToMatch)
		{
			if (stringToMatch == null)
				throw new System.ArgumentNullException ("stringToMatch");
			
			for (int i = 0; i < stringToMatch.Length; i++)
			{
				char c = stringToMatch[i];
				ExtendStringToMatchByOneCharacter(c);
			}
		}

		///	<summary>
		/// Resets this matcher to it's state right After construction.
		/// </summary>
		public void Reset()
		{
			_heads.Clear();
			_heads.AddRange(_headsStartValue);
		}

		///	<summary>
		/// </summary>
		///	<returns>
		/// A <seealso cref="FileNameMatcher"/> instance which uses the same pattern
		///	like this matcher, but has the current state of this matcher as
		///	Reset and start point.
		/// </returns>
		public FileNameMatcher CreateMatcherForSuffix()
		{
			var copyOfHeads = new List<IHead>(_heads.Count);
			copyOfHeads.AddRange(_heads);
			return new FileNameMatcher(copyOfHeads);
		}

		///	<summary>
		/// </summary>
		///	<returns>
		/// True, if the string currently being matched does match.
		/// </returns>
		public bool IsMatch()
		{
			var reverseList = Enumerable.Reverse(_heads);

			foreach (IHead head in reverseList)
			{
				if (ReferenceEquals(head, LastHead.Instance))
				{
					return true;
				}
			}

			return false;
		}

		///	<summary>
		/// </summary>
		///	<returns>
		/// False, if the string being matched will not match when the string gets extended.
		/// </returns>
		public bool CanAppendMatch()
		{
			for (int i = 0; i < _heads.Count; i++)
			{
				if (!ReferenceEquals(_heads[i], LastHead.Instance))
				{
					return true;
				}
			}

			return false;
		}
	}
}