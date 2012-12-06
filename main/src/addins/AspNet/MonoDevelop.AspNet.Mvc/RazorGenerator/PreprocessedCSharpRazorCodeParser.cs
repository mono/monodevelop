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
using System.Web.Mvc.Properties;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;
using System.Web.Razor.Text;
using System.Collections.Generic;
using System.Web.Razor.Parser.SyntaxTree;
using System.Web.Razor.Tokenizer.Symbols;
using System;

namespace MonoDevelop.RazorGenerator
{
	class PreprocessedCSharpRazorCodeParser : CSharpCodeParser
	{
		public const string ModelKeyword = "model";
		public const string PropertyKeyword = "__property";
		public const string NameKeyword = "__name";
		public const string AccessKeyword = "__access";

		Dictionary<string,string> directives;
		List<string[]> properties;

		public PreprocessedCSharpRazorCodeParser(Dictionary<string,string> directives, List<string[]> properties)
		{
			this.directives = directives;
			this.properties = properties;

			MapDirectives (ModelDirective, ModelKeyword);
			MapDirectives (PropertyDirective, PropertyKeyword);
			MapDirectives (ClassAccessDirective, AccessKeyword);
			MapDirectives (ClassNameDirective, NameKeyword);
		}

		void ModelDirective ()
		{
			ValueDirective (ModelKeyword, true, s => {
				directives [ModelKeyword] = s;
			});
		}

		void PropertyDirective ()
		{
			ValueDirective (PropertyKeyword, true, s => {
				var split = s.Split (new[] { ' ', '\t'}, StringSplitOptions.RemoveEmptyEntries);
				if (split.Length == 0) {
				}
				properties.Add (split);
			});
		}

		void ClassNameDirective ()
		{
			ValueDirective (NameKeyword, true, s => {
				directives [NameKeyword] = s;
			});
		}

		void ClassAccessDirective ()
		{
			ValueDirective (AccessKeyword, true, s => {
				switch (s) {
				case "public":
				case "internal":
					directives [AccessKeyword] = s;
					break;
				default:
					Context.OnError (CurrentLocation, "Invalid access value");
					break;
				}
			});
		}
		
		void ValueDirective (string keyword, bool checkOne, Action<string> valueParsed)
		{
			AssertDirective (NameKeyword);
			AcceptAndMoveNext ();
			if (checkOne && directives.ContainsKey (keyword)) {
				Context.OnError (
					CurrentLocation,
					string.Format ("Only one '{0}' directive is permitted", keyword)
					);
			}
			BaseTypeDirective (
				string.Format ("The '{0}' directive must have a value", AccessKeyword),
				s => {
				if (s != null)
					valueParsed (s);
				return null;
			}
			);
		}
	}
}