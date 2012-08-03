//
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;

namespace MonoDevelop.AspNet.Mvc.Completion
{
	// Directives and statements present in C# are left out to avoid duplication
	static class RazorCompletion
	{
		public static CompletionDataList GetRazorDirectives ()
		{
			var list = new CompletionDataList ();
			AddRazorDirectives (list);
			return list;
		}

		public static void AddRazorDirectives (CompletionDataList list)
		{
			string icon = "md-keyword";
			list.Add ("inherits", icon, "Defines a base class of the view");
			list.Add ("layout", icon, "Defines a layout file to use in this view");
			list.Add ("model", icon, "References a strongly-typed model");
			list.Add ("sessionstate", icon, "Defines a sessionstate mode");
			list.Add ("helper", icon, "Defines a helper");
			list.Add ("section", icon, "Defines a section");
			list.Add ("functions", icon, "Enables to define functions in this view");
		}

		private static void AddRazorTemplates (CompletionDataList list)
		{
			string icon = "md-template";
			list.Add ("inherits", icon, "Template for inherits directive");
			list.Add ("model", icon, "Template for model directive");
			list.Add ("helper", icon, "Template for helper directive");
			list.Add ("section", icon, "Template for section directive");
			list.Add ("functions", icon, "Template for functions directive");
			list.Add ("using", icon, "Template for using statement");
		}

		public static CompletionDataList GetAllRazorSymbols ()
		{
			var list = new CompletionDataList ();
			AddAllRazorSymbols (list);
			return list;
		}

		public static void AddAllRazorSymbols (CompletionDataList list)
		{
			if (list == null)
				return;
			AddRazorBeginExpressions (list);
			AddRazorDirectives (list);
			AddRazorTemplates (list);
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
