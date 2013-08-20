//
// CodeIssueBatchRunner.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
// Copyright (c) 2013 Simon Lindgren
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
using System.Linq;
using System.Threading;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;
using System.IO;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.Refactoring;
using MonoDevelop.Core;
using System.Collections.Concurrent;
using ICSharpCode.NRefactory.TypeSystem;
using Mono.TextEditor;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CodeIssues
{
	public class CodeAnalysisBatchRunner
	{
		object _lock = new object ();
		CancellationTokenSource tokenSource;
		IIssueSummarySink destinationGroup;

		ConcurrentDictionary<string, object> processedFiles;
		
		public IIssueSummarySink IssueSink {
			get {
				lock (_lock) {
					return destinationGroup;
				}
			}
			set {
				lock (_lock) {
					if (state == AnalysisState.Running) {
						throw new InvalidOperationException ("Cannot change destination group while the analysis is running.");
					}
					destinationGroup = value;
				}
			}
		}
		
		AnalysisState state = AnalysisState.NeverStarted;
		/// <summary>
		/// Gets or sets the state.
		/// </summary>
		/// <value>The state.</value>
		public AnalysisState State {
			get {
				lock (_lock) {
					return state;
				}
			}
			private set {
				AnalysisState old;
				lock (_lock) {
					old = state;
					state = value;
				}
				OnAnalysisStateChanged (new AnalysisStateChangeEventArgs (old, value));
			}
		}
		
		protected virtual void OnAnalysisStateChanged (AnalysisStateChangeEventArgs e)
		{
			var handler = analysisStateChanged;
			if (handler != null)
				handler (this, e);
		}

		event EventHandler<AnalysisStateChangeEventArgs> analysisStateChanged;
		/// <summary>
		/// Occurs when the state of the runner is changed.
		/// </summary>		
		public event EventHandler<AnalysisStateChangeEventArgs> AnalysisStateChanged {
			add {
				lock (_lock) {
					analysisStateChanged += value;
				}
			}
			remove {
				lock (_lock) {
					analysisStateChanged -= value;
				}
			}
		}
		
		public void StartAnalysis (WorkspaceItem solution)
		{
			lock (_lock) {
				tokenSource = new CancellationTokenSource ();
				processedFiles = new ConcurrentDictionary<string, object> ();
				ThreadPool.QueueUserWorkItem (delegate {
					State = AnalysisState.Running;

					using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor ("Analyzing solution", null, false)) {
						AnalysisState oldState;
						AnalysisState newState;
						try {
							int work = 0;
							foreach (var project in solution.GetAllProjects ()) {
								work += project.Files.Count (f => f.BuildAction == BuildAction.Compile);
							}
							monitor.BeginTask ("Analyzing solution", work);
							foreach (var project in solution.GetAllProjects ()) {
								if (tokenSource.IsCancellationRequested)
									break;
								var content = TypeSystemService.GetProjectContext (project);
								Parallel.ForEach (project.Files, file => {
									try {
										AnalyzeFile (file, content);
										monitor.Step (1);
									} catch (Exception ex) {
										LoggingService.LogError ("Error while running code issue on:" + file.Name, ex);
									}
								});
							}
							// Cleanup
							lock (_lock) {
								oldState = state;
								if (tokenSource.IsCancellationRequested) {
									newState = AnalysisState.Cancelled;
								} else {
									newState = AnalysisState.Completed;
								}
								state = newState;
								tokenSource = null;
							}
							OnAnalysisStateChanged (new AnalysisStateChangeEventArgs (oldState, newState));
						} catch (Exception e) {
							lock (_lock) {
								oldState = state;
								state = AnalysisState.Error;
								newState = state;
								tokenSource = null;
							}
							OnAnalysisStateChanged (new AnalysisStateChangeEventArgs (oldState, newState));
							// Do not rethrow in a thread pool
							MessageService.ShowException (e);
						}
					}
				});
			}
		}

		void AnalyzeFile (ProjectFile file, IProjectContent content)
		{
			var me = new object ();
			var owner = processedFiles.AddOrUpdate (file.Name, me, (key, old) => old);
			if (me != owner)
				return;

			if (file.BuildAction != BuildAction.Compile || tokenSource.IsCancellationRequested)
				return;

			TextEditorData editor;
			try {
				editor = TextFileProvider.Instance.GetReadOnlyTextEditorData (file.FilePath);
			} catch (FileNotFoundException) {
				// Swallow exception and ignore this file
				return;
			}
			var document = TypeSystemService.ParseFile (file.Project, editor);
			if (document == null)
				return;

			var compilation = content.AddOrUpdateFiles (document.ParsedFile).CreateCompilation ();

			CSharpAstResolver resolver;
			using (var timer = ExtensionMethods.ResolveCounter.BeginTiming ()) {
				resolver = new CSharpAstResolver (compilation, document.GetAst<SyntaxTree> (), document.ParsedFile as ICSharpCode.NRefactory.CSharp.TypeSystem.CSharpUnresolvedFile);
				resolver.ApplyNavigator (new ExtensionMethods.ConstantModeResolveVisitorNavigator (ResolveVisitorNavigationMode.Resolve, null));
			}
			var context = document.CreateRefactoringContextWithEditor (editor, resolver, tokenSource.Token);

			CodeIssueProvider[] codeIssueProvider = RefactoringService.GetInspectors (editor.MimeType).ToArray ();
			foreach (var provider in codeIssueProvider) {
				var severity = provider.GetSeverity ();
				if (severity == Severity.None || tokenSource.IsCancellationRequested)
					return;
				try {
					foreach (var issue in provider.GetIssues (context, tokenSource.Token)) {
						AddIssue (file, provider, issue);
					}
				} catch (OperationCanceledException) {
					// The operation was cancelled, no-op as the user-visible parts are
					// handled elsewhere
				}
			}
		}

		void AddIssue (ProjectFile file, CodeIssueProvider provider, CodeIssue r)
		{
			var issue = new IssueSummary {
				IssueDescription = r.Description,
				Region = r.Region,
				ProviderTitle = provider.Title,
				ProviderDescription = provider.Description,
				ProviderCategory = provider.Category,
				Severity = provider.GetSeverity (),
				IssueMarker = provider.IssueMarker,
				File = file,
				Project = file.Project,
				InspectorIdString = r.InspectorIdString
			};
			issue.Actions = r.Actions.Select (a => new ActionSummary {
				Batchable = a.SupportsBatchRunning,
				SiblingKey = a.SiblingKey,
				Title = a.Title,
				Region = a.DocumentRegion,
				IssueSummary = issue
			}).ToList ();
			IssueSink.AddIssue (issue);
		}
		
		public void Stop() {
			lock (_lock) {
				if (state != AnalysisState.Running) {
					throw new InvalidOperationException ("Cannot stop the runner since it is not running");
				}
				tokenSource.Cancel ();
			}
		}
	}
}

