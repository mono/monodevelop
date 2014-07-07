// 
// IBracketMatcher.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using Mono.TextEditor.Highlighting;


namespace Mono.TextEditor
{
	public interface IBracketMatcher
	{
		int SearchMatchingBracketForward (System.ComponentModel.BackgroundWorker worker, TextDocument document, int offset, char openBracket, char closingBracket);
		int SearchMatchingBracketBackward (System.ComponentModel.BackgroundWorker worker, TextDocument document, int offset, char openBracket, char closingBracket);
	}
	
	public class DefaultBracketMatcher : IBracketMatcher
	{
		static List<string> GetList (TextDocument document, string name)
		{
			var mode = document.SyntaxMode as SyntaxMode;
			if (mode != null) {
				if (mode.Properties.ContainsKey(name)) 
					return mode.Properties[name];
			}
			return new List<string> ();
		}
		
		static int StartsWithListMember (TextDocument document, List<string> list, int offset)
		{
			for (int i = 0; i < list.Count; i++) {
				string item = list[i];
				if (offset + item.Length < document.TextLength) {
					if (document.GetTextAt (offset, item.Length) == item) 
						return i;
				}
			}
			return -1;
		}
		
		public int SearchMatchingBracketForward (System.ComponentModel.BackgroundWorker worker, TextDocument document, int offset, char openBracket, char closingBracket)
		{
			bool isInBlockComment = false;
			bool isInLineComment  = false;
			int  curStringQuote   = -1;
			
			bool startsInLineComment = StartsInLineComment (document, offset);
			
			List<string> lineComments       = GetList (document, "LineComment");
			List<string> blockCommentStarts = GetList (document, "BlockCommentStart");
			List<string> blockCommentEnds   = GetList (document, "BlockCommentEnd");
			List<string> stringQuotes       = GetList (document, "StringQuote");
			int depth = -1;
			while (offset >= 0 && offset < document.TextLength) {
				if (worker != null && worker.CancellationPending)
					return -1;
				if (curStringQuote < 0) {
					// check line comments
					if (!isInBlockComment && !isInLineComment) 
						isInLineComment = StartsWithListMember (document, lineComments, offset) >= 0;
					
					// check block comments
					if (!isInLineComment) {
						if (!isInBlockComment) { 
							isInBlockComment = StartsWithListMember (document, blockCommentStarts, offset) >= 0;
						} else {
							isInBlockComment = StartsWithListMember (document, blockCommentEnds, offset) < 0;
						}
					}
				}
				
				if (!isInBlockComment && !isInLineComment) {
					int i = StartsWithListMember (document, stringQuotes, offset);
					if (i >= 0) {
						if (curStringQuote >= 0) {
							if (curStringQuote == i)
								curStringQuote = -1;
						} else {
							curStringQuote = i;
						}
					}
				}
				
				char ch = document.GetCharAt (offset);
				switch (ch) {
					case '\n':
					case '\r':
						if (startsInLineComment)
							return -1;
						isInLineComment = false;
						break;
					default :
						if (ch == closingBracket) {
							if (!(isInLineComment || curStringQuote >= 0 || isInBlockComment)) 
								--depth;
						} else if (ch == openBracket) {
							if (!(isInLineComment || curStringQuote >= 0 || isInBlockComment)) {
								++depth;
								if (depth == 0) 
									return offset;
							}
						}
						break;
				}
				offset++;
			}
			return -1;
		}
		bool StartsInLineComment (TextDocument document, int offset)
		{
			List<string> lineComments = GetList (document, "LineComment");
			DocumentLine line = document.GetLineByOffset (offset);
			for (int i = line.Offset ; i < offset; i++) {
				if (StartsWithListMember (document, lineComments, i) >= 0)
					return true;
			}
			return false;
		}
		
		int GetLastSourceCodePosition (TextDocument document, int lineOffset)
		{
			DocumentLine line = document.GetLineByOffset (lineOffset);
			if (line == null)
				return lineOffset;
			bool isInBlockComment = false;
			bool isInLineComment  = false;
			int  curStringQuote   = -1;
			
			List<string> lineComments       = GetList (document, "LineComment");
			List<string> blockCommentStarts = GetList (document, "BlockCommentStart");
			List<string> blockCommentEnds   = GetList (document, "BlockCommentEnd");
			List<string> stringQuotes       = GetList (document, "StringQuote");
			
			for (int i = 0 ; i < line.Length; i++) {
				int offset = line.Offset + i;
				// check line comments
				if (!isInBlockComment && curStringQuote < 0) {
					isInLineComment = StartsWithListMember (document, lineComments, offset) >= 0;
					if (isInLineComment) 
						return System.Math.Min (offset, lineOffset);
				}
				// check block comments
				if (!isInLineComment && curStringQuote < 0) {
					if (!isInBlockComment) { 
						isInBlockComment = StartsWithListMember (document, blockCommentStarts, offset) >= 0;
					} else {
						isInBlockComment = StartsWithListMember (document, blockCommentEnds, offset) < 0;
					}
				}
				
				if (!isInBlockComment && !isInLineComment) {
					int j = StartsWithListMember (document, stringQuotes, offset);
					if (j >= 0) {
						if (curStringQuote >= 0) {
							if (curStringQuote == j)
								curStringQuote = -1;
						} else {
							curStringQuote = j;
						}
					}
				}
			}
			return lineOffset;
		}
		
		public int SearchMatchingBracketBackward (System.ComponentModel.BackgroundWorker worker, TextDocument document, int offset, char openBracket, char closingBracket)
		{
			bool isInBlockComment = false;
			bool isInLineComment  = false;
			int  curStringQuote   = -1;
			
			List<string> blockCommentStarts = GetList (document, "BlockCommentStart");
			List<string> blockCommentEnds   = GetList (document, "BlockCommentEnd");
			List<string> stringQuotes       = GetList (document, "StringQuote");
			
			bool startsInLineComment = StartsInLineComment (document, offset);
			int depth = -1;
			
			if (!startsInLineComment)
				offset = GetLastSourceCodePosition (document, offset);
			
			while (offset >= 0 && offset < document.TextLength) {
				if (worker != null && worker.CancellationPending)
					return -1;
				char ch = document.GetCharAt (offset);
				
				// check block comments
				if (!isInLineComment && curStringQuote < 0) {
					if (!isInBlockComment) { 
						isInBlockComment = StartsWithListMember (document, blockCommentEnds, offset) >= 0;
					} else {
						isInBlockComment = StartsWithListMember (document, blockCommentStarts, offset) < 0;
					}
				}
				
				if (!isInBlockComment && !isInLineComment) {
					int i = StartsWithListMember (document, stringQuotes, offset);
					if (i >= 0) {
						if (curStringQuote >= 0) {
							if (curStringQuote == i)
								curStringQuote = -1;
						} else {
							curStringQuote = i;
						}
					}
				}
				
				switch (ch) {
					case '\n':
					case '\r':
						if (startsInLineComment)
							return -1;
						offset--;
						while (offset > 0 && (document.GetCharAt (offset) == '\n' || document.GetCharAt (offset) == '\r')) {
							offset--;
						}
						offset = GetLastSourceCodePosition (document, offset) + 1;
						break;
					default:
						if (ch == closingBracket) {
							if (!(curStringQuote >= 0 || isInBlockComment)) 
								--depth;
						} else if (ch == openBracket) {
							if (!(curStringQuote >= 0 || isInBlockComment)) {
								++depth;
								if (depth == 0) 
									return offset;
							}
						}
						break;
				}
				offset--;
			}
			return -1;
		}
	}
}
