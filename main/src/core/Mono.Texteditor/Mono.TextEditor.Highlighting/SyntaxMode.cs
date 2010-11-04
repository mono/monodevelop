// SyntaxMode.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Xml;
using System.Diagnostics;

namespace Mono.TextEditor.Highlighting
{
	public class SyntaxMode : Rule
	{
		public static readonly SyntaxMode Default = new SyntaxMode ();
		protected List<Rule> rules = new List<Rule> ();

		public string MimeType {
			get;
			private set;
		}

		public IEnumerable<Rule> Rules {
			get {
				return rules;
			}
		}

		public SyntaxMode() : base (null)
		{
			DefaultColor = "text";
			Name = "<root>";
			this.Delimiter = "&()<>{}[]~!%^*-+=|\\#/:;\"' ,\t.?";
		}

		public bool Validate (Style style)
		{
			if (!GetIsValid (style)) {
				return false;
			}
			foreach (Rule rule in Rules) {
				if (!rule.GetIsValid (style)) {
					return false;
				}
			}
			return true;
		}

		public virtual Chunk GetChunks (Document doc, Style style, LineSegment line, int offset, int length)
		{
			SpanParser spanParser = CreateSpanParser (doc, this, line, null);
			ChunkParser chunkParser = CreateChunkParser (spanParser, doc, style, this, line);
			Chunk result = chunkParser.GetChunks (chunkParser.lineOffset, line.EditableLength);
			if (SemanticRules != null) {
				foreach (SemanticRule sematicRule in SemanticRules) {
					sematicRule.Analyze (doc, line, result, offset, offset + length);
				}
			}
			if (result != null) {
				// crop to begin
				if (result.Offset != offset) {
					while (result != null && result.EndOffset < offset)
						result = result.Next;
					if (result != null) {
						int endOffset = result.EndOffset;
						result.Offset = offset;
						result.Length = endOffset - offset;
					}
				}

				if (result != null && offset + length != chunkParser.lineOffset + line.EditableLength) {
					// crop to end
					Chunk cur = result;
					while (cur != null && cur.EndOffset < offset + length) {
						cur = cur.Next;
					}
					if (cur != null) {
						cur.Length = offset + length - cur.Offset;
						cur.Next = null;
					}
				}
			}

			return result;
		}

		public virtual string GetTextWithoutMarkup (Document doc, Style style, int offset, int length)
		{
			return doc.GetTextAt (offset, length);
		}

		public string GetMarkup (Document doc, ITextEditorOptions options, Style style, int offset, int length, bool removeIndent)
		{
			return GetMarkup (doc, options, style, offset, length, removeIndent, true, true);
		}

		public static string ColorToPangoMarkup (Gdk.Color color)
		{
			return string.Format ("#{0:X2}{1:X2}{2:X2}", color.Red >> 8, color.Green >> 8, color.Blue >> 8);
		}

		public string GetMarkup (Document doc, ITextEditorOptions options, Style style, int offset, int length, bool removeIndent, bool useColors, bool replaceTabs)
		{
			int indentLength = GetIndentLength (doc, offset, length, false);
			int curOffset = offset;

			StringBuilder result = new StringBuilder ();
			while (curOffset < offset + length && curOffset < doc.Length) {
				LineSegment line = doc.GetLineByOffset (curOffset);
				int toOffset = System.Math.Min (line.Offset + line.EditableLength, offset + length);
				Stack<ChunkStyle> styleStack = new Stack<ChunkStyle> ();
				for (Chunk chunk = GetChunks (doc, style, line, curOffset, toOffset - curOffset); chunk != null; chunk = chunk.Next) {

					ChunkStyle chunkStyle = chunk.GetChunkStyle (style);
					bool setBold = chunkStyle.Bold && (styleStack.Count == 0 || !styleStack.Peek ().Bold) ||
						!chunkStyle.Bold && (styleStack.Count == 0 || styleStack.Peek ().Bold);
					bool setItalic = chunkStyle.Italic && (styleStack.Count == 0 || !styleStack.Peek ().Italic) ||
						!chunkStyle.Italic && (styleStack.Count == 0 || styleStack.Peek ().Italic);
					bool setUnderline = chunkStyle.Underline && (styleStack.Count == 0 || !styleStack.Peek ().Underline) ||
						!chunkStyle.Underline && (styleStack.Count == 0 || styleStack.Peek ().Underline);
					bool setColor = styleStack.Count == 0 || TextViewMargin.GetPixel (styleStack.Peek ().Color) != TextViewMargin.GetPixel (chunkStyle.Color);
					if (setColor || setBold || setItalic || setUnderline) {
						if (styleStack.Count > 0) {
							result.Append("</span>");
							styleStack.Pop ();
						}
						result.Append("<span");
						if (useColors) {
							result.Append(" foreground=\"");
							result.Append(ColorToPangoMarkup (chunkStyle.Color));
							result.Append("\"");
						}
						if (chunkStyle.Bold)
							result.Append(" weight=\"bold\"");
						if (chunkStyle.Italic)
							result.Append(" style=\"italic\"");
						if (chunkStyle.Underline)
							result.Append(" underline=\"single\"");
						result.Append(">");
						styleStack.Push (chunkStyle);
					}

					for (int i = 0; i < chunk.Length && chunk.Offset + i < doc.Length; i++) {
						char ch = chunk.GetCharAt (doc, chunk.Offset + i);
						switch (ch) {
						case '&':
							result.Append ("&amp;");
							break;
						case '<':
							result.Append ("&lt;");
							break;
						case '>':
							result.Append ("&gt;");
							break;
						case '\t':
							if (replaceTabs) {
								result.Append (new string (' ', options.TabSize));
							} else {
								result.Append ('\t');
							}
							break;
						default:
							result.Append (ch);
							break;
						}
					}
				}
				while (styleStack.Count > 0) {
					result.Append("</span>");
					styleStack.Pop ();
				}

				curOffset = line.EndOffset;
				if (removeIndent)
					curOffset += indentLength;
				if (result.Length > 0 && curOffset < offset + length)
					result.AppendLine ();
			}
			return result.ToString ();
		}

