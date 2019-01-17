﻿//
// RazorCompletionBuilder.cs
//
// Author:
//		Piotr Dowgiallo <sparekd@gmail.com>
//
// Copyright (c) 2012 Piotr Dowgiallo
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

using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;

namespace MonoDevelop.AspNet.Razor
{
	// Directives and statements present in C# are left out to avoid duplication
	static class RazorCompletion
	{
		public static CompletionDataList GetRazorDirectives (RazorHostKind kind)
		{
			var list = new CompletionDataList ();
			AddRazorDirectives (list, kind);
			return list;
		}

		public static void AddRazorDirectives (CompletionDataList list, RazorHostKind kind)
		{
			string icon = "md-keyword";

			list.Add ("helper", icon, GettextCatalog.GetString ("Defines a helper"));
			list.Add ("functions", icon, GettextCatalog.GetString ("Defines a region of class members"));
			list.Add ("using", icon, GettextCatalog.GetString ("Imports a namespace"));

			if (kind == RazorHostKind.WebCode)
				return;

			list.Add ("inherits", icon, GettextCatalog.GetString ("Defines a base class of the view"));
			list.Add ("model", icon, GettextCatalog.GetString ("References a strongly-typed model"));

			if (kind == RazorHostKind.WebPage) {
				list.Add ("layout", icon, GettextCatalog.GetString ("Defines a layout file to use in this view"));
				list.Add ("sessionstate", icon, GettextCatalog.GetString ("Defines a sessionstate mode"));
				list.Add ("section", icon, GettextCatalog.GetString ("Defines a section"));
			} else if (kind == RazorHostKind.Template) {
				list.Add ("__class", icon, GettextCatalog.GetString ("Customizes the generated class"));
				list.Add ("__property", icon, GettextCatalog.GetString ("Adds a property"));
			}
		}

		static void AddRazorTemplates (CompletionDataList list, RazorHostKind kind)
		{
			string icon = "md-template";
			list.Add ("inherits", icon, GettextCatalog.GetString ("Template for inherits directive"));
			list.Add ("model", icon, GettextCatalog.GetString ("Template for model directive"));
			list.Add ("helper", icon, GettextCatalog.GetString ("Template for helper directive"));
			list.Add ("functions", icon, GettextCatalog.GetString ("Template for functions directive"));
			list.Add ("using", icon, GettextCatalog.GetString ("Template for using statement"));

			if (kind == RazorHostKind.WebPage) {
				list.Add ("section", icon, GettextCatalog.GetString ("Template for section directive"));
			}
		}

		public static CompletionDataList GetAllRazorSymbols (RazorHostKind kind)
		{
			var list = new CompletionDataList ();
			AddAllRazorSymbols (list, kind);
			return list;
		}

		public static void AddAllRazorSymbols (CompletionDataList list, RazorHostKind kind)
		{
			if (list == null)
				return;
			AddRazorBeginExpressions (list);
			AddRazorDirectives (list, kind);
			AddRazorTemplates (list, kind);
		}

		public static void AddRazorBeginExpressions (CompletionDataList list)
		{
			string icon = "md-literal";
			list.Add ("{", icon, GettextCatalog.GetString ("Razor code block"));
			list.Add ("*", icon, GettextCatalog.GetString ("Razor comment"));
			list.Add ("(", icon, GettextCatalog.GetString ("Razor explicit expression"));
		}
	}
}
