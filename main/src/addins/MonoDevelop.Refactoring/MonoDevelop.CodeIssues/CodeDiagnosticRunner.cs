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
using System.Globalization;

namespace MonoDevelop.CodeIssues
{
	static class CodeDiagnosticRunner
	{
		static List<DiagnosticAnalyzer> providers = new List<DiagnosticAnalyzer> ();
		static IEnumerable<CodeDiagnosticDescriptor> diagnostics;
		static Dictionary<string, CodeDiagnosticDescriptor> diagnosticTable;
		static SemaphoreSlim diagnosticLock = new SemaphoreSlim (1, 1);
		static TraceListener consoleTraceListener = new ConsoleTraceListener ();

		static bool SkipContext (DocumentContext ctx)
		{
			return (ctx.IsAdHocProject || !(ctx.Project is MonoDevelop.Projects.DotNetProject));
		}

		static async Task GetDescriptorTable (AnalysisDocument analysisDocument, CancellationToken cancellationToken)
		{
			if (diagnosticTable != null)
				return;
			
			bool locked = await diagnosticLock.WaitAsync (Timeout.Infinite, cancellationToken).ConfigureAwait (false);
			if (diagnosticTable != null)
				return;

			try {
				var language = CodeRefactoringService.MimeTypeToLanguage (analysisDocument.Editor.MimeType);
				var alreadyAdded = new HashSet<Type> ();

				var table = new Dictionary<string, CodeDiagnosticDescriptor> ();

				diagnostics = await CodeRefactoringService.GetCodeDiagnosticsAsync (analysisDocument.DocumentContext, language, cancellationToken);
				foreach (var diagnostic in diagnostics) {
					if (!alreadyAdded.Add (diagnostic.DiagnosticAnalyzerType))
						continue;
					var provider = diagnostic.GetProvider ();
					if (provider == null)
						continue;
					foreach (var diag in provider.SupportedDiagnostics)
						table [diag.Id] = diagnostic;

					providers.Add (provider);
				}

				diagnosticTable = table;
			} finally {
				if (locked)
					diagnosticLock.Release ();
			}
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

				await GetDescriptorTable (analysisDocument, cancellationToken);

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
					.Where (d => !diagnosticTable.TryGetValue (d.Id, out var desc) || desc.GetIsEnabled (d.Descriptor))
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
			try {
#if DEBUG
				Debug.Listeners.Add (consoleTraceListener);
#endif

				await GetDescriptorTable (analysisDocument, cancellationToken);

				var resultList = new List<Result> (results.Length);
				foreach (var data in results) {
					if (input.IsAdHocProject && SkipError (data.Id))
						continue;

					if (DataHasTag (data, WellKnownDiagnosticTags.EditAndContinue))
						continue;

					if (diagnosticTable.TryGetValue (data.Id, out var descriptor) && !descriptor.IsEnabled)
						continue;
					
					var diagnostic = await data.ToDiagnosticAsync (analysisDocument, cancellationToken);
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

		static string [] lexicalError = {
			"CS0594", // ERR_FloatOverflow
			"CS0595", // ERR_InvalidReal
			"CS1009", // ERR_IllegalEscape
			"CS1010", // ERR_NewlineInConst
			"CS1011", // ERR_EmptyCharConst
			"CS1012", // ERR_TooManyCharsInConst
			"CS1015", // ERR_TypeExpected
			"CS1021", // ERR_IntOverflow
			"CS1032", // ERR_PPDefFollowsTokenpp
			"CS1035", // ERR_OpenEndedComment
			"CS1039", // ERR_UnterminatedStringLit
			"CS1040", // ERR_BadDirectivePlacementpp
			"CS1056", // ERR_UnexpectedCharacter
			"CS1056", // ERR_UnexpectedCharacter_EscapedBackslash
			"CS1646", // ERR_ExpectedVerbatimLiteral
			"CS0078", // WRN_LowercaseEllSuffix
			"CS1002", // ; expected
			"CS1519", // Invalid token ';' in class, struct, or interface member declaration
			"CS1031", // Type expected
			"CS0106", // The modifier 'readonly' is not valid for this item
			"CS1576", // The line number specified for #line directive is missing or invalid
			"CS1513" // } expected
		};

		static bool SkipError (string errorId)
		{
			return !lexicalError.Contains (errorId);
		}

		static async Task<Diagnostic> ToDiagnosticAsync (this DiagnosticData data, AnalysisDocument analysisDocument, CancellationToken cancellationToken)
		{
			var project = analysisDocument.DocumentContext.AnalysisDocument.Project;
			var location = await data.DataLocation.ConvertLocationAsync (project, cancellationToken).ConfigureAwait (false);
			var additionalLocations = await data.AdditionalLocations.ConvertLocationsAsync (project, cancellationToken).ConfigureAwait (false);

			DiagnosticSeverity severity;
			if (diagnosticTable.TryGetValue (data.Id, out var desc))
				severity = diagnosticTable [data.Id].GetSeverity (data.Id, data.Severity);
			else
				severity = data.Severity;
			
			return Diagnostic.Create (
				data.Id, data.Category, data.Message, severity, data.DefaultSeverity,
				data.IsEnabledByDefault, GetWarningLevel (severity), data.IsSuppressed, data.Title, data.Description, data.HelpLink,
				location, additionalLocations, customTags: data.CustomTags, properties: data.Properties);
		}

		static bool DataHasTag (DiagnosticData desc, string tag)
		{
			return desc.CustomTags.Any (c => CultureInfo.InvariantCulture.CompareInfo.Compare (c, tag) == 0);
		}

		static int GetWarningLevel (DiagnosticSeverity severity)
		{
			return severity == DiagnosticSeverity.Error ? 0 : 1;
		}

	}
}
