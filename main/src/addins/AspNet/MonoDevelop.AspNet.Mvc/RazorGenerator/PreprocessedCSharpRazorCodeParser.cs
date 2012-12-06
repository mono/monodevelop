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
		public const string NameKeyword = "__name";
		public const string AccessKeyword = "__access";

		HashSet<string> directives = new HashSet<string> ();

		public PreprocessedCSharpRazorCodeParser()
		{
			MapDirectives (ModelDirective, ModelKeyword);
			MapDirectives (PropertyDirective, PropertyKeyword);
			MapDirectives (ClassAccessDirective, AccessKeyword);
			MapDirectives (ClassNameDirective, NameKeyword);
		}

		void ModelDirective ()
		{
			ValueDirective (ModelKeyword, true, s => {
				var split = GetArgumentWords (s);
				if (split.Length == 1) {
					return new PropertyCodeGenerator (split[0], "Model");
				}
				Context.OnError (CurrentLocation, string.Format ("The '{0}' directive requires exactly one argument", ModelKeyword));
				return null;
			});
		}

		void PropertyDirective ()
		{
			ValueDirective (PropertyKeyword, true, s => {
				var split = GetArgumentWords (s);
				if (split.Length == 2) {
					return new PropertyCodeGenerator (split[0], split[1]);
				}
				Context.OnError (CurrentLocation, string.Format ("The '{0}' directive requires exactly two arguments", PropertyKeyword));
				return null;
			});
		}

		void ClassNameDirective ()
		{
			ValueDirective (NameKeyword, true, s => {
				var split = GetArgumentWords (s);
				if (split.Length == 1) {
					return new ClassNameCodeGenerator (split[0]);
				}
				Context.OnError (CurrentLocation, string.Format ("The '{0}' directive requires exactly one argument", NameKeyword));
				return null;
			});
		}

		void ClassAccessDirective ()
		{
			ValueDirective (AccessKeyword, true, s => {
				switch (s) {
				case "public":
				case "internal":
					return new ClassAccessCodeGenerator (s);
				default:
					Context.OnError (CurrentLocation, "Invalid access value");
					return null;
				}
			});
		}

		static char[] wordSplitChars = new[] { ' ', '\t'};
		static string[] GetArgumentWords (string value)
		{
			return value.Split (wordSplitChars, StringSplitOptions.RemoveEmptyEntries);
		}
		
		void ValueDirective (string keyword, bool checkOne, Func<string,SpanCodeGenerator> valueParsed)
		{
			AssertDirective (NameKeyword);
			AcceptAndMoveNext ();
			if (checkOne && !directives.Add (keyword)) {
				Context.OnError (
					CurrentLocation,
					string.Format ("Only one '{0}' directive is permitted", keyword)
					);
			}
			BaseTypeDirective (
				string.Format ("The '{0}' directive must have a value", AccessKeyword),
				s => {
					if (s != null)
						return valueParsed (s);
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

		class ClassAccessCodeGenerator : SpanCodeGenerator
		{
			public ClassAccessCodeGenerator (string access)
			{
				this.Access = access;
			}

			public string Access {get; private set; }

			public override void GenerateCode (Span target, CodeGeneratorContext context)
			{
				if (Access == "public") {
					context.GeneratedClass.TypeAttributes |= System.Reflection.TypeAttributes.Public;
				} else {
					context.GeneratedClass.TypeAttributes &= ~System.Reflection.TypeAttributes.Public;
				}
			}
		}

		class ClassNameCodeGenerator : SpanCodeGenerator
		{
			public ClassNameCodeGenerator (string name)
			{
				this.Name = name;
			}

			public string Name {get; private set; }

			public override void GenerateCode (Span target, CodeGeneratorContext context)
			{
				var idx = Name.LastIndexOf ('.');
				if (idx > 0) {
					context.Namespace.Name = Name.Substring (0, idx);
					context.GeneratedClass.Name = Name.Substring (idx + 1);
				} else {
					context.GeneratedClass.Name = Name;
				}
			}
		}
	}
}