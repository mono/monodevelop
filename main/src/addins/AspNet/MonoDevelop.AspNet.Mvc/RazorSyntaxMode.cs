//
// RazorSyntaxMode.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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
using System.Linq;
using Mono.TextEditor.Highlighting;
using System.Xml;
using Mono.TextEditor;
using System.IO;
using System.Web.Razor.Text;
using System.Web.Razor.Tokenizer;
using System.Web.Razor.Tokenizer.Symbols;
using System.Web.Razor.Parser;
using System.Web.Mvc.Razor;
using MonoDevelop.AspNet.Mvc.Parser;
using MonoDevelop.Ide;
using RazorSpan = System.Web.Razor.Parser.SyntaxTree.Span;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.AspNet.Mvc
{
	public class RazorSyntaxMode : SyntaxMode
	{
		public RazorSyntaxMode ()
		{
			ResourceXmlProvider provider = new ResourceXmlProvider (typeof (IXmlProvider).Assembly, "RazorSyntaxMode.xml");
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

		enum State
		{
			None,
			InTag
		}

		IList<RazorSpan> currentSpans;
		State currentState;
		IList<Chunk> chunks;
		Document guiDocument;

		public override IEnumerable<Chunk> GetChunks (ColorScheme style, DocumentLine line, int offset, int length)
		{
			// Multiline comment
			if (line.StartSpan.Count != 0 && line.StartSpan.Peek ().Begin.Pattern == "@*")
				return base.GetChunks (style, line, offset, length);

			var tokenizer = new CSharpTokenizer (new SeekableTextReader (doc.GetTextAt (offset, length)));
			chunks = new List<Chunk> ();
			CSharpSymbol symbol;
			CSharpSymbol prevSymbol = null;
			int off = line.Offset;
			currentState = State.None;

			while ((symbol = tokenizer.NextSymbol ()) != null) {
				// Apostrophes in text
				bool inApostrophes = false;
				if (symbol.Type == CSharpSymbolType.CharacterLiteral && prevSymbol != null && Char.IsLetterOrDigit (prevSymbol.Content.Last ())) {
					if (symbol.Content.Last () == '\'')
						inApostrophes = true;
					else {
						chunks.Add (new Chunk (off, 1, "text"));
						off++;
						tokenizer = new CSharpTokenizer (new SeekableTextReader (symbol.Content.Substring (1)));
						symbol = tokenizer.NextSymbol ();
						prevSymbol = null;
					}
				}

				string chunkStyle = inApostrophes ? "text" : GetStyleForChunk (symbol, prevSymbol, off);
				chunks.Add (new Chunk (off, symbol.Content.Length, chunkStyle));
				prevSymbol = symbol;
				off += symbol.Content.Length;
			}

			return chunks;
		}

		string GetStyleForChunk (CSharpSymbol symbol, CSharpSymbol prevSymbol, int off)
		{
			if (prevSymbol != null) {
				// Razor directives, statements and expressions
				if (prevSymbol.Type == CSharpSymbolType.Transition)
					return GetStyleForRazorFragment (symbol);
				// End of Razor comments
				if (prevSymbol.Type == CSharpSymbolType.Star && symbol.Type == CSharpSymbolType.Transition) {
					chunks.Last ().Style = "comment";
					return "comment";
				}
				// Email addresses
				if (symbol.Type == CSharpSymbolType.Transition && Char.IsLetterOrDigit (prevSymbol.Content.Last ()))
					return "text";
				// Html tags
				char c = symbol.Content.First ();
				if ((!symbol.Keyword.HasValue && prevSymbol.Type == CSharpSymbolType.LessThan && (Char.IsLetterOrDigit (c) || c == '/'))
					|| (prevSymbol.Type == CSharpSymbolType.Slash && currentState == State.InTag)) {
					currentState = State.InTag;
					chunks.Last ().Style = "text.markup";
					return "text.markup";
				}
				if (symbol.Type == CSharpSymbolType.GreaterThan && currentState == State.InTag) {
					currentState = State.None;
					if (prevSymbol.Type == CSharpSymbolType.Slash)
						chunks.Last ().Style = "text.markup";
					return "text.markup";
				}
			}
			if (symbol.Type == CSharpSymbolType.RightBrace || symbol.Type == CSharpSymbolType.RightParenthesis)
				return GetStyleForEndBracket (symbol, off);
			// Text in html tags
			if ((symbol.Keyword.HasValue || symbol.Type == CSharpSymbolType.IntegerLiteral || symbol.Type == CSharpSymbolType.RealLiteral)
				&& EnsureGuiDocumentSet () && IsInHtmlContext (symbol, off))
				return "text";

			return GetStyleForCSharpSymbol (symbol);
		}

		string GetStyleForEndBracket (CSharpSymbol symbol, int off)
		{
			int matchingOff = doc.GetMatchingBracketOffset (off);
			if (matchingOff == -1 || doc.GetCharAt (matchingOff - 1) != '@')
				return "text";
			else
				return "template.tag";
		}

		string GetStyleForRazorFragment (CSharpSymbol symbol)
		{
			if (symbol.Type == CSharpSymbolType.LeftParenthesis || symbol.Type == CSharpSymbolType.LeftBrace
				|| RazorSymbols.IsDirective (symbol.Content))
				return "template.tag";
			if (symbol.Type == CSharpSymbolType.Star)
				return "comment";
			return GetStyleForCSharpSymbol (symbol);
		}

		bool EnsureGuiDocumentSet ()
		{
			if (guiDocument == null) {
				try {
					if (!File.Exists (Document.FileName))
						return false;
					guiDocument = IdeApp.Workbench.GetDocument (Document.FileName);
					guiDocument.DocumentParsed += (sender, e) =>
					{
						var parsedDoc = (sender as Document).ParsedDocument as RazorCSharpParsedDocument;
						if (parsedDoc != null)
							currentSpans = parsedDoc.PageInfo.Spans.ToList ();
					};
				} catch (Exception) {
					guiDocument = null;
					return false;
				}
			}
			return true;
		}

		bool IsInHtmlContext (CSharpSymbol symbol, int off)
		{
			if (currentSpans == null)
				CreateSpans ();
			var owner = currentSpans.LastOrDefault (s => s.Start.AbsoluteIndex <= off);
			if (owner != null && owner.Kind == System.Web.Razor.Parser.SyntaxTree.SpanKind.Markup)
				return true;
			return false;
		}

		// Creates spans when the parsed document isn't available yet.
		void CreateSpans ()
		{
			using (SeekableTextReader source = new SeekableTextReader (doc.Text)) {
				var markupParser = new HtmlMarkupParser ();
				var codeParser = new MvcCSharpRazorCodeParser ();
				var context = new ParserContext (source, codeParser, markupParser, markupParser) { DesignTimeMode = true };
				codeParser.Context = context;
				markupParser.Context = context;

				context.ActiveParser.ParseDocument ();
				var results = context.CompleteParse ();
				currentSpans = results.Document.Flatten ().ToList ();
			}
		}

		string GetStyleForCSharpSymbol (CSharpSymbol symbol)
		{
			if (symbol.Content == "var" || symbol.Content == "dynamic")
				return "keyword.type";

			string style = "text";
			switch (symbol.Type) {
				case CSharpSymbolType.CharacterLiteral:
				case CSharpSymbolType.StringLiteral:
					style = "string";
					break;
				case CSharpSymbolType.Comment:
					style = "comment";
					break;
				case CSharpSymbolType.IntegerLiteral:
				case CSharpSymbolType.RealLiteral:
					style = "constant.digit";
					break;
				case CSharpSymbolType.Keyword:
					style = GetStyleForKeyword (symbol.Keyword);
					break;
				case CSharpSymbolType.RazorComment:
				case CSharpSymbolType.RazorCommentStar:
				case CSharpSymbolType.RazorCommentTransition:
					style = "comment";
					break;
				case CSharpSymbolType.Transition:
					style = "template.tag";
					break;
				default:
					style = "text";
					break;
			}

			return style;
		}

		string GetStyleForKeyword (CSharpKeyword? keyword)
		{
			switch (keyword.Value) {
				case CSharpKeyword.Abstract:
				case CSharpKeyword.Const:
				case CSharpKeyword.Event:
				case CSharpKeyword.Extern:
				case CSharpKeyword.Internal:
				case CSharpKeyword.Override:
				case CSharpKeyword.Private:
				case CSharpKeyword.Protected:
				case CSharpKeyword.Public:
				case CSharpKeyword.Readonly:
				case CSharpKeyword.Sealed:
				case CSharpKeyword.Static:
				case CSharpKeyword.Virtual:
				case CSharpKeyword.Volatile:
					return "keyword.modifier";
				case CSharpKeyword.As:
				case CSharpKeyword.Is:
				case CSharpKeyword.New:
				case CSharpKeyword.Sizeof:
				case CSharpKeyword.Stackalloc:
				case CSharpKeyword.Typeof:
					return "keyword.operator";
				case CSharpKeyword.Base:
				case CSharpKeyword.This:
					return "keyword.access";
				case CSharpKeyword.Bool:
				case CSharpKeyword.Byte:
				case CSharpKeyword.Char:
				case CSharpKeyword.Decimal:
				case CSharpKeyword.Double:
				case CSharpKeyword.Enum:
				case CSharpKeyword.Float:
				case CSharpKeyword.Int:
				case CSharpKeyword.Long:
				case CSharpKeyword.Object:
				case CSharpKeyword.Sbyte:
				case CSharpKeyword.Short:
				case CSharpKeyword.String:
				case CSharpKeyword.Struct:
				case CSharpKeyword.Uint:
				case CSharpKeyword.Ulong:
				case CSharpKeyword.Ushort:
					return "keyword.type";
				case CSharpKeyword.Break:
				case CSharpKeyword.Continue:
				case CSharpKeyword.Goto:
				case CSharpKeyword.Return:
					return "keyword.jump";
				case CSharpKeyword.Case:
				case CSharpKeyword.Else:
				case CSharpKeyword.Default:
				case CSharpKeyword.If:
				case CSharpKeyword.Switch:
					return "keyword.selection";
				case CSharpKeyword.Catch:
				case CSharpKeyword.Finally:
				case CSharpKeyword.Throw:
				case CSharpKeyword.Try:
					return "keyword.exceptions";
				case CSharpKeyword.Checked:
				case CSharpKeyword.Fixed:
				case CSharpKeyword.Lock:
				case CSharpKeyword.Unchecked:
				case CSharpKeyword.Unsafe:
					return "keyword.misc";
				case CSharpKeyword.Class:
				case CSharpKeyword.Delegate:
				case CSharpKeyword.Interface:
					return "keyword.declaration";
				case CSharpKeyword.Do:
				case CSharpKeyword.For:
				case CSharpKeyword.Foreach:
				case CSharpKeyword.In:
				case CSharpKeyword.While:
					return "keyword.iteration";
				case CSharpKeyword.Explicit:
				case CSharpKeyword.Implicit:
				case CSharpKeyword.Operator:
					return "keyword.operator.declaration";
				case CSharpKeyword.False:
				case CSharpKeyword.Null:
				case CSharpKeyword.True:
					return "constant.language";
				case CSharpKeyword.Namespace:
				case CSharpKeyword.Using:
					return "keyword.namespace";
				case CSharpKeyword.Out:
				case CSharpKeyword.Params:
				case CSharpKeyword.Ref:
					return "keyword.parameter";
				case CSharpKeyword.Void:
					return "constant.language.void";
				default:
					return "keyword";
			}
		}
	}
}
