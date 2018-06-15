//
// AnalyzeCurrentProjectHandler.cs
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
using MonoDevelop.Projects;
using GLib;
using MonoDevelop.CodeIssues;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.Refactoring
{
	class AnalyzeCurrentProjectHandler : CommandHandler
	{

		protected override void Update (CommandInfo info)
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (project == null || TypeSystemService.GetCodeAnalysisProject (project) == null) {
				info.Text = GettextCatalog.GetString ("Current Project");
				info.Enabled = false;
				return;
			}
			info.Text = GettextCatalog.GetString ("Project '{0}'", project.Name);
			info.Enabled = true;
		}

		protected override void Run ()
		{
			Execute ();
		}

		internal static async void Execute ()
		{
			var project = IdeApp.ProjectOperations.CurrentSelectedProject;
			if (project == null)
				return;
			var analysisProject = TypeSystemService.GetCodeAnalysisProject (project);
			if (analysisProject == null)
				return;
			try {
				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Analyzing project"), null, false, true, false, null, true)) {
					CancellationToken token = monitor.CancellationToken;
					var allDiagnostics = await Task.Run (async delegate {
						var diagnosticList = new List<Diagnostic> ();
						monitor.BeginTask (GettextCatalog.GetString ("Analyzing {0}", project.Name), 1);
						var providers = await AnalyzeWholeSolutionHandler.GetProviders (analysisProject);
						diagnosticList.AddRange (await AnalyzeWholeSolutionHandler.GetDiagnostics (analysisProject, providers, token));
						monitor.EndTask ();
						return diagnosticList;
					}).ConfigureAwait (false);

					await Runtime.RunInMainThread (delegate {
						AnalyzeWholeSolutionHandler.Report (monitor, allDiagnostics, project);
					}).ConfigureAwait (false);
				}
			} catch (OperationCanceledException) {
			} catch (AggregateException ae) {
				ae.Flatten ().Handle (ix => ix is OperationCanceledException);
			} catch (Exception e) {
				LoggingService.LogError ("Error while running diagnostics.", e);
			}
		}
	}
}