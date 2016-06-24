using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Linq;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public class SyntaxHighlighting : ISyntaxMode
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
			yield break;
		}
	}

	public class SyntaxContext
	{
		public string Name { get; private set; }

		public string MetaScope { get; private set; }
		public string MetaContentScope { get; private set; }
		public string MetaIncludePrototype { get; private set; }

		public string Include { get; private set; }

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
						Include = ((YamlScalarNode)val).Value;
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
	}

	public class ContextNameContextReference : ContextReference
	{
		public string Name { get; private set; }
	}

	public class ContextNameListContextReference : ContextReference
	{
		public IReadOnlyList<string> Names { get; private set; }
	}

	public class AnonymousMatchContextReference : ContextReference
	{
		public SyntaxContext Context { get; private set; }

		internal AnonymousMatchContextReference (SyntaxContext context)
		{
			Context = context;
		}
	}
}