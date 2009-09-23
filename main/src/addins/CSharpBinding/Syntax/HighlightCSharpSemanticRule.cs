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
using Mono.TextEditor.Highlighting;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;
using System.Text;
using ICSharpCode.NRefactory.Ast;

namespace MonoDevelop.CSharpBinding
{
	class HighlightCSharpSemanticRule : SemanticRule
	{
		ProjectDom ctx;
//		Mono.TextEditor.Document document;
//		MonoDevelop.Ide.Gui.Document doc;
//		IParser parser;
//		IResolver resolver;
//		IExpressionFinder expressionFinder;
		
		void Init (Mono.TextEditor.Document document)
		{
			
//			parser = ProjectDomService.GetParser (document.FileName, document.MimeType);
//			expressionFinder = ProjectDomService.GetExpressionFinder (document.FileName);
		}
		
		
		ProjectDom GetParserContext (Mono.TextEditor.Document document)
		{
			Project project = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (project != null)
				return ProjectDomService.GetProjectDom (project);
			return ProjectDom.Empty;
		}
		
	//	string expression;
/*		IMember GetLanguageItem (Mono.TextEditor.Document document, LineSegment line, int offset, string expression)
		{
			string txt = document.Text;
			ExpressionResult expressionResult = new ExpressionResult (expression);
//			ExpressionResult expressionResult = expressionFinder.FindFullExpression (txt, offset);
			int lineNumber = document.OffsetToLineNumber (offset);
			expressionResult.Region = new DomRegion (lineNumber, offset - line.Offset, lineNumber, offset + expression.Length - line.Offset);
			expressionResult.ExpressionContext = ExpressionContext.IdentifierExpected;
			
			resolver = new NRefactoryResolver (ctx, doc.CompilationUnit, doc.TextEditor, document.FileName);
			ResolveResult result = resolver.Resolve (expressionResult, expressionResult.Region.Start);
			
			if (result is MemberResolveResult) 
				return ((MemberResolveResult)result).ResolvedMember;
			return null;
		}*/
		
		public override void Analyze (Mono.TextEditor.Document doc, LineSegment line, Chunk startChunk, int startOffset, int endOffset)
		{
			if (!MonoDevelop.Core.PropertyService.Get ("EnableSemanticHighlighting", false) || doc == null || line == null || startChunk == null)
				return;
			ctx = GetParserContext (doc);
			int lineNumber = doc.OffsetToLineNumber (line.Offset);
			ParsedDocument parsedDocument = ProjectDomService.GetParsedDocument (ctx, doc.FileName);
			for (Chunk chunk = startChunk; chunk != null; chunk = chunk.Next) {
				if (chunk.Style != "text")
					continue;
				for (int i = chunk.Offset; i < chunk.EndOffset; i++) {
					char charBefore = i > 0 ? doc.GetCharAt (i - 1) : '}';
					if (Char.IsLetter (doc.GetCharAt (i)) && !Char.IsLetterOrDigit (charBefore)) {
					} else {
						continue;
					}
					
					int start = i;
					bool wasWhitespace = false;
					bool wasDot = false;
					int bracketCount = 0;
					while (start > 0) {
						char ch = doc.GetCharAt (start);
						if (wasWhitespace && IsNamePart(ch)) {
							start++;
							if (start < chunk.Offset)
								start = Int32.MaxValue;
							break;
						}
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
						if (!IsNamePart(ch) && !Char.IsWhiteSpace (ch) && ch != '.') {
							start++;
							break;
						}
						wasWhitespace = Char.IsWhiteSpace (ch);
						wasDot = ch == '.' || wasDot && wasWhitespace;
						start--;
					}
					
					int end = i;
					int genericCount = 0;
					wasWhitespace = false;
					List<Segment> nameSegments = new List<Segment> ();
					while (end < chunk.EndOffset) {
						char ch = doc.GetCharAt (end);
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
						if (!IsNamePart(ch) && !Char.IsWhiteSpace (ch)) 
							break;
						wasWhitespace = Char.IsWhiteSpace (ch);
						end++;
					}
					if (start >= end) 
						continue;
					string typeString = doc.GetTextBetween (start, end);
					IReturnType returnType = NRefactoryResolver.ParseReturnType (new ExpressionResult (typeString));
					
					int nameEndOffset = start;
					for (; nameEndOffset < end; nameEndOffset++) {
						char ch = doc.GetCharAt (nameEndOffset);
						if (nameEndOffset >= i && ch == '<') {
							nameEndOffset++;
							break;
						}
					}
					nameSegments.Add (new Segment (i, nameEndOffset - i));

					int column = i - line.Offset;
					IType callingType = parsedDocument.CompilationUnit.GetTypeAt (lineNumber, column);
					List<IReturnType> genericParams = null;
					if (genericCount > 0) {
						genericParams = new List<IReturnType> ();
						for (int n = 0; n < genericCount; n++) 
							genericParams.Add (new DomReturnType ("A"));
					}
					
					IType type = ctx.SearchType (new SearchTypeRequest (parsedDocument.CompilationUnit, returnType, callingType));
					if (type != null)
						nameSegments.ForEach (segment => HighlightSegment (startChunk, segment, "keyword.semantic.type"));
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
