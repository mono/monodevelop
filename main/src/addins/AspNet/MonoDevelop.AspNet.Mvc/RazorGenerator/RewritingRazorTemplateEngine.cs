//
// RewritingRazorTemplateEngine.cs
//
// Author:
//       Michael Hutchinson <mhutch@xamarin.com>
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

using System.Web.Razor;
using System.Web.Razor.Parser.SyntaxTree;
using System.Collections.Generic;
using System.Threading;
using System.Web.Razor.Text;
using System.Web.Razor.Generator;
using System.Web.Razor.Parser;

namespace MonoDevelop.RazorGenerator
{
	class RewritingRazorTemplateEngine : RazorTemplateEngine
	{
		public RewritingRazorTemplateEngine (RazorEngineHost host, params ISyntaxTreeRewriter[] rewriters) : base (host)
		{
			this.Rewriters = rewriters;
		}

		internal IList<ISyntaxTreeRewriter> Rewriters { get; private set; }

		protected override ParserResults ParseTemplateCore (ITextDocument input, CancellationToken? cancelToken)
		{
			var results = base.ParseTemplateCore (input, cancelToken);
			if (Host.DesignTimeMode || !results.Success || cancelToken.HasValue && cancelToken.Value.IsCancellationRequested)
				return results;

			var current = results.Document;
			foreach (ISyntaxTreeRewriter rewriter in Rewriters)
				current = rewriter.Rewrite (current);

			return new ParserResults (current, results.ParserErrors);
		}

		//copied from base impl in RazorTemplateEngine.ParseTemplateCore, only change is that it actually calls ParseTemplateCore
		protected override GeneratorResults GenerateCodeCore (ITextDocument input, string className, string rootNamespace, string sourceFileName, CancellationToken? cancelToken)
		{
			className = (className ?? Host.DefaultClassName) ?? DefaultClassName;
			rootNamespace = (rootNamespace ?? Host.DefaultNamespace) ?? DefaultNamespace;

			ParserResults results = ParseTemplateCore (input, cancelToken);

			// Generate code
			RazorCodeGenerator generator = CreateCodeGenerator(className, rootNamespace, sourceFileName);
			generator.DesignTimeMode = Host.DesignTimeMode;
			generator.Visit(results);

			// Post process code
			Host.PostProcessGeneratedCode(generator.Context);

			// Extract design-time mappings
			IDictionary<int, GeneratedCodeMapping> designTimeLineMappings = null;
			if (Host.DesignTimeMode)
			{
				designTimeLineMappings = generator.Context.CodeMappings;
			}

			// Collect results and return
			return new GeneratorResults(results, generator.Context.CompileUnit, designTimeLineMappings);
		}
	}
}