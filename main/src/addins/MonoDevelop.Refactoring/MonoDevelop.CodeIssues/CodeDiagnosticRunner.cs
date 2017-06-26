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
using System.Diagnostics;
using MonoDevelop.Ide.Editor;
using System.Collections.Immutable;

namespace MonoDevelop.CodeIssues
{
	static class CodeDiagnosticRunner
	{
		static IEnumerable<CodeDiagnosticDescriptor> diagnostics;
		static TraceListener consoleTraceListener = new ConsoleTraceListener ();

		static bool SkipContext (DocumentContext ctx)
		{
			return (ctx.IsAdHocProject || !(ctx.Project is MonoDevelop.Projects.DotNetProject));
		}

		// Old code, until we get EditorFeatures into composition so we can switch code fix service.
		public static async Task<IEnumerable<Result>> Check (AnalysisDocument analysisDocument, CancellationToken cancellationToken)
		{
			var input = analysisDocument.DocumentContext;
			if (!AnalysisOptions.EnableFancyFeatures || input.Project == null || !input.IsCompileableInProject || input.AnalysisDocument == null)
				return Enumerable.Empty<Result> ();
			if (SkipContext (input))
				return Enumerable.Empty<Result> ();
			try {
				var model = await analysisDocument.DocumentContext.AnalysisDocument.GetSemanticModelAsync (cancellationToken);
				if (model == null)
					return Enumerable.Empty<Result> ();
				var compilation = model.Compilation;
				var language = CodeRefactoringService.MimeTypeToLanguage (analysisDocument.Editor.MimeType);

				var providers = new List<DiagnosticAnalyzer> ();
				var alreadyAdded = new HashSet<Type>();
				if (diagnostics == null) {
					diagnostics = await CodeRefactoringService.GetCodeDiagnosticsAsync (analysisDocument.DocumentContext, language, cancellationToken);
				}
				var diagnosticTable = new Dictionary<string, CodeDiagnosticDescriptor> ();
				foreach (var diagnostic in diagnostics) {
					if (alreadyAdded.Contains (diagnostic.DiagnosticAnalyzerType))
						continue;
					if (!diagnostic.IsEnabled)
						continue;
					alreadyAdded.Add (diagnostic.DiagnosticAnalyzerType);
					var provider = diagnostic.GetProvider ();
					if (provider == null)
						continue;
					foreach (var diag in provider.SupportedDiagnostics)
						diagnosticTable [diag.Id] = diagnostic;
					providers.Add (provider);
				}

				if (providers.Count == 0 || cancellationToken.IsCancellationRequested)
					return Enumerable.Empty<Result> ();
				#if DEBUG
				Debug.Listeners.Add (consoleTraceListener); 
				#endif

				CompilationWithAnalyzers compilationWithAnalyzer;
				var analyzers = ImmutableArray<DiagnosticAnalyzer>.Empty.AddRange (providers);
				var diagnosticList = new List<Diagnostic> ();
				try {
					var sol = analysisDocument.DocumentContext.AnalysisDocument.Project.Solution;
					var options = new CompilationWithAnalyzersOptions (
						new WorkspaceAnalyzerOptions (
							new AnalyzerOptions (ImmutableArray<AdditionalText>.Empty),
							sol.Options,
							sol),
						delegate (Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diag) {
							LoggingService.LogError ("Exception in diagnostic analyzer " + diag.Id + ":" + diag.GetMessage (), exception);
						},
						false, 
						false
					);

					compilationWithAnalyzer = compilation.WithAnalyzers (analyzers, options);
					if (input.ParsedDocument == null || cancellationToken.IsCancellationRequested)
						return Enumerable.Empty<Result> ();

					diagnosticList.AddRange (await compilationWithAnalyzer.GetAnalyzerSemanticDiagnosticsAsync (model, null, cancellationToken).ConfigureAwait (false));
					diagnosticList.AddRange (await compilationWithAnalyzer.GetAnalyzerSyntaxDiagnosticsAsync (model.SyntaxTree, cancellationToken).ConfigureAwait (false));
				} catch (OperationCanceledException) {
				} catch (AggregateException ae) {
					ae.Flatten ().Handle (ix => ix is OperationCanceledException);
				} catch (Exception ex) {
					LoggingService.LogError ("Error creating analyzer compilation", ex);
					return Enumerable.Empty<Result> ();
				} finally {
					#if DEBUG
					Debug.Listeners.Remove (consoleTraceListener); 
					#endif
					CompilationWithAnalyzers.ClearAnalyzerState (analyzers);
				}

				return diagnosticList
					.Where (d => !d.Id.StartsWith("CS", StringComparison.Ordinal))
					.Where (d => diagnosticTable[d.Id].GetIsEnabled (d.Descriptor))
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

		public static async Task<IEnumerable<Result>> Check (AnalysisDocument analysisDocument, CancellationToken cancellationToken, ImmutableArray<DiagnosticData> results)
		{
			var input = analysisDocument.DocumentContext;
			if (!AnalysisOptions.EnableFancyFeatures || input.Project == null || !input.IsCompileableInProject || input.AnalysisDocument == null)
				return Enumerable.Empty<Result> ();
			if (SkipContext (input))
				return Enumerable.Empty<Result> ();
			try {
#if DEBUG
				Debug.Listeners.Add (consoleTraceListener);
#endif

				var resultList = new List<Result> ();
				foreach (var data in results) {
					if (data.Id.StartsWith ("CS", StringComparison.Ordinal))
						continue;

					var diagnostic = await data.ToDiagnosticAsync (input.AnalysisDocument.Project, cancellationToken);
					if (DiagnosticResult.DescriptorHasTag (diagnostic.Descriptor, WellKnownDiagnosticTags.EditAndContinue))
						continue;
					
					resultList.Add (new DiagnosticResult (diagnostic));

				}
				return resultList;
			} catch (OperationCanceledException) {
				return Enumerable.Empty<Result> ();
			} catch (AggregateException ae) {
				ae.Flatten ().Handle (ix => ix is OperationCanceledException);
				return Enumerable.Empty<Result> ();
			} catch (Exception e) {
				LoggingService.LogError ("Error while running diagnostics.", e);
				return Enumerable.Empty<Result> ();
			}
		}

	}
}