		public static int GetIndentLength (Document doc, int offset, int length, bool skipFirstLine)
		{
			int curOffset = offset;
			int indentLength = int.MaxValue;
			while (curOffset < offset + length) {
				LineSegment line = doc.GetLineByOffset (curOffset);
				if (!skipFirstLine) {
					indentLength = System.Math.Min (indentLength, line.GetIndentation (doc).Length);
				} else {
					skipFirstLine = false;
				}
				curOffset = line.EndOffset + 1;
			}
			return indentLength == int.MaxValue ? 0 : indentLength;
		}

		public virtual SpanParser CreateSpanParser (Document doc, SyntaxMode mode, LineSegment line, CloneableStack<Span> spanStack)
		{
			return new SpanParser (doc, mode, spanStack ?? line.StartSpan.Clone ());
		}

		public virtual ChunkParser CreateChunkParser (SpanParser spanParser, Document doc, Style style, SyntaxMode mode, LineSegment line)
		{
			return new ChunkParser (spanParser, doc, style, mode, line);
		}

		public class SpanParser
		{
			protected SyntaxMode mode;
			protected CloneableStack<Span> spanStack;
			protected Stack<Rule> ruleStack;
			protected Document doc;
			int maxEnd;

			public Rule CurRule {
				get;
				private set;
			}

			public Span CurSpan {
				get;
				private set;
			}

			public CloneableStack<Span> SpanStack {
				get {
					return spanStack;
				}
				set {
					this.spanStack = value;
				}
			}

			public Stack<Rule> RuleStack {
				get {
					return ruleStack;
				}
				set {
					this.ruleStack = value;
					this.CurRule = ruleStack.Peek ();
				}
			}

			Stack<Rule> CreateRuleStack ()
			{
				var result = new Stack<Rule> ();
				if (mode == null)
					return result;
				Rule rule = mode;
				result.Push (mode);
				foreach (Span span in this.spanStack.Reverse ()) {
					Rule tmp = rule.GetRule (span.Rule) ?? this.CurRule;
					result.Push (tmp);
					rule = tmp;
				}
				return result;
			}

			public SpanParser (Document doc, SyntaxMode mode, CloneableStack<Span> spanStack)
			{
				if (doc == null)
					throw new ArgumentNullException ("doc");
				this.doc  = doc;
				this.mode = mode;
				this.SpanStack = spanStack;
				this.CurRule = mode;
				this.ruleStack = CreateRuleStack ();
				this.CurRule = ruleStack.Peek ();
				this.CurSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				FoundSpanBegin = DefaultFoundSpanBegin;
				FoundSpanEnd = DefaultFoundSpanEnd;
				FoundSpanExit = DefaultFoundSpanEnd;
				ParseChar = delegate (ref int i, char ch) {};
			}

			void DefaultFoundSpanBegin (Span span, int offset, int length)
			{
				PushSpan (span, GetRule (span));
			}

			void DefaultFoundSpanEnd (Span span, int offset, int length)
			{
				PopSpan ();
			}

			public void PushSpan (Span span, Rule rule)
			{
				spanStack.Push (span);
				ruleStack.Push (rule);
				this.CurRule = rule;
				this.CurSpan = span;
			}

