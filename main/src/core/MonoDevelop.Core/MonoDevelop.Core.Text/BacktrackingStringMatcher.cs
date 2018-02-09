// 
// BacktrackingStringMatcher.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
//       Andrea Krüger <andrea@shakuras.homeunix.net>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Collections.Generic;

namespace MonoDevelop.Core.Text
{
	class BacktrackingStringMatcher : StringMatcher
	{
		readonly string filterTextUpperCase;
		readonly ulong filterTextLowerCaseTable;
		readonly ulong filterIsNonLetter;
		readonly ulong filterIsDigit;
		readonly string filterText;
		int[] cachedResult;

		public override StringMatcher Clone ()
		{
			var clone = (BacktrackingStringMatcher)base.Clone ();

			// Don't reuse the results buffer for the clone
			clone.cachedResult = null;
			return clone;
		}

		public BacktrackingStringMatcher (string filterText)
		{
			this.filterText = filterText ?? "";
			if (filterText != null) {
				for (int i = 0; i < filterText.Length && i < 64; i++) {
					filterTextLowerCaseTable |= char.IsLower (filterText [i]) ? 1ul << i : 0;
					filterIsNonLetter |= !char.IsLetterOrDigit (filterText [i]) ? 1ul << i : 0;
					filterIsDigit |= char.IsDigit (filterText [i]) ? 1ul << i : 0;
				}

				filterTextUpperCase = filterText.ToUpper ();
			} else {
				filterTextUpperCase = "";
			}
		}

		public override bool CalcMatchRank (string name, out int matchRank)
		{
			if (filterTextUpperCase.Length == 0) {
				matchRank = int.MinValue;
				return true;
			}
			var lane = GetMatch (name);
			if (lane != null) {
				if (name.Length == filterText.Length) {
					matchRank = int.MaxValue;
					for (int n = 0; n < name.Length; n++) {
						if (filterText[n] != name[n])
							matchRank--;
					}
					return true;
				}
				// exact named parameter case see discussion in bug #9114
				if (name.Length - 1  == filterText.Length && name[name.Length - 1] == ':') {
					matchRank = int.MaxValue - 1;
					for (int n = 0; n < name.Length - 1; n++) {
						if (filterText[n] != name[n])
							matchRank--;
					}
					return true;
				}
				int capitalMatches = 0;
				int nonCapitalMatches = 0;
				int matching = 0;
				int fragments = 0;
				int lastIndex = -1;
				for (int n = 0; n < lane.Length; n++) {
					var ch = filterText [n];
					var i = lane [n];
					bool newFragment = i > lastIndex + 1;
					if (newFragment)
						fragments++;
					lastIndex = i;
					if (ch == name [i]) {
						matching += 1000 / (1 + fragments);
						if (char.IsUpper (ch))
							capitalMatches += Math.Max (1, 10000 - 1000 * fragments);
					} else if (newFragment || i == 0) {
						matching += 900 / (1 + fragments);
						if (char.IsUpper (ch))
							capitalMatches += Math.Max (1, 1000 - 100 * fragments);
					} else {
						var x = 600  / (1 + fragments);
						nonCapitalMatches += x;
					}
				}
				matchRank = capitalMatches + matching - fragments + nonCapitalMatches + filterText.Length - name.Length;
				// devalue named parameters.
				if (name[name.Length - 1] == ':')
					matchRank /= 2;
				return true;
			}
			matchRank = int.MinValue;
			return false;
		}

		public override bool IsMatch (string text)
		{
			int[] match = GetMatch (text);
			// no need to clear the cache
			cachedResult = cachedResult ?? match;
			return match != null;
		}

		int GetMatchChar (string text, int i, int j, bool onlyWordStart)
		{
			char filterChar = filterTextUpperCase [i];
			char ch;
			// filter char is no letter -> next char should match it - see Bug 674512 - Space doesn't commit generics
			var flag = 1ul << i;
			if ((filterIsNonLetter & flag) != 0) {
				for (; j < text.Length; j++) {
					if (filterChar == text [j])
						return j;
				}
				return -1;
			}
			// letter case
			ch = text [j];
			bool textCharIsUpper = char.IsUpper (ch);
			if (!onlyWordStart) {
				if (filterChar == (textCharIsUpper ? ch : char.ToUpper (ch)) && char.IsLetter (ch)) {
					// cases don't match. Filter is upper char & letter is low, now prefer the match that does the word skip.
					if (!(textCharIsUpper || (filterTextLowerCaseTable & flag) != 0) && j + 1 < text.Length) {
						// Since we are looking for a char match that does the word skip, use onlyWordStart=true
						int possibleBetterResult = GetMatchChar (text, i, j + 1, onlyWordStart:true);
						if (possibleBetterResult >= 0)
							return possibleBetterResult;
					}
					return j;
				}
			} else {
				if (textCharIsUpper && filterChar == ch && char.IsLetter (ch)) {
					return j;
				}
			}

			// no match, try to continue match at the next word start
			bool lastWasLower = false;
			bool lastWasUpper = false;
			int wordStart = j + 1;
			for (; j < text.Length; j++) {
				// word start is either a upper case letter (FooBar) or a char that follows a non letter
				// like foo:bar 
				ch = text [j];
				var category = char.GetUnicodeCategory (ch);
				if (category == System.Globalization.UnicodeCategory.LowercaseLetter) {
					if (lastWasUpper && (j - wordStart) > 0) {
						if (filterChar == char.ToUpper (text [j - 1]))
							return j - 1;
					}
					lastWasLower = true;
					lastWasUpper = false;
				} else if (category == System.Globalization.UnicodeCategory.UppercaseLetter) {
					if (lastWasLower) {
						if (filterChar == char.ToUpper (ch))
							return j;
					}
					lastWasLower = false;
					lastWasUpper = true;
				} else {
					if (filterChar == ch)
						return j;
					if (j + 1 < text.Length && filterChar == char.ToUpper (text [j + 1]))
						return j + 1;
					lastWasLower = lastWasUpper = false;
				} 
			}
			return -1;
		}

		/// <summary>
		/// Gets the match indices.
		/// </summary>
		/// <returns>
		/// The indices in the text which are matched by our filter.
		/// </returns>
		/// <param name='text'>
		/// The text to match.
		/// </param>
		public override int[] GetMatch (string text)
		{
			if (string.IsNullOrEmpty (filterTextUpperCase))
				return Array.Empty<int> ();
			if (string.IsNullOrEmpty (text) || filterText.Length  > text.Length)
				return null;
			int[] result;
			if (cachedResult != null) {
				result = cachedResult;
			} else {
				cachedResult = result = new int[filterTextUpperCase.Length];
			}
			int j = 0;
			int i = 0;
			bool onlyWordStart = false;
			while (i < filterText.Length) {
				if (j >= text.Length) {
					if (i > 0) {
						j = result [--i] + 1;
						onlyWordStart = true;
						continue;
					}
					return null;
				}

				j = GetMatchChar (text, i, j, onlyWordStart);
				onlyWordStart = false;
				if (j == -1) {
					if (i > 0) {
						j = result [--i] + 1;
						onlyWordStart = true;
						continue;
					}
					return null;
				} else {
					result [i] = j++;
				}
				i++;
			}
			cachedResult = null;
			// clear cache
			return result;
		}
	}
}

