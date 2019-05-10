//
// PatternSearcher.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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

namespace MonoDevelop.Ide.FindInFiles
{
	class PatternSearcher
	{
		readonly char [] patternArray;
		readonly int patternLength;

		const int SkipTableLength = 128;
		readonly int [] skipTable;

		readonly bool isCaseSensitive;
		readonly bool wholeWordsOnly;

		public PatternSearcher (string pattern, bool isCaseSensitive, bool wholeWordsOnly)
		{
			this.isCaseSensitive = isCaseSensitive;
			this.wholeWordsOnly = wholeWordsOnly;
			patternArray = this.isCaseSensitive ? pattern.ToCharArray () : pattern.ToLowerInvariant ().ToCharArray ();
			patternLength = pattern.Length;
			skipTable = CalculateSkipTable ();
		}

		int [] CalculateSkipTable ()
		{
			var result = new int [SkipTableLength];
			for (int i = 0; i < SkipTableLength; i++) {
				char ch = (char)i;
				int index;
				for (index = patternLength - 2; index >= 0; index--) {
					if (CharEquals (patternArray [index], ch))
						break;
				}
				result [ch] = patternLength - index - 1;
			}
			return result;
		}

		public int [] FindAll (string text)
		{
			return FindAll (text.AsSpan ());
		}

		public int [] FindAll (ReadOnlySpan<char> text)
		{
			var result = new List<int> ();
			int index = 0;
			int end = text.Length;
			while ((index = Find (text, index, end)) > -1) {
				result.Add (index);
				index++;
			}
			return result.ToArray ();
		}

		public int Find (string text, int startIndex, int endIndex)
		{
			return Find (text.AsSpan (), startIndex, endIndex);
		}

		public int Find (ReadOnlySpan<char> text, int startIndex, int endIndex)
		{
			if (startIndex > endIndex)
				throw new ArgumentException ($"end:{endIndex} > start:{startIndex}");
			int length = text.Length;
			if (endIndex > length)
				throw new ArgumentException ($"end:{endIndex} > length:{length}");

			int start = startIndex;
			int end = endIndex - patternLength;

			while (start <= end) {
				int i = patternLength - 1;
				char lastChar = text [start + i];

				if (CharEquals (patternArray [i], lastChar)) {
					i--;
					while (i >= 0) {
						char c = text [start + i];
						if (!CharEquals (patternArray [i], c)) break;
						i--;
					}
					if (i < 0) {
						if (wholeWordsOnly) {
							if (IsWholeWordAt (text, start))
								return start;
						} else {
							return start;
						}
					}
				}

				start += lastChar < SkipTableLength ? skipTable [lastChar] : 1;
			}
			return -1;
		}

		bool IsWholeWordAt (ReadOnlySpan<char> text, int start)
		{
			if (start > 0 && !FindInFilesModel.IsWordSeparator (text [start - 1]))
				return false;

			int end = start + patternLength;
			if (end < text.Length && !FindInFilesModel.IsWordSeparator (text [end]))
				return false;

			return true;
		}

		bool CharEquals (char patternChar, char ch)
		{
			return patternChar == (isCaseSensitive ? ch : char.ToLowerInvariant (ch));
		}
	}
}
