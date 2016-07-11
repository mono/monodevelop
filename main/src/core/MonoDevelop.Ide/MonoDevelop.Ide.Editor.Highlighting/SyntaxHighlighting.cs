using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using System.Collections.Immutable;
using MonoDevelop.Core;

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
		}

		public IEnumerable<ColoredSegment> GetColoredSegments (IDocumentLine line, int offset, int length)
		{
			if (Document == null) {
				yield return new ColoredSegment (offset, length, "");
				yield break;
			}
			var cur = Document.GetLine (1);
			var contextStack = ImmutableStack<SyntaxContext>.Empty;
			contextStack = contextStack.Push (GetContext ("main"));
			var scopeStack = ImmutableStack<string>.Empty;
			scopeStack = scopeStack.Push (this.definition.Scope);
			SyntaxContext currentContext;

			var matchStack = ImmutableStack<SyntaxMatch>.Empty;
			if (cur != null && cur.Offset < line.EndOffsetIncludingDelimiter) {
				do {
					currentContext = contextStack.Peek ();
					foreach (var m in currentContext.Matches) {
						var r = new Regex (m.Match);
						var possibleMatch = r.Match (this.Document, cur.Offset, cur.Length);
						if (possibleMatch.Success) {
							if (m.Pop) {
								contextStack = contextStack.Pop ();
								scopeStack = scopeStack.Pop ();
							}
							if (m.Push != null) {
								var nextContext = m.Push.GetContexts (this).FirstOrDefault ();
								if (nextContext != null) {
									contextStack = contextStack.Push (nextContext);
									scopeStack = scopeStack.Push (m.Scope ?? scopeStack.Peek ());
								}
							}
						}
					}
					cur = cur.NextLine;
				} while (cur != null && cur.Offset < line.Offset);
			}
			int curSegmentOffset = line.Offset;
			bool deep = true;
		restart:
			currentContext = contextStack.Peek ();
			Match match = null;
			SyntaxMatch curMatch = null;
			foreach (var m in currentContext.GetMatches (this, deep)) {
				var r = new Regex (m.Match);
				var possibleMatch = r.Match (this.Document, offset, length);
				if (possibleMatch.Success) {
					if (match == null || possibleMatch.Index < match.Index) {
						match = possibleMatch;
						curMatch = m;
					}
				}
			}
			if (match != null) {
				if (curSegmentOffset < match.Index) {
					yield return new ColoredSegment (curSegmentOffset, match.Index - curSegmentOffset, scopeStack.Peek ());
					curSegmentOffset = match.Index;
				}

				if (curMatch.Captures.Count > 0) {
					var captureList = new List<ColoredSegment> ();
					foreach (var capture in curMatch.Captures) {
						var grp = match.Groups [capture.Item1];
						if (grp.Length == 0)
							continue;
						if (curSegmentOffset < grp.Index) {
							Insert (captureList, new ColoredSegment (curSegmentOffset, grp.Index - curSegmentOffset, scopeStack.Peek ()));
						}

						Insert (captureList, new ColoredSegment (grp.Index, grp.Length, capture.Item2));
						curSegmentOffset = grp.Index + grp.Length;
					}
					foreach (var item in captureList)
						yield return item;
				}

				if (curMatch.Pop) {
					if (match.Index + match.Length - curSegmentOffset > 0) {
						yield return new ColoredSegment (curSegmentOffset, match.Index + match.Length - curSegmentOffset, scopeStack.Peek ());
					}
					curSegmentOffset = match.Index + match.Length;
					contextStack = contextStack.Pop ();
					scopeStack = scopeStack.Pop ();
				}

				if (curMatch.Push != null) {
					if (curMatch.Scope != null && curSegmentOffset < match.Index + match.Length) {
						yield return new ColoredSegment (curSegmentOffset, match.Index + match.Length - curSegmentOffset, curMatch.Scope);
						curSegmentOffset = match.Index + match.Length;
					}

					var nextContext = curMatch.Push.GetContexts (this).FirstOrDefault ();
					if (nextContext != null) {
						contextStack = contextStack.Push (nextContext);
						scopeStack = scopeStack.Push (curMatch.Scope ?? scopeStack.Peek ());

						// deep = false;
						length -= (match.Index + match.Length) - offset;
						offset = match.Index + match.Length;

						goto restart;
					}
				}
				offset += match.Length;
				length -= match.Length;
				if (length > 0) {
					deep = true;
					goto restart;
				}
			}
		
			if (line.EndOffset - curSegmentOffset > 0) {
				yield return new ColoredSegment (curSegmentOffset, line.EndOffset - curSegmentOffset, scopeStack.Peek ());
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
				list.Insert (i + 1, new ColoredSegment(newSegment.EndOffset, newSegment.EndOffset - item.EndOffset, item.ColorStyleKey));
			
			list.Insert (i + 1, newSegment);
			list [i] = new ColoredSegment (item.Offset, newSegment.Offset - item.Offset, item.ColorStyleKey);
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