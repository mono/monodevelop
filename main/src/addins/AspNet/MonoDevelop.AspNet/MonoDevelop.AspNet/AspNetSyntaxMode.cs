// 
// AspNetSyntaxMode.cs
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
using System.Linq;
using Mono.TextEditor.Highlighting;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Xml;
using System.Text;
using MonoDevelop.AspNet.Parser;

namespace MonoDevelop.AspNet
{
	public class AspNetSyntaxMode : Mono.TextEditor.Highlighting.SyntaxMode
	{
	//	SyntaxMode charpMode;
		
		public AspNetSyntaxMode ()
		{
			ResourceXmlProvider provider = new ResourceXmlProvider (typeof(IXmlProvider).Assembly, "AspNetSyntaxMode.xml");
			using (XmlReader reader = provider.Open ()) {
				SyntaxMode baseMode = SyntaxMode.Read (reader);
				this.rules = new List<Rule> (baseMode.Rules);
				this.keywords = new List<Keywords> (baseMode.Keywords);
				this.spans = baseMode.Spans;
				this.matches = baseMode.Matches;
				this.prevMarker = baseMode.PrevMarker;
				this.SemanticRules = new List<SemanticRule> (baseMode.SemanticRules);
				this.keywordTable = baseMode.keywordTable;
				this.keywordTableIgnoreCase = baseMode.keywordTableIgnoreCase;
			}
		}
		
		public override SpanParser CreateSpanParser (Mono.TextEditor.Document doc, SyntaxMode mode, LineSegment line, CloneableStack<Span> spanStack)
		{
			return new ASPNetSpanParser (doc, mode, spanStack ?? line.StartSpan.Clone ());
		}

		protected class ASPNetSpanParser : SpanParser
		{
			public ASPNetSpanParser (Mono.TextEditor.Document doc, SyntaxMode mode, CloneableStack<Span> spanStack) : base (doc, mode, spanStack)
			{}
			
			class CodeExpressionSpan : Span
			{
				public CodeExpressionSpan (string language)
				{
					Rule = "mode:" + language;
					Begin = new Regex ("<%");
					End = new Regex ("<%");
					Color = "template";
					TagColor = "template.tag";
				}
			}
			
			class CodeDeclarationSpan : Span
			{
				public CodeDeclarationSpan (string language)
				{
					Rule = "mode:" + language;
					Begin = new Regex ("<script");
					End = new Regex ("</script");
					Color = "";
					TagColor = "text.markup.tag";
				}
			}
			
			static string GetMimeForLanguage (string language)
			{
				switch (language) {
				case "C#":
					return "text/x-csharp";
				case "VB":
					return "text/x-vb";
				case "javascript":
				case "JScript.NET":
					return "application/javascript";
				}
				return null;
			}
			
			string GetDefaultMime ()
			{
				var ideDocument = doc.Tag as MonoDevelop.Ide.Gui.Document;
				if (ideDocument != null && ideDocument.ParsedDocument != null) {
					var parsedDocument = ideDocument.ParsedDocument as AspNetParsedDocument;
					if (parsedDocument != null)
						return GetMimeForLanguage (parsedDocument.Info.Language) ?? "text/x-csharp";
				}
				return "text/x-csharp";
			}
			
			
			public string GetAttributeValue (ref int j)
			{
				bool inString = false;
				StringBuilder langBuilder = new StringBuilder ();
				while (j < doc.Length) {
					char ch = doc.GetCharAt (j);
					if (ch == '"' || ch == '\'') {
						if (inString)
							break;
						inString = true;
						j++;
						continue;
					}
					if (inString)
						langBuilder.Append (ch);
					j++;
				}
				return langBuilder.ToString ();
			}

			
			protected override void ScanSpan (ref int i)
			{
				if (!spanStack.Any (s => s is CodeDeclarationSpan || s is CodeExpressionSpan) &&  i + 3 < doc.Length && doc.GetTextAt (i, 2) == "<%" && doc.GetCharAt (i + 2) != '@') {
					var span = new CodeExpressionSpan (GetDefaultMime ());
					FoundSpanBegin (span, i, "#$=:".IndexOf (doc.GetCharAt (i + 2)) >= 0? 3 : 2);
					return;
				}
				
				if (i > 0 && doc.GetCharAt (i - 1) == '>') {
					int k = i - 1;
					while (k > 0 && doc.GetCharAt (k) != '<') {
						k--;
					}
					if (k + 7 < doc.Length && doc.GetTextAt (k, 7) == "<script") {
						int j = k + 7;
						string mime = "application/javascript";
						while (j < doc.Length && doc.GetCharAt (j) != '>') {
							if (j + 8 < doc.Length && doc.GetTextAt (j, 8) == "language") {
								j += 8;
								mime = GetMimeForLanguage (GetAttributeValue (ref j));
								break;
							}
							if (j + 5 < doc.Length && doc.GetTextAt (j, 5) == "runat") {
								j += 5;
								GetAttributeValue (ref j);
								mime = GetDefaultMime ();
								break;
							}
							if (j + 4 < doc.Length && doc.GetTextAt (j, 4) == "type") {
								j += 4;
								mime = GetAttributeValue (ref j);
								break;
							}
							
							j++;
						}
						
						if (mime != null) {
							CodeDeclarationSpan span = new CodeDeclarationSpan (mime);
							FoundSpanBegin (span, i, 0);
							return;
						}
					}
				}
				
				base.ScanSpan (ref i);
			}
			
			protected override bool ScanSpanEnd (Mono.TextEditor.Highlighting.Span cur, ref int i)
			{
				if (spanStack.Any (s => s is CodeDeclarationSpan) && i + 9 <= doc.Length && doc.GetTextAt (i, 9) == "</script>") {
					while (!(spanStack.Peek () is CodeDeclarationSpan)) {
						FoundSpanEnd (spanStack.Peek (), i, 0);
					}
					cur = spanStack.Peek ();
					i += "</script>".Length;
					FoundSpanEnd (cur, i, "</script>".Length);
					return true;
				}
				
				if (spanStack.Any (s => s is CodeExpressionSpan) && i + 2 < doc.Length && doc.GetTextAt (i, 2) == "%>") {
					while (!(spanStack.Peek () is CodeExpressionSpan)) {
						FoundSpanEnd (spanStack.Peek (), i, 0);
					}
					cur = spanStack.Peek ();
					FoundSpanEnd (cur, i, 2);
					return true;
				}
				return base.ScanSpanEnd (cur, ref i);
			}
		}
	}
}
