// 
// CodeAnalysisRunner.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
//#define PROFILE
using System;
using System.Linq;
using MonoDevelop.AnalysisCore;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using System.Threading;
using MonoDevelop.CodeIssues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using MonoDevelop.CodeActions;
using MonoDevelop.Core;
using MonoDevelop.AnalysisCore.Gui;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;

namespace MonoDevelop.CodeIssues
{
	static class CodeDiagnosticRunner
	{
		public static IEnumerable<Result> Check (AnalysisDocument analysisDocument, CancellationToken cancellationToken)
		{
			var input = analysisDocument.DocumentContext;
			if (!AnalysisOptions.EnableFancyFeatures || input.Project == null || !input.IsCompileableInProject || input.AnalysisDocument == null)
				return Enumerable.Empty<Result> ();
			try {
				var model = input.ParsedDocument.GetAst<SemanticModel> ();
				if (model == null)
					return Enumerable.Empty<Result> ();
				var compilation = model.Compilation;
				var language = CodeRefactoringService.MimeTypeToLanguage (analysisDocument.Editor.MimeType);

				var providers = new List<DiagnosticAnalyzer> ();
				var alreadyAdded = new HashSet<Type>();
				var diagnostics = CodeRefactoringService.GetCodeDiagnosticsAsync (analysisDocument.DocumentContext, language, cancellationToken);
				foreach (var diagnostic in diagnostics.Result) {
					if (alreadyAdded.Contains (diagnostic.DiagnosticAnalyzerType))
						continue;
					alreadyAdded.Add (diagnostic.DiagnosticAnalyzerType);
					var provider = diagnostic.GetProvider ();
					if (provider == null)
						continue;
					providers.Add (provider);
				}

				if (providers.Count == 0 || cancellationToken.IsCancellationRequested)
					return Enumerable.Empty<Result> ();
				var localCompilation = CSharpCompilation.Create (
					compilation.AssemblyName, 
					new[] { model.SyntaxTree }, 
					compilation.References, 
					(CSharpCompilationOptions)compilation.Options
				);

				CompilationWithAnalyzers compilationWithAnalyzer;
				try {
					compilationWithAnalyzer = localCompilation.WithAnalyzers (System.Collections.Immutable.ImmutableArray<DiagnosticAnalyzer>.Empty.AddRange (providers), null, cancellationToken); 
				} catch (Exception) {
					return Enumerable.Empty<Result> ();
				}

				if (input.ParsedDocument == null || cancellationToken.IsCancellationRequested)
					return Enumerable.Empty<Result> ();
				var diagnosticList = new List<Diagnostic> ();
				diagnosticList.AddRange (compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync ().Result);
				return diagnosticList
					.Where (d => !d.Id.StartsWith("CS", StringComparison.Ordinal))
					.Select (diagnostic => {
						var res = new DiagnosticResult(diagnostic);
						// var line = analysisDocument.Editor.GetLineByOffset (res.Region.Start);
						// Console.WriteLine (diagnostic.Id + "/" + res.Region +"/" + analysisDocument.Editor.GetTextAt (line));
						return res;
					});
			} catch (OperationCanceledException) {
				return Enumerable.Empty<Result> ();
			}  catch (AggregateException ae) {
				ae.Flatten ().Handle (ix => ix is OperationCanceledException);
				return Enumerable.Empty<Result> ();
			} catch (Exception e) {
				LoggingService.LogError ("Error while running diagnostics.", e); 
				return Enumerable.Empty<Result> ();
			}
		}
	}
}