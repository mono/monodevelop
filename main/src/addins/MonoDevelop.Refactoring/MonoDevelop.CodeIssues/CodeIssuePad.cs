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
		
		IssueGroup rootGroup;
		TreeStore store;
		CancellationTokenSource tokenSource;

		public CodeIssuePadControl ()
		{
			runButton.Clicked += StartAnalyzation;
			buttonRow.PackStart (runButton);
			
			cancelButton.Clicked += StopAnalyzation;
			cancelButton.Sensitive = false;
			buttonRow.PackStart (cancelButton);
			
			var groupingProvider = new CategoryGroupingProvider ();
			groupingProviderControl = new GroupingProviderChainControl (groupingProvider, new [] { typeof(CategoryGroupingProvider) });
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
			rootGroup.ChildGroupAdded += GetGroupAddedHandler (rootGroup);
			rootGroup.IssueSummaryAdded += GetIssueSummaryAddedHandler (rootGroup);
			rootGroup.ChildrenInvalidated += (obj) => {
				store.Clear ();
				InsertTopRow ();
				rootGroup.EnableProcessing ();
			};
			rootGroup.EnableProcessing ();
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
											ProviderCategory = provider.Category
										};
										rootGroup.Push (issue);
									}
								} catch (OperationCanceledException)  {
									// The operation was cancelled, no-op as the user-visible parts are
									// handled elsewhere
								} catch (Exception ex) {
									System.Console.WriteLine (ex.ToString());
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
					monitor.EndTask ();
				}
			});
		}

		void StopAnalyzation (object sender, EventArgs e)
		{
			cancelButton.Sensitive = false;
			tokenSource.Cancel ();
		}
		
		Action<IssueGroup> GetGroupAddedHandler (IssueGroup parentGroup) 
		{
			return group => {
				Application.Invoke (delegate {
					var navigator = GetNavigatorForGroup (parentGroup);
					
					group.Position = navigator.CurrentPosition;
					group.ChildGroupAdded += GetGroupAddedHandler(group);
					group.IssueSummaryAdded += GetIssueSummaryAddedHandler(group);
					group.ChildrenInvalidated += GetChildrenInvalidatedHandler (navigator);
					
					navigator.SetValue (groupField, group);
					UpdateParents (navigator);
				});
			};
		}

		Action<IssueSummary> GetIssueSummaryAddedHandler (IssueGroup parentGroup)
		{
			return issue => {
				Application.Invoke (delegate {
					TreeNavigator navigator = GetNavigatorForGroup (parentGroup);
					navigator.SetValue (summaryField, issue);
					
					UpdateParents (navigator);
				});
			};
		}

		Action<IssueGroup> GetChildrenInvalidatedHandler (TreeNavigator navigator)
		{
			return group => {
				navigator.RemoveChildren ();
				UpdateParents (navigator);
			};
		}

		TreeNavigator GetNavigatorForGroup (IssueGroup parentGroup)
		{
			var position = parentGroup.Position;
			TreeNavigator navigator;
			if (position == null) {
				navigator = store.AddNode ();
			} else {
				navigator = store.GetNavigatorAt (position).AddChild ();
			}
			return navigator;
		}

		void UpdateParents (TreeNavigator navigator)
		{
			do {
				var group = navigator.GetValue (groupField);
				if (group != null) {
					navigator.SetValue (textField, string.Format ("{0} ({1} issues)", group.Description, group.IssueCount));
					if (group.IssueCount > 0) {
						// Add a fake child to show the expander button
						if (!navigator.MoveToChild ()) {
							navigator.AddChild ();
							navigator.SetValue(textField, "Loading...");
							navigator.MoveToParent();
						} else {
							navigator.MoveToParent ();
						}
					}
				} else {
					var issue = navigator.GetValue (summaryField);
					if (issue != null) {
						navigator.SetValue (textField, string.Format ("{0}: {1}", issue.Severity, issue.IssueDescription));
					}
				}
			} while (navigator.MoveToParent());
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
			var row = store.GetNavigatorAt(e.Position);
			var group = row.GetValue (groupField);
			if (group == null || group.ProcessingEnabled)
				return;
			ThreadPool.QueueUserWorkItem(delegate {
				group.EnableProcessing ();
				Application.Invoke(delegate {
					if (row.MoveToChild() && row.GetValue(groupField) == null && row.GetValue(summaryField) == null) {
						// there is a dummy child present
						row.Remove ();
					}
				});
			});
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

