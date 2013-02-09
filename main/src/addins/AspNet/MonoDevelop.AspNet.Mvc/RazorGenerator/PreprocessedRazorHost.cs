//
// PreprocessedRazorHost.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using MonoDevelop.RazorGenerator;
using System.Collections.Generic;

namespace MonoDevelop.RazorGenerator
{
	static class PreprocessedRazorHost
	{
		static readonly IEnumerable<string> defaultImports = new[] {
			"System",
			"System.Collections.Generic",
			"System.Linq",
			"System.Text"
		};

		public static RazorHost Create (string fullPath)
		{
			var transformers = new RazorCodeTransformer[] {
				PreprocessedTemplateCodeTransformers.AddGeneratedTemplateClassAttribute,
				PreprocessedTemplateCodeTransformers.SimplifyHelpers,
				PreprocessedTemplateCodeTransformers.InjectBaseClass,
				PreprocessedTemplateCodeTransformers.MakePartialAndRemoveCtor,
			};
			var host = new RazorHost (fullPath, transformers: transformers) {
				DefaultBaseClass = "",
				EnableLinePragmas = true,
			};
			foreach (var import in defaultImports) {
				host.NamespaceImports.Add (import);
			}
			host.ParserFactory = (h) => new PreprocessedCSharpRazorCodeParser ();
			return host;
		}
	}
}
