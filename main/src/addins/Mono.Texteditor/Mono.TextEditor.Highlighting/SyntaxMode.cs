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
using System.Text.RegularExpressions;
using System.Xml;


namespace Mono.TextEditor.Highlighting
{
	public class SyntaxMode : Rule
	{
		public static readonly SyntaxMode Default = new SyntaxMode ();
		string mimeType;
		List<Rule> rules = new List<Rule> ();
		
		public string MimeType {
			get {
				return mimeType;
			}
		}
		
		public ReadOnlyCollection<Rule> Rules {
			get {
				return rules.AsReadOnly ();
			}
		}
		
		public SyntaxMode()
		{
		}
		
		public Chunk[] GetChunks (Document doc, Style style, LineSegment line, int offset, int length)
		{
			return new ChunkParser (doc, style, this, line).GetChunks (offset, length);
		}
		
		class ChunkParser
		{
			Rule curRule;
			SyntaxMode mode;
			Style style;
			Stack<Span> spanStack;
			Document doc;
			LineSegment line;
			
			List<Chunk> result = new List<Chunk> ();
			Dictionary<char, Rule.Pair<Keywords, object>> tree;
			Dictionary<char, Pair<Span, object>> spanTree;			
			Rule.Pair<Keywords, object> pair     = null;
			Rule.Pair<Span, object>   spanPair = null;
			
			Span curSpan;
			
			public ChunkParser (Document doc, Style style, SyntaxMode mode, LineSegment line)
			{
				this.doc  = doc;
				this.style = style;
				this.mode = mode;
				this.line = line;
			}
			void AddChunk (ref Chunk curChunk, int length, ChunkStyle style)
			{
				if (curChunk.Length > 0) {
					result.Add (curChunk);
					curChunk = new Chunk (curChunk.EndOffset, 0, ChunkStyle.Default);
				}
				if (curChunk.Style == ChunkStyle.Default)
					curChunk.Style = style;
				curChunk.Length = length;
				result.Add (curChunk);
				curChunk = new Chunk (curChunk.EndOffset, 0, ChunkStyle.Default);
				curChunk.Style.Color = this.style.Default;
			}
			
			ChunkStyle GetChunkStyleColor (Stack<Span> spanStack, Keywords word)
			{
				ChunkStyle result ;
				if (!String.IsNullOrEmpty (word.Color)) {
					result = style.GetChunkStyle (word.Color);
				} else {
					result = spanStack.Count > 0 ? style.GetChunkStyle (spanStack.Peek ().Color) : ChunkStyle.Default;
				}
				return result;
			}
			
			ChunkStyle GetSpanStyle (Stack<Span> spanStack)
			{
				if (spanStack.Count == 0)
					return ChunkStyle.Default;
				return style.GetChunkStyle (spanStack.Peek ().Color);
			}
			
			void SetTree ()
			{
				curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				pair    = null;
				if (curSpan != null) { 
					curRule = mode.GetRule (curSpan.Rule);
					if (curRule == null) {
						tree     = null;
					} else {
						tree     = curRule.parseTree;
					}
				} else {
					tree     = curRule.parseTree;
				}
				wordOffset = 0;
			}
			
			void SetSpan ()
			{
				curSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				if (curSpan != null) { 
					curRule = mode.GetRule (curSpan.Rule);
					if (curRule == null) {
						curRule  = mode;
						spanTree = null;
					} else {
						spanTree = curRule.spanTree;
					}
				} else {
					curRule  = mode;
					spanTree = curRule.spanTree;
				}
			}
			
			int wordOffset =  0; 
			
			Chunk curChunk;
			int maxEnd;
			
