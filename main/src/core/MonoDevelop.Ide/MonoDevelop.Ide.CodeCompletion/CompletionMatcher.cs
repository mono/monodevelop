// 
// CompletionMatcher.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
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

namespace MonoDevelop.Ide.CodeCompletion
{
	/// <summary>
	/// A class for computing sub word matches (ex. WL matches WriteLine).
	/// </summary>
	class CompletionMatcher
	{
		readonly string filterTextUpperCase;

		readonly bool[] filterTextLowerCaseTable;
		readonly bool[] filterIsNonLetter;

		readonly List<int> matchIndices;

		public CompletionMatcher (string filterText)
		{
			matchIndices = new List<int> ();
			if (filterText != null) {
				filterTextLowerCaseTable = new bool[filterText.Length];
				filterIsNonLetter        = new bool[filterText.Length];
				for (int  i = 0; i < filterText.Length; i++) {
					filterTextLowerCaseTable[i] = char.IsLower (filterText[i]);
					filterIsNonLetter[i] = !char.IsLetter (filterText[i]);
				}
				
				filterTextUpperCase = filterText.ToUpper ();
			}
		}

		public bool IsMatch (string text)
		{
			return GetMatch (text) != null;
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
		public int[] GetMatch (string text)
		{
			if (string.IsNullOrEmpty (filterTextUpperCase))
				return new int[0];
			if (string.IsNullOrEmpty (text))
				return null;

			matchIndices.Clear ();
			int j = 0;
			
			for (int i = 0; i < filterTextUpperCase.Length; i++) {
				if (j >= text.Length)
					return null;
				bool wasMatch = false;
				char filterChar = filterTextUpperCase[i];
				// filter char is no letter -> search for next exact match
				if (filterIsNonLetter[i]) {
					for (; j < text.Length; j++) {
						if (filterChar == text[j]) {
							matchIndices.Add (j);
							j++;
							wasMatch = true;
							break;
						}
					}
					if (!wasMatch)
						return null;
					continue;
				}
				
				// letter case
				bool textCharIsUpper = char.IsUpper (text[j]);
				if ((textCharIsUpper || filterTextLowerCaseTable[i]) && filterChar == (textCharIsUpper ? text[j] : char.ToUpper (text[j]))) {
					matchIndices.Add (j++);
					continue;
				}

				// no match, try to continue match at the next word start
				j++;
				for (; j < text.Length; j++) {
					if (char.IsUpper (text[j]) && filterChar == text[j]) {
						matchIndices.Add (j);
						j++;
						wasMatch = true;
						break;
					}
				}
				
				if (!wasMatch)
					return null;
			}
			
			return matchIndices.ToArray ();
		}
	}

}

