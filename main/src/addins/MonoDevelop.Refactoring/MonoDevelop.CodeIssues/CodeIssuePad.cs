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
using System.Threading;
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using System.IO;
using System.Diagnostics;

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
		CodeAnalysisBatchRunner runner = new CodeAnalysisBatchRunner();
		
		IssueGroup rootGroup;
		TreeStore store;

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
					SyncStateToUi (runner.State);
					UpdateUi ();
				});
			};
			
			runner.DestinationGroup = rootGroup;
			runner.AnalysisStateChanged += HandleAnalysisStateChanged;
		}

		void HandleAnalysisStateChanged (object sender, AnalysisStateChangeEventArgs e)
		{
			SyncStateToUi(e.NewState);
			if (e.NewState == AnalysisState.Running) {
				Debug.Assert (redrawTimer == null);
				redrawTimer = new Timer (arg => QueueUiUpdate (), null, 500, 500);
			} else if (e.NewState == AnalysisState.Completed || e.NewState == AnalysisState.Cancelled) {
				redrawTimer.Dispose ();
				redrawTimer = null;
			}
		}

		void SyncStateToUi (AnalysisState state)
		{
			Application.Invoke (delegate {
				// Update the top row
				string text;
				switch (state) {
				case AnalysisState.Running:
					text = "Running...";
					break;
				case AnalysisState.Cancelled:
					text = string.Format ("Found issues: {0} (Cancelled)", rootGroup.IssueCount);
					break;
				case AnalysisState.Completed:
					text = string.Format ("Found issues: {0}", rootGroup.IssueCount);
					break;
				}
				if (text != null) {
					var topRow = store.GetFirstNode ();
					// Weird way to check if the store was empty during the call above.
					// Might not be portable...
					if (topRow.CurrentPosition == null) {
						topRow = store.AddNode ();
					}
					topRow.SetValue (textField, text);
				}
				
				// Set button sensitivity
				bool running = state == AnalysisState.Running;
				runButton.Sensitive = !running;
				cancelButton.Sensitive = running;
			});
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
			// Force the ui to update, so that the ui update timer does not race with the creation of the top row
			// TODO: This has turned very ugly since factoring out CodeAnalysisBatchRunner. 
			SyncStateToUi (AnalysisState.Running);
			
			runner.StartAnalysis (solution);
		}

		void StopAnalyzation (object sender, EventArgs e)
		{
			runner.Stop ();
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

