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
using System.Text;
using System.Xml;
using System.IO;

namespace Mono.TextEditor.Highlighting
{
	public class SyntaxMode : Rule, ISyntaxMode
	{
		protected TextDocument doc;

		public TextDocument Document {
			get {
				return doc;
			}
			set {
				if (doc != null) {
					doc.TextSet -= HandleTextSet;
					doc.TextReplaced -= HandleTextReplaced;
				}
				doc = value;
				if (doc != null) {
					doc.TextSet += HandleTextSet;
					doc.TextReplaced += HandleTextReplaced;
				}
				HandleTextSet (doc, EventArgs.Empty);
				OnDocumentSet (EventArgs.Empty);

			}
		}

		void HandleTextReplaced (object sender, DocumentChangeEventArgs e)
		{
			if (doc == null || doc.SuppressHighlightUpdate)
				return;
			SyntaxModeService.StartUpdate (doc, this, e.Offset, e.Offset + e.InsertionLength);
		}

		void HandleTextSet (object sender, EventArgs e)
		{
			if (doc == null || doc.SuppressHighlightUpdate)
				return;
			SyntaxModeService.StartUpdate (doc, this, 0, doc.TextLength);
		}
		
		public event EventHandler DocumentSet;
		
		protected virtual void OnDocumentSet (EventArgs e)
		{
			var handler = DocumentSet;
			if (handler != null)
				handler (this, e);
		}	
		
		internal protected List<Rule> rules = new List<Rule> ();
		
	
		public string MimeType {
			get;
			internal set;
		}

		public IEnumerable<Rule> Rules {
			get {
				return rules;
			}
		}
		
		protected SyntaxMode () : base (null)
		{
			DefaultColor = "Plain Text";
			Name = "<root>";
			this.Delimiter = "&()<>{}[]~!%^*-+=|\\#/:;\"' ,\t.?";
		}
		
		public SyntaxMode (TextDocument doc) : this ()
		{
			this.doc = doc;
		}

		public bool Validate (ColorScheme style)
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

		public virtual IEnumerable<Chunk> GetChunks (ColorScheme style, DocumentLine line, int offset, int length)
		{
			SpanParser spanParser = CreateSpanParser (line, null);
			ChunkParser chunkParser = CreateChunkParser (spanParser, style, line);
			Chunk result = chunkParser.GetChunks (chunkParser.lineOffset, line.Length);
			if (SemanticRules != null) {
				foreach (SemanticRule sematicRule in SemanticRules) {
					sematicRule.Analyze (doc, line, result, offset, offset + length);
				}
			}
			if (result != null) {
				// crop to begin
				if (result.Offset != offset) {
					while (result != null && result.EndOffset < offset) {
						result = result.Next;
					}
					if (result != null) {
						int endOffset = result.EndOffset;
						result.Offset = offset;
						result.Length = endOffset - offset;
					}
				}
			}
			while (result != null) {
				// crop to end
				if (result.EndOffset > offset + length) {
					result.Length = offset + length - result.Offset;
					if (result.Length < 0) {
						result.Length = 0;
						result.Next = null;
						yield break;
					}
				}
				yield return result;
				result = result.Next;
			}
		}

		public static string ColorToPangoMarkup (Gdk.Color color)
		{
			return string.Format ("#{0:X2}{1:X2}{2:X2}", color.Red >> 8, color.Green >> 8, color.Blue >> 8);
		}
		public static string ColorToPangoMarkup (Cairo.Color color)
		{
			return ColorToPangoMarkup ((Gdk.Color)((HslColor)color));
		}

		public static int GetIndentLength (TextDocument doc, int offset, int length, bool skipFirstLine)
		{
			int curOffset = offset;
			int indentLength = int.MaxValue;
			while (curOffset < offset + length) {
				DocumentLine line = doc.GetLineByOffset (curOffset);
				if (!skipFirstLine) {
					indentLength = System.Math.Min (indentLength, line.GetIndentation (doc).Length);
				} else {
					skipFirstLine = false;
				}
				curOffset = line.EndOffsetIncludingDelimiter + 1;
			}
			return indentLength == int.MaxValue ? 0 : indentLength;
		}

