using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Linq;
using MonoDevelop.Ide.Editor.Highlighting.RegexEngine;
using System.Collections.Immutable;

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
			if (cur != line) {
				do {
					currentContext = contextStack.Peek ();
					foreach (var curMatch in currentContext.Matches) {
						var r = new Regex (curMatch.Match);
						var match = r.Match (this.Document, cur.Offset, cur.Length);
						if (match.Success) {
							if (curMatch.Pop) {
								contextStack = contextStack.Pop ();
								scopeStack = scopeStack.Pop ();
							}
							if (curMatch.Push != null) {
								var nextContext = curMatch.Push.GetContexts (this).FirstOrDefault ();
								if (nextContext != null) {
									contextStack = contextStack.Push (nextContext);
									scopeStack = scopeStack.Push (curMatch.Scope ?? scopeStack.Peek ());
								}
							}
						}
					}
					cur = cur.NextLine;
				} while (cur != null && cur.Offset < line.Offset);
				currentContext = contextStack.Peek ();

				int curSegmentOffset = line.Offset;

				var currentContexts = new List<SyntaxContext> ();
				currentContexts.Add (currentContext);
				currentContexts.AddRange (currentContext.Includes.Select (ctx => GetContext (ctx)).Where (ctx => ctx != null));
				foreach (var ctx in currentContexts) {
					foreach (var curMatch in ctx.Matches) {
						var r = new Regex (curMatch.Match);
						var match = r.Match (this.Document, offset, length);
						if (match.Success) {
							curSegmentOffset = match.Index + match.Length - line.Offset;

							if (curMatch.Captures.Count > 0) {

								foreach (var capture in curMatch.Captures) {
									var grp = match.Groups [capture.Item1];
									if (curSegmentOffset < grp.Index) {
										yield return new ColoredSegment (curSegmentOffset, grp.Index - curSegmentOffset, scopeStack.Peek ());
									}
									yield return new ColoredSegment (grp.Index, grp.Length, capture.Item2);
									curSegmentOffset = grp.Index + grp.Length;
								}

								if (curSegmentOffset < match.Index + match.Length) {
									yield return new ColoredSegment (curSegmentOffset, match.Index + match.Length - curSegmentOffset, scopeStack.Peek ());
								}
							}

							if (curMatch.Pop) {
								contextStack = contextStack.Pop ();
								scopeStack = scopeStack.Pop ();
							}

							if (curMatch.Push != null) {
								var nextContext = curMatch.Push.GetContexts (this).FirstOrDefault ();
								if (nextContext != null) {
									contextStack = contextStack.Push (nextContext);
									scopeStack = scopeStack.Push (curMatch.Scope ?? scopeStack.Peek ());
								}
							}
						}
					}
				}
			
				if (line.EndOffset - curSegmentOffset > 0)
					yield return new ColoredSegment (curSegmentOffset, line.EndOffset - curSegmentOffset, scopeStack.Peek ());
			}
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

		internal void ParseMapping (KeyValuePair<YamlNode, YamlNode> mapping)
		{
			var seqNode = mapping.Value as YamlSequenceNode;
			if (seqNode != null) {
				foreach (var node in seqNode.Children.OfType<YamlMappingNode> ()) {
					if (node.Children.ContainsKey (new YamlScalarNode ("match"))) {
						matches.Add (Sublime3Format.ReadMatch (node));
						continue;
					}
					YamlNode val;
					if (node.Children.TryGetValue (new YamlScalarNode ("meta_scope"), out val)) {
						MetaScope = ((YamlScalarNode)val).Value;
					}
					if (node.Children.TryGetValue (new YamlScalarNode ("meta_content_scope"), out val)) {
						MetaContentScope = ((YamlScalarNode)val).Value;
					}
					if (node.Children.TryGetValue (new YamlScalarNode ("meta_include_prototype"), out val)) {
						MetaIncludePrototype = ((YamlScalarNode)val).Value;
					}
					if (node.Children.TryGetValue (new YamlScalarNode ("include"), out val)) {
						includes.Add (((YamlScalarNode)val).Value);
					}
				}
			}

			//var scalarNode = mapping.Value as YamlScalarNode;
			//if (scalarNode != null) {
			//	Console.WriteLine (mapping.Key +"/"+scalarNode.Value);
			//}
		}

		public SyntaxContext (string name)
		{
			Name = name;
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