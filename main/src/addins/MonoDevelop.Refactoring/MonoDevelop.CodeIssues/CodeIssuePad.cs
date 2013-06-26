//
// CodeIssuePad.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Ide.Gui;
using Xwt;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;
using System.Threading;
using ICSharpCode.NRefactory.CSharp.Resolver;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using MonoDevelop.Refactoring;
using System.Linq;
using System.Collections.Generic;
using MonoDevelop.Core;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.TypeSystem;
using System.IO;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.CodeIssues
{
	public class CodeIssuePadControl : VBox
	{
		TreeView view = new TreeView ();
		DataField<string> textField = new DataField<string> ();
		DataField<IssueSummary> summaryField = new DataField<IssueSummary> ();
		DataField<IssueGroup> groupField = new DataField<IssueGroup> ();
		Button runButton = new Button ("Run");
		Button cancelButton = new Button ("Cancel");
		GroupingProviderChainControl groupingProviderControl;
		HBox buttonRow = new HBox();
		Timer redrawTimer;
		
		IssueGroup rootGroup;
		TreeStore store;
		CancellationTokenSource tokenSource;

		static Type[] groupingProviders = new[] {
			typeof(CategoryGroupingProvider),
			typeof(ProviderGroupingProvider),
			typeof(SeverityGroupingProvider)
		};

		public CodeIssuePadControl ()
		{
			runButton.Clicked += StartAnalyzation;
			buttonRow.PackStart (runButton);
			
			cancelButton.Clicked += StopAnalyzation;
			cancelButton.Sensitive = false;
			buttonRow.PackStart (cancelButton);
			
			var groupingProvider = new CategoryGroupingProvider {
					Next = new ProviderGroupingProvider()
			};
			groupingProviderControl = new GroupingProviderChainControl (groupingProvider, groupingProviders);
			buttonRow.PackStart (groupingProviderControl);
			
			PackStart (buttonRow);

			store = new TreeStore (textField, summaryField, groupField);
			view.DataSource = store;
			view.HeadersVisible = false;

			view.Columns.Add ("Name", textField);
			
			view.RowActivated += OnRowActivated;
			view.RowExpanding += OnRowExpanding;
			PackStart (view, BoxMode.FillAndExpand);
			
			var rootProvider = groupingProviderControl.RootGroupingProvider;
			rootGroup = new IssueGroup (rootProvider, rootProvider.Next, "root group");
			rootGroup.ChildrenInvalidated += (sender, group) => {
				Application.Invoke (delegate {
					store.Clear ();
					InsertTopRow ();
					UpdateUi ();
				});
			};
		}

		TreeNavigator InsertTopRow ()
		{
			var rootNode = store.AddNode ();
			rootNode.SetValue (textField, "Analyzing...");
			return rootNode;
		}
		
		void StartAnalyzation (object sender, EventArgs e)
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution == null)
				return;
			redrawTimer = new Timer (arg => QueueUiUpdate (), null, 500, 500);
			runButton.Sensitive = false;
			cancelButton.Sensitive = true;
			store.Clear ();
			rootGroup.ClearStatistics();
			tokenSource = new CancellationTokenSource ();
			var rootNode = InsertTopRow ();
			ThreadPool.QueueUserWorkItem (delegate {

				using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor ("Analyzing solution", null, false)) {
					int work = 0;
					foreach (var project in solution.GetAllProjects ()) {
						work += project.Files.Count (f => f.BuildAction == BuildAction.Compile);
					}
					monitor.BeginTask ("Analyzing solution", work);
					TypeSystemParser parser = null;
					string lastMime = null;
					CodeIssueProvider[] codeIssueProvider = null;
					foreach (var project in solution.GetAllProjects ()) {
						if (tokenSource.IsCancellationRequested)
							break;
						var compilation = TypeSystemService.GetCompilation (project);
						Parallel.ForEach (project.Files, file => {
							if (file.BuildAction != BuildAction.Compile || tokenSource.IsCancellationRequested)
								return;

							var editor = TextFileProvider.Instance.GetReadOnlyTextEditorData (file.FilePath);

							if (lastMime != editor.MimeType || parser == null)
								parser = TypeSystemService.GetParser (editor.MimeType);
							if (parser == null)
								return;
							var reader = new StreamReader (editor.OpenStream ());
							var document = parser.Parse (true, editor.FileName, reader, project);
							reader.Close ();
							if (document == null) 
								return;

							var resolver = new CSharpAstResolver (compilation, document.GetAst<SyntaxTree> (), document.ParsedFile as CSharpUnresolvedFile);
							var context = document.CreateRefactoringContextWithEditor (editor, resolver, tokenSource.Token);

							if (lastMime != editor.MimeType || codeIssueProvider == null)
								codeIssueProvider = RefactoringService.GetInspectors (editor.MimeType).ToArray ();
							Parallel.ForEach (codeIssueProvider, (provider) => { 
								var severity = provider.GetSeverity ();
								if (severity == Severity.None || tokenSource.IsCancellationRequested)
									return;
								try {
									foreach (var r in provider.GetIssues (context, tokenSource.Token)) {
										var issue = new IssueSummary {
											IssueDescription = r.Description,
											Region = r.Region,
											ProviderTitle = provider.Title,
											ProviderDescription = provider.Description,
											ProviderCategory = provider.Category,
											Severity = provider.GetSeverity (),
											IssueMarker = provider.IssueMarker,
											File = file,
											Project = project
										};
										rootGroup.Push (issue);
									}
								} catch (OperationCanceledException)  {
									// The operation was cancelled, no-op as the user-visible parts are
									// handled elsewhere
								} catch (Exception ex) {
									LoggingService.LogError ("Error while running code issue on:"+ editor.FileName, ex);
								}
							});
							lastMime = editor.MimeType;
							monitor.Step (1);
						});
					}
					Application.Invoke (delegate {
						var status = string.Format ("Found issues: {0}{1}",
							rootGroup.IssueCount,
							tokenSource.IsCancellationRequested ? " (Cancelled)" : string.Empty);
						rootNode.SetValue (textField, status);
						runButton.Sensitive = true;
						cancelButton.Sensitive = false;
					});
					redrawTimer.Dispose ();
					monitor.EndTask ();
				}
			});
		}

		void StopAnalyzation (object sender, EventArgs e)
		{
			cancelButton.Sensitive = false;
			tokenSource.Cancel ();
		}

		void QueueUiUpdate ()
		{
			Application.Invoke (delegate {
				UpdateUi ();
			});
		}

		void UpdateUi ()
		{
			var navigator = store.GetFirstNode ();
			
			UpdateGroups (rootGroup.Groups, navigator);
			UpdateIssues (rootGroup.Issues, navigator);
		}
		
		void UpdateGroup (IssueGroup group, TreeNavigator navigator, bool forceExpansion = false)
		{
			UpdateText (navigator, group);
			bool isExpanded = forceExpansion || view.IsRowExpanded (navigator.CurrentPosition);
			if (navigator.MoveToChild ()) {
				if (isExpanded) {
					UpdateGroups (group.Groups, navigator);
					UpdateIssues (group.Issues, navigator);
				}
				navigator.MoveToParent ();
			} else if (group.HasChildren) {
				AddDummyChild (navigator);
			}
		}

		void UpdateGroups (IList<IssueGroup> groups, TreeNavigator navigator)
		{
			var unprocessedGroups = groups;
			var existingGroups = new HashSet<IssueGroup> (unprocessedGroups);
			
			// Begin by updating any existing nodes
			do {
				if (navigator.GetValue (summaryField) != null)
					break;
				var group = navigator.GetValue (groupField);
				if (group == null)
					continue;
				if (!existingGroups.Contains (group)) {
					store.GetNavigatorAt (navigator.CurrentPosition).Remove ();
				} else {
					unprocessedGroups.Remove (group);
					UpdateGroup (group, navigator);
				}
			} while (navigator.MoveNext());
			
			// Add any new groups to the tree
			foreach (var group in unprocessedGroups) {
				navigator.InsertAfter ();
				navigator.SetValue (groupField, group);
				UpdateText (navigator, group);
				if (group.HasChildren) {
					AddDummyChild (navigator);
				}
				var position = navigator.CurrentPosition;
				group.ChildrenInvalidated += GetChildrenInvalidatedHandler (position);
			}
		}

		void UpdateText (TreeNavigator navigator, IssueGroup group)
		{
			navigator.SetValue (textField, string.Format ("{0} ({1} issues)", group.Description, group.IssueCount));
		}

		void UpdateText (TreeNavigator navigator, IssueSummary issue)
		{
			var region = issue.Region;
			string lineDescription;
			if (region.BeginLine == region.EndLine) {
				lineDescription = region.BeginLine.ToString ();
			} else {
				lineDescription = string.Format ("{0}-{1}", region.BeginLine, region.EndLine);
			}
			var fileName = Path.GetFileName (issue.File.Name);
			var title = string.Format ("{0} [{1}:{2}]", issue.IssueDescription, fileName, lineDescription);
			navigator.SetValue (textField, title);
		}

		void AddDummyChild (TreeNavigator navigator)
		{
			navigator.AddChild ();
			navigator.SetValue (textField, "Loading...");
			navigator.MoveToParent ();
		}

		void UpdateIssues (IList<IssueSummary> issues, TreeNavigator navigator)
		{
			var unprocessedIssues = issues;
			var existingIssues = new HashSet<IssueSummary> (unprocessedIssues);
			do {
				var issue = navigator.GetValue (summaryField);
				if (issue == null) 
					continue;
				if (!existingIssues.Contains (issue)) {
					navigator.Remove ();
				} else {
					unprocessedIssues.Remove (issue);
					UpdateText (navigator, issue);
				}
			} while (navigator.MoveNext());
			
			foreach (var issue in unprocessedIssues) {
				navigator.InsertAfter ();
				navigator.SetValue (summaryField, issue);
				UpdateText (navigator, issue);
			}
		}

		EventHandler<IssueGroupEventArgs> GetChildrenInvalidatedHandler (TreePosition position)
		{
			return (sender, eventArgs) => {
				Application.Invoke(delegate {
					var expanded = view.IsRowExpanded (position);
					var newNavigator = store.GetNavigatorAt (position);
					newNavigator.RemoveChildren ();
					UpdateGroup (eventArgs.IssueGroup, newNavigator, expanded);
					if (expanded) {
						view.ExpandRow (position, false);
					}
				});
			};
		}
		
		void OnRowActivated (object sender, TreeViewRowEventArgs e)
		{
			var navigator = store.GetNavigatorAt (e.Position);
			var issueSummary = navigator.GetValue (summaryField);
			if (issueSummary != null) {
				var region = issueSummary.Region;
				IdeApp.Workbench.OpenDocument (region.FileName, region.BeginLine, region.BeginColumn);
			} else {
				var issueGroup = navigator.GetValue (groupField);
				if (issueGroup != null) {
					var position = issueGroup.Position;
					if (!view.IsRowExpanded (position)) {
						view.ExpandRow (position, false);
					} else {
						view.CollapseRow (position);
					}
				}
			}
		}

		void OnRowExpanding (object sender, TreeViewRowEventArgs e)
		{
			var navigator = store.GetNavigatorAt (e.Position);
			var group = navigator.GetValue (groupField);
			if (group == null)
				return;
			bool hasDummyChild = false;
			if (navigator.MoveToChild ()) {
				var issueGroup = navigator.GetValue (groupField);
				var issueSummary = navigator.GetValue (summaryField);
				hasDummyChild = issueGroup == null && issueSummary == null;
				navigator.MoveToParent ();
			}
			
			UpdateGroup (group, navigator, true);
					
			if (hasDummyChild) {
				navigator.MoveToChild ();
				navigator.Remove ();
			}
		}
	}

	public class CodeIssuePad : AbstractPadContent
	{
		CodeIssuePadControl issueControl;

		public override Gtk.Widget Control {
			get {
				if (issueControl == null)
					issueControl = new CodeIssuePadControl ();
				return (Gtk.Widget)Xwt.Toolkit.CurrentEngine.GetNativeWidget (issueControl);
			}
		}
	}
}

