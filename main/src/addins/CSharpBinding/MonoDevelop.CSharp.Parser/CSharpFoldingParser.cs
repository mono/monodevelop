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
using System.Collections.Generic;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Parser
{
	unsafe class CSharpFoldingParser : IFoldingParser
	{
		#region IFoldingParser implementation

		static unsafe bool StartsIdentifier (char* ptr, char* endPtr, string identifier)
		{
			fixed (char* startId = identifier) {
				char* idPtr = startId;
				char* endId = startId + identifier.Length;
				while (idPtr < endId) {
					if (ptr >= endPtr)
						return false;
					if (*idPtr != *ptr)
						return false;
					idPtr++;
					ptr++;
				}
				return true;
			}
		}

		static unsafe void SkipWhitespaces (ref char* ptr, char* endPtr, ref int column)
		{
			while (ptr < endPtr) {
				char ch = *ptr;
				if (ch != ' ' && ch != '\t')
					return;
				column++;
				ptr++;
			}
		}

		static unsafe string ReadToEol (string content, ref char* ptr, char* endPtr, ref int line, ref int column)
		{
			char* lineBeginPtr = ptr;
			char* lineEndPtr = lineBeginPtr;

			while (ptr < endPtr) {
				switch (*ptr) {
				case '\n':
					if (lineEndPtr == lineBeginPtr)
						lineEndPtr = ptr;
					line++;
					column = 1;
					ptr++;
					fixed (char* startPtr = content) {
						return content.Substring ((int)(lineBeginPtr - startPtr), (int)(lineEndPtr - lineBeginPtr));
					}
				case '\r':
					lineEndPtr = ptr;
					if (ptr + 1 < endPtr && *(ptr + 1) == '\n')
						ptr++;
					goto case '\n';
				}
				column++;
				ptr++;
			}
			return "";
		}

		public unsafe ParsedDocument Parse (string fileName, string content)
		{
			var regionStack = new Stack<Tuple<string, DocumentLocation>> ();
			var result = new DefaultParsedDocument (fileName);
			bool inSingleComment = false, inMultiLineComment = false;
			bool inString = false, inVerbatimString = false;
			bool inChar = false;
			bool inLineStart = true, hasStartedAtLine = false;
			int line = 1, column = 1;
			int bracketDepth = 0;
			var startLoc = DocumentLocation.Empty;
			
			fixed (char* startPtr = content) {
				char* endPtr = startPtr + content.Length;
				char* ptr = startPtr;
				char* beginPtr = ptr;
				while (ptr < endPtr) {
					switch (*ptr) {
					case '{':
						if (inString || inChar || inVerbatimString || inMultiLineComment || inSingleComment) 
							break;
						bracketDepth++;
						break;
					case '}':
						if (inString || inChar || inVerbatimString || inMultiLineComment || inSingleComment) 
							break;
						bracketDepth--;
						break;
					case '#':
						if (!inLineStart)
							break;
						inLineStart = false;
						ptr++;

						if (StartsIdentifier (ptr, endPtr, "region")) {
							var regionLocation = new DocumentLocation (line, column);
							column++;
							ptr += "region".Length;
							column += "region".Length;
							SkipWhitespaces (ref ptr, endPtr, ref column);
							regionStack.Push (Tuple.Create (ReadToEol (content, ref ptr, endPtr, ref line, ref column), regionLocation));
							continue;
						} else if (StartsIdentifier (ptr, endPtr, "endregion")) {
							column++;
							ptr += "endregion".Length;
							column += "endregion".Length;
							if (regionStack.Count > 0) {
								var beginRegion = regionStack.Pop ();
								result.Add (new FoldingRegion (
									beginRegion.Item1, 
									new DocumentRegion (beginRegion.Item2.Line, beginRegion.Item2.Column, line, column),
									FoldType.UserRegion,
									true));
							}
							continue;
						} else {
							column++;
						}
						break;
					case '/':
						if (inString || inChar || inVerbatimString || inMultiLineComment || inSingleComment) {
							inLineStart = false;
							break;
						}
						if (ptr + 1 < endPtr) {
							char nextCh = *(ptr + 1);
							if (nextCh == '/') {
								hasStartedAtLine = inLineStart;
								beginPtr = ptr + 2;
								startLoc = new DocumentLocation (line, column);
								ptr++;
								column++;
								inSingleComment = true;
							} else if (nextCh == '*') {
								hasStartedAtLine = inLineStart;
								beginPtr = ptr + 2;
								startLoc = new DocumentLocation (line, column);
								ptr++;
								column++;
								inMultiLineComment = true;
							}
						}
						inLineStart = false;
						break;
					case '*':
						inLineStart = false;
						if (inString || inChar || inVerbatimString || inSingleComment)
							break;
						if (inMultiLineComment && ptr + 1 < endPtr) {
							if (ptr + 1 < endPtr && *(ptr + 1) == '/') {
								ptr += 2;
								column += 2;
								inMultiLineComment = false;
								if (bracketDepth <= 1) {
									result.Add (new MonoDevelop.Ide.TypeSystem.Comment () {
										Region = new DocumentRegion (startLoc, new DocumentLocation (line, column)),
										OpenTag = "/*",
										CommentType = MonoDevelop.Ide.TypeSystem.CommentType.Block,
										Text = content.Substring ((int)(beginPtr - startPtr), (int)(ptr - beginPtr)),
										CommentStartsLine = hasStartedAtLine
									});
								}
								continue;
							}
						}
						break;
					case '@':
						inLineStart = false;
						if (inString || inChar || inVerbatimString || inSingleComment || inMultiLineComment)
							break;
						if (ptr + 1 < endPtr && *(ptr + 1) == '"') {
							ptr++;
							column++;
							inVerbatimString = true;
						}
						break;
					case '\n':
						if (inSingleComment && hasStartedAtLine) {
							bool isDocumentation = *beginPtr == '/';
							if (isDocumentation)
								beginPtr++;
							if (isDocumentation || bracketDepth <= 1) {
								// Doesn't matter much that some comments are not correctly recognized - they'll get added later
								// It's important that header comments are in.
								result.Add (new MonoDevelop.Ide.TypeSystem.Comment () { 
									Region = new DocumentRegion (startLoc, new DocumentLocation (line, column)),
									CommentType = MonoDevelop.Ide.TypeSystem.CommentType.SingleLine, 
									OpenTag = "//",
									Text = content.Substring ((int)(beginPtr - startPtr), (int)(ptr - beginPtr)),
									CommentStartsLine = hasStartedAtLine,
									IsDocumentation = isDocumentation
								});
							}
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
					default:
						inLineStart &= *ptr == ' ' || *ptr == '\t';
						break;
					}

					column++;
					ptr++;
				}
			}
			foreach (var fold in ToFolds (result.GetCommentsAsync().Result)) {
				result.Add (fold);
			}
			return result;
		}
		#endregion

		static IEnumerable<FoldingRegion> ToFolds (IReadOnlyList<Comment> comments)
		{
			for (int i = 0; i < comments.Count; i++) {
				Comment comment = comments [i];

				if (comment.CommentType == CommentType.Block) {
					int startOffset = 0;
					if (comment.Region.BeginLine == comment.Region.EndLine)
						continue;
					while (startOffset < comment.Text.Length) {
						char ch = comment.Text [startOffset];
						if (!char.IsWhiteSpace (ch) && ch != '*')
							break;
						startOffset++;
					}
					int endOffset = startOffset;
					while (endOffset < comment.Text.Length) {
						char ch = comment.Text [endOffset];
						if (ch == '\r' || ch == '\n' || ch == '*')
							break;
						endOffset++;
					}

					string txt;
					if (endOffset > startOffset) {
						txt = "/* " + GetFirstLine (comment.Text) + " ...";
					} else {
						txt = "/* */";
					}
					yield return new FoldingRegion (txt, comment.Region, FoldType.Comment);
					continue;
				}

				if (!comment.CommentStartsLine)
					continue;
				int j = i;
				int curLine = comment.Region.BeginLine - 1;
				var end = comment.Region.End;
				var commentText = StringBuilderCache.Allocate ();
				for (; j < comments.Count; j++) {
					Comment curComment = comments [j];
					if (curComment == null || !curComment.CommentStartsLine
						|| curComment.CommentType != comment.CommentType
						|| curLine + 1 != curComment.Region.BeginLine)
						break;
					commentText.Append (curComment.Text);
					end = curComment.Region.End;
					curLine = curComment.Region.BeginLine;
				}

				if (j - i > 1 || (comment.IsDocumentation && comment.Region.BeginLine < comment.Region.EndLine)) {
					string txt = null;
					if (comment.IsDocumentation) {
						string cmtText = commentText.ToString ();
						int idx = cmtText.IndexOf ("<summary>", StringComparison.Ordinal);
						if (idx >= 0) {
							int maxOffset = cmtText.IndexOf ("</summary>", StringComparison.Ordinal);
							while (maxOffset > 0 && cmtText [maxOffset - 1] == ' ')
								maxOffset--;
							if (maxOffset < 0)
								maxOffset = cmtText.Length;
							int startOffset = idx + "<summary>".Length;
							while (startOffset < maxOffset) {
								char ch = cmtText [startOffset];
								if (!char.IsWhiteSpace (ch) && ch != '/')
									break;
								startOffset++;
							}
							int endOffset = startOffset;
							while (endOffset < maxOffset) {
								char ch = cmtText [endOffset];
								if (ch == '\r' || ch == '\n')
									break;
								endOffset++;
							}
							if (endOffset > startOffset)
								txt = "/// <summary> " + cmtText.Substring (startOffset, endOffset - startOffset).Trim () + " ...";
						}
						if (txt == null)
							txt = "/// " + comment.Text.Trim () + " ...";
					} else {
						txt = "// " + comment.Text.Trim () + " ...";
					}
					StringBuilderCache.Free (commentText);
					yield return new FoldingRegion (txt,
						new DocumentRegion (comment.Region.Begin, end),
						FoldType.Comment);
					i = j - 1;
				}
			}
		}

		static string GetFirstLine (string text)
		{
			int start = 0;
			while (start < text.Length) {
				char ch = text [start];
				if (ch != ' ' && ch != '\t')
					break;
				start++;
			}
			int end = start;

			while (end < text.Length) {
				char ch = text [end];
				if (MonoDevelop.Core.Text.NewLine.IsNewLine (ch))
					break;
				end++;
			}
			if (end <= start)
				return "";
			return text.Substring (start, end - start);
		}
	}
}