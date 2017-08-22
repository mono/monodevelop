//
// AnalyzeWholeSolutionHandler.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using MonoDevelop.CodeActions;
using MonoDevelop.Ide.Editor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Options;
using MonoDevelop.CodeIssues;
using MonoDevelop.Core;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;
using System.Collections.Immutable;
using System.Linq;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Refactoring
{
	class AnalyzeWholeSolutionHandler : CommandHandler
	{
		internal static async Task<List<DiagnosticAnalyzer>> GetProviders (Project project)
		{
			var providers = new List<DiagnosticAnalyzer> ();
			var alreadyAdded = new HashSet<Type> ();
			var	diagnostics = await CodeRefactoringService.GetCodeDiagnosticsAsync (new EmptyDocumentContext (project), LanguageNames.CSharp, default (CancellationToken));
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

			return providers;
		}

		internal static async Task<ImmutableArray<Diagnostic>> GetDiagnostics (Project project, List<DiagnosticAnalyzer> providers, CancellationToken token)
		{
			var analyzers = ImmutableArray<DiagnosticAnalyzer>.Empty.AddRange (providers);
			try {
				var compilation = await project.GetCompilationAsync (token).ConfigureAwait (false);
				CompilationWithAnalyzers compilationWithAnalyzer;
				var options = new CompilationWithAnalyzersOptions (
					null,
					delegate (Exception exception, DiagnosticAnalyzer analyzer, Diagnostic diag) {
						LoggingService.LogError ("Exception in diagnostic analyzer " + diag.Id + ":" + diag.GetMessage (), exception);
					},
					true,
					false
				);

				compilationWithAnalyzer = compilation.WithAnalyzers (analyzers, options);
				if (token.IsCancellationRequested)
					return ImmutableArray<Diagnostic>.Empty;

				return await compilationWithAnalyzer.GetAnalyzerDiagnosticsAsync ().ConfigureAwait (false);
			} catch (Exception) {
				return ImmutableArray<Diagnostic>.Empty;
			} finally {
				CompilationWithAnalyzers.ClearAnalyzerState (analyzers);
			}
		}

		protected override async void Run ()
		{
			Execute ();
		}

		static TaskSeverity GetSeverity (Diagnostic diagnostic)
		{
			switch (diagnostic.Severity) {
			case DiagnosticSeverity.Warning:
				return TaskSeverity.Warning;
			case DiagnosticSeverity.Error:
				return TaskSeverity.Error;
			default:
				return TaskSeverity.Information;
			}
		}

		internal static async void Execute ()
		{
			var workspace = TypeSystemService.Workspace;
			try {
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Analyzing solution"), null, false, true, false, null, true)) {
					CancellationToken token = monitor.CancellationToken;
					var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
					var allDiagnostics = await Task.Run (async delegate {
						var diagnosticList = new List<Diagnostic> ();
						monitor.BeginTask ("Analyzing solution", workspace.CurrentSolution.Projects.Count ());
						foreach (var project in workspace.CurrentSolution.Projects) {
							var providers = await GetProviders (project);
							monitor.BeginStep (GettextCatalog.GetString ("Analyzing {0}", project.Name), 1);
							diagnosticList.AddRange (await GetDiagnostics (project, providers, token));
							monitor.EndStep ();
						}
						monitor.EndTask ();
						return diagnosticList;
					}).ConfigureAwait (false);
					await Runtime.RunInMainThread (delegate {
						Report (monitor, allDiagnostics, solution);
					}).ConfigureAwait (false);
				}
			} catch (OperationCanceledException) {
			} catch (AggregateException ae) {
				ae.Flatten ().Handle (ix => ix is OperationCanceledException);
			} catch (Exception e) {
				LoggingService.LogError ("Error while running diagnostics.", e);
			}
		}

		internal static void Report (ProgressMonitor monitor, List<Diagnostic> allDiagnostics, Projects.WorkspaceObject parent)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Reporting results..."), allDiagnostics.Count);
			TaskService.Errors.Clear ();
			TaskService.Errors.AddRange (allDiagnostics.Select (diagnostic => {
				var startLinePosition = diagnostic.Location.GetLineSpan ().StartLinePosition;
				return new TaskListEntry (
					diagnostic.Location.SourceTree.FilePath,
					diagnostic.GetMessage (),
					startLinePosition.Character + 1,
					startLinePosition.Line + 1,
					GetSeverity (diagnostic),
					TaskPriority.Normal,
					parent,
					null,
					diagnostic.Descriptor.Category
				);
			}));

			monitor.EndTask ();
			if (!allDiagnostics.Any ())
				monitor.ReportSuccess (GettextCatalog.GetString ("Analysis successful."));
			else
				ShowAnalyzationResults ();
		}

		static void ShowAnalyzationResults ()
		{
			TaskService.ShowErrors ();
			var errorsPad = IdeApp.Workbench.GetPad<ErrorListPad> ().Content as ErrorListPad;
			errorsPad.SetFilter (true, true, true);
		}

#pragma warning disable RECS0083 // Shows NotImplementedException throws in the quick task bar
		class EmptyDocumentContext : DocumentContext
		{
			Project project;

			public EmptyDocumentContext (Project project)
			{
				this.project = project;
			}

			public override Document AnalysisDocument {
				get {
					throw new NotImplementedException ();
				}
			}

			public override string Name {
				get {
					throw new NotImplementedException ();
				}
			}

			public override ParsedDocument ParsedDocument {
				get {
					throw new NotImplementedException ();
				}
			}

			public override Projects.SolutionItem Owner {
				get {
					return TypeSystemService.GetMonoProject (project);
				}
			}

			public override void AttachToProject (Projects.Project project)
			{
				throw new NotImplementedException ();
			}

			public override OptionSet GetOptionSet ()
			{
				throw new NotImplementedException ();
			}

			public override void ReparseDocument ()
			{
				throw new NotImplementedException ();
			}

			public override Task<ParsedDocument> UpdateParseDocument ()
			{
				throw new NotImplementedException ();
			}
		}
#pragma warning restore RECS0083 // Shows NotImplementedException throws in the quick task bar
	}

	class AnalyzeSourceHandler : CommandHandler
	{
		protected override void Run ()
		{
			if (IdeApp.ProjectOperations.CurrentSelectedProject == null){
				AnalyzeWholeSolutionHandler.Execute ();
			} else {
				AnalyzeCurrentProjectHandler.Execute ();
			}
		}
	}
}