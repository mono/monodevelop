//
// EmacsWordFindStrategy.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated docation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using CC = Mono.TextEditor.SharpDevelopWordFindStrategy.CharacterClass;

namespace Mono.TextEditor
{
	public class EmacsWordFindStrategy : IWordFindStrategy
	{
		bool treat_;
		
		public EmacsWordFindStrategy (bool treat_)
		{
			this.treat_ = treat_;
		}
		
		int FindNextWordOffset (Document doc, int offset, bool subword)
		{
			if (offset + 1 >= doc.Length)
				return doc.Length;
			int result = offset + 1;
			CC previous = SharpDevelopWordFindStrategy.GetCharacterClass (doc.GetCharAt (result), subword, treat_);
			bool inIndentifier = previous != CC.Unknown && previous != CC.Whitespace;			
			while (result < doc.Length) {
				char ch = doc.GetCharAt (result);
				CC current = SharpDevelopWordFindStrategy.GetCharacterClass (ch, subword, treat_);
				
				//camelCase / PascalCase splitting
				if (subword) {
					if (current == CC.Digit && previous != CC.Digit) {
						result++;
						break;
					} else if (current == CC.UppercaseLetter && previous == CC.LowercaseLetter) {
						break;
					} else if (current == CC.LowercaseLetter && previous == CC.UppercaseLetter && result - 2 > 0
					           && SharpDevelopWordFindStrategy.GetCharacterClass (doc.GetCharAt (result - 2), subword, treat_) == CC.UppercaseLetter)
					{
						result--;
						break;
					}
				}
				
				//else break at end of identifiers
				if (previous != CC.Unknown && previous != CC.Whitespace) {
					inIndentifier = true;
				} else if (inIndentifier) {
					result--;
					break;
				}
				previous = current;
				result++;
			}
			foreach (FoldSegment segment in doc.GetFoldingsFromOffset (result)) {
				if (segment.IsFolded)
					result = System.Math.Max (result, segment.EndLine.Offset + segment.EndColumn);
			}
			return result;
		}
		
		int FindPrevWordOffset (Document doc, int offset, bool subword)
		{
			if (offset <= 0)
				return 0;
			int  result = offset - 1;
			CC previous = SharpDevelopWordFindStrategy.GetCharacterClass (doc.GetCharAt (result), subword, treat_);
			bool inIndentifier = previous != CC.Unknown && previous != CC.Whitespace;			
			while (result > 0) {
				char ch = doc.GetCharAt (result);
				CC current = SharpDevelopWordFindStrategy.GetCharacterClass (ch, subword, treat_);
				
				//camelCase / PascalCase splitting
				if (subword) {
					if (current == CC.Digit && previous != CC.Digit) {
						result++;
						break;
					} else if (current == CC.UppercaseLetter && previous == CC.LowercaseLetter) {
						break;
					} else if (current == CC.LowercaseLetter && previous == CC.UppercaseLetter && result + 2 < doc.Length
					           && SharpDevelopWordFindStrategy.GetCharacterClass (doc.GetCharAt (result + 2), subword, treat_) == CC.UppercaseLetter)
					{
						result++;
						break;
					}
				}
				
				//else break at end of identifiers
				if (previous != CC.Unknown && previous != CC.Whitespace) {
					inIndentifier = true;
				} else if (inIndentifier) {
					result += 2;
					break;
				}
				previous = current;
				result--;
			}
			foreach (FoldSegment segment in doc.GetFoldingsFromOffset (result)) {
				if (segment.IsFolded)
					result = System.Math.Min (result, segment.StartLine.Offset + segment.Column);
			}
			return result;
		}
		
		public int FindNextWordOffset (Document doc, int offset)
		{
			return FindNextWordOffset (doc, offset, false);
		}
		
		public int FindPrevWordOffset (Document doc, int offset)
		{
			return FindPrevWordOffset (doc, offset, false);
		}
		
		public int FindNextSubwordOffset (Document doc, int offset)
		{
			return FindNextWordOffset (doc, offset, true);
		}
		
		public int FindPrevSubwordOffset (Document doc, int offset)
		{
			return FindPrevWordOffset (doc, offset, true);
		}
	}
}
