// 
// JaySyntaxMode.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.Collections.Generic;
using Mono.TextEditor.Highlighting;
using Mono.TextEditor;
using System.Xml;

namespace Mono.TextEditor.Highlighting
{
	public class JaySyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode
	{
		public JaySyntaxMode ()
		{
			ResourceXmlProvider provider = new ResourceXmlProvider (typeof(IXmlProvider).Assembly, "JaySyntaxMode.xml");
			using (XmlReader reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				this.rules = new List<Rule> (baseMode.Rules);
				this.keywords = new List<Keywords> (baseMode.Keywords);
				this.spans = new List<Span> (baseMode.Spans).ToArray ();
				this.matches = baseMode.Matches;
				this.prevMarker = baseMode.PrevMarker;
				this.SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				this.table = baseMode.Table;
				this.properties = baseMode.Properties;
			}
		}
		
		public override SpanParser CreateSpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack)
		{
			return new JaySpanParser (doc, mode, line, spanStack);
		}
		
		class JayBlockSpan : Span
		{
			public int Offset {
				get;
				set;
			}
			
			public JayBlockSpan (int offset)
			{
				this.Offset = offset;
				this.Rule = "mode:text/x-csharp";
				this.Begin = new Regex ("}");
				this.End = new Regex ("}");
				this.TagColor = "keyword.access";
			}
			
			public override string ToString ()
			{
				return string.Format ("[JayBlockSpan: Offset={0}]", Offset);
			}
		}
		
		class ForcedJayBlockSpan : Span
		{
			public ForcedJayBlockSpan ()
			{
				this.Rule = "mode:text/x-csharp";
				this.Begin = new Regex ("%{");
				this.End = new Regex ("%}");
				this.TagColor = "keyword.access";
			}
		}
		
		class JayDefinitionSpan : Span
		{
			public JayDefinitionSpan ()
			{
				this.Rule = "token";
				this.Begin = this.End = new Regex ("%%");
				this.TagColor = "keyword.access";
			}
		}
		
		protected class JaySpanParser : SpanParser
		{
			public JaySpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack) : base (doc, mode, line, spanStack)
			{
			}
			
			protected override void ScanSpan (ref int i)
			{
				bool hasJayDefinitonSpan = spanStack.Any (s => s is JayDefinitionSpan);
				
				if (i + 1 < doc.Length && doc.GetCharAt (i) == '%')  {
					char next = doc.GetCharAt (i + 1);
					if (next == '{') {
						ForcedJayBlockSpan forcedBlockSpan = new ForcedJayBlockSpan ();
						OnFoundSpanBegin (forcedBlockSpan, i, 2);
						spanStack.Push (forcedBlockSpan);
						ruleStack.Push (GetRule (forcedBlockSpan));
						i++;
						return;
					}
					
					if (!hasJayDefinitonSpan && next == '%') {
						JayDefinitionSpan jayDefinitionSpan = new JayDefinitionSpan ();
						OnFoundSpanBegin (jayDefinitionSpan, i, 2);
						spanStack.Push (jayDefinitionSpan);
						ruleStack.Push (GetRule (jayDefinitionSpan));
						return;
					}
					
					if (next == '}' && spanStack.Any (s => s is ForcedJayBlockSpan)) {
						foreach (Span span in spanStack.ToArray ().Reverse ()) {
							OnFoundSpanEnd (span, i, span.End.Pattern.Length);
							if (spanStack.Count > 0) {
								spanStack.Pop ();
							}
							if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
								ruleStack.Pop ();
							if (span is ForcedJayBlockSpan)
								break;
						}
						return;
					}
				}
				
				
				if (CurSpan is JayDefinitionSpan && doc.GetCharAt (i) == '{' && hasJayDefinitonSpan && !spanStack.Any (s => s is JayBlockSpan)) {
					JayBlockSpan jayBlockSpan = new JayBlockSpan (i);
					OnFoundSpanBegin (jayBlockSpan, i, 1);
					spanStack.Push (jayBlockSpan);
					ruleStack.Push (GetRule (jayBlockSpan));
					return;
				}
				
				
				base.ScanSpan (ref i);
			}
			
			protected override bool ScanSpanEnd (Mono.TextEditor.Highlighting.Span cur, int i)
			{
				JayBlockSpan jbs = cur as JayBlockSpan;
				if (jbs != null) {
					if (doc.GetCharAt (i) == '}') {
						int brackets = 0;
						bool isInString = false, isInChar = false, isVerbatimString = false;
						bool isInLineComment  = false, isInBlockComment = false;
						
						for (int j = jbs.Offset; j <= i; j++) {
							char ch = doc.GetCharAt (j);
							switch (ch) {
								case '\n':
								case '\r':
									isInLineComment = false;
									if (!isVerbatimString)
										isInString = false;
									break;
								case '/':
									if (isInBlockComment) {
										if (j > 0 && doc.GetCharAt (j - 1) == '*') 
											isInBlockComment = false;
									} else if (!isInString && !isInChar && j + 1 < doc.Length) {
										char nextChar = doc.GetCharAt (j + 1);
										if (nextChar == '/')
											isInLineComment = true;
										if (!isInLineComment && nextChar == '*')
											isInBlockComment = true;
									}
									break;
								case '\\':
									if (isInChar || (isInString && !isVerbatimString))
										j++;
									break;
								case '@':
									if (!(isInString || isInChar || isInLineComment || isInBlockComment) && j + 1 < doc.Length && doc.GetCharAt (j + 1) == '"') {
										isInString = true;
										isVerbatimString = true;
										j++;
									}
									break;
								case '"':
									if (!(isInChar || isInLineComment || isInBlockComment))  {
										if (isInString && isVerbatimString && j + 1 < doc.Length && doc.GetCharAt (j + 1) == '"') {
											j++;
										} else {
											isInString = !isInString;
											isVerbatimString = false;
										}
									}
									break;
								case '\'':
									if (!(isInString || isInLineComment || isInBlockComment)) 
										isInChar = !isInChar;
									break;
								case '{':
									if (!(isInString || isInChar || isInLineComment || isInBlockComment))
										brackets++;
									break;
								case '}':
									if (!(isInString || isInChar || isInLineComment || isInBlockComment))
										brackets--;
									break;
							}
						}
						if (brackets == 0) {
							OnFoundSpanEnd (cur, i, 1);
							if (spanStack.Count > 0)
								spanStack.Pop ();
							if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
								ruleStack.Pop ();
							return true;
						}
						return false;
					}
				}
				
				if (cur is ForcedJayBlockSpan) {
					if (i + 1 < doc.Length && doc.GetCharAt (i) == '%' && doc.GetCharAt (i + 1) == '}') {
						OnFoundSpanEnd (cur, i, 2);
						if (spanStack.Count > 0)
							spanStack.Pop ();
						if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
							ruleStack.Pop ();
						return true;
					}
				}
				
				if (cur is JayDefinitionSpan) {
					if (i + 1 < doc.Length && doc.GetCharAt (i) == '%' && doc.GetCharAt (i + 1) == '%') {
						OnFoundSpanEnd (cur, i, 2);
						if (spanStack.Count > 0)
							spanStack.Pop ();
						if (ruleStack.Count > 1) // rulStack[1] is always syntax mode
							ruleStack.Pop ();
						return true;
					}
				}
				return base.ScanSpanEnd (cur, i);
			}
		}
	}
}