			public void PopSpan ()
			{
				if (spanStack.Count > 0)
					spanStack.Pop ();
				if (ruleStack.Count > 1)
					ruleStack.Pop ();
				this.CurRule = ruleStack.Peek ();
				this.CurSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
			}

			public Rule GetRule (Span span)
			{
				if (string.IsNullOrEmpty (span.Rule))
					return new Rule (mode);
				return CurRule.GetRule (span.Rule);
			}

			public delegate void CharParser (ref int i, char ch);

			public Action<Span, int, int> FoundSpanBegin;
			public Action<Span, int, int> FoundSpanExit;
			public Action<Span, int, int> FoundSpanEnd;

			public CharParser ParseChar;

			protected virtual void ScanSpan (ref int i)
			{
				int textOffset = i - StartOffset;
				
				for (int j = 0; j < CurRule.Spans.Length; j++) {
					Span span = CurRule.Spans[j];

					if ((span.BeginFlags & SpanBeginFlags.StartsLine) == SpanBeginFlags.StartsLine && (textOffset == 0 || CurText[textOffset - 1] == '\n'|| CurText[textOffset - 1] == '\r'))
						continue;

					RegexMatch match = span.Begin.TryMatch (CurText, textOffset);
					if (!match.Success)
						continue;
					bool mismatch = false;
					if ((span.BeginFlags & SpanBeginFlags.FirstNonWs) == SpanBeginFlags.FirstNonWs)
						mismatch = CurText.Take (i).Any (ch => !char.IsWhiteSpace (ch));
					if (mismatch)
						continue;
					FoundSpanBegin (span, i, match.Length);
					i += match.Length - 1;
					return;
				}
			}

			protected virtual bool ScanSpanEnd (Span cur, ref int i)
			{
				int textOffset = i - StartOffset;
				
				if (cur.End != null) {
					RegexMatch match = cur.End.TryMatch (CurText, textOffset);
					if (match.Success) {
						FoundSpanEnd (cur, i, match.Length);
						i += match.Length - 1;
						return true;
					}
				}

				if (cur.Exit != null) {
					RegexMatch match = cur.Exit.TryMatch (CurText, textOffset);
					if (match.Success) {
						FoundSpanExit (cur, i, match.Length);
						i += match.Length - 1;
						return true;
					}
				}
				return false;
			}
			
			public string CurText {
				get;
				private set;
			}
			public int StartOffset {
				get;
				private set;
			}

			public void ParseSpans (int offset, int length)
			{
				if (offset < 0 || offset + length >= doc.Length || length <= 0)
					return;
				StartOffset = offset;
				CurText = doc.GetTextAt (offset, length);
				maxEnd = offset + length;
				for (int i = offset; i < maxEnd; i++) {
					int textIndex = i - StartOffset;
					Span cur = CurSpan;
					if (cur != null) {
						if (cur.Escape != null) {
							bool mismatch = false;
							for (int j = 0; j < cur.Escape.Length; j++) {
								if (textIndex + j >= CurText.Length || CurText[textIndex + j] != cur.Escape[j]) {
									mismatch = true;
									break;
								}
							}
							if (!mismatch) {
								i += cur.Escape.Length;
								if (cur.Escape.Length > 1)
									i--;
								continue;
							}
						}
						if (ScanSpanEnd (cur, ref i))
							continue;
					}
					ScanSpan (ref i);
					if (i < doc.Length)
						ParseChar (ref i, CurText[textIndex]);
				}
			}
		}

		public class ChunkParser
		{
			readonly string defaultStyle = "text";
			protected SpanParser spanParser;
			protected Document doc;
			protected LineSegment line;
			internal int lineOffset;
			protected SyntaxMode mode;

			public ChunkParser (SpanParser spanParser, Document doc, Style style, SyntaxMode mode, LineSegment line)
			{
				this.mode = mode;
				this.doc = doc;
				this.line = line;
				this.lineOffset = line.Offset;
				this.spanParser = spanParser;
				spanParser.FoundSpanBegin = FoundSpanBegin;
				spanParser.FoundSpanEnd = FoundSpanEnd;
				spanParser.FoundSpanExit = FoundSpanExit;
				spanParser.ParseChar += ParseChar;
				if (line == null)
					throw new ArgumentNullException ("line");
			}

			Chunk startChunk = null;
			Chunk endChunk;

