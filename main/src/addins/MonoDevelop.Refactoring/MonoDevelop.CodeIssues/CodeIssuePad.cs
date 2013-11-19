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
using System.Collections.Generic;
using ICSharpCode.NRefactory.TypeSystem;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using MonoDevelop.Refactoring;
using Xwt.Drawing;
using IconSize = Gtk.IconSize;

namespace MonoDevelop.CodeIssues
{
	public class CodeIssuePadControl : VBox
	{
		const int UpdatePeriod = 500;
		const int BatchChoiceCount = 5;

		readonly TreeView view = new TreeView ();
		readonly DataField<string> textField = new DataField<string> ();
		readonly DataField<IIssueTreeNode> nodeField = new DataField<IIssueTreeNode> ();
		readonly Button runButton = new Button ("Run");
		readonly Button cancelButton = new Button ("Cancel");

		readonly IssueGroup rootGroup;

		readonly TreeStore store;
		
		readonly ISet<IIssueTreeNode> syncedNodes = new HashSet<IIssueTreeNode> ();
		readonly Dictionary<IIssueTreeNode, TreePosition> nodePositions = new Dictionary<IIssueTreeNode, TreePosition> ();

		bool runPeriodicUpdate;
		readonly object queueLock = new object ();
		readonly Queue<IIssueTreeNode> updateQueue = new Queue<IIssueTreeNode> ();

		IJobContext currentJobContext;

		public IJobContext CurrentJobContext {
			get {
				return currentJobContext;
			}
			set {
				currentJobContext = value;
				bool working = currentJobContext != null;
				runButton.Sensitive = !working;
				cancelButton.Sensitive = working;
			}
		}

		static readonly Type[] groupingProviders = {
			typeof(CategoryGroupingProvider),
			typeof(ProviderGroupingProvider),
			typeof(SeverityGroupingProvider),
			typeof(ProjectGroupingProvider),
			typeof(FileGroupingProvider)
		};

		public CodeIssuePadControl ()
		{
			var buttonRow = new HBox();
			runButton.Image = GetStockImage(Gtk.Stock.Execute);
			runButton.Clicked += StartAnalyzation;
			buttonRow.PackStart (runButton);

			cancelButton.Image = GetStockImage(Gtk.Stock.Stop);
			cancelButton.Clicked += StopAnalyzation;
			cancelButton.Sensitive = false;
			buttonRow.PackStart (cancelButton);
			var groupingProvider = new CategoryGroupingProvider {
				Next = new ProviderGroupingProvider()
			};
			rootGroup = new IssueGroup (groupingProvider, "root group");
			var groupingProviderControl = new GroupingProviderChainControl (rootGroup, groupingProviders);
			buttonRow.PackStart (groupingProviderControl);
			
			PackStart (buttonRow);

			store = new TreeStore (textField, nodeField);
			view.DataSource = store;
			view.HeadersVisible = false;
			view.Columns.Add ("Name", textField);
			view.SelectionMode = SelectionMode.Multiple;

			view.RowActivated += OnRowActivated;
			view.RowExpanding += OnRowExpanding;
			view.ButtonPressed += HandleButtonPressed;
			view.ButtonReleased += HandleButtonReleased;
			PackStart (view, true);
			
			IIssueTreeNode node = rootGroup;
			node.ChildrenInvalidated += (sender, group) => {
				Application.Invoke (delegate {
					ClearSiblingNodes (store.GetFirstNode ());
					store.Clear ();
					foreach(var child in ((IIssueTreeNode)rootGroup).Children) {
						var navigator = store.AddNode ();
						SetNode (navigator, child);
						SyncNode (navigator);
					}
				});
			};
			node.ChildAdded += HandleRootChildAdded;

			IdeApp.Workspace.LastWorkspaceItemClosed += HandleLastWorkspaceItemClosed;
		}

	    private static Image GetStockImage (string name)
	    {
            // HACK: Assume we are running with the GTK backend, which supports the pixbuf type
	        return Toolkit.CurrentEngine.WrapImage (ImageService.GetPixbuf (name, IconSize.SmallToolbar));
	    }

