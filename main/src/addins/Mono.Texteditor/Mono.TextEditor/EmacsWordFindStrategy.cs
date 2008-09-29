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

namespace Mono.TextEditor
{
	public class EmacsWordFindStrategy : IWordFindStrategy
	{
		public int FindNextWordOffset (Document doc, int offset)
		{
			if (offset + 1 >= doc.Length)
				return doc.Length;
			int result = offset + 1;
			SharpDevelopWordFindStrategy.CharacterClass charClass = SharpDevelopWordFindStrategy.GetCharacterClass (doc.GetCharAt (result));
			bool done = false;
			while (!done && result < doc.Length) {
				char ch = doc.GetCharAt (result);
				SharpDevelopWordFindStrategy.CharacterClass curCharClass = SharpDevelopWordFindStrategy.GetCharacterClass (ch);
				switch (curCharClass) {
				case SharpDevelopWordFindStrategy.CharacterClass.IdentifierPart:
					charClass = SharpDevelopWordFindStrategy.CharacterClass.IdentifierPart;
					break;
				default:
					if (charClass == SharpDevelopWordFindStrategy.CharacterClass.IdentifierPart) {
						done = true;
						result--;
					}
					break;
				}
				result++;
			}
			foreach (FoldSegment segment in doc.GetFoldingsFromOffset (result)) {
				if (segment.IsFolded)
					result = System.Math.Max (result, segment.EndLine.Offset + segment.EndColumn);
			}
			return result;
		}
		
		public int FindPrevWordOffset (Document doc, int offset)
		{
			if (offset <= 0)
				return 0;
			int  result = offset - 1;
			SharpDevelopWordFindStrategy.CharacterClass charClass = SharpDevelopWordFindStrategy.GetCharacterClass (doc.GetCharAt (result));
			bool done = false;
			while (!done && result > 0) {
				char ch = doc.GetCharAt (result);
				SharpDevelopWordFindStrategy.CharacterClass curCharClass = SharpDevelopWordFindStrategy.GetCharacterClass (ch);
				switch (curCharClass) {
				case SharpDevelopWordFindStrategy.CharacterClass.IdentifierPart:
					charClass = SharpDevelopWordFindStrategy.CharacterClass.IdentifierPart;
					break;
				default:
					if (charClass == SharpDevelopWordFindStrategy.CharacterClass.IdentifierPart) {
						done = true;
						result += 2;
					}
					break;
				}
				result--;
			}
			foreach (FoldSegment segment in doc.GetFoldingsFromOffset (result)) {
				if (segment.IsFolded)
					result = System.Math.Min (result, segment.StartLine.Offset + segment.Column);
			}
			return result;
		}
		
	}
}