			void AddRealChunk (Chunk chunk)
			{
				if (startChunk == null) {
					startChunk = endChunk = chunk;
					chunk.SpanStack = spanParser.SpanStack.Clone ();
					return;
				}

				if (endChunk.Style.Equals (chunk.Style) && chunk.SpanStack.Equals (endChunk.SpanStack)) {
					endChunk.Length += chunk.Length;
					return;
				}

				endChunk = endChunk.Next = chunk;
				chunk.SpanStack = spanParser.SpanStack.Clone ();
			}

			void AddChunk (ref Chunk curChunk, int length, string style)
			{
				if (curChunk.Length > 0) {
					AddRealChunk (curChunk);
					curChunk = new Chunk (curChunk.EndOffset, 0, defaultStyle);
				}
				if (length > 0) {
					curChunk.Style  = style;
					curChunk.Length = length;
					AddRealChunk (curChunk);
				}
				curChunk = new Chunk (curChunk.EndOffset, 0, defaultStyle);
				curChunk.Style = GetSpanStyle ();
			}

			string GetChunkStyleColor (string topColor)
			{
				if (!String.IsNullOrEmpty (topColor))
					return topColor;
				if (String.IsNullOrEmpty (spanParser.SpanStack.Peek ().Color)) {
					Span span = spanParser.SpanStack.Pop ();
					string result = GetChunkStyleColor (topColor);
					spanParser.SpanStack.Push (span);
					return result;
				}
				return spanParser.SpanStack.Count > 0 ? spanParser.SpanStack.Peek ().Color : defaultStyle;
			}

			string GetSpanStyle ()
			{
				if (spanParser.SpanStack.Count == 0)
					return defaultStyle;
				if (String.IsNullOrEmpty (spanParser.SpanStack.Peek ().Color)) {
					Span span = spanParser.SpanStack.Pop ();
					string result = GetSpanStyle ();
					spanParser.SpanStack.Push (span);
					return result;
				}
				string rule = spanParser.SpanStack.Peek ().Rule;
				if (!string.IsNullOrEmpty (rule) && rule.StartsWith ("mode:"))
					return defaultStyle;
				return spanParser.SpanStack.Peek ().Color;
			}

			Chunk curChunk;

			string GetChunkStyle (Span span)
			{
				if (span == null)
					return GetSpanStyle ();
				return span.Color ?? GetSpanStyle ();
			}

			public void FoundSpanBegin (Span span, int offset, int length)
			{
				curChunk.Length = offset - curChunk.Offset;
				curChunk.Style  = GetStyle (curChunk);
				if (string.IsNullOrEmpty (curChunk.Style)) {
					Span tmpSpan = spanParser.SpanStack.Count > 0 ? spanParser.SpanStack.Pop () : null;
					curChunk.Style = GetSpanStyle ();
					if (tmpSpan != null)
						spanParser.SpanStack.Push (tmpSpan);
				}
				AddChunk (ref curChunk, 0, curChunk.Style);

				Rule spanRule = spanParser.GetRule (span);
				
				if (spanRule == null)
					throw new Exception ("Rule " + span.Rule + " not found in " + span);
				spanParser.PushSpan (span, spanRule);

				curChunk.Offset = offset;
				curChunk.Length = length;
				curChunk.Style  = span.TagColor ?? GetChunkStyle (span);
				curChunk.SpanStack.Push (span);
				AddChunk (ref curChunk, 0, curChunk.Style);
				foreach (SemanticRule semanticRule in spanRule.SemanticRules) {
					semanticRule.Analyze (this.doc, line, curChunk, offset, line.EndOffset);
				}
			}

			public void FoundSpanExit (Span span, int offset, int length)
			{
				curChunk.Length = offset - curChunk.Offset;
				AddChunk (ref curChunk, 0, GetChunkStyle (span));
				spanParser.PopSpan ();

			}

			public void FoundSpanEnd (Span span, int offset, int length)
			{
				curChunk.Length = offset - curChunk.Offset;
				curChunk.Style  = GetStyle (curChunk) ?? GetChunkStyle (span);
				AddChunk (ref curChunk, 0, defaultStyle);

				curChunk.Offset = offset;
				curChunk.Length = length;
				curChunk.Style  = span.TagColor ?? GetChunkStyle (span);
				AddChunk (ref curChunk, 0, defaultStyle);
				spanParser.PopSpan ();
			}