		public virtual SpanParser CreateSpanParser (DocumentLine line, CloneableStack<Span> spanStack)
		{
			return new SpanParser (this, spanStack ?? line.StartSpan.Clone ());
		}

		public virtual ChunkParser CreateChunkParser (SpanParser spanParser, ColorScheme style, DocumentLine line)
		{
			return new ChunkParser (this, spanParser, style, line);
		}

		public class SpanParser
		{
			protected SyntaxMode mode;
			protected CloneableStack<Span> spanStack;
			protected Stack<Rule> ruleStack;
			protected TextDocument doc;
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
					spanStack = value;
				}
			}

			public Stack<Rule> RuleStack {
				get {
					return ruleStack;
				}
				set {
					ruleStack = value;
					CurRule = ruleStack.Peek ();
				}
			}

			Stack<Rule> CreateRuleStack ()
			{
				var result = new Stack<Rule> ();
				if (mode == null)
					return result;
				Rule rule = mode;
				result.Push (mode);
				foreach (Span span in spanStack.Reverse ()) {
					Rule tmp = rule.GetRule (doc, span.Rule) ?? CurRule;
					result.Push (tmp);
					rule = tmp;
				}
				return result;
			}

			public SpanParser (SyntaxMode mode, CloneableStack<Span> spanStack)
			{
				if (mode == null)
					throw new ArgumentNullException ("mode");
				this.doc  = mode.Document;
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
				CurRule = rule;
				CurSpan = span;
			}

			public Span PopSpan ()
			{
				Span result = null;
				if (spanStack.Count > 0) {
					result = spanStack.Pop ();
				}
				if (ruleStack.Count > 1)
					ruleStack.Pop ();
				CurRule = ruleStack.Peek ();
				CurSpan = spanStack.Count > 0 ? spanStack.Peek () : null;
				return result;
			}

			public Rule GetRule (Span span)
			{
				if (string.IsNullOrEmpty (span.Rule))
					return new Rule (mode);
				return CurRule.GetRule (doc, span.Rule);
			}

			public delegate void CharParser (ref int i, char ch);

			public Action<Span, int, int> FoundSpanBegin;
			public Action<Span, int, int> FoundSpanExit;
			public Action<Span, int, int> FoundSpanEnd;

			public CharParser ParseChar;
			
			public StringBuilder wordBuilder = new StringBuilder ();
			
			protected bool IsFirstNonWsChar (int textOffset)
			{
				while (textOffset > 0) {
					char ch = CurText [textOffset - 1];
					if (ch != '\n' && ch != '\r' && !char.IsWhiteSpace (ch))
						return false;
					textOffset--;
				}
				return true;
			}
		
						
			protected virtual bool ScanSpan (ref int i)
			{
				int textOffset = i - StartOffset;
				for (int j = 0; j < CurRule.Spans.Length; j++) {
					Span span = CurRule.Spans [j];

					if ((span.BeginFlags & SpanBeginFlags.StartsLine) == SpanBeginFlags.StartsLine) {
						if (textOffset != 0) {
							char ch = CurText [textOffset - 1];
							if (ch != '\n' && ch != '\r')
								continue;
						}
					} 

					RegexMatch match = span.Begin.TryMatch (CurText, textOffset);
					if (!match.Success) {
						continue;
					}
					// scan for span exit which cancels the span.
					if ((span.ExitFlags & SpanExitFlags.CancelSpan) == SpanExitFlags.CancelSpan && span.Exit != null) {
						bool foundEnd = false;
						for (int k = i + match.Length; k < CurText.Length; k++) {
							if (span.Exit.TryMatch (CurText, k - StartOffset).Success)
								return false;
							if (span.End.TryMatch (CurText, k - StartOffset).Success) {
								foundEnd = true;
								break;
							}
						}
						if (!foundEnd)
							return false;
					}
					
					bool mismatch = false;
					if ((span.BeginFlags & SpanBeginFlags.FirstNonWs) == SpanBeginFlags.FirstNonWs)
						mismatch = CurText.Take (i).Any (ch => !char.IsWhiteSpace (ch));
					if (mismatch)
						continue;
					FoundSpanBegin (span, i, match.Length);
					i += System.Math.Max (0, match.Length - 1);
					return true;
				}
				return false;
			}

