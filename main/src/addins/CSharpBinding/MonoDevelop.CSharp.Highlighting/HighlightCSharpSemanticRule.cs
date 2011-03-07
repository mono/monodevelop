//
// HighlightPropertiesSemanticRule.cs
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
using System.Collections.Generic;

using Mono.TextEditor;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Ide;
using System.Linq;

namespace MonoDevelop.CSharp.Highlighting
{
	class HighlightCSharpSemanticRule : SemanticRule
	{
		public override void Analyze (Mono.TextEditor.Document doc, LineSegment line, Chunk startChunk, int startOffset, int endOffset)
		{
			if (!MonoDevelop.Core.PropertyService.Get ("EnableSemanticHighlighting", false) || doc == null || line == null || startChunk == null)
				return;
			int lineNumber = doc.OffsetToLineNumber (line.Offset);
			var ideDocument = IdeApp.Workbench.Documents.FirstOrDefault (d => d.FileName == doc.FileName);
			if (ideDocument == null)
				return;
			ParsedDocument parsedDocument = ideDocument.ParsedDocument;
			ICompilationUnit unit = parsedDocument != null ? parsedDocument.CompilationUnit : null;
			if (unit == null)
				return;
			
			var ctx = ProjectDomService.GetProjectDom (ideDocument.Project);
			for (Chunk chunk = startChunk; chunk != null; chunk = chunk.Next) {
				if (chunk.Style != "text")
					continue;
				char charBefore = chunk.Offset > 0 ? doc.GetCharAt (chunk.Offset - 1) : '}';
				char ch;
				for (int i = chunk.Offset; i < chunk.EndOffset; i++) {
					ch = doc.GetCharAt (i);
					if (!Char.IsLetter (ch) || Char.IsLetterOrDigit (charBefore)) {
						charBefore = ch;
						continue;
					}
					
					int start = i;
					bool wasWhitespace = Char.IsWhiteSpace (charBefore);
					bool wasDot = false;
					int bracketCount = 0;
					while (start > 0) {
						ch = doc.GetCharAt (start);
						if (ch == '\n' || ch == '\r')
							break;
						bool isNamePart = IsNamePart (ch);
						if (wasWhitespace && isNamePart)
							break;
						
						if (ch == '<') {
							bracketCount--;
							if (bracketCount < 0) {
								start++;
								break; 
							}
							start--;
							wasWhitespace = false;
							continue;
						}
						if (ch == '>') {
							if (wasWhitespace && !wasDot)
								break;
							bracketCount++;
							start--;
							wasWhitespace = false;
							continue;
						}
						bool isWhiteSpace = Char.IsWhiteSpace (ch);
						if (!isNamePart && !isWhiteSpace && ch != '.') {
							start++;
							break;
						}
						wasWhitespace = isWhiteSpace;
						wasDot = ch == '.' || wasDot && wasWhitespace;
						start--;
					}
					
					int end = i;
					int genericCount = 0;
					wasWhitespace = false;
					List<Segment> nameSegments = new List<Segment> ();
					while (end < chunk.EndOffset) {
						ch = doc.GetCharAt (end);
						if (wasWhitespace && IsNamePart(ch))
							break;
						if (ch == '<') {
							genericCount = 1;
							while (end < doc.Length) {
								ch = doc.GetCharAt (end);
								if (ch == ',')
									genericCount++;
								if (ch == '>') {
									nameSegments.Add (new Segment (end, 1));
									break;
								}
								end++;
							}
							break;
						}
						bool isWhiteSpace = Char.IsWhiteSpace (ch);
						if (!IsNamePart(ch) && !isWhiteSpace) 
							break;
						wasWhitespace = isWhiteSpace;
						end++;
					}
					if (start >= end) {
						charBefore = ch;
						continue;
					}
					string typeString = doc.GetTextBetween (start, end);
					IReturnType returnType = NRefactoryResolver.ParseReturnType (new ExpressionResult (typeString));
					
					int nameEndOffset = start;
					for (; nameEndOffset < end; nameEndOffset++) {
						ch = doc.GetCharAt (nameEndOffset);
						if (nameEndOffset >= i && ch == '<') {
							nameEndOffset++;
							break;
						}
					}
					nameSegments.Add (new Segment (i, nameEndOffset - i));
					
					int column = i - line.Offset;
					IType callingType = unit.GetTypeAt (lineNumber, column);
					List<IReturnType> genericParams = null;
					if (genericCount > 0) {
						genericParams = new List<IReturnType> ();
						for (int n = 0; n < genericCount; n++) 
							genericParams.Add (new DomReturnType ("A"));
					}
					
					IType type = null;
					if (ctx != null)
						type = ctx.SearchType (unit, callingType, new DomLocation (lineNumber, 1), returnType);
					if (type == null && unit != null && returnType != null)
						type = unit.GetType (returnType.FullName, returnType.GenericArguments.Count);
					if (ctx != null && type == null && returnType != null) {
						returnType.Name += "Attribute";
						type = ctx.SearchType (unit, callingType, new DomLocation (lineNumber, 1), returnType);
					}
					if (type != null)
						nameSegments.ForEach (segment => HighlightSegment (startChunk, segment, "keyword.semantic.type"));
					charBefore = ch;
				}
			}
		}

		static void HighlightSegment (Chunk startChunk, Segment namePart, string style)
		{
			for (Chunk searchChunk = startChunk; searchChunk != null; searchChunk = searchChunk.Next) {
				if (!searchChunk.Contains (namePart))
					continue;
				if (searchChunk.Length == namePart.Length) {
					searchChunk.Style = style;
					break;
				}
				Chunk propertyChunk = new Chunk (namePart.Offset, namePart.Length, style);
				propertyChunk.Next = searchChunk.Next;
				searchChunk.Next = propertyChunk;
				if (searchChunk.EndOffset - propertyChunk.EndOffset > 0) {
					Chunk newChunk = new Chunk (propertyChunk.EndOffset, searchChunk.EndOffset - propertyChunk.EndOffset, searchChunk.Style);
					newChunk.Next = propertyChunk.Next;
					propertyChunk.Next = newChunk;
				}
				searchChunk.Length = namePart.Offset - searchChunk.Offset;
				break;
			}
		}


		bool IsNamePart (char ch)
		{
			return Char.IsLetterOrDigit (ch) || ch == '_';
		}
	}
}
