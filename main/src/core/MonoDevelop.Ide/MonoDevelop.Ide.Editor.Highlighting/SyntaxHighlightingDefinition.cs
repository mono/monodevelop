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

		readonly List<string> fileTypes;
		public IReadOnlyList<string> FileTypes { get { return fileTypes; } }

		public string Scope { get; internal set; }

		public bool Hidden { get; internal set; }

		public string FirstLineMatch { get; internal set; }

		readonly List<SyntaxContext> contexts;
		public IReadOnlyList<SyntaxContext> Contexts { get { return contexts; } }

		public SyntaxContext MainContext {
			get {
				return contexts [0];
			}
		}

		internal SyntaxHighlightingDefinition (string name, string scope, string firstLineMatch, bool hidden, List<string> fileTypes, List<SyntaxContext> contexts)
		{
			this.fileTypes = fileTypes;
			this.contexts = contexts;
			Name = name;
			Scope = scope;
			FirstLineMatch = firstLineMatch;
			Hidden = hidden;
			foreach (var ctx in contexts) {
				ctx.SetDefinition (this);
			}
		}

		internal void PrepareMatches()
		{
			foreach (var ctx in Contexts) {
				ctx.PrepareMatches ();
			}
		}
	}


	class SyntaxContextWithPrototype : SyntaxContext
	{
		SyntaxContext ctx;

		public override IReadOnlyList<string> MetaContentScope { get { return ctx.MetaContentScope; } }
		public override IReadOnlyList<string> MetaScope { get { return ctx.MetaScope; } }
		public override bool MetaIncludePrototype { get { return ctx.MetaIncludePrototype; }  }

		public SyntaxContextWithPrototype (SyntaxContext ctx, ContextReference withPrototype) :  base (ctx.Name)
		{
			this.ctx = ctx;
			this.definition = ctx.definition;
			matches = new List<SyntaxMatch> (ctx.Matches);
			foreach (var c in withPrototype.GetContexts (ctx)) {
				if (c.Matches == null)
					c.PrepareMatches ();
				this.matches.AddRange (c.Matches);
			}
		}
	}

	public class SyntaxContext
	{
		protected List<SyntaxMatch> matches;
		internal SyntaxHighlightingDefinition definition;

		public string Name { get; private set; }

		protected List<string> metaScope = new List<string> ();
		public virtual IReadOnlyList<string> MetaScope { get { return metaScope; } }

		protected List<string> metaContentScope = new List<string> ();
		public virtual IReadOnlyList<string> MetaContentScope { get { return metaContentScope; } }


		public virtual bool MetaIncludePrototype { get; private set; }

		public virtual IEnumerable<SyntaxMatch> Matches { get { return matches; } }

		readonly List<object> includesAndMatches;

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
				includesAndMatches.Add (Sublime3Format.ReadMatch (node, variables));
				return;
			}

			YamlNode val;
			if (children.TryGetValue (new YamlScalarNode ("meta_scope"), out val)) {
				Sublime3Format.ParseScopes (metaScope, ((YamlScalarNode)val).Value);
			}
			if (children.TryGetValue (new YamlScalarNode ("meta_content_scope"), out val)) {
				Sublime3Format.ParseScopes (metaContentScope, ((YamlScalarNode)val).Value);
			}
			if (children.TryGetValue (new YamlScalarNode ("meta_include_prototype"), out val)) {
				MetaIncludePrototype = ((YamlScalarNode)val).Value != "false";
			}
			if (children.TryGetValue (new YamlScalarNode ("include"), out val)) {
				includesAndMatches.Add (((YamlScalarNode)val).Value);
			}
		}

		internal SyntaxContext (string name)
		{
			Name = name;
			includesAndMatches = new List<object> ();
			MetaIncludePrototype = true;
		}

		internal SyntaxContext (string name, List<object> includesAndMatches, IReadOnlyList<string> metaScope = null, IReadOnlyList<string> metaContentScope = null, bool metaIncludePrototype = true)
		{
			this.includesAndMatches = includesAndMatches;
			Name = name;
			if (metaScope != null)
				this.metaScope.AddRange (metaScope);
			if (metaContentScope !=  null)
				this.metaContentScope.AddRange (metaContentScope);
			
			MetaIncludePrototype = metaIncludePrototype;
		}

		internal virtual SyntaxContext GetContext (string name)
		{
			if (name.StartsWith ("scope:", StringComparison.Ordinal)) {
				var splittedNames = name.Substring ("scope:".Length).Split (new [] { '#' }, StringSplitOptions.RemoveEmptyEntries);
				if (splittedNames.Length == 0)
					return null;
				foreach (var bundle in SyntaxHighlightingService.AllBundles) {
					foreach (var highlighting in bundle.Highlightings) {
						if (highlighting.Scope == splittedNames [0]) {
							var searchName = splittedNames.Length == 1 ? "main" : splittedNames [1];
							foreach (var ctx in highlighting.Contexts) {
								if (ctx.Name == searchName) {
									return ctx;
								}
							}
						}
					}
				}
				return null;
			}
			foreach (var ctx in definition.Contexts) {
				if (ctx.Name == name)
					return ctx;
			}
			return null;
		}

		IEnumerable<SyntaxMatch> GetMatches ()
		{
			return GetMatches (new List<string> ());
		}

		IEnumerable<SyntaxMatch> GetMatches (List<string> alreadyIncluded)
		{
			foreach (var o in includesAndMatches) {
				var match = o as SyntaxMatch;
				if (match != null) {
					yield return match;
					continue;
				}
				var include = o as string;
				var ctx = GetContext (include);
				if (ctx == null) {
					// LoggingService.LogWarning ($"highlighting {definition.Name} can't find include {include}.");
					continue;
				}
				if (alreadyIncluded.Contains (include))
					continue;
				alreadyIncluded.Add (include);
				foreach (var match2 in ctx.GetMatches (alreadyIncluded))
					yield return match2;
			}
		}

		internal void AddMatch (SyntaxMatch match)
		{
			this.matches.Add (match); 
		}

		internal void SetDefinition (SyntaxHighlightingDefinition definition)
		{
			this.definition = definition;
			foreach (var o in includesAndMatches) {
				var match = o as SyntaxMatch;
				if (match != null) {
					if (match.Push is AnonymousMatchContextReference)
						((AnonymousMatchContextReference)match.Push).Context.SetDefinition (definition);
					if (match.Set is AnonymousMatchContextReference)
						((AnonymousMatchContextReference)match.Set).Context.SetDefinition (definition);
				}
			}
		}

		internal void PrepareMatches ()
		{
			if (this.matches != null)
				return;
			var preparedMatches = new List<SyntaxMatch> ();
			IEnumerable<object> list = includesAndMatches;
			if (MetaIncludePrototype &&  Name != "prototype") {
				var prototypeContext = GetContext ("prototype");
				if (prototypeContext != null)
					list = list.Concat (prototypeContext.GetMatches ());
			}
			foreach (var o in list) {
				var match = o as SyntaxMatch;
				if (match != null) {
					if (match.Push is AnonymousMatchContextReference)
						match.Push.GetContexts (this).First ().PrepareMatches ();
					if (match.Set is AnonymousMatchContextReference)
						match.Set.GetContexts (this).First ().PrepareMatches ();
					preparedMatches.Add (match);
					continue;
				}
				var include = o as string;
				var ctx = GetContext (include);
				if (ctx == null) {
					// LoggingService.LogWarning ($"highlighting {definition.Name} can't find include {include}.");
					continue;
				}
				preparedMatches.AddRange (ctx.GetMatches ());
			}
			this.matches = preparedMatches;
		}

		public override string ToString ()
		{
			return string.Format ("[SyntaxContext: Name={0}, MetaScope={1}, MetaContentScope={2}, MetaIncludePrototype={3}]", Name, MetaScope.Count == 0 ? "empty" : string.Join (", ", MetaScope), MetaContentScope.Count == 0 ? "empty" : string.Join (", ", MetaContentScope), MetaIncludePrototype);
		}

		public SyntaxContext Clone ()
		{
			return (SyntaxContext)this.MemberwiseClone ();
		}
	}

	public class Captures
	{
		public static readonly Captures Empty = new Captures (new List<Tuple<int, string>> (), new List<Tuple<string, string>> ());
		public IReadOnlyList<Tuple<int, string>> Groups { get; private set; }
		public IReadOnlyList<Tuple<string, string>> NamedGroups { get; private set; }

		public Captures (IReadOnlyList<Tuple<int, string>> groups)
		{
			Groups = groups;
			NamedGroups = Captures.Empty.NamedGroups;
		}

		public Captures (IReadOnlyList<Tuple<int, string>> groups, IReadOnlyList<Tuple<string, string>> namedGroups)
		{
			Groups = groups;
			NamedGroups = namedGroups;
		}
	}

	public class SyntaxMatch
	{
		public string Match { get; private set; }
		public IReadOnlyList<string> Scope { get; private set; }
		public Captures Captures { get; private set; }
		public ContextReference Push { get; private set; }
		public bool Pop { get; private set; }
		public ContextReference Set { get; private set; }
		public ContextReference WithPrototype { get; private set; }
		internal bool GotTimeout { get; set; }

		internal SyntaxMatch (string match, IReadOnlyList<string> scope, Captures captures, ContextReference push, bool pop, ContextReference set, ContextReference withPrototype)
		{
			Match = match;
			Scope = scope;
			Captures = captures ?? Captures.Empty;
			Push = push;
			Pop = pop;
			Set = set;
			WithPrototype = withPrototype;
		}

		public override string ToString ()
		{
			return string.Format ("[SyntaxMatch: Match={0}, Scope={1}]", Match, Scope.Count == 0 ? "empty" : string.Join (", ", Scope));
		}

		bool hasRegex;
		Regex cachedRegex;
		object lockObj = new object ();

		internal Regex GetRegex ()
		{
			if (hasRegex)
				return cachedRegex;
			
			lock (lockObj) {
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
	}

	public abstract class ContextReference
	{
		public abstract IEnumerable<SyntaxContext> GetContexts (SyntaxContext context);
	}

	public class ContextNameContextReference : ContextReference
	{
		public string Name { get; private set; }

		internal ContextNameContextReference (string value)
		{
			this.Name = value;
		}

		public override IEnumerable<SyntaxContext> GetContexts (SyntaxContext context)
		{
			var localContext =context.GetContext (Name);
			if (localContext != null) {
				yield return localContext;
				yield break;
			}

			foreach (var bundle in SyntaxHighlightingService.AllBundles) {
				foreach (var highlighting in bundle.Highlightings) {
					if (highlighting.Name == Name) {
						yield return highlighting.MainContext;
					}
				}
			}
		}
	}

	public class ContextNameListContextReference : ContextReference
	{
		public ContextNameListContextReference (IReadOnlyList<string> names)
		{
			this.Names = names;
		}

		public IReadOnlyList<string> Names { get; private set; }

		public override IEnumerable<SyntaxContext> GetContexts (SyntaxContext context)
		{
			foreach (var name in Names)
				yield return context.GetContext (name);
		}
	}

	public class AnonymousMatchContextReference : ContextReference
	{
		public SyntaxContext Context { get; private set; }

		internal AnonymousMatchContextReference (SyntaxContext context)
		{
			Context = context;
		}

		public override IEnumerable<SyntaxContext> GetContexts (SyntaxContext context)
		{
			yield return Context;
		}
	}
}