			protected virtual bool ScanSpanEnd (Span cur, ref int i)
			{
				int textOffset = i - StartOffset;
				
				if (cur.End != null) {
					RegexMatch match = cur.End.TryMatch (CurText, textOffset);
					if (match.Success) {
						FoundSpanEnd (cur, i, match.Length);
						i += System.Math.Max (0, match.Length - 1);
						return true;
					}
				}

				if (cur.Exit != null) {
					RegexMatch match = cur.Exit.TryMatch (CurText, textOffset);
					if (match.Success) {
						FoundSpanExit (cur, i, match.Length);
						i += System.Math.Max (0, match.Length - 1);
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
				if (offset < 0 || offset + length > doc.TextLength || length <= 0)
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
								if (textIndex + j >= CurText.Length || CurText [textIndex + j] != cur.Escape [j]) {
									mismatch = true;
									break;
								}
							}
							if (!mismatch) {
								for (int j = 0; j < cur.Escape.Length - 1 && i < maxEnd;j++) {
									ParseChar (ref i, CurText [textIndex]);
									i++;
								}
//								ScanSpanEnd (cur, ref i);
								continue;
							}
						}
						if (ScanSpanEnd (cur, ref i))
							continue;

					}
					if (!ScanSpan (ref i)) {
						if (i < doc.TextLength)
							ParseChar (ref i, CurText [textIndex]);
					}
				}
			}
		}

		public class ChunkParser
		{
			readonly string defaultStyle = "Plain Text";
			protected SpanParser spanParser;
			protected TextDocument doc;
			protected DocumentLine line;
			internal int lineOffset;
			protected SyntaxMode mode;

			public ChunkParser (SyntaxMode mode, SpanParser spanParser, ColorScheme style, DocumentLine line)
			{
				this.mode = mode;
				this.doc = mode.Document;
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

			Chunk startChunk;
			Chunk endChunk;

			protected virtual void AddRealChunk (Chunk chunk)
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
					curChunk.Style = style;
					curChunk.Length = length;
					AddRealChunk (curChunk);
				}
				curChunk = new Chunk (curChunk.EndOffset, 0, defaultStyle);
				curChunk.Style = GetSpanStyle ();
				wordbuilder.Length = 0;
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
					return spanParser.CurRule.DefaultColor ?? defaultStyle;
				if (String.IsNullOrEmpty (spanParser.SpanStack.Peek ().Color)) {
					Span span = spanParser.SpanStack.Pop ();
					string result = GetSpanStyle ();
					spanParser.SpanStack.Push (span);
					return result;
				}
				string rule = spanParser.SpanStack.Peek ().Rule;
				if (!string.IsNullOrEmpty (rule) && rule.StartsWith ("mode:", StringComparison.Ordinal))
					return spanParser.CurRule.DefaultColor ?? defaultStyle;
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
				curChunk.Style = GetStyle (curChunk);
				if (string.IsNullOrEmpty (curChunk.Style)) {
//					Span tmpSpan = spanParser.SpanStack.Count > 0 ? spanParser.SpanStack.Pop () : null;
					curChunk.Style = GetSpanStyle ();
//					if (tmpSpan != null)
//						spanParser.SpanStack.Push (tmpSpan);
				}
				AddChunk (ref curChunk, 0, curChunk.Style);

				Rule spanRule = spanParser.GetRule (span);
				
				if (spanRule == null)
					throw new Exception ("Rule " + span.Rule + " not found in " + span);
				spanParser.PushSpan (span, spanRule);

				curChunk.Offset = offset;
				curChunk.Length = length;
				curChunk.Style = span.TagColor ?? GetChunkStyle (span);
				curChunk.SpanStack.Push (span);
				AddChunk (ref curChunk, 0, curChunk.Style);
				foreach (SemanticRule semanticRule in spanRule.SemanticRules) {
					semanticRule.Analyze (doc, line, curChunk, offset, line.EndOffsetIncludingDelimiter);
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
			protected StringBuilder wordbuilder = new StringBuilder ();
			bool inWord;
			public void ParseChar (ref int i, char ch)
			{
				Rule cur = spanParser.CurRule;
				bool isWordPart = cur.Delimiter.IndexOf (ch) < 0;
				if (inWord && !isWordPart || !inWord && isWordPart)
					AddChunk (ref curChunk, 0, curChunk.Style = GetStyle (curChunk) ?? GetSpanStyle ());

				inWord = isWordPart;

				if (cur.HasMatches && (i - curChunk.Offset == 0 || string.IsNullOrEmpty (cur.Delimiter))) {
					Match foundMatch = null;
					var   foundMatchLength = new int[0];
					int textOffset = i - spanParser.StartOffset;
					foreach (Match ruleMatch in cur.Matches) {
						var matchLength = ruleMatch.TryMatch (spanParser.CurText, textOffset);
						if (foundMatchLength.Length < matchLength.Length) {
							foundMatch = ruleMatch;
							foundMatchLength = matchLength;
						}
					}
					if (foundMatch != null) {
						if (foundMatch.IsGroupMatch) {
							for (int j = 1; j < foundMatchLength.Length; j++) {
								var len = foundMatchLength [j];
								if (len > 0) {
									AddChunk (ref curChunk, len, GetChunkStyleColor (foundMatch.Groups [j - 1]));
									i += len - 1;
									curChunk.Length = 0;
								}
							}
							return;
						}
						if (foundMatchLength[0] > 0) {
							AddChunk (ref curChunk, foundMatchLength[0], GetChunkStyleColor (foundMatch.Color));
							i += foundMatchLength[0] - 1;
							curChunk.Length = 0;
							return;
						}
					}
				}
				wordbuilder.Append (ch);
				curChunk.Length = i - curChunk.Offset + 1;
				if (!isWordPart) {
					AddChunk (ref curChunk, 0, curChunk.Style = GetStyle (curChunk) ?? GetSpanStyle ());
				}

			}

			protected virtual string GetStyle (Chunk chunk)
			{
				if (chunk.Length > 0) {
					Keywords keyword = spanParser.CurRule.GetKeyword (wordbuilder.ToString ());
					if (keyword != null)
						return keyword.Color;
				}
				return null;
			}

			public virtual Chunk GetChunks (int offset, int length)
			{
				if (lineOffset < offset)
					SyntaxModeService.ScanSpans (doc, mode, spanParser.CurRule, spanParser.SpanStack, lineOffset, offset);
				length = System.Math.Min (doc.TextLength - offset, length);
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

		public override Rule GetRule (TextDocument document, string name)
		{
			if (name == null || name == "<root>") {
				return this;
			}
			if (name.StartsWith ("mode:", StringComparison.Ordinal))
				return SyntaxModeService.GetSyntaxMode (document, name.Substring ("mode:".Length));

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
			AddSemanticRule (GetRule (doc, addToRuleName), semanticRule);
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
			RemoveSemanticRule (GetRule (doc, removeFromRuleName), type);
		}

		public override string ToString ()
		{
			return String.Format ("[SyntaxMode: Name={0}, MimeType={1}]", Name, MimeType);
		}

		new const string Node = "SyntaxMode";

		public const string MimeTypesAttribute = "mimeTypes";

		public static SyntaxMode Read (Stream stream)
		{
			var reader = XmlReader.Create (stream);
			var result = new SyntaxMode (null);
			var matches = new List<Match> ();
			var spanList = new List<Span> ();
			var prevMarkerList = new List<Marker> ();
			XmlReadHelper.ReadList (reader, Node, delegate () {
				if (reader == null)
					return true;
				switch (reader.LocalName) {
				case Node:
					string extends = reader.GetAttribute ("extends");
					if (!String.IsNullOrEmpty (extends)) {
						result = (SyntaxMode)SyntaxModeService.GetSyntaxMode (null, extends).MemberwiseClone ();
					}
					result.Name = reader.GetAttribute ("name");
					result.MimeType = reader.GetAttribute (MimeTypesAttribute);
					if (!String.IsNullOrEmpty (reader.GetAttribute ("ignorecase")))
						result.IgnoreCase = Boolean.Parse (reader.GetAttribute ("ignorecase"));
					return true;
				case Rule.Node:
					result.rules.Add (Rule.Read (result, reader, result.IgnoreCase));
					return true;
				}
				return result.ReadNode (reader, matches, spanList, prevMarkerList);
			});
			result.spans = spanList.ToArray ();
			result.prevMarker = prevMarkerList.ToArray ();
			result.matches = matches.ToArray ();
			return result;
		}
	}

	public interface ISyntaxModeProvider
	{
		SyntaxMode Create (TextDocument doc);
	}
	
	public class ProtoTypeSyntaxModeProvider : ISyntaxModeProvider
	{
		SyntaxMode prototype;
		
		public ProtoTypeSyntaxModeProvider (SyntaxMode prototype)
		{
			this.prototype = prototype;
		}
		
		SyntaxMode DeepCopy (TextDocument doc, SyntaxMode mode)
		{
			var newMode = new SyntaxMode (doc);
			
			newMode.MimeType = mode.MimeType;
			newMode.spans = new Span[mode.Spans.Length];
			for( int i = 0; i < mode.Spans.Length; i++) {
				newMode.spans [i] = mode.Spans [i].Clone ();
			}
			
			newMode.matches = mode.Matches;
			newMode.prevMarker = mode.PrevMarker;
			newMode.keywords = mode.keywords;
			newMode.keywordTable = mode.keywordTable;
			newMode.keywordTableIgnoreCase = mode.keywordTableIgnoreCase;
			foreach (var pair in mode.Properties)
				newMode.Properties.Add (pair.Key, pair.Value);
			
			foreach (var rule in mode.Rules) {
				if (rule is SyntaxMode) {
					newMode.rules.Add (DeepCopy (doc, (SyntaxMode)rule));
				} else {
					newMode.rules.Add (DeepCopy (doc, newMode, rule));
				}
			}
			return newMode;
		}
		
		Rule DeepCopy (TextDocument doc, SyntaxMode mode, Rule rule)
		{
			var newRule = new Rule (mode);
			newRule.spans = new Span[rule.Spans.Length];
			for (int i = 0; i < rule.Spans.Length; i++) {
				newRule.spans [i] = rule.Spans [i].Clone ();
			}
			newRule.Delimiter = rule.Delimiter;
			newRule.IgnoreCase = rule.IgnoreCase;
			newRule.Name = rule.Name;
			newRule.DefaultColor = rule.DefaultColor;
			newRule.matches = rule.Matches;
			newRule.prevMarker = rule.PrevMarker;
			newRule.keywords = rule.keywords;
			newRule.keywordTable = rule.keywordTable;
			newRule.keywordTableIgnoreCase = rule.keywordTableIgnoreCase;
			foreach (var pair in rule.Properties)
				newRule.Properties.Add (pair.Key, pair.Value);
			return newRule;
		}
		
		public SyntaxMode Create (TextDocument doc)
		{
			return DeepCopy (doc, prototype);
		}
	}
	
	public class SyntaxModeProvider : ISyntaxModeProvider
	{
		Func<TextDocument, SyntaxMode> createFunction;
		
		public SyntaxModeProvider (Func<TextDocument, SyntaxMode> createFunction)
		{
			this.createFunction = createFunction;
		}
		
		public SyntaxMode Create (TextDocument doc)
		{
			return createFunction (doc);
		}
	}
}
