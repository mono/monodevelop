using System;
using System.Collections.Generic;
using System.IO;
using YamlDotNet.RepresentationModel;
using System.Linq;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	static class Sublime3Format
	{
		public static SyntaxHighlighting ReadHighlighting (TextReader input)
		{
			string name = null, scope = null, firstLineMatch = null;
			var extensions = new List<string> ();
			var contexts = new List<SyntaxContext> ();

			var yaml = new YamlStream ();
			yaml.Load (input);
			var mapping = (YamlMappingNode)yaml.Documents [0].RootNode;
			foreach (var entry in mapping.Children) {
				switch (((YamlScalarNode)entry.Key).Value) {
				case "name":
					name = ((YamlScalarNode)entry.Value).Value;
					break;
				case "file_extensions":
					foreach (var nn in entry.Value.AllNodes.OfType<YamlScalarNode> ()) {
						extensions.Add (nn.Value);
					}
					break;
				case "scope":
					scope = ((YamlScalarNode)entry.Value).Value;
					break;
				case "first_line_match":
					firstLineMatch = ((YamlScalarNode)entry.Value).Value;
					break;
				case "contexts":
					foreach (var contextMapping in ((YamlMappingNode)entry.Value).Children) {
						contexts.Add (ReadContext (contextMapping));
					}
					break;
				}
			}
			return new SyntaxHighlighting (name, scope, firstLineMatch, extensions, contexts);
		}


		internal static SyntaxContext ReadContext (KeyValuePair<YamlNode, YamlNode> mapping)
		{
			var result = new SyntaxContext (((YamlScalarNode)mapping.Key).Value);
			result.ParseMapping (mapping.Value as YamlSequenceNode);
			return result;
		}

		internal static SyntaxMatch ReadMatch (YamlMappingNode mapping)
		{
			string match = null, scope = null;
			var captures = new List<Tuple<int, string>> ();
			ContextReference push = null, set = null;
			bool pop = false;

			foreach (var entry in mapping.Children) {
				switch (((YamlScalarNode)entry.Key).Value) {
				case "match":
					match = ((YamlScalarNode)entry.Value).Value;
					break;
				case "scope":
					scope = ((YamlScalarNode)entry.Value).Value;
					break;
				case "captures":
					foreach (var captureEntry in ((YamlMappingNode)entry.Value).Children) {
						captures.Add (
							Tuple.Create (
								int.Parse (((YamlScalarNode)captureEntry.Key).Value),
								((YamlScalarNode)captureEntry.Value).Value
							)
						);
					}
					break;
				case "push":
					push = ReadContextReference (entry.Value);
					break;
				case "pop":
					// according to the spec the only accepted value
					pop = true;
					break;
				case "set":
					set = ReadContextReference (entry.Value);
					break;
				case "syntax":
					// ?
					break;
				}
			}
			return new SyntaxMatch (match, scope, captures, push, pop, set);
		}

		internal static ContextReference ReadContextReference (YamlNode value)
		{
			var seq = value as YamlSequenceNode;
			if (seq != null)
				return ReadAnonymousMatchContextReference (seq);
			return null;
		}

		internal static ContextReference ReadAnonymousMatchContextReference (YamlSequenceNode seq)
		{
			var ctx = new SyntaxContext (null);

			foreach (var c in seq.Children.OfType<YamlMappingNode> ()) {
				ctx.ParseMapping (c);
			}

			return new AnonymousMatchContextReference (ctx);
		}
	}
}