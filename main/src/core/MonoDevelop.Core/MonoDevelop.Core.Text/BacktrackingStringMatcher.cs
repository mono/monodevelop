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
	class BacktrackingStringMatcher: StringMatcher
	{
		readonly string filterTextUpperCase;

		readonly bool[] filterTextLowerCaseTable;
		readonly bool[] filterIsNonLetter;
		readonly string filterText;

		int[] cachedResult;

		public BacktrackingStringMatcher (string filterText)
		{
			this.filterText = filterText ?? "";
			if (filterText != null) {
				filterTextLowerCaseTable = new bool[filterText.Length];
				filterIsNonLetter = new bool[filterText.Length];
				for (int i = 0; i < filterText.Length; i++) {
					filterTextLowerCaseTable[i] = char.IsLower (filterText[i]);
					filterIsNonLetter[i] = !char.IsLetter (filterText[i]);
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
				int caseMatches = 0;
				for (int n=0; n<lane.Length; n++)
					if (filterText[n] == name [lane[n]]) caseMatches++;
				matchRank = caseMatches * 10 - (lane[0] + (name.Length - filterTextUpperCase.Length));
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
			char filterChar = filterTextUpperCase[i];
			// filter char is no letter -> next char should match it - see Bug 674512 - Space doesn't commit generics
			if (filterIsNonLetter[i]) {
				if (filterChar == text[j])
					return j;
				return -1;
			}
			
			// letter case
			bool textCharIsUpper = char.IsUpper (text[j]);
			if (!onlyWordStart && filterChar == (textCharIsUpper ? text[j] : char.ToUpper (text[j]))) {
				// cases don't match. Filter is upper char & letter is low, now prefer the match that does the word skip.
				if (!(textCharIsUpper || filterTextLowerCaseTable[i]) && j + 1 < text.Length) {
					int possibleBetterResult = GetMatchChar (text, i, j + 1, onlyWordStart);
					if (possibleBetterResult >= 0)
						return possibleBetterResult;
				}
				return j;
			}
			
			// no match, try to continue match at the next word start
			j++;
			for (; j < text.Length; j++) {
				// word start is either a upper case letter (FooBar) or a char that follows a non letter
				// like foo:bar 
				if (char.IsUpper (text[j]) && filterChar == text[j] || 
					(filterChar == char.ToUpper (text[j]) && j > 0 && !char.IsLetterOrDigit (text[j - 1])))
					return j;
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
				return new int[0];
			if (string.IsNullOrEmpty (text))
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
			while (i < filterTextUpperCase.Length) {
				if (j >= text.Length) {
					if (i > 0) {
						j = result[--i] + 1;
						onlyWordStart = true;
						continue;
					}
					return null;
				}
				j = GetMatchChar (text, i, j, onlyWordStart);
				onlyWordStart = false;
				if (j == -1) {
					if (i > 0) {
						j = result[--i] + 1;
						onlyWordStart = true;
						continue;
					}
					return null;
				} else {
					result[i] = j++;
				}
				i++;
			}
			cachedResult = null;
			// clear cache
			return result;
		}
	}
}

