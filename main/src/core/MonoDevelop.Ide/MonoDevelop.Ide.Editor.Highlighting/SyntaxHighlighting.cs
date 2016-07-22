using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using System.Collections.Immutable;
using MonoDevelop.Core;
using System.IO.Compression;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public class SyntaxHighlighting : ISyntaxHighlighting
	{
		readonly SyntaxHighlightingDefinition definition;

		public IReadonlyTextDocument Document {
			get;
			set;
		}

		internal SyntaxHighlightingDefinition Definition {
			get {
				return definition;
			}
		}

		public SyntaxHighlighting (SyntaxHighlightingDefinition definition, IReadonlyTextDocument document)
		{
			this.definition = definition;
			Document = document;
			if (document is ITextDocument)
				((ITextDocument)document).TextChanged += Handle_TextChanged;
		}

		void Handle_TextChanged (object sender, Core.Text.TextChangeEventArgs e)
		{
			var ln = Document.OffsetToLineNumber (e.Offset);
			if (ln >= stateCache.Count)
				return;
			var line = Document.GetLineByOffset (e.Offset);
			var lastState = GetState (line); 

			var high = new Highlighter (this, lastState);
			high.GetColoredSegments (line.Offset, line.LengthIncludingDelimiter).Count ();

			if (!stateCache [ln].Equals (high.State)) {
				stateCache.RemoveRange (ln - 1, stateCache.Count - ln + 1);
			}
		}

		public IEnumerable<ColoredSegment> GetColoredSegments (IDocumentLine line, int offset, int length)
		{
			if (Document == null) {
				return new [] { new ColoredSegment (offset, length, ImmutableStack<string>.Empty.Push ("")) };
			}
			var high = new Highlighter (this, GetState (line));
			if (offset - line.Offset > 0) {
				high.GetColoredSegments (line.Offset, offset - line.Offset).Count ();
			}

			return high.GetColoredSegments (offset, length);
		}

		public ImmutableStack<string> GetLinStartScopeStack (IDocumentLine line)
		{
			return GetState (line).ScopeStack;
		}

		List<HighlightState> stateCache = new List<HighlightState> ();

		HighlightState GetState (IDocumentLine line)
		{
 			var pl = line.PreviousLine;
			if (pl == null)
				return HighlightState.CreateNewState (this);
			if (stateCache.Count == 0)
				stateCache.Add (HighlightState.CreateNewState (this));
			var ln = line.LineNumber;
			if (ln <= stateCache.Count) {
				return stateCache [ln - 1].Clone ();
			}

			var lastState = stateCache [stateCache.Count - 1];
			var cur = Document.GetLine (stateCache.Count);
			if (cur != null && cur.Offset < line.Offset) {
				do {
					var high = new Highlighter (this, lastState.Clone ());
					high.GetColoredSegments (cur.Offset, cur.LengthIncludingDelimiter).Count ();
					stateCache.Add (lastState = high.State);
					cur = cur.NextLine;
				} while (cur != null && cur.Offset < line.Offset);
			}

			return lastState.Clone ();
		}

			
		class HighlightState : IEquatable<HighlightState>
		{
			public ImmutableStack<SyntaxContext> ContextStack;
			public ImmutableStack<SyntaxMatch> MatchStack;
			public ImmutableStack<string> ScopeStack;


			public static HighlightState CreateNewState (SyntaxHighlighting highlighting)
			{
				return new HighlightState {
					ContextStack = ImmutableStack<SyntaxContext>.Empty.Push (highlighting.GetContext ("main")),
					ScopeStack = ImmutableStack<string>.Empty.Push (highlighting.definition.Scope),
					MatchStack = ImmutableStack<SyntaxMatch>.Empty
				};
			}


			internal HighlightState Clone ()
			{
				return new HighlightState {
					ContextStack = this.ContextStack,
					ScopeStack = this.ScopeStack,
					MatchStack = this.MatchStack
				};
			}


			public bool Equals (HighlightState other)
			{
				return ContextStack.SequenceEqual (other.ContextStack) && ScopeStack.SequenceEqual (other.ScopeStack) && MatchStack.SequenceEqual (other.MatchStack);
			}
		}

		class Highlighter
		{
			HighlightState state;
			SyntaxHighlighting highlighting;
			ImmutableStack<SyntaxContext> ContextStack { get { return state.ContextStack; } set { state.ContextStack = value; } }
			ImmutableStack<SyntaxMatch> MatchStack { get { return state.MatchStack; } set { state.MatchStack = value; } }
			ImmutableStack<string> ScopeStack { get { return state.ScopeStack; } set { state.ScopeStack = value; } }

			public HighlightState State {
				get {
					return state;
				}
			}

			public Highlighter (SyntaxHighlighting highlighting, HighlightState state)
			{
				this.highlighting = highlighting;
				this.state = state;
			}

			public IEnumerable<ColoredSegment> GetColoredSegments (int offset, int length)
			{
				SyntaxContext currentContext;
				Match match = null;
				SyntaxMatch curMatch = null;

				int curSegmentOffset = offset;
				int endOffset = offset + length;
			restart:
				currentContext = ContextStack.Peek ();
				match = null;
				curMatch = null;
				foreach (var m in currentContext.GetMatches (highlighting)) {
					var r = m.GetRegex ();
					if (r == null)
						continue;

					var possibleMatch = r.Match (highlighting.Document, offset, length);
					if (possibleMatch.Success) {
						if (match == null || possibleMatch.Index < match.Index) {
							match = possibleMatch;
							curMatch = m;
							// Console.WriteLine (match.Index + "possible match : " + m+ "/" + possibleMatch.Index + "-" + possibleMatch.Length);
						} else {
							// Console.WriteLine (match.Index + "skip match : " + m + "/" + possibleMatch.Index + "-" + possibleMatch.Length);
						}
					} else {
						// Console.WriteLine ("fail match : " + m);
					}
				}

				if (match != null) {
					var matchEndOffset = match.Index + match.Length;
					if (curSegmentOffset < match.Index) {
						yield return new ColoredSegment (curSegmentOffset, match.Index - curSegmentOffset, ScopeStack);
						curSegmentOffset = match.Index;
					}
					if (curMatch.Scope != null) {
						ScopeStack = ScopeStack.Push (curMatch.Scope);
					}
					if (curMatch.Captures.Count > 0) {
						var captureList = new List<ColoredSegment> ();
						foreach (var capture in curMatch.Captures) {
							var grp = match.Groups [capture.Item1];
							if (grp.Length == 0)
								continue;
							if (curSegmentOffset < grp.Index) {
								Insert (captureList, new ColoredSegment (curSegmentOffset, grp.Index - curSegmentOffset, ScopeStack));
							}
							Insert (captureList, new ColoredSegment (grp.Index, grp.Length, ScopeStack.Push (capture.Item2)));
							curSegmentOffset = grp.Index + grp.Length;
						}
						foreach (var item in captureList)
							yield return item;
					}

					if (curMatch.Scope != null && curSegmentOffset < matchEndOffset) {
						yield return new ColoredSegment (curSegmentOffset, matchEndOffset - curSegmentOffset, ScopeStack);
						curSegmentOffset = matchEndOffset;
					}

					if (curMatch.Pop) {
						if (matchEndOffset - curSegmentOffset > 0)
							yield return new ColoredSegment (curSegmentOffset, matchEndOffset - curSegmentOffset, ScopeStack);
						//if (curMatch.Scope != null)
						//	scopeStack = scopeStack.Pop ();
						curSegmentOffset = PopStack (currentContext, curMatch, matchEndOffset);
					} else if (curMatch.Set != null) {
						if (matchEndOffset - curSegmentOffset > 0)
							yield return new ColoredSegment (curSegmentOffset, matchEndOffset - curSegmentOffset, ScopeStack);
						//if (curMatch.Scope != null)
						//	scopeStack = scopeStack.Pop ();
						curSegmentOffset = PopStack (currentContext, curMatch, matchEndOffset);
						var nextContexts = curMatch.Set.GetContexts (highlighting);
						PushStack (curMatch, nextContexts);
					} else if (curMatch.Push != null) {
						var nextContexts = curMatch.Push.GetContexts (highlighting);
						PushStack (curMatch, nextContexts);
					} else {
						if (curMatch.Scope != null) {
							ScopeStack = ScopeStack.Pop ();
						}
					}
					length -= curSegmentOffset - offset;
					offset = curSegmentOffset;
					goto restart;
				}

				if (endOffset - curSegmentOffset > 0) {
					yield return new ColoredSegment (curSegmentOffset, endOffset - curSegmentOffset, ScopeStack);
				}
			}

			void PushStack (SyntaxMatch curMatch, IEnumerable<SyntaxContext> nextContexts)
			{
				if (nextContexts != null) {
					bool first = true;
					foreach (var nextContext in nextContexts) {
						if (first) {
							MatchStack = MatchStack.Push (curMatch);
							first = false;
						} else {
							MatchStack = MatchStack.Push (null);
						}
						ContextStack = ContextStack.Push (nextContext);
						if (nextContext.MetaScope != null)
							ScopeStack = ScopeStack.Push (nextContext.MetaScope);
						if (nextContext.MetaContentScope != null)
							ScopeStack = ScopeStack.Push (nextContext.MetaContentScope);
					}
				}
			}

			int PopStack (SyntaxContext currentContext, SyntaxMatch curMatch, int matchEndOffset)
			{
				int curSegmentOffset = matchEndOffset;
				if (curMatch.Pop || ContextStack.Count () > 1) {
					ContextStack = ContextStack.Pop ();
					if (!MatchStack.IsEmpty) {
						if (MatchStack.Peek ()?.Scope != null)
							ScopeStack = ScopeStack.Pop ();
						MatchStack = MatchStack.Pop ();
					}
					if (currentContext.MetaScope != null)
						ScopeStack = ScopeStack.Pop ();
					if (currentContext.MetaContentScope != null)
						ScopeStack = ScopeStack.Pop ();
				}

				return curSegmentOffset;
			}
		}
	
		static void Insert (List<ColoredSegment> list, ColoredSegment newSegment)
		{
			if (list.Count == 0) {
				list.Add (newSegment);
				return;
			}
			int i = list.Count;
			while (i > 0 && list [i - 1].EndOffset > newSegment.Offset) {
				i--;
			}
			if (i >= list.Count) {
				list.Add (newSegment);
				return;
			}
			var item = list [i];


			if (newSegment.EndOffset - item.EndOffset > 0)
				list.Insert (i + 1, new ColoredSegment(newSegment.EndOffset, newSegment.EndOffset - item.EndOffset, item.ScopeStack));
			
			list.Insert (i + 1, newSegment);
			list [i] = new ColoredSegment (item.Offset, newSegment.Offset - item.Offset, item.ScopeStack);
		}

		internal SyntaxContext GetContext (string name)
		{
			foreach (var ctx in definition.Contexts) {
				if (ctx.Name == name)
					return ctx;
			}
			return null;
		}
	}
}