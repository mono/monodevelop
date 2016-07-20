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
				yield return new ColoredSegment (offset, length, ImmutableStack<string>.Empty.Push (""));
				yield break;
			}
			var cur = Document.GetLine (1);
			var contextStack = ImmutableStack<SyntaxContext>.Empty;
			contextStack = contextStack.Push (GetContext ("main"));
			var scopeStack = ImmutableStack<string>.Empty;
			scopeStack = scopeStack.Push (this.definition.Scope);
			SyntaxContext currentContext;
			Match match = null;
			SyntaxMatch curMatch = null;
			bool deep = true;

			var matchStack = ImmutableStack<SyntaxMatch>.Empty;
			if (cur != null && cur.Offset < line.Offset) {
				do {
					int co = cur.Offset;
					int cl = cur.Length;
				restart2:
					currentContext = contextStack.Peek ();
					match = null;
					curMatch = null;
					foreach (var m in currentContext.GetMatches (this)) {
						var r = m.GetRegex ();
						if (r == null)
							continue;

						var possibleMatch = r.Match (this.Document, co, cl);
						if (possibleMatch.Success) {
							if (match == null || possibleMatch.Index < match.Index) {
								match = possibleMatch;
								curMatch = m;
							}
						}
					}
					if (match != null) {
						var matchEndOffset = match.Index + match.Length;

						if (curMatch.Pop || curMatch.Set != null) {
							if (curMatch.Pop || contextStack.Count () > 1) {
								contextStack = contextStack.Pop ();
								scopeStack = scopeStack.Pop ();
							}
						}

						if (curMatch.Push != null || curMatch.Set != null) {
							var nextContexts = (curMatch.Push ?? curMatch.Set).GetContexts (this);
							if (nextContexts != null) {
								foreach (var nextContext in nextContexts) {
									contextStack = contextStack.Push (nextContext);
									scopeStack = scopeStack.Push (curMatch.Scope ??
																  nextContext.MetaContentScope ??
																  nextContext.MetaScope ??
																  scopeStack.Peek ());
								}
								// deep = false;
								cl -= matchEndOffset - co;
								co = matchEndOffset;

								goto restart2;
							}
						}
						cl -= matchEndOffset - co;
						co = matchEndOffset;
						if (cl > 0) {
							deep = true;
							goto restart2;
						}
					}
					cur = cur.NextLine;
				} while (cur != null && cur.Offset < line.Offset);
			}
			int curSegmentOffset = line.Offset;
			deep = true;
		restart:
			currentContext = contextStack.Peek ();
			match = null;
			curMatch = null;
			foreach (var m in currentContext.GetMatches (this)) {
				var r = m.GetRegex ();
				if (r == null)
					continue;

				var possibleMatch = r.Match (this.Document, offset, length);
				if (possibleMatch.Success && possibleMatch.Length > 0) {
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
					yield return new ColoredSegment (curSegmentOffset, match.Index - curSegmentOffset, scopeStack);
					curSegmentOffset = match.Index;
				}
				if (curMatch.Scope != null) {
					scopeStack = scopeStack.Push (curMatch.Scope);
				}
				if (curMatch.Captures.Count > 0) {
					var captureList = new List<ColoredSegment> ();
					foreach (var capture in curMatch.Captures) {
						var grp = match.Groups [capture.Item1];
						if (grp.Length == 0)
							continue;
						if (curSegmentOffset < grp.Index) {
							Insert (captureList, new ColoredSegment (curSegmentOffset, grp.Index - curSegmentOffset, scopeStack));
						}
						Insert (captureList, new ColoredSegment (grp.Index, grp.Length, scopeStack.Push (capture.Item2)));
						curSegmentOffset = grp.Index + grp.Length;
					}
					foreach (var item in captureList)
						yield return item;	
				}

				if (curMatch.Pop || curMatch.Set != null) {
					if (matchEndOffset - curSegmentOffset > 0) {
						yield return new ColoredSegment (curSegmentOffset, matchEndOffset - curSegmentOffset, scopeStack);
					}
					curSegmentOffset = matchEndOffset;
					if (curMatch.Pop || contextStack.Count () > 1) {
						contextStack = contextStack.Pop ();
						if (!matchStack.IsEmpty) {
							if (matchStack.Peek ()?.Scope != null)
								scopeStack = scopeStack.Pop ();
							matchStack = matchStack.Pop ();
						}
						if (currentContext.MetaScope != null)
							scopeStack = scopeStack.Pop ();
						if (currentContext.MetaContentScope != null)
							scopeStack = scopeStack.Pop ();
					}
				}
				if (curMatch.Scope != null && curSegmentOffset < matchEndOffset) {
					yield return new ColoredSegment (curSegmentOffset, matchEndOffset - curSegmentOffset, scopeStack);
					curSegmentOffset = matchEndOffset;
				}

				if (curMatch.Push != null || curMatch.Set != null) {
					var nextContexts = (curMatch.Push ?? curMatch.Set).GetContexts (this);
					if (nextContexts != null) {
						bool first = true;
						foreach (var nextContext in nextContexts) {
							if (first) {
								matchStack = matchStack.Push (curMatch);
								if (curMatch.Scope != null) {
									scopeStack = scopeStack.Push (curMatch.Scope);
								}

							} else {
								matchStack = matchStack.Push (null);
							}
							contextStack = contextStack.Push (nextContext);
							if (nextContext.MetaScope != null)
								scopeStack = scopeStack.Push (nextContext.MetaScope);
							if (nextContext.MetaContentScope != null)
								scopeStack = scopeStack.Push (nextContext.MetaContentScope);
						}

					}
				}
				if (curMatch.Scope != null) {
					scopeStack = scopeStack.Pop ();
				}
				length -= curSegmentOffset - offset;
				offset = curSegmentOffset;
				goto restart;
			}
		
			if (line.EndOffset - curSegmentOffset > 0) {
				yield return new ColoredSegment (curSegmentOffset, line.EndOffset - curSegmentOffset, scopeStack);
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