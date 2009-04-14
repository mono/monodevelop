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
using System.Text.RegularExpressions;
using System.Xml;


namespace Mono.TextEditor.Highlighting
{
	public class SyntaxMode : Rule
	{
		public static readonly SyntaxMode Default = new SyntaxMode ();
		List<Rule> rules = new List<Rule> ();
		
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
			return new ChunkParser (doc, style, this, line).GetChunks (offset, length);
		}
		
		public virtual string GetTextWithoutMarkup (Document doc, Style style, int offset, int length)
		{
			return doc.GetTextAt (offset, length);
		}
		
		public string GetMarkup (Document doc, ITextEditorOptions options, Style style, int offset, int length, bool removeIndent)
		{
			int curOffset = offset;
			int indentLength = int.MaxValue;
			while (curOffset < offset + length) {
				LineSegment line = doc.GetLineByOffset (curOffset);
				indentLength = System.Math.Min (indentLength, line.GetIndentation (doc).Length);
				curOffset = line.EndOffset + 1;
			}
			curOffset = offset;
			
			StringBuilder result = new StringBuilder ();
			while (curOffset < offset + length && curOffset < doc.Length) {
				LineSegment line = doc.GetLineByOffset (curOffset);
				int toOffset = System.Math.Min (line.Offset + line.EditableLength, offset + length);
				Stack<ChunkStyle> styleStack = new Stack<ChunkStyle> ();
				for (Chunk chunk = GetChunks (doc, style, line, curOffset, toOffset - curOffset); chunk != null; chunk = chunk.Next) {
					
					ChunkStyle chunkStyle = chunk.GetChunkStyle (style);
					bool setBold   = chunkStyle.Bold && (styleStack.Count == 0 || !styleStack.Peek ().Bold);
					bool setItalic = chunkStyle.Italic && (styleStack.Count == 0 || !styleStack.Peek ().Italic);
					bool setUnderline = chunkStyle.Underline && (styleStack.Count == 0 || !styleStack.Peek ().Underline);
					bool setColor = styleStack.Count == 0 || TextViewMargin.GetPixel (styleStack.Peek ().Color) != TextViewMargin.GetPixel (chunkStyle.Color);
					if (setColor || setBold || setItalic || setUnderline) {
						if (styleStack.Count > 0) {
							result.Append("</span>");
							styleStack.Pop ();
						}
						result.Append("<span");
						result.Append(" foreground=\"");
						result.Append(String.Format ("#{0:X2}{1:X2}{2:X2}", 
						                             chunkStyle.Color.Red   >> 8,
						                             chunkStyle.Color.Green >> 8,
						                             chunkStyle.Color.Blue  >> 8));
						result.Append("\"");
						if (chunkStyle.Bold)
							result.Append(" weight=\"bold\"");
						if (chunkStyle.Italic)
							result.Append(" style=\"italic\"");
						if (chunkStyle.Underline)
							result.Append(" underline=\"single\"");
						result.Append(">");
						styleStack.Push (chunkStyle);
					}
					
					for (int i = 0; i < chunk.Length; i++) {
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
							result.Append (new string (' ', options.TabSize));
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
		
		internal class SpanParser
		{
			protected SyntaxMode mode;
			protected Stack<Span> spanStack;
			protected Stack<Rule> ruleStack;

			protected Document doc;
			protected LineSegment line;
			
			int ruleStart;
			int maxEnd;
			
			protected Rule CurRule {
				get {
					return ruleStack.Peek ();
				}
			}
			
			protected Span CurSpan {
				get {
					if (spanStack.Count == 0)
						return null;
					return spanStack.Peek ();
				}
			}
			
			public SpanParser (Document doc, SyntaxMode mode, LineSegment line, Stack<Span> spanStack)
			{
				if (doc == null)
					throw new ArgumentNullException ("doc");
				if (mode == null)
					throw new ArgumentNullException ("mode");
				this.doc  = doc;
				this.mode = mode;
				this.line = line;
				this.spanStack = spanStack ?? new Stack<Span> (line.StartSpan != null ? line.StartSpan : new Span[0]);
				//this.ruleStack = ruleStack ?? new Stack<Span> (line.StartRule != null ? line.StartRule : new Rule[0]);
				
				ruleStack = new Stack<Rule> ();
				ruleStack.Push (mode);
				foreach (Span span in this.spanStack) {
					Rule rule = CurRule.GetRule (span.Rule);
					ruleStack.Push (rule ?? CurRule);
				}
			}
			
			Rule GetRule (Span span)
			{
				if (string.IsNullOrEmpty (span.Rule))
					return new Rule (mode);
				return CurRule.GetRule (span.Rule);
			}
			
			public virtual void FoundSpanBegin (Span span, int offset, RegexMatch match)
			{
			}
			
			public virtual void FoundSpanExit (Span span, int offset, RegexMatch match)
			{
			}
			
			public virtual void FoundSpanEnd (Span span, int offset, RegexMatch match)
			{
			}
			
			public virtual void ParseChar (ref int i, char ch)
			{
			}
			
			public void ParseSpans (int offset, int length)
			{
				this.ruleStart = offset;
				maxEnd = System.Math.Min (doc.Length, System.Math.Min (offset + length, line != null ? System.Math.Min (line.Offset + line.EditableLength, doc.Length) : doc.Length));
				
				int endOffset = 0;
				int len = maxEnd - offset;
				
				for (int i = offset; i < maxEnd; i++) {
					Span cur = CurSpan;
					if (cur != null) {
						if (cur.Escape != null) {
							bool mismatch = false;
							for (int j = 0; j < cur.Escape.Length; j++) {
								if (i + j >= doc.Length || doc.GetCharAt (i + j) != cur.Escape[j]) {
									mismatch = true;
									break;
								}
							}
							if (!mismatch) {
								i++;
								continue;
							}
						} 
							
						if (cur.End != null) {
							RegexMatch match = cur.End.TryMatch (doc, i);
							if (match.Success) {
								FoundSpanEnd (cur, i, match);
								spanStack.Pop ();
								ruleStack.Pop ();
								endOffset = 0;
								continue;
							}
						}
						
						if (cur.Exit != null) {
							RegexMatch match = cur.Exit.TryMatch (doc, i);
							if (match.Success) {
								spanStack.Pop ();
								ruleStack.Pop ();
								FoundSpanExit (cur, i, match);
								endOffset = 0;
								continue;
							}
						}
					}
					
					foreach (Span span in CurRule.Spans) {
						if (span.BeginFlags.Contains ("startsLine") && line != null && i != line.Offset)
							continue;
						
						RegexMatch match = span.Begin.TryMatch (doc, i);
						if (match.Success) {
							bool mismatch = false;
							if (span.BeginFlags.Contains ("firstNonWs") && line != null) {
								for (int k = line.Offset; k < i; k++) {
									if (!Char.IsWhiteSpace (doc.GetCharAt (k))) {
										mismatch = true;
										break;
									}
								}
							}
							if (mismatch)
								continue;
							FoundSpanBegin (span, i, match);
							spanStack.Push (span);
							ruleStack.Push (GetRule (span));
							endOffset = 0;
							
							i += match.Length - 1;
							break;
						}
					}
					if (i < doc.Length)
						ParseChar (ref i, doc.GetCharAt (i));
				}
			}
		}
		
		class ChunkParser : SpanParser
		{
			readonly string defaultStyle = "text";
			List<Chunk> result = new List<Chunk> ();
			
			int ruleStart;
			
			public ChunkParser (Document doc, Style style, SyntaxMode mode, LineSegment line) : base (doc, mode, line, null)
			{
				if (line == null)
					throw new ArgumentNullException ("line");
			}
			
			Chunk startChunk = null;
			Chunk endChunk;
			void AddRealChunk (Chunk chunk)
			{
				const int MaxChunkLength = 80;
				int divisor = chunk.Length / MaxChunkLength;
				int reminder = chunk.Length % MaxChunkLength;
				for (int i = 0; i < divisor; i++) {
					Chunk newChunk = new Chunk (chunk.Offset + i * MaxChunkLength, MaxChunkLength, chunk.Style);
					if (startChunk == null)
						startChunk = endChunk = newChunk;
					else 
						endChunk = endChunk.Next = newChunk;
				}
				if (reminder > 0) {
					Chunk newChunk = new Chunk (chunk.Offset + divisor * MaxChunkLength, reminder, chunk.Style);
					if (startChunk == null)
						startChunk = endChunk = newChunk;
					else 
						endChunk = endChunk.Next = newChunk;
				}
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
				if (String.IsNullOrEmpty (spanStack.Peek ().Color)) {
					Span span = spanStack.Pop ();
					string result = GetChunkStyleColor (topColor);
					spanStack.Push (span);
					return result;
				}
				return spanStack.Count > 0 ? spanStack.Peek ().Color : defaultStyle;
			}
			
			string GetSpanStyle ()
			{
				if (spanStack.Count == 0)
					return defaultStyle;
				if (String.IsNullOrEmpty (spanStack.Peek ().Color)) {
					Span span = spanStack.Pop ();
					string result = GetSpanStyle ();
					spanStack.Push (span);
					return result;
				}
				if (spanStack.Peek ().Rule.StartsWith ("mode:"))
					return defaultStyle;
				return spanStack.Peek ().Color;
			}
			
			Chunk curChunk;
			int maxEnd;
			
			string GetChunkStyle (Span span) 
			{
				if (span == null)
					return GetSpanStyle ();
				return span.TagColor ?? span.Color ?? GetSpanStyle ();
			}
			
			public override void FoundSpanBegin (Span span, int offset, RegexMatch match)
			{
				curChunk.Length = offset - curChunk.Offset;
				curChunk.Style  = GetStyle (curChunk) ?? GetSpanStyle ();
				AddChunk (ref curChunk, 0, curChunk.Style);
				
				curChunk.Offset = offset;
				curChunk.Length = match.Length;
				curChunk.Style  = GetChunkStyle (span);
				Console.WriteLine ("style:" + curChunk.Style);
				AddChunk (ref curChunk, 0, curChunk.Style);
				/*
				curChunk.Length = offset - curChunk.Offset;
				AddChunk (ref curChunk, match.Length, GetChunkStyle (span));*/
			}
			
			public override void FoundSpanExit (Span span, int offset, RegexMatch match)
			{
				curChunk.Length = offset - curChunk.Offset;
				AddChunk (ref curChunk, 0, GetChunkStyle (span));
			}
			
			public override void FoundSpanEnd (Span span, int offset, RegexMatch match)
			{
				curChunk.Length = offset - curChunk.Offset;
				curChunk.Style  = GetStyle (curChunk) ?? GetChunkStyle (span);
				AddChunk (ref curChunk, 0, defaultStyle);
				
				curChunk.Offset = offset;
				curChunk.Length = match.Length;
				curChunk.Style  = GetChunkStyle (span);
				AddChunk (ref curChunk, 0, defaultStyle);
			}
			
			bool inWord = false;
			public override void ParseChar (ref int i, char ch)
			{
				int textOffset = i - line.Offset;
				
				bool isWordPart = Char.IsLetterOrDigit (ch) || ch == '_';
				
				if (inWord && !isWordPart || !inWord && isWordPart) 
					AddChunk (ref curChunk, 0, curChunk.Style = GetStyle (curChunk) ?? GetSpanStyle ());
				
				inWord = isWordPart;
				
				if (CurRule.HasMatches && i - curChunk.Offset == 0) {
					Match foundMatch = null;
					int   foundMatchLength = 0;
					foreach (Match ruleMatch in CurRule.Matches) {
						int matchLength = ruleMatch.TryMatch (str, textOffset);
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
			
			string GetStyle (Chunk chunk)
			{
				if (chunk.Length > 0) {
					Keywords keyword = CurRule.GetKeyword (doc, chunk.Offset, chunk.Length); 
					if (keyword != null)
						return keyword.Color;
				}
				return null;
			}
			
			string str;
			int curOffset;
			public virtual Chunk GetChunks (int offset, int length)
			{
				SyntaxModeService.ScanSpans (doc, mode, CurRule, spanStack, line.Offset, offset);
				curOffset = offset;
				str = length > 0 ? doc.GetTextAt (offset, length) : null;
				curChunk = new Chunk (offset, 0, GetSpanStyle ());
				ParseSpans (offset, length);
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
		
		new public static SyntaxMode Read (XmlReader reader)
		{
			SyntaxMode result = new SyntaxMode ();
			List<Match> matches = new List<Match> ();
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case Node:
					string extends = reader.GetAttribute ("extends");
					if (!String.IsNullOrEmpty (extends)) {
						result = SyntaxModeService.GetSyntaxMode (extends);
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
				return result.ReadNode (reader, matches);
			});
			result.matches = matches.ToArray ();
			return result;
		}
	}
}
