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
		public string Name { get; internal set; }

		readonly List<string> extensions;
		public IReadOnlyList<string> FileExtensions { get { return extensions; } }

		public string Scope { get; internal set; }

		public string FirstLineMatch { get; internal set; }

		readonly List<SyntaxContext> contexts;
		public IReadOnlyList<SyntaxContext> Contexts { get { return contexts; } }

		public IReadonlyTextDocument Document {
			get;
			set;
		}

		internal SyntaxHighlighting (string name, string scope, string firstLineMatch, List<string> extensions, List<SyntaxContext> contexts)
		{
			this.extensions = extensions;
			this.contexts = contexts;
			Name = name;
			Scope = scope;
			FirstLineMatch = firstLineMatch;
		}

		public IEnumerable<ColoredSegment> GetColoredSegments (IDocumentLine line, int offset, int length)
		{
			var cur = Document.GetLine (1);
			var contextStack = ImmutableStack<SyntaxContext>.Empty;
			contextStack = contextStack.Push (GetContext ("main"));
			var scopeStack = ImmutableStack<string>.Empty;
			scopeStack = scopeStack.Push (this.Scope);
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
			foreach (var ctx in contexts) {
				if (ctx.Name == name)
					return ctx;
			}
			return null;
		}
	}

	public class SyntaxContext
	{
		public string Name { get; private set; }

		public string MetaScope { get; private set; }
		public string MetaContentScope { get; private set; }
		public string MetaIncludePrototype { get; private set; }

		readonly List<string> includes = new List<string> ();
		public IReadOnlyList<string> Includes { get { return includes; } }

		readonly List<SyntaxMatch> matches = new List<SyntaxMatch> ();
		public IReadOnlyList<SyntaxMatch> Matches { get { return matches; } }

		internal void ParseMapping (YamlSequenceNode seqNode)
		{
			if (seqNode != null) {
				foreach (var node in seqNode.Children.OfType<YamlMappingNode> ()) {
					ParseMapping (node);
				}
			}

			//var scalarNode = mapping.Value as YamlScalarNode;
			//if (scalarNode != null) {
			//	Console.WriteLine (mapping.Key +"/"+scalarNode.Value);
			//}
		}

		internal void ParseMapping (YamlMappingNode node)
		{
			var children = node.Children;
			if (children.ContainsKey (new YamlScalarNode ("match"))) {
				matches.Add (Sublime3Format.ReadMatch (node));
				return;
			}

			YamlNode val;
			if (children.TryGetValue (new YamlScalarNode ("meta_scope"), out val)) {
				MetaScope = ((YamlScalarNode)val).Value;
			}
			if (children.TryGetValue (new YamlScalarNode ("meta_content_scope"), out val)) {
				MetaContentScope = ((YamlScalarNode)val).Value;
			}
			if (children.TryGetValue (new YamlScalarNode ("meta_include_prototype"), out val)) {
				MetaIncludePrototype = ((YamlScalarNode)val).Value;
			}
			if (children.TryGetValue (new YamlScalarNode ("include"), out val)) {
				includes.Add (((YamlScalarNode)val).Value);
			}
		}

		public SyntaxContext (string name)
		{
			Name = name;
		}

		public IEnumerable<SyntaxMatch> GetMatches (SyntaxHighlighting highlighting, bool deep)
		{
			foreach (var match in Matches)
				yield return match;
			if (!deep)
				yield break;
			foreach (var include in Includes) {
				var ctx = highlighting.GetContext (include);
				if (ctx == null) {
					LoggingService.LogWarning ($"highlighting {highlighting.Name} can't find include {include}.");
					continue;
				}
				foreach (var match in ctx.GetMatches (highlighting, deep))
					yield return match;
			}
		}
	}

	public class SyntaxMatch
	{
		public string Match { get; private set; }
		public string Scope { get; private set; }
		public List<Tuple<int, string>> Captures { get; private set; }
		public ContextReference Push { get; private set; }
		public bool Pop { get; private set; }
		public ContextReference Set { get; private set; }

		internal SyntaxMatch (string match, string scope, List<Tuple<int, string>> captures, ContextReference push, bool pop, ContextReference set)
		{
			Match = match;
			Scope = scope;
			Captures = captures;
			Push = push;
			Pop = pop;
			Set = set;
		}
	}

	public abstract class ContextReference
	{
		public abstract IEnumerable<SyntaxContext> GetContexts (SyntaxHighlighting highlighting);
	}

	public class ContextNameContextReference : ContextReference
	{
		public string Name { get; private set; }

		public override IEnumerable<SyntaxContext> GetContexts (SyntaxHighlighting highlighting)
		{
			yield return highlighting.GetContext (Name);
		}
	}

	public class ContextNameListContextReference : ContextReference
	{
		public IReadOnlyList<string> Names { get; private set; }

		public override IEnumerable<SyntaxContext> GetContexts (SyntaxHighlighting highlighting)
		{
			foreach (var name in Names)
				yield return highlighting.GetContext (name);
		}
	}

	public class AnonymousMatchContextReference : ContextReference
	{
		public SyntaxContext Context { get; private set; }

		internal AnonymousMatchContextReference (SyntaxContext context)
		{
			Context = context;
		}

		public override IEnumerable<SyntaxContext> GetContexts (SyntaxHighlighting highlighting)
		{
			yield return Context;
		}
	}
}