	    void HandleLastWorkspaceItemClosed (object sender, EventArgs e)
		{
			ClearState ();
		}
		
		void ClearState ()
		{
			store.Clear ();
			rootGroup.ClearStatistics ();
			rootGroup.EnableProcessing ();
			
			syncedNodes.Clear ();
			nodePositions.Clear ();
			lock (queueLock) {
				updateQueue.Clear ();
			}
		}

		void StartPeriodicUpdate ()
		{
			Debug.Assert (!runPeriodicUpdate);
			runPeriodicUpdate = true;
			Application.TimeoutInvoke (UpdatePeriod, RunPeriodicUpdate);
		}

		void ProcessUpdateQueue ()
		{
			IList<IIssueTreeNode> nodes;
			lock (queueLock) {
				nodes = new List<IIssueTreeNode> (updateQueue);
				updateQueue.Clear ();
			}
			foreach (var node in nodes) {
				TreePosition position;
				if (!nodePositions.TryGetValue (node, out position)) {
					// This might be an event for a group that has been invalidated and removed
					continue;
				}
				var navigator = store.GetNavigatorAt (position);
				if (!node.Visible) {
					// Check above means node is always in nodePositions
					nodePositions.Remove (node);
					if (syncedNodes.Contains (node)) {
						syncedNodes.Remove (node);
					}
					ClearChildNodes (navigator);
					navigator.Remove ();
					continue;
				}
				UpdateText (navigator, node);
				if (!syncedNodes.Contains (node) && node.HasVisibleChildren) {
					if (navigator.MoveToChild ()) {
						navigator.MoveToParent ();
					}
					else {
						AddDummyChild (navigator);
					}
				}
			}
		}
		
		bool RunPeriodicUpdate ()
		{
			ProcessUpdateQueue ();
			return runPeriodicUpdate;
		}

		void EndPeriodicUpdate ()
		{
			Debug.Assert (runPeriodicUpdate);
			runPeriodicUpdate = false;
		}

		void HandleRootChildAdded (object sender, IssueTreeNodeEventArgs e)
		{
			Application.Invoke (delegate {
				Debug.Assert (e.Parent == rootGroup);
				var navigator = store.AddNode ();
				SetNode (navigator, e.Child);
				SyncNode (navigator);
			});
		}
		
		void StartAnalyzation (object sender, EventArgs e)
		{
			var solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (solution == null)
				return;
				
			ClearState ();

			var job = new SolutionAnalysisJob (solution);
			job.CodeIssueAdded += HandleCodeIssueAdded;
			job.Completed += delegate {
				CurrentJobContext = null;
			};
			CurrentJobContext = RefactoringService.QueueCodeIssueAnalysis (job, "Analyzing solution");
			StartPeriodicUpdate ();
		}

		void HandleCodeIssueAdded (object sender, CodeIssueEventArgs e)
		{
			foreach (var issue in e.CodeIssues) {
				var summary = IssueSummary.FromCodeIssue (e.File, e.Provider, issue);
				rootGroup.AddIssue (summary);
			}
		}

		void StopAnalyzation (object sender, EventArgs e)
		{
			if (CurrentJobContext != null) {
				CurrentJobContext.CancelJob ();
				CurrentJobContext = null;
			}
			EndPeriodicUpdate ();
		}

		void SetNode (TreeNavigator navigator, IIssueTreeNode node)
		{
			if (navigator == null)
				throw new ArgumentNullException ("navigator");
			if (node == null)
				throw new ArgumentNullException ("node");
			
			navigator.SetValue (nodeField, node);
			Debug.Assert (!nodePositions.ContainsKey (node));
			var position = navigator.CurrentPosition;
			nodePositions.Add (node, position);
			
			node.ChildAdded += (sender, e) => {
				Debug.Assert (e.Parent == node);
				Application.Invoke (delegate {
					var newNavigator = store.GetNavigatorAt (position);
					newNavigator.AddChild ();
					SetNode (newNavigator, e.Child);
					SyncNode (newNavigator);
				});
			};
			node.ChildrenInvalidated += (sender, e) => {
				Application.Invoke (delegate {
					SyncNode (store.GetNavigatorAt (position));
				});
			};
			node.TextChanged += (sender, e) => {
				lock (queueLock) {
					if (!updateQueue.Contains (e.Node)) {
						updateQueue.Enqueue (e.Node);
					}
				}
			};
			node.VisibleChanged += (sender, e) => {
				lock (queueLock) {
					if (!updateQueue.Contains (e.Node)) {
						updateQueue.Enqueue (e.Node);
					}
				}
			};
		}

