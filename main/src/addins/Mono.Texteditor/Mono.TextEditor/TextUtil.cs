//
// TextUtil.cs
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
	public sealed class TextUtil
	{
		public const string openBrackets    = "([{<";
		public const string closingBrackets = ")]}>";
		
		public static bool IsBracket (char ch)
		{
			return (openBrackets + closingBrackets).IndexOf (ch) >= 0;
		}
		
		public static bool IsWordSeparator (char c)
		{
			return Char.IsWhiteSpace (c) || (Char.IsPunctuation (c) && c != '_');
		}
		
		public static bool IsWholeWordAt (Document doc, int offset, int length)
		{
			return (offset == 0 || IsWordSeparator (doc.GetCharAt (offset - 1))) &&
				   (offset + length == doc.Length || IsWordSeparator (doc.GetCharAt (offset + length)));
		}
		
		public static int GetMatchingBracketOffset (Document document, int offset)
		{
			if (offset < 0 || offset >= document.Length)
				return -1;
			char ch = document.GetCharAt (offset);
			int bracket = TextUtil.openBrackets.IndexOf (ch);
			int result;
			if (bracket >= 0) {
				result = TextUtil.SearchMatchingBracketForward (document, offset + 1, bracket);
			} else {
				bracket = TextUtil.closingBrackets.IndexOf (ch);
				if (bracket >= 0) {
					result = TextUtil.SearchMatchingBracketBackward (document, offset - 1, bracket);
				} else {
					result = -1;
				}
			}
			return result;
		}
		
		public static int SearchMatchingBracketForward (Document document, int offset, int bracket)
		{
			return SearchMatchingBracket (document, offset, closingBrackets[bracket], openBrackets[bracket], 1);
		}
		
		public static int SearchMatchingBracketBackward (Document document, int offset, int bracket)
		{
			return SearchMatchingBracket (document, offset, openBrackets[bracket], closingBrackets[bracket], -1);
		}
		
		static int SearchMatchingBracket (Document document, int offset, char openBracket, char closingBracket, int direction)
		{
			bool isInString       = false;
			bool isInChar         = false;	
			bool isInBlockComment = false;
			int depth = -1;
			while (offset >= 0 && offset < document.Length) {
				char ch = document.GetCharAt (offset);
				switch (ch) {
					case '/':
						if (isInBlockComment) 
							isInBlockComment = document.GetCharAt (offset + direction) != '*';
						if (!isInString && !isInChar && offset - direction < document.Length) 
							isInBlockComment = offset > 0 && document.GetCharAt (offset - direction) == '*';
						break;
					case '"':
						if (!isInChar && !isInBlockComment) 
							isInString = !isInString;
						break;
					case '\'':
						if (!isInString && !isInBlockComment) 
							isInChar = !isInChar;
						break;
					default :
						if (ch == closingBracket) {
							if (!(isInString || isInChar || isInBlockComment)) 
								--depth;
						} else if (ch == openBracket) {
							if (!(isInString || isInChar || isInBlockComment)) {
								++depth;
								if (depth == 0) 
									return offset;
							}
						}
						break;
				}
				offset += direction;
			}
			return -1;
		}
		
		public enum CharacterClass {
			Unknown,
			Whitespace,
			IdentifierPart
		}
		
		public static CharacterClass GetCharacterClass (char ch)
		{
			if (Char.IsWhiteSpace (ch))
				return CharacterClass.Whitespace;
			if (Char.IsLetterOrDigit (ch) || ch == '_')
				return CharacterClass.IdentifierPart;
			return CharacterClass.Unknown;
		}
		
		public static int FindNextWordOffset (Document document, int offset)
		{
			int lineNumber   = document.OffsetToLineNumber (offset);
			LineSegment line = document.GetLine (lineNumber);
			if (line == null)
				return offset;
			
			int result    = offset;
			int endOffset = line.Offset + line.EditableLength;
			if (result == endOffset) {
				line = document.GetLine (lineNumber + 1);
				if (line != null)
					result = line.Offset;
				return result;
			}
				
			CharacterClass startClass = GetCharacterClass (document.GetCharAt (result));
			while (offset < endOffset && GetCharacterClass (document.GetCharAt (result)) == startClass) {
				result++;
			}
			while (result < endOffset && GetCharacterClass (document.GetCharAt (result)) == CharacterClass.Whitespace) {
				result++;
			}
			return result;
		}
		
		public static int FindPrevWordOffset (Document document, int offset)
		{
			int lineNumber = document.OffsetToLineNumber (offset);
			LineSegment line = document.GetLine (lineNumber);
			if (line == null)
				return offset;
			
			int result = offset;
			if (result == line.Offset) {
				line = document.GetLine (lineNumber - 1);
				if (line != null)
					result = line.Offset + line.EditableLength;
				return result;
			}
			
			CharacterClass startClass = GetCharacterClass (document.GetCharAt (result - 1));
			while (result > line.Offset && GetCharacterClass (document.GetCharAt (result - 1)) == startClass) {
				result--;
			}
			if (startClass == CharacterClass.Whitespace && result > line.Offset) {
				startClass = GetCharacterClass (document.GetCharAt (result - 1));
				while (result > line.Offset && GetCharacterClass (document.GetCharAt (result - 1)) == startClass) {
					result--;
				}
			}
			return result;
		}
		
//gedit like routines
//		public static int FindPrevWordOffset (Document document, int offset)
//		{
//			if (offset <= 0)
//				return 0;
//			int  result = offset - 1;
//			bool crossedEol = false;
//			while (result > 0 && !Char.IsLetterOrDigit (document.GetCharAt (result))) {
//				crossedEol |= document.GetCharAt (result) == '\n';
//				crossedEol |= document.GetCharAt (result) == '\r';
//				result--;
//			}
//			
//			bool isLetter = Char.IsLetter (document.GetCharAt (result));
//			bool isDigit  = Char.IsDigit (document.GetCharAt (result));
//			if (crossedEol && (isLetter || isDigit))
//				return result + 1;
//			while (result > 0) {
//				char ch = document.GetCharAt (result);
//				if (isLetter) {
//					if (Char.IsLetter (ch)) 
//						result--;
//					else {
//						result++;
//						break;
//					}
//				} else if (isDigit) {
//					if (Char.IsDigit (ch)) 
//						result--;
//					else {
//						result++;
//						break;
//					}
//				} else {
//					if (Char.IsLetterOrDigit (ch)) {
//						result++;
//						break;
//					} else 
//						result--;
//				}
//			}
//			foreach (FoldSegment segment in document.GetFoldingsFromOffset (result)) {
//				if (segment.IsFolded)
//					result = System.Math.Min (result, segment.StartLine.Offset + segment.Column);
//			}
//			return result;
//		}
//		public static int FindNextWordOffset (Document document, int offset)
//		{
//			if (offset + 1 >= document.Length)
//				return document.Length;
//			int result = offset + 1;
//			bool crossedEol = false;
//			while (result < document.Length && !Char.IsLetterOrDigit (document.GetCharAt (result))) {
//				crossedEol |= document.GetCharAt (result) == '\n';
//				crossedEol |= document.GetCharAt (result) == '\r';
//				result++;
//			}
//			
//			bool isLetter = Char.IsLetter (document.GetCharAt (result));
//			bool isDigit  = Char.IsDigit (document.GetCharAt (result));
//			if (crossedEol && (isLetter || isDigit))
//				return result;
//			while (result < document.Length) {
//				char ch = document.GetCharAt (result);
//				if (isLetter) {
//					if (Char.IsLetter (ch)) 
//						result++;
//					else {
//						break;
//					}
//				} else if (isDigit) {
//					if (Char.IsDigit (ch)) 
//						result++;
//					else {
//						break;
//					}
//				} else {
//					if (Char.IsLetterOrDigit (ch)) {
//						break;
//					} else 
//						result++;
//				}
//			}
//			foreach (FoldSegment segment in document.GetFoldingsFromOffset (result)) {
//				if (segment.IsFolded)
//					result = System.Math.Max (result, segment.EndLine.Offset + segment.EndColumn);
//			}
//			return result;
//		}

		
	}
}
