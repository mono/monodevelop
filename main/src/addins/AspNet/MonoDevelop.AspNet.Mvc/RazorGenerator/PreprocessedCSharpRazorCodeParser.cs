//
// Based on MvcCSharpRazorCodeParser.cs
//     Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
//     Licensed under the Microsoft Public License (MS-PL)
//
// Changes:
//     Author: Michael Hutchinson <mhutch@xamarin.com>
//     Copyright (c) 2012 Xamarin Inc (http://xamarin.com)
//     Licensed under the Microsoft Public License (MS-PL)
//

using System.Globalization;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Text;
using System.Collections.Generic;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Tokenizer.Symbols;
using System;
using System.CodeDom;

namespace MonoDevelop.RazorGenerator
{
	class PreprocessedCSharpRazorCodeParser : CSharpCodeParser
	{
		public const string ModelKeyword = "model";
		public const string PropertyKeyword = "__property";
		public const string ClassKeyword = "__class";

		HashSet<string> directives = new HashSet<string> ();

		public PreprocessedCSharpRazorCodeParser()
		{
			MapDirectives (ModelDirective, ModelKeyword);
			MapDirectives (PropertyDirective, PropertyKeyword);
			MapDirectives (ClassDirective, ClassKeyword);
		}

		void ModelDirective ()
		{
			ValueDirective (ModelKeyword, true, (s, l) => {
				var split = GetArgumentWords (s);
				if (split.Length == 1) {
					return new PropertyCodeGenerator (split[0], "Model");
				}
				Context.OnError (l, string.Format ("The '{0}' directive requires exactly one argument", ModelKeyword));
				return null;
			});
		}

		void PropertyDirective ()
		{
			ValueDirective (PropertyKeyword, true, (s, l) => {
				var split = GetArgumentWords (s);
				if (split.Length == 2) {
					return new PropertyCodeGenerator (split[0], split[1]);
				}
				Context.OnError (l, string.Format ("The '{0}' directive requires exactly two arguments", PropertyKeyword));
				return null;
			});
		}

		void ClassDirective ()
		{
			ValueDirective (ClassKeyword, true, (s, l) => {
				var split = GetArgumentWords (s);
				if (split.Length != 1 && split.Length != 2) {
					Context.OnError (l, string.Format ("The '{0}' directive requires one or two arguments", ClassKeyword));
					return null;
				}
				string name = null, access = null;
				if (split[0] == "public" || split[0] == "internal") {
					access = split[0];
				} else {
					name = split[0];
				}
				if (split.Length == 2) {
					if (access == null) {
						string err = "If '{0}' directive has two arguments, the first must be 'public' or 'internal'.";
						Context.OnError (l, string.Format (err, ClassKeyword));
						return null;
					}
					name = split[1];
				}
				return new ClassNameCodeGenerator (access, name);
			});
		}

		static char[] wordSplitChars = new[] { ' ', '\t'};
		static string[] GetArgumentWords (string value)
		{
			return value.Split (wordSplitChars, StringSplitOptions.RemoveEmptyEntries);
		}
		
		void ValueDirective (string keyword, bool checkOne, Func<string,SourceLocation,SpanCodeGenerator> valueParsed)
		{
			AssertDirective (ClassKeyword);
			AcceptAndMoveNext ();
			if (checkOne && !directives.Add (keyword)) {
				Context.OnError (
					CurrentLocation,
					string.Format ("Only one '{0}' directive is permitted", keyword)
					);
			}
			SourceLocation location = CurrentLocation;
			BaseTypeDirective (
				string.Format ("The '{0}' directive must have a value", keyword),
				s => {
					if (s != null)
						return valueParsed (s, location);
					return null;
				}
			);
		}

		class PropertyCodeGenerator : SpanCodeGenerator
		{
			public PropertyCodeGenerator (string type, string name)
			{
				this.Type = type;
				this.Name = name;
			}

			public string Type {get; private set; }
			public string Name { get; private set; }

			public override void GenerateCode (Span target, CodeGeneratorContext context)
			{
				var text = string.Format ("public {0} {1} {{ get; set; }}\n", Type, Name);
				var prop = new CodeSnippetTypeMember (text);
				prop.LinePragma = new CodeLinePragma (context.SourceFile, target.Start.LineIndex + 1);
				context.GeneratedClass.Members.Add (prop);
			}
		}

		class ClassNameCodeGenerator : SpanCodeGenerator
		{
			public ClassNameCodeGenerator (string access, string name)
			{
				this.Access = access;
				this.Name = name;
			}

			public string Name {get; private set; }
			public string Access {get; private set; }

			public override void GenerateCode (Span target, CodeGeneratorContext context)
			{
				if (Name != null) {
					var idx = Name.LastIndexOf ('.');
					if (idx > 0) {
						context.Namespace.Name = Name.Substring (0, idx);
						context.GeneratedClass.Name = Name.Substring (idx + 1);
					} else {
						context.GeneratedClass.Name = Name;
					}
				}

				if (Access == "public") {
					context.GeneratedClass.TypeAttributes |= System.Reflection.TypeAttributes.Public;
				} else if (Access == "internal") {
					context.GeneratedClass.TypeAttributes &= ~System.Reflection.TypeAttributes.Public;
				}
			}
		}
	}
}