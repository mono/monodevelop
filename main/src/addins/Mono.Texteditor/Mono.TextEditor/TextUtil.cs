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
	}
}
