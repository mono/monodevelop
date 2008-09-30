//
// SharpDevelopWordFindStrategy.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
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
	public class SharpDevelopWordFindStrategy : IWordFindStrategy
	{
		public enum CharacterClass {
			Unknown,
			Whitespace,
			IdentifierPart
		}
		
		public static CharacterClass GetCharacterClass (char ch)
		{
			return  GetCharacterClass (ch, true);
		}
		
		public static CharacterClass GetCharacterClass (char ch, bool treat_)
		{
			if (Char.IsWhiteSpace (ch))
				return CharacterClass.Whitespace;
			if (Char.IsLetterOrDigit (ch) || (treat_ && ch == '_'))
				return CharacterClass.IdentifierPart;
			return CharacterClass.Unknown;
		}
		
		public int FindNextWordOffset (Document doc, int offset)
		{
			int lineNumber   = doc.OffsetToLineNumber (offset);
			LineSegment line = doc.GetLine (lineNumber);
			if (line == null)
				return offset;
			
			int result    = offset;
			int endOffset = line.Offset + line.EditableLength;
			if (result == endOffset) {
				line = doc.GetLine (lineNumber + 1);
				if (line != null)
					result = line.Offset;
				return result;
			}
			
			CharacterClass startClass = GetCharacterClass (doc.GetCharAt (result));
			while (offset < endOffset && GetCharacterClass (doc.GetCharAt (result)) == startClass) {
				result++;
			}
			while (result < endOffset && GetCharacterClass (doc.GetCharAt (result)) == CharacterClass.Whitespace) {
				result++;
			}
			return result;
		}
		
		public int FindPrevWordOffset (Document doc, int offset)
		{
			int lineNumber = doc.OffsetToLineNumber (offset);
			LineSegment line = doc.GetLine (lineNumber);
			if (line == null)
				return offset;
			
			int result = offset;
			if (result == line.Offset) {
				line = doc.GetLine (lineNumber - 1);
				if (line != null)
					result = line.Offset + line.EditableLength;
				return result;
			}
			
			CharacterClass startClass = GetCharacterClass (doc.GetCharAt (result - 1));
			while (result > line.Offset && GetCharacterClass (doc.GetCharAt (result - 1)) == startClass) {
				result--;
			}
			if (startClass == CharacterClass.Whitespace && result > line.Offset) {
				startClass = GetCharacterClass (doc.GetCharAt (result - 1));
				while (result > line.Offset && GetCharacterClass (doc.GetCharAt (result - 1)) == startClass) {
					result--;
				}
			}
			return result;
		}
		
	}
}
