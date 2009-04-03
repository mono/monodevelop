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
		
		public SyntaxMode()
		{
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
		
		class ChunkParser
		{
			Rule curRule;
			SyntaxMode mode;
			//Style style;
			Stack<Span> spanStack;
			Document doc;
			LineSegment line;
			readonly string defaultStyle = "text";
			List<Chunk> result = new List<Chunk> ();
			Dictionary<char, Rule.Pair<Keywords, object>> tree;
			Dictionary<char, Span[]> spanTree;
			Rule.Pair<Keywords, object> pair     = null;
			
			Span curSpan;
			int ruleStart;
			
			public ChunkParser (Document doc, Style style, SyntaxMode mode, LineSegment line)
			{
				this.doc  = doc;
			//	this.style = style;
				this.mode = mode;
				this.line = line;
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
				curChunk.Style = style;
				curChunk.Length = length;
				AddRealChunk (curChunk);
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
				
				return spanStack.Peek ().Color;
			}
			
			void SetTree ()
			{
				curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				pair    = null;
				if (curSpan != null) { 
					if (String.IsNullOrEmpty (curSpan.Rule)) {
						Span span = spanStack.Pop ();
						SetTree ();
						spanStack.Push (span);
						return;
					}
					curRule = mode.GetRule (curSpan.Rule);
					tree     = curRule.parseTree;
				} else {
					tree     = curRule.parseTree;
				}
				wordOffset = 0;
			}
			
			void SetSpan (int offset)
			{
				curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				foreach (SemanticRule semanticRule in curRule.SemanticRules) {
					semanticRule.Analyze (this.doc, line, result, ruleStart, offset);
				}
				if (curSpan != null) { 
					if (String.IsNullOrEmpty (curSpan.Rule)) {
						Span span = spanStack.Pop ();
						SetSpan (offset);
						spanStack.Push (span);
						return;
					}
					curRule  = mode.GetRule (curSpan.Rule);
					spanTree = curRule.spanStarts;
				} else {
					curRule  = mode;
					spanTree = curRule.spanStarts;
				}
				
				ruleStart = offset;
			}
			
			int wordOffset =  0; 

			Chunk curChunk;
			int maxEnd;
			
			public virtual Chunk GetChunks (int offset, int length)
			{
				curRule = mode;
				startChunk = null;
				this.ruleStart = offset;
				spanStack = line.StartSpan != null ? new Stack<Span> (line.StartSpan) : new Stack<Span> ();
				SyntaxModeService.ScanSpans (doc, curRule, spanStack, line.Offset, offset);
				pair = null;
				curChunk = new Chunk (offset, 0, GetSpanStyle ());
				maxEnd = System.Math.Min (offset + length, System.Math.Min (line.Offset + line.EditableLength, doc.Length));
				
				int endOffset = 0;
				SetSpan (offset);
				SetTree ();
				bool isNoKeyword = false;
				int len = maxEnd - offset;
				string str = len > 0 ? doc.GetTextAt (offset, len) : null;
				
				for (int i = offset; i < maxEnd; i++) {
					int textOffset = i - offset;
					char ch = str [textOffset];
					
					if (curSpan != null && !String.IsNullOrEmpty (curSpan.End)) {
						if (!String.IsNullOrEmpty (curSpan.Escape) && i + 1 < maxEnd && endOffset == 0) {
							bool match = true;
							for (int j = 0; j < curSpan.Escape.Length && i + j < maxEnd; j++) {
								if (doc.GetCharAt (i + j) != curSpan.Escape[j]) {
									match = false;
									break;
								}
							}
							if (match) {
								curChunk.Length += curSpan.Escape.Length;
								if (curSpan.Escape.Length == 1)
									curChunk.Length++;
								i += curSpan.Escape.Length - 1;
								continue;
							}
						}
						
						if (curSpan.End[endOffset] == ch) {
							// check if keyword is before span end
							if (pair != null && pair.o1 != null) {
								curChunk.Length -= wordOffset;
								AddChunk (ref curChunk, wordOffset, GetChunkStyleColor (pair.o1.Color));
								isNoKeyword = false;
							}
							
							endOffset++;
							if (endOffset >= curSpan.End.Length) {
								curChunk.Length -= curSpan.End.Length - 1;
								AddChunk (ref curChunk, curSpan.End.Length, !String.IsNullOrEmpty (curSpan.TagColor) ? curSpan.TagColor : GetSpanStyle ());
								spanStack.Pop ();
								SetSpan (i);
								SetTree ();
								curChunk.Style  = GetSpanStyle ();
								endOffset = 0;
								continue;
							}
						} else if (endOffset != 0) {
							endOffset = 0;
							if (curSpan.End[endOffset] == ch) {
								i--;
								continue;
							}
						}
						if (String.IsNullOrEmpty (curSpan.Rule)) {
							curChunk.Length++;
							continue;
						}
							
					}
					if (spanTree != null && spanTree.ContainsKey (ch)) {
						bool found = false;
						Span[] spanList = spanTree[ch];
						for (int l = 0; l < spanList.Length; l++) {
							Span span = spanList[l];
							if (span.BeginFlags.Contains ("startsLine") && i != line.Offset)
								continue;
							bool mismatch = false;
							for (int j = 1; j < span.Begin.Length; j++) {
								if (i + j >= doc.Length || span.Begin [j] != doc.GetCharAt (i + j)) {
									mismatch = true;
									break;
								}
							}
							if (!mismatch && span.BeginFlags.Contains ("firstNonWs")) {
								for (int k = line.Offset; k < i; k++) {
									if (!Char.IsWhiteSpace (doc.GetCharAt (k))) {
										mismatch = true;
										break;
									}
								}
							}
							if (!mismatch) {
								spanStack.Push (span);
//								curChunk.Length -= span.Begin.Length - 1;
								
								AddChunk (ref curChunk, span.Begin.Length, !String.IsNullOrEmpty (span.TagColor) ? span.TagColor : GetSpanStyle ());
								SetSpan (i);
								endOffset = 0;
								SetTree ();
								if (!String.IsNullOrEmpty (span.NextColor))
									curChunk.Style = span.NextColor;
								i += span.Begin.Length - 1;
								found = true; 
								break;
							}
						}
						if (found) 
							continue;
					}
//				 skip:
//						;
					if (!Char.IsLetterOrDigit (ch) && ch != '_' && pair != null && pair.o1 != null) {
						curChunk.Length -= wordOffset;
						AddChunk (ref curChunk, wordOffset, GetChunkStyleColor (pair.o1.Color));
						isNoKeyword = false;
					}
					
					if (!isNoKeyword && wordOffset == 0 && curRule.HasMatches) {
						Match foundMatch = null;
						int   foundMatchLength = 0;
						foreach (Match ruleMatch in curRule.Matches) {
							int matchLength = ruleMatch.TryMatch (str, textOffset);
							if (foundMatchLength < matchLength) {
								foundMatch = ruleMatch;
								foundMatchLength = matchLength;
							}
						}
						if (foundMatch != null) {
							AddChunk (ref curChunk, foundMatchLength, GetChunkStyleColor (foundMatch.Color));
							i += foundMatchLength - 1;
							continue;
						}
					}
					
					if (tree != null) {
						if (!isNoKeyword && tree.ContainsKey (ch)) {
							wordOffset++;
							pair = tree[ch];
							tree = (Dictionary<char, Rule.Pair<Keywords, object>>)pair.o2;
						} else {
							SetTree ();
							isNoKeyword = Char.IsLetterOrDigit (ch) || ch == '_';
						}
					} else {
						SetTree ();
						isNoKeyword = false;
					}
					
					curChunk.Length++;
				}
				curChunk.Length = maxEnd - curChunk.Offset;
				if (pair != null && pair.o1 != null) {
					curChunk.Length -= wordOffset;
					AddChunk (ref curChunk, wordOffset, GetChunkStyleColor (pair.o1.Color));
					curChunk.Style = GetChunkStyleColor (pair.o1.Color);
				}
				
				if (curChunk.Length > 0) 
					AddRealChunk (curChunk);
				
				SetSpan (maxEnd);
				return startChunk;
			}
		}
		
		public Rule GetRule (string name)
		{
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
					result.rules.Add (Rule.Read (reader));
					return true;
				}
				return result.ReadNode (reader, matches);
			});
			result.matches = matches.ToArray ();
			result.SetupSpanTree ();
			result.SetupParseTree ();
			return result;
		}
	}
}
