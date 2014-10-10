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

namespace MonoDevelop.CodeIssues
{
	static class CodeDiagnosticRunner
	{
		public static IEnumerable<Result> Check (AnalysisDocument analysisDocument, CancellationToken cancellationToken)
		{
			var input = analysisDocument.DocumentContext;
			if (!AnalysisOptions.EnableFancyFeatures || input.Project == null || !input.IsCompileableInProject || input.AnalysisDocument == null)
				return Enumerable.Empty<Result> ();

			var compilation = input.GetCompilationAsync (cancellationToken).Result;
			var language = CodeRefactoringService.MimeTypeToLanguage (analysisDocument.Editor.MimeType);
			try {
				var options = new AnalyzerOptions(new AdditionalStream[0], new Dictionary<string, string> ());
				var providers = new List<DiagnosticAnalyzer> ();
				var alreadyAdded = new HashSet<Type>();
				foreach (var issue in CodeDiagnosticService.GetCodeIssues (language)) {
					if (alreadyAdded.Contains (issue.CodeIssueType))
						continue;
					alreadyAdded.Add (issue.CodeIssueType);
					var provider = issue.GetProvider ();
					providers.Add (provider);
				}
			
				var driver = AnalyzerDriver<SyntaxKind>.Create(
					compilation,
					System.Collections.Immutable.ImmutableArray<DiagnosticAnalyzer>.Empty.AddRange(providers),
					options,
					out compilation,
					CancellationToken.None
				);

				var model = compilation.GetSemanticModel (input.AnalysisDocument.GetSyntaxTreeAsync ().Result);
				model.GetDiagnostics ();
				model.GetSyntaxDiagnostics ();
				model.GetDeclarationDiagnostics ();
				model.GetMethodBodyDiagnostics ();

				var diagnosticList = driver.GetDiagnosticsAsync ().Result;
				return diagnosticList
					.Where (d => !string.IsNullOrEmpty (d.Description))
					.Select (diagnostic => {
						var res = new DiagnosticResult(diagnostic);
						var line = analysisDocument.Editor.GetLineByOffset (res.Region.Start);
//						Console.WriteLine (diagnostic.Id + "/" + res.Region +"/" + analysisDocument.Editor.GetTextAt (line));
						return res;
					});
			} catch (Exception e) {
				LoggingService.LogError ("Error while running diagnostics.", e); 
				return Enumerable.Empty<Result> ();
			}
		}
	}
}