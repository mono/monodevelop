//
// SyntaxHighlightingDefinition.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
	public class SyntaxHighlightingDefinition
	{
		public string Name { get; internal set; }

		readonly List<string> extensions;
		public IReadOnlyList<string> FileExtensions { get { return extensions; } }

		public string Scope { get; internal set; }

		public bool Hidden { get; internal set; }

		public string FirstLineMatch { get; internal set; }

		readonly List<SyntaxContext> contexts;
		public IReadOnlyList<SyntaxContext> Contexts { get { return contexts; } }

		internal SyntaxHighlightingDefinition (string name, string scope, string firstLineMatch, bool hidden, List<string> extensions, List<SyntaxContext> contexts)
		{
			this.extensions = extensions;
			this.contexts = contexts;
			Name = name;
			Scope = scope;
			FirstLineMatch = firstLineMatch;
			Hidden = hidden;
		}

	}

	public class SyntaxContext
	{
		public string Name { get; private set; }

		public string MetaScope { get; private set; }
		public string MetaContentScope { get; private set; }
		public string MetaIncludePrototype { get; private set; }

		readonly List<string> includes;
		public IReadOnlyList<string> Includes { get { return includes; } }

		readonly List<SyntaxMatch> matches;
		public IReadOnlyList<SyntaxMatch> Matches { get { return matches; } }

		internal void ParseMapping (YamlSequenceNode seqNode, Dictionary<string, string> variables)
		{
			if (seqNode != null) {
				foreach (var node in seqNode.Children.OfType<YamlMappingNode> ()) {
					ParseMapping (node, variables);
				}
			}

			//var scalarNode = mapping.Value as YamlScalarNode;
			//if (scalarNode != null) {
			//	Console.WriteLine (mapping.Key +"/"+scalarNode.Value);
			//}
		}

		internal void ParseMapping (YamlMappingNode node, Dictionary<string, string> variables)
		{
			var children = node.Children;
			if (children.ContainsKey (new YamlScalarNode ("match"))) {
				matches.Add (Sublime3Format.ReadMatch (node, variables));
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

		internal SyntaxContext (string name)
		{
			Name = name;
			includes = new List<string> ();
			matches = new List<SyntaxMatch> ();
		}

		internal SyntaxContext (string name, List<string> includes, List<SyntaxMatch> matches, string metaScope = null, string metaContentScope = null, string metaIncludePrototype = null)
		{
			this.includes = includes;
			this.matches = matches;
			Name = name;
			MetaScope = metaScope;
			MetaContentScope = metaContentScope;
			MetaIncludePrototype = metaIncludePrototype;
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
					LoggingService.LogWarning ($"highlighting {highlighting.Definition.Name} can't find include {include}.");
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
			Captures = captures ?? new List<Tuple<int, string>> ();
			Push = push;
			Pop = pop;
			Set = set;
		}

		public override string ToString ()
		{
			return string.Format ("[SyntaxMatch: Match={0}, Scope={1}]", Match, Scope);
		}

		bool hasRegex;
		Regex cachedRegex;

		internal Regex GetRegex ()
		{
			if (hasRegex)
				return cachedRegex;
			hasRegex = true;
			try {
				cachedRegex = new Regex (Match);
			} catch (Exception e) {
				LoggingService.LogWarning ("Warning regex : '" + Match + "' can't be parsed.", e);
			}
			return cachedRegex;
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