			public Chunk[] GetChunks (int offset, int length)
			{
				curRule = mode;
				spanStack = line.StartSpan != null ? new Stack<Span> (line.StartSpan) : new Stack<Span> ();
				SyntaxModeService.ScanSpans (doc, curRule, spanStack, line.Offset, offset);
				pair = null;
				curChunk = new Chunk (offset, 0, GetSpanStyle (spanStack));
				maxEnd = System.Math.Min (offset + length, line.Offset + line.EditableLength);
				
				int endOffset = 0;
				SetSpan ();
				SetTree ();
				bool isNoKeyword = false;
				bool isAfterSpan = false;
				for (int i = offset; i < maxEnd; i++) {
					char ch = doc.Buffer.GetCharAt (i);
					if (curSpan != null && !String.IsNullOrEmpty (curSpan.End)) {
						if (curSpan.End[endOffset] == ch) {
							endOffset++;
							if (endOffset >= curSpan.End.Length) {
								curChunk.Length -= curSpan.End.Length - 1;
								AddChunk (ref curChunk, curSpan.End.Length, !String.IsNullOrEmpty (curSpan.TagColor) ? style.GetChunkStyle (curSpan.TagColor) : GetSpanStyle (spanStack));
								spanStack.Pop ();
								SetSpan ();
								SetTree ();
								curChunk.Style  = GetSpanStyle (spanStack);
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
					}
					if (spanTree != null && spanTree.ContainsKey (ch)) {
						spanPair = spanTree[ch];
						spanTree = (Dictionary<char, Rule.Pair<Span, object>>)spanPair.o2;
						if (spanPair.o1 != null) {
							Span span = spanPair.o1;
							if (!String.IsNullOrEmpty(span.Constraint)) {
								if (span.Constraint.Length == 2 && span.Constraint.StartsWith ("!") && i + 1 < maxEnd) 
									if (doc.Buffer.GetCharAt (i + 1) == span.Constraint [1]) {
										goto skip;
									}
							}
							spanStack.Push (span);
							curChunk.Length -= span.Begin.Length - 1;
							
							AddChunk (ref curChunk, span.Begin.Length, !String.IsNullOrEmpty (span.TagColor) ? style.GetChunkStyle (span.TagColor) : GetSpanStyle (spanStack));
							SetSpan ();
							SetTree ();
							if (!String.IsNullOrEmpty (span.NextColor))
								curChunk.Style = style.GetChunkStyle (span.NextColor);
							continue;
						}
					} else {
						SetSpan ();
					}
				 skip:
						;
					if (!Char.IsLetterOrDigit (ch) && pair != null && pair.o1 != null) {
						curChunk.Length -= wordOffset;
						AddChunk (ref curChunk, wordOffset, GetChunkStyleColor (spanStack, pair.o1));
						isNoKeyword = false;
					}
					
					if (tree != null) {
						if (!isNoKeyword && tree.ContainsKey (ch)) {
							wordOffset++;
							pair = tree[ch];
							tree = (Dictionary<char, Rule.Pair<Keywords, object>>)pair.o2;
						} else {
							SetTree ();
							if (!Char.IsLetterOrDigit (ch)) {
								isNoKeyword = false;
							}  else 
								isNoKeyword = true;
						}
					} else {
						SetTree ();
						isNoKeyword = false;
					}
					
					curChunk.Length++;
					curChunk.Style = GetSpanStyle (spanStack);
				}
				curChunk.Length = maxEnd - curChunk.Offset;
				if (pair != null && pair.o1 != null) {
					curChunk.Length -= wordOffset;
					AddChunk (ref curChunk, wordOffset, GetChunkStyleColor (spanStack, pair.o1));
					curChunk.Style = GetChunkStyleColor (spanStack, pair.o1);
				}
				if (curChunk.Length > 0) {
					result.Add (curChunk);
				}
				
				return result.ToArray ();
			}
		}
		
		public Rule GetRule (string name)
		{
			foreach (Rule rule in rules) {
				if (rule.Name == name)
					return rule;
			}
			return null;
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
			XmlReadHelper.ReadList (reader, Node, delegate () {
				switch (reader.LocalName) {
				case Node:
					result.name     = reader.GetAttribute ("name");
					result.mimeType = reader.GetAttribute (MimeTypesAttribute);
					return true;
				case Rule.Node:
					result.rules.Add (Rule.Read (reader));
					return true;
				}
				return result.ReadNode (reader);
			});
			result.SetupSpanTree ();
			result.SetupParseTree ();
			return result;
		}
	}
}