			bool inWord = false;
			public void ParseChar (ref int i, char ch)
			{
				int textOffset = i - spanParser.StartOffset;

				Rule cur = spanParser.CurRule;
				bool isWordPart = cur.Delimiter.IndexOf (ch) < 0;

				if (inWord && !isWordPart || !inWord && isWordPart)
					AddChunk (ref curChunk, 0, curChunk.Style = GetStyle (curChunk) ?? GetSpanStyle ());

				inWord = isWordPart;

				if (cur.HasMatches && i - curChunk.Offset == 0) {
					Match foundMatch = null;
					int   foundMatchLength = 0;
					foreach (Match ruleMatch in cur.Matches) {
						int matchLength = ruleMatch.TryMatch (spanParser.CurText, textOffset);
						if (foundMatchLength < matchLength) {
							foundMatch = ruleMatch;
							foundMatchLength = matchLength;
						}
					}
					if (foundMatch != null) {
						AddChunk (ref curChunk, foundMatchLength, GetChunkStyleColor (foundMatch.Color));
						i += foundMatchLength - 1;
						curChunk.Length = i - curChunk.Offset + 1;
						return;
					}
				}

				curChunk.Length = i - curChunk.Offset + 1;
			}

			protected virtual string GetStyle (Chunk chunk)
			{
				if (chunk.Length > 0) {
					Keywords keyword = spanParser.CurRule.GetKeyword (doc, chunk.Offset, chunk.Length);
					if (keyword != null)
						return keyword.Color;
				}
				return null;
			}

			public virtual Chunk GetChunks (int offset, int length)
			{
				if (lineOffset < offset)
					SyntaxModeService.ScanSpans (doc, mode, spanParser.CurRule, spanParser.SpanStack, lineOffset, offset);
				length = System.Math.Min (doc.Length - offset, length);
				curChunk = new Chunk (offset, 0, GetSpanStyle ());
				spanParser.ParseSpans (offset, length);
				curChunk.Length = offset + length - curChunk.Offset;
				if (curChunk.Length > 0) {
					curChunk.Style = GetStyle (curChunk) ?? GetSpanStyle ();
					AddRealChunk (curChunk);
				}
				return startChunk;
			}
		}

		public override Rule GetRule (string name)
		{
			if (name == null || name == "<root>") {
				return this;
			}
			if (name.StartsWith ("mode:"))
				return SyntaxModeService.GetSyntaxMode (name.Substring ("mode:".Length));

			foreach (Rule rule in rules) {
				if (rule.Name == name)
					return rule;
			}
			return this;
		}

		void AddSemanticRule (Rule rule, SemanticRule semanticRule)
		{
			if (rule != null)
				rule.SemanticRules.Add (semanticRule);
		}

		public void AddSemanticRule (SemanticRule semanticRule)
		{
			AddSemanticRule (this, semanticRule);
		}

		public void AddSemanticRule (string addToRuleName, SemanticRule semanticRule)
		{
			AddSemanticRule (GetRule (addToRuleName), semanticRule);
		}

		void RemoveSemanticRule (Rule rule, Type type)
		{
			if (rule != null) {
				for (int i = 0; i < rule.SemanticRules.Count; i++) {
					if (rule.SemanticRules[i].GetType () == type) {
						rule.SemanticRules.RemoveAt (i);
						i--;
					}
				}
			}
		}
		public void RemoveSemanticRule (Type type)
		{
			RemoveSemanticRule (this, type);
		}
		public void RemoveSemanticRule (string removeFromRuleName, Type type)
		{
			RemoveSemanticRule (GetRule (removeFromRuleName), type);
		}

		public override string ToString ()
		{
			return String.Format ("[SyntaxMode: Name={0}, MimeType={1}]", Name, MimeType);
		}

		new const string Node = "SyntaxMode";

		public const string MimeTypesAttribute = "mimeTypes";

		public static SyntaxMode Read (XmlReader reader)
		{
			SyntaxMode result = new SyntaxMode ();
			List<Match> matches = new List<Match> ();
			List<Span> spanList = new List<Span> ();
			List<Marker> prevMarkerList = new List<Marker> ();
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case Node:
					string extends = reader.GetAttribute ("extends");
					if (!String.IsNullOrEmpty (extends)) {
						result = (SyntaxMode)SyntaxModeService.GetSyntaxMode (extends).MemberwiseClone ();
					}
					result.Name       = reader.GetAttribute ("name");
					result.MimeType   = reader.GetAttribute (MimeTypesAttribute);
					if (!String.IsNullOrEmpty (reader.GetAttribute ("ignorecase")))
						result.IgnoreCase = Boolean.Parse (reader.GetAttribute ("ignorecase"));
					return true;
				case Rule.Node:
					result.rules.Add (Rule.Read (result, reader, result.IgnoreCase));
					return true;
				}
				return result.ReadNode (reader, matches, spanList, prevMarkerList);
			});
			result.spans   = spanList.ToArray ();
			result.prevMarker = prevMarkerList.ToArray ();
			result.matches = matches.ToArray ();
			return result;
		}
	}
}
