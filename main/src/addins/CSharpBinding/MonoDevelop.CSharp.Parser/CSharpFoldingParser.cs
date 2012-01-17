// 
// CSharpFoldingParser.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc.
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
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using System.Collections.Generic;
 
namespace MonoDevelop.CSharp.Parser
{
	public unsafe class CSharpFoldingParser : IFoldingParser
	{
		#region IFoldingParser implementation
		public unsafe ParsedDocument Parse (string fileName, string content)
		{
			var result = new ParsedDocument (fileName);
			bool inSingleComment = false, inMultiLineComment = false;
			bool inString = false, inVerbatimString = false;
			bool inChar = false;
			bool inLineStart = true, hasStartedAtLine = false;
			int line = 1, column = 1;
			var startLoc = DomLocation.Empty;
			
			fixed (char* startPtr = content) {
				char* endPtr = startPtr + content.Length;
				char* ptr = startPtr;
				char* beginPtr = ptr;
				while (ptr < endPtr) {
					switch (*ptr) {
					case '/':
						if (inString || inChar || inVerbatimString || inMultiLineComment || inSingleComment)
							break;
						if (ptr + 1 < endPtr) {
							char nextCh = *(ptr + 1);
							if (nextCh == '/') {
								hasStartedAtLine = inLineStart;
								beginPtr = ptr + 2;
								startLoc = new DomLocation (line, column);
								ptr++;
								column++;
								inSingleComment = true;
							} else if (nextCh == '*') {
								hasStartedAtLine = inLineStart;
								beginPtr = ptr + 2;
								startLoc = new DomLocation (line, column);
								ptr++;
								column++;
								inMultiLineComment = true;
							}
						}
						break;
					case '*':
						if (inString || inChar || inVerbatimString || inSingleComment)
							break;
						if (inMultiLineComment && ptr + 1 < endPtr) {
							if (ptr + 1 < endPtr && *(ptr + 1) == '/') {
								ptr += 2;
								column += 2;
								inMultiLineComment = false;
								result.Add (new Comment () {
									Region = new DomRegion (startLoc, new DomLocation (line, column)),
									CommentType = CommentType.MultiLine,
									Text = content.Substring ((int)(beginPtr - startPtr), (int)(ptr - beginPtr)),
									CommentStartsLine = hasStartedAtLine
								});
								continue;
							}
						}
						break;
					case '@':
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
							break;
						if (ptr + 1 < endPtr && *(ptr + 1) == '"') {
							ptr++;
							column++;
							inVerbatimString = true;
						}
						break;
					case '\n':
						if (inSingleComment) {
							bool isDocumentation = *beginPtr == '/';
							if (isDocumentation)
								beginPtr++;
							result.Add (new Comment () { 
								Region = new DomRegion (startLoc, new DomLocation (line, column)),
								CommentType = CommentType.SingleLine, 
								Text = content.Substring ((int)(beginPtr - startPtr), (int)(ptr - beginPtr)),
								CommentStartsLine = hasStartedAtLine,
								IsDocumentation = isDocumentation
							});
							inSingleComment = false;
						}
						inString = false;
						inChar = false;
						inLineStart = true;
						line++;
						column = 1;
						ptr++;
						continue;
					case '\r':
						if (ptr + 1 < endPtr && *(ptr + 1) == '\n')
							ptr++;
						goto case '\n';
					case '\\':
						if (inString || inChar)
							ptr++;
						break;
					case '"':
						if (inSingleComment || inMultiLineComment || inChar)
							break;
						if (inVerbatimString) {
							if (ptr + 1 < endPtr && *(ptr + 1) == '"') {
								ptr++;
								column++;
								break;
							}
							inVerbatimString = false;
							break;
						}
						inString = !inString;
						break;
					case '\'':
						if (inSingleComment || inMultiLineComment || inString || inVerbatimString)
							break;
						inChar = !inChar;
						break;
					}

					column++;
					ptr++;
				}
			}
			return result;
		}
		#endregion
	}
}