		void ClearSiblingNodes (TreeNavigator navigator)
		{
			if (navigator.CurrentPosition == null)
				return;

			do {
				var node = navigator.GetValue (nodeField);
				if (node != null) {
					if (syncedNodes.Contains (node)) {
						syncedNodes.Remove (node);
					}
					if (nodePositions.ContainsKey (node)) {
						nodePositions.Remove (node);
					}
				}
				ClearChildNodes (navigator);
			} while (navigator.MoveNext ());
		}
		
		void ClearChildNodes (TreeNavigator navigator)
		{
			if (navigator.MoveToChild ()) {
				ClearSiblingNodes (navigator);
				navigator.MoveToParent ();
			}
		}
		
		void SyncNode (TreeNavigator navigator, bool forceExpansion = false)
		{
			var node = navigator.GetValue (nodeField);
			UpdateText (navigator, node);
			bool isExpanded = forceExpansion || view.IsRowExpanded (navigator.CurrentPosition);
			ClearChildNodes (navigator);
			syncedNodes.Remove (node);
			navigator.RemoveChildren ();
			if (!node.HasVisibleChildren) 
				return;
			if (isExpanded) {
				foreach (var childNode in node.Children.Where (child => child.Visible)) {
					navigator.AddChild ();
					SetNode (navigator, childNode);
					SyncNode (navigator);
					navigator.MoveToParent ();
				}
			} else {
				AddDummyChild (navigator);
			}
			
			if (isExpanded) {
				syncedNodes.Add (node);
				view.ExpandRow (navigator.CurrentPosition, false);
			}
			
		}

		void UpdateText (TreeNavigator navigator, IIssueTreeNode node)
		{
			navigator.SetValue (textField, node.Text);
		}

		void AddDummyChild (TreeNavigator navigator)
		{
			navigator.AddChild ();
			navigator.SetValue (textField, "Loading...");
			navigator.MoveToParent ();
		}

		EventHandler<IssueGroupEventArgs> GetChildrenInvalidatedHandler (TreePosition position)
		{
			return (sender, eventArgs) => {
				Application.Invoke(delegate {
					var expanded = view.IsRowExpanded (position);
					var newNavigator = store.GetNavigatorAt (position);
					newNavigator.RemoveChildren ();
					SyncNode (newNavigator, expanded);
					if (expanded) {
						view.ExpandRow (position, false);
					}
				});
			};
		}
		
		void OnRowActivated (object sender, TreeViewRowEventArgs e)
		{
			var position = e.Position;
			var node = store.GetNavigatorAt (position).GetValue (nodeField);
			
			var issueSummary = node as IssueSummary;
			if (issueSummary != null) {
				var region = issueSummary.Region;
				IdeApp.Workbench.OpenDocument (region.FileName, region.BeginLine, region.BeginColumn);
			} else {
				if (!view.IsRowExpanded (position)) {
					view.ExpandRow (position, false);
				} else {
					view.CollapseRow (position);
				}
			}
		}

		void OnRowExpanding (object sender, TreeViewRowEventArgs e)
		{
			var navigator = store.GetNavigatorAt (e.Position);
			var node = navigator.GetValue (nodeField);
			if (!syncedNodes.Contains (node)) {
				SyncNode (navigator, true);
			}
		}

