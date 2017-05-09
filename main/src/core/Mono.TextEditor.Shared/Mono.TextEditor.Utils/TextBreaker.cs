// 
// TextBreaker.cs
//  
// Author:
//       IBBoard <dev@ibboard.co.uk>
// 
// Copyright (c) 2011 IBBoard
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
using MonoDevelop.Core.Text;

namespace Mono.TextEditor.Utils
{
	/// <summary>
	/// A utility class for breaking up the text in TextEditors
	/// </summary>
	class TextBreaker
	{
		/// <summary>
		/// Breaks the lines into words in the form of a list of <see cref="TextSegment">TextSegments</see>. A 'word' is defined as an identifier (a series of letters, digits or underscores)
		/// or a single non-identifier character (including white space characters)
		/// </summary>
		/// <returns>
		/// The list of segments representing the 'words' in the lines
		/// </returns>
		/// <param name='editor'>
		/// The text editor to get the words from
		/// </param>
		/// <param name='startLine'>
		/// The first line in the editor's documents to get the words from
		/// </param>
		/// <param name='lineCount'>
		/// The number of lines to get words from
		/// </param>
		public static List<ISegment> BreakLinesIntoWords (MonoTextEditor editor, int startLine, int lineCount, bool includeDelimiter = true)
		{
			return BreakLinesIntoWords (editor.Document, startLine, lineCount, includeDelimiter);
		}

		
		/// <summary>
		/// Breaks the lines into words in the form of a list of <see cref="TextSegment">TextSegments</see>. A 'word' is defined as an identifier (a series of letters, digits or underscores)
		/// or a single non-identifier character (including white space characters)
		/// </summary>
		/// <returns>
		/// The list of segments representing the 'words' in the lines
		/// </returns>
		/// <param name='document'>
		/// The document to get the words from
		/// </param>
		/// <param name='startLine'>
		/// The first line in the documents to get the words from
		/// </param>
		/// <param name='lineCount'>
		/// The number of lines to get words from
		/// </param>
		public static List<ISegment> BreakLinesIntoWords (TextDocument document, int startLine, int lineCount, bool includeDelimiter = true)
		{
			var result = new List<ISegment> ();
			for (int line = startLine; line < startLine + lineCount; line++) {
				var lineSegment = document.GetLine (line);
				int offset = lineSegment.Offset;
				bool wasIdentifierPart = false;
				int lastWordEnd = 0;
				for (int i = 0; i < lineSegment.Length; i++) {
					char ch = document.GetCharAt (offset + i);
					bool isIdentifierPart = char.IsLetterOrDigit (ch) || ch == '_';
					if (!isIdentifierPart) {
						if (wasIdentifierPart) {
							result.Add (new TextSegment (offset + lastWordEnd, i - lastWordEnd));
						}
						result.Add (new TextSegment (offset + i, 1));
						lastWordEnd = i + 1;
					}
					wasIdentifierPart = isIdentifierPart;
				}
				
				if (lastWordEnd != lineSegment.Length) {
					result.Add (new TextSegment (offset + lastWordEnd, lineSegment.Length - lastWordEnd));
				}
				if (includeDelimiter && lineSegment.DelimiterLength > 0)
					result.Add (new TextSegment (lineSegment.Offset + lineSegment.Length, lineSegment.DelimiterLength));
			}
			
			return result;
		}
	}
}