		#region Button event handlers
		// Event handling of right click on the TreeView is split in two parts
		// This is because no single handler can support intuitive behavior regarding
		// what happens to the selection when the right mouse button is pressed:
		// if only a single row is selected: change the selection and then show menu
		// if multiple rows are selected: show the menu directly

		void HandleButtonReleased (object sender, ButtonEventArgs e)
		{
			if (e.Button != PointerButton.Right || handledByPress)
				return;

			var rows = view.SelectedRows;
			if (rows.Length <= 1) {
				// Single row or no row
				ShowBatchFixContextMenu (e.X, e.Y, view.SelectedRows);
			}
		}

		bool handledByPress;

		void HandleButtonPressed (object sender, ButtonEventArgs e)
		{
			if (e.Button != PointerButton.Right)
				return;

			var rows = view.SelectedRows;
			if (rows.Length > 1) {
				// this is a multiple selection
				// waiting in this case means the selection disappears
				ShowBatchFixContextMenu (e.X, e.Y, rows);

				// Don't let the selection be reset
				e.Handled = true;
				handledByPress = true;
			} else {
				handledByPress = false;
			}
		}

		#endregion

		void UpdateParents (TreeNavigator navigator)
		{
			do {
				var node = navigator.GetValue (nodeField);
				UpdateText (navigator, node);
			} while (navigator.MoveToParent ());
		}

		void ShowBatchFixContextMenu (double x, double y, IEnumerable<TreePosition> rows)
		{
			var possibleFixes = rows
				.Select (row => store.GetNavigatorAt (row).GetValue (nodeField))
				.Where (node1 => node1 != null)
				.SelectMany (node2 => node2.AllChildren.Union (new [] { node2 }))
				.Where (node3 => node3.Visible)
				.OfType<IssueSummary> ()
				.Where (issue => issue.Actions.Any (a => a.Batchable))
				.Distinct()
				.GroupBy(issue => issue.InspectorIdString)
				.OrderBy (group => -group.Count ());
				
			var groups = possibleFixes.Take (BatchChoiceCount).ToList ();
			if (!groups.Any ())
				return;

			if (groups.Count == 1) {
				CreateIssueMenu (groups.First ()).Popup (view, x, y);
			} else {
				var menu = new Menu ();
				foreach (var g in groups) {
					var menuItem = new MenuItem (g.First ().ProviderTitle);
					menuItem.SubMenu = CreateIssueMenu (g);
					menu.Items.Add (menuItem);
				}
				menu.Popup (view, x, y);
			}
		}

		Menu CreateIssueMenu (IEnumerable<IssueSummary> issues)
		{
			var allIssues = issues as IList<IssueSummary> ?? issues.ToList ();
			var issueMenu = new Menu ();
			
			var actionGroups = allIssues
				.SelectMany (issue => issue.Actions)
				.GroupBy (action => action.SiblingKey);
			foreach (var _actionGroup in actionGroups) {
				var actionGroup = _actionGroup;
				
				var actionMenuItem = new MenuItem (actionGroup.First ().Title);
				actionMenuItem.Clicked += delegate {
					ThreadPool.QueueUserWorkItem (delegate {
						try {
							using (var monitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor ("Applying fixes", null, false)) {
								var fixer = new BatchFixer (new ExactIssueMatcher (), monitor);
								var appliedActions = fixer.TryFixIssues (actionGroup);
								foreach (var action in appliedActions) {
									((IIssueTreeNode)action.IssueSummary).Visible = false;
								}
							}
							Application.Invoke (delegate {
								ProcessUpdateQueue ();
							});
						} catch (Exception e) {
							MessageService.ShowException (e);
						}
					});
				};
				issueMenu.Items.Add (actionMenuItem);
			}
			return issueMenu;
		}
		
	}

	public class CodeIssuePad : AbstractPadContent
	{
		CodeIssuePadControl issueControl;

		public override Gtk.Widget Control {
			get {
				if (issueControl == null)
					issueControl = new CodeIssuePadControl ();
				return (Gtk.Widget)Toolkit.CurrentEngine.GetNativeWidget (issueControl);
			}
		}
	}
}

