//
// BuildOuputWidget.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2017 Microsoft Corp. (http://microsoft.com)
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
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Xwt;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Components.Extensions;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutputWidget : VBox, IPathedDocument, IBuildOutputContextProvider
	{
		const string binLogExtension = "binlog";

		TreeView treeView;
		CheckBox showDiagnosticsButton;
		Button saveButton;
		SearchEntry searchEntry;
		Gtk.VBox box;
		PathBar pathBar;
		Button buttonSearchBackward;
		Button buttonSearchForward;
		Label resultInformLabel;
		BuildOutputDataSearch currentSearch;
		BuildOutputTreeCellView cellView;

		public string ViewContentName { get; private set; }
		public BuildOutput BuildOutput { get; private set; }
		public PathEntry [] CurrentPath { get; set; }

		bool isDirty;
		public bool IsDirty {
			get => isDirty;
			private set {
				if (isDirty == value)
					return;
				isDirty = value;
				saveButton.Sensitive = value;
			}
		}

		public event EventHandler<FilePath> FileSaved;
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;

		public BuildOutputWidget (BuildOutput output, string viewContentName, DocumentToolbar toolbar)
		{
			Initialize (toolbar);
			ViewContentName = viewContentName;
			SetupBuildOutput (output);
			filePathLocation = FilePath.Empty;
		}

		public BuildOutputWidget (FilePath filePath, DocumentToolbar toolbar)
		{
			Initialize (toolbar);

			ViewContentName = filePath;
			var output = new BuildOutput ();
			filePathLocation = filePath;
			output.Load (filePath.FullPath, false);
			SetupBuildOutput (output);
			IsDirty = false;
		}

		void SetupBuildOutput (BuildOutput output)
		{
			BuildOutput = output;

			BuildOutput.OutputChanged += (sender, e) => ProcessLogs (showDiagnosticsButton.Active);
			ProcessLogs (false);

			pathBar = new PathBar (this.CreatePathWidget) {
				DrawBottomBorder = false
			};
			var entries = new PathEntry [] {
				new PathEntry (GettextCatalog.GetString ("No selection"))
			};
			UpdatePathBarEntries (entries);
			pathBar.Show ();

			box.PackStart (pathBar, true, true, 10);
			box.ReorderChild (pathBar, 0);
			box.Show ();
		}

		void Initialize (DocumentToolbar toolbar)
		{
			Spacing = 0;

			// FIXME: DocumentToolbar does not support native widgets
			// Toolbar items must use Gtk, for now
			Xwt.Toolkit.Load (ToolkitType.Gtk).Invoke (() => {
				showDiagnosticsButton = new CheckBox (GettextCatalog.GetString ("Diagnostic log verbosity"));
				showDiagnosticsButton.Accessible.Identifier = "BuildOutputWidget.ShowDiagnosticsButton";
				showDiagnosticsButton.TooltipText = GettextCatalog.GetString ("Show full (diagnostics enabled) or reduced log");
				showDiagnosticsButton.Accessible.Description = GettextCatalog.GetString ("Diagnostic log verbosity");
				showDiagnosticsButton.Clicked += (sender, e) => ProcessLogs (showDiagnosticsButton.Active);

				saveButton = new Button (GettextCatalog.GetString ("Save"));
				saveButton.Accessible.Identifier = "BuildOutputWidget.SaveButton";
				saveButton.TooltipText = GettextCatalog.GetString ("Save build output");
				saveButton.Accessible.Description = GettextCatalog.GetString ("Save build output");

				saveButton.Clicked += SaveButtonClickedAsync;

				searchEntry = new SearchEntry ();
				searchEntry.Accessible.SetLabel (GettextCatalog.GetString ("Search"));
				searchEntry.Accessible.Name = "BuildOutputWidget.Search";
				searchEntry.Accessible.Description = GettextCatalog.GetString ("Search the build log");
				searchEntry.WidthRequest = 200;
				searchEntry.Visible = true;
				searchEntry.EmptyMessage = GettextCatalog.GetString ("Search Build Output");

				resultInformLabel = new Label ();
				searchEntry.AddLabelWidget ((Gtk.Label)resultInformLabel.ToGtkWidget ());

				searchEntry.Entry.Changed += FindFirst;
				searchEntry.Entry.Activated += FindNext;

				buttonSearchBackward = new Button ();
				buttonSearchForward = new Button ();
				buttonSearchBackward.Clicked += FindPrevious;
				buttonSearchForward.Clicked += FindNext;
				buttonSearchForward.TooltipText = GettextCatalog.GetString ("Find next {0}", GetShortcut (SearchCommands.FindNext, true));
				buttonSearchBackward.TooltipText = GettextCatalog.GetString ("Find previous {0}", GetShortcut (SearchCommands.FindPrevious, true));
				buttonSearchBackward.Image = ImageService.GetIcon ("gtk-go-up", Gtk.IconSize.Menu);
				buttonSearchForward.Image = ImageService.GetIcon ("gtk-go-down", Gtk.IconSize.Menu);
			});

			box = new Gtk.VBox ();
			box.Spacing = 0;
			toolbar.Add (box, true);

			toolbar.AddSpace ();
			toolbar.Add (showDiagnosticsButton.ToGtkWidget ());
			toolbar.Add (saveButton.ToGtkWidget ());
			toolbar.AddSpace ();
			toolbar.Add (searchEntry, false);
			toolbar.Add (buttonSearchBackward.ToGtkWidget ());
			toolbar.Add (buttonSearchForward.ToGtkWidget ());

			treeView = new TreeView ();
			treeView.HeadersVisible = false;
			treeView.BorderVisible = false;
			treeView.UseAlternatingRowColors = true;
			treeView.Accessible.Identifier = "BuildOutputWidget.TreeView";
			treeView.Accessible.Description = GettextCatalog.GetString ("Structured build output");
			treeView.HorizontalScrollPolicy = ScrollPolicy.Never;
			treeView.SelectionChanged += TreeView_SelectionChanged;
			treeView.ButtonPressed += TreeView_ButtonPressed;


			treeView.SelectionMode = Xwt.SelectionMode.Single;
			var treeColumn = new ListViewColumn {
				CanResize = false,
				Expands = true
			};
			cellView = new BuildOutputTreeCellView (this);
			treeColumn.Views.Add (cellView, true);
			treeView.Columns.Add (treeColumn);

			PackStart (treeView, expand: true, fill: true);
		}

		internal Task GoToError (string description, string project)
		{
			return ExpandNode (project, BuildOutputNodeType.Error, description);
		}

		internal Task GoToWarning (string description, string project)
		{
			return ExpandNode (project, BuildOutputNodeType.Warning, description);
		}

		internal Task GoToMessage (string description, string project)
		{
			return ExpandNode (project, BuildOutputNodeType.Message, description);
		}

		async Task ExpandNode (string project, BuildOutputNodeType nodeType, string message) 
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null) {
				return;
			}

			await processingCompletion.Task;
			var projectNode = dataSource.RootNodes.SearchFirstNode (BuildOutputNodeType.Project, project);
			var node = projectNode.SearchFirstNode (nodeType, message);
			FocusRow (node);
		}

		void FocusRow (BuildOutputNode match)
		{
			treeView.ExpandToRow (match);
			treeView.FocusedRow = match;
		}

		async void SaveButtonClickedAsync (object sender, EventArgs e) => await SaveAs ();

		FilePath filePathLocation;
		public async Task SaveAs ()
		{
			var dlg = new Gui.Dialogs.OpenFileDialog (GettextCatalog.GetString ("Save as..."), MonoDevelop.Components.FileChooserAction.Save) {
				TransientFor = IdeApp.Workbench.RootWindow,
				InitialFileName = ViewContentName
			};
			if (dlg.Run ()) {
				var outputFile = dlg.SelectedFile;
				if (!outputFile.HasExtension (binLogExtension))
					outputFile = outputFile.ChangeExtension (binLogExtension);

				await BuildOutput.Save (outputFile);
				FileSaved?.Invoke (this, outputFile);
				filePathLocation = outputFile;
				IsDirty = false;
			}
		}

		bool IsSelectableTask (BuildOutputNode node)
		{
			return filePathLocation == FilePath.Empty && (node.NodeType == BuildOutputNodeType.Error || node.NodeType == BuildOutputNodeType.Warning);
		}

		void TreeView_SelectionChanged (object sender, EventArgs e)
		{
			var selectedNode = treeView.SelectedRow as BuildOutputNode;
			if (selectedNode == null)
				return;

			var stack = new Stack<BuildOutputNode> ();

			if (selectedNode.HasChildren)
				stack.Push (selectedNode);	
			var parent = selectedNode.Parent;

			while (parent != null) {
				stack.Push (parent);
				parent = parent.Parent;
			}

			var entries = new PathEntry [stack.Count];
			var index = 0;
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			while (stack.Count > 0) {
				var node = stack.Pop ();
				var pathEntry = new PathEntry (node.GetImage (), node.Message);
				pathEntry.Tag = node;
				entries [index] = pathEntry;
				index++;
			}

			UpdatePathBarEntries (entries);
		}

		void TreeView_ButtonPressed (object sender, ButtonEventArgs e)
		{
			var selectedNode = treeView.GetRowAtPosition (e.Position) as BuildOutputNode;
			if (selectedNode == null) {
				return;
			}
			if (e.MultiplePress == 2 && e.Button == PointerButton.Left) {
				if (treeView.IsRowExpanded (selectedNode)) {
					treeView.CollapseRow (selectedNode);
				} else {
					treeView.ExpandRow (selectedNode, false);
				}
				return;
			}

			if (e.Button == PointerButton.Right) {
				var menu = new ContextMenu ();

				ContextMenuItem jump = null;
				if (IsSelectableTask (selectedNode)) {
					jump = new ContextMenuItem (GettextCatalog.GetString ("_Jump to {0}", selectedNode.NodeType.ToString ()));
					jump.Clicked += (s,evnt) => {
						var path = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (selectedNode.Project), selectedNode.File);
						IdeApp.Workbench.OpenDocument (new FilePath (path), 
						                               null, 
						                               Math.Max (1, selectedNode.LineNumber),
						                               Math.Max (1, 0)
						                              );
					};
					menu.Add (jump);
					menu.Add (new SeparatorContextMenuItem ());
				}

				var expandAllMenu = new ContextMenuItem (GettextCatalog.GetString ("Expand All"));
				expandAllMenu.Clicked += (s, args) => treeView.ExpandAll ();
				menu.Items.Add (expandAllMenu);

				var collapseAllMenu = new ContextMenuItem (GettextCatalog.GetString ("Collapse All"));
				collapseAllMenu.Clicked += (s, args) => {
					var dataSource = (BuildOutputDataSource) treeView.DataSource;
					if (dataSource != null) {
						foreach (var root in dataSource.RootNodes) {
							treeView.CollapseRow (root);
						}
					}
				};
				menu.Items.Add (collapseAllMenu);

				menu.Add (new SeparatorContextMenuItem ());
				var copyElementMenu = new ContextMenuItem (GettextCatalog.GetString ("Copy\t\t\t{0}", GetShortcut (EditCommands.Copy, false)));
				copyElementMenu.Clicked += (s, args) => {
					if (cellView.SelectionStart != cellView.SelectionEnd) {
						var init = Math.Min (cellView.SelectionStart, cellView.SelectionEnd);
						var end = Math.Max (cellView.SelectionStart, cellView.SelectionEnd);
						Clipboard.SetText (selectedNode.Message.Substring (init, end - init));
					} else {
						ClipboardCopy (selectedNode);
					}
				};
				menu.Items.Add (copyElementMenu);

				menu.Show (treeView, (int) e.X, (int) e.Y);
			}
		}

		void UpdatePathBarEntries (PathEntry[] entries)
		{
			var previousPathEntry = CurrentPath;
			pathBar.SetPath (entries);
			CurrentPath = pathBar.Path;
			PathChanged?.Invoke (this, new DocumentPathChangedEventArgs (CurrentPath));
		}

		public bool CanClipboardCopy () => treeView.SelectedRow != null;

		public void ClipboardCopy ()
		{
			ClipboardCopy (treeView.SelectedRow as BuildOutputNode);
		}

		private void ClipboardCopy (BuildOutputNode selectedNode)
		{
			if (selectedNode != null) {
				Clipboard.SetText (selectedNode.ToString (true));
			}
		}

		async void FindFirst (object sender, EventArgs args)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null)
				return;

			currentSearch = new BuildOutputDataSearch (dataSource.RootNodes);
			Find (await currentSearch.FirstMatch (searchEntry.Entry.Text));
		}

		public void FindNext (object sender, EventArgs args)
		{
			if (currentSearch == null) {
				return;
			}

			Find (currentSearch.NextMatch ());

			if (currentSearch.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (
					Gtk.Stock.Find, GettextCatalog.GetString ("Reached top, continued from bottom"));
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
		}

		public void FindPrevious (object sender, EventArgs e)
		{
			if (currentSearch == null) {
				return;
			}

			Find (currentSearch.PreviousMatch ());

			if (currentSearch.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (
					Gtk.Stock.Find, GettextCatalog.GetString ("Reached bottom, continued from top"));
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
		}

		void Find (BuildOutputNode node)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null)
				return;

			if (node != null) {
				FocusRow (node);

				resultInformLabel.Text = GettextCatalog.GetString ("{0} of {1}", currentSearch.CurrentAbsoluteMatchIndex, currentSearch.MatchesCount);
				resultInformLabel.TextColor = searchEntry.Style.Foreground (Gtk.StateType.Insensitive).ToXwtColor();
			} else if (string.IsNullOrEmpty (searchEntry.Entry.Text)) {
				resultInformLabel.Text = string.Empty;
				IdeApp.Workbench.StatusBar.ShowReady ();
			} else {
				resultInformLabel.Text = GettextCatalog.GetString ("0 of 0");
				resultInformLabel.TextColor = Ide.Gui.Styles.Editor.SearchErrorForegroundColor;
			}
			resultInformLabel.Show ();

			buttonSearchForward.Sensitive = currentSearch?.MatchesCount > 0;
			buttonSearchBackward.Sensitive = currentSearch?.MatchesCount > 0; 
		}

		static string GetShortcut (object commandId, bool includeParen)
		{
			var key = IdeApp.CommandService.GetCommand (commandId).AccelKey;
			if (string.IsNullOrEmpty (key))
				return "";
			var nextShortcut = KeyBindingManager.BindingToDisplayLabel (key, false);
			return includeParen ? "(" + nextShortcut + ")" : nextShortcut;
		}

		static void ExpandChildrenWithErrors (TreeView tree, BuildOutputDataSource dataSource, BuildOutputNode parent)
		{
			int totalChildren = dataSource.GetChildrenCount (parent);
			for (int i = 0; i < totalChildren; i++) {
				var child = dataSource.GetChild (parent, i) as BuildOutputNode;
				if ((child?.HasErrors ?? false)) {
					tree.ExpandToRow (child);
					ExpandChildrenWithErrors (tree, dataSource, child);
				}
			}
		}

		CancellationTokenSource cts;
		TaskCompletionSource<object> processingCompletion = new TaskCompletionSource<object> ();

		void ProcessLogs (bool showDiagnostics)
		{
			cts?.Cancel ();
			cts = new CancellationTokenSource ();
			processingCompletion = new TaskCompletionSource<object> ();

			IsDirty = true;

			Task.Run (async () => {
				try {
					BuildOutput.ProcessProjects ();

					await InvokeAsync (() => {
						currentSearch = null;
						searchEntry.Entry.Text = String.Empty;
						Find (null);

						var buildOutputDataSource = new BuildOutputDataSource (BuildOutput.GetRootNodes (showDiagnostics));
						treeView.DataSource = buildOutputDataSource;

						(treeView.Columns [0].Views [0] as BuildOutputTreeCellView).BuildOutputNodeField = buildOutputDataSource.BuildOutputNodeField;

						// Expand root nodes and nodes with errors
						int rootsCount = buildOutputDataSource.GetChildrenCount (null);
						for (int i = 0; i < rootsCount; i++) {
							var root = buildOutputDataSource.GetChild (null, i) as BuildOutputNode;
							treeView.ExpandRow (root, false);
							ExpandChildrenWithErrors (treeView, buildOutputDataSource, root);
						}
						processingCompletion.TrySetResult (null);
					});
				} catch (Exception ex) {
					processingCompletion.TrySetException (ex);
				}
			}, cts.Token);
		}

		public bool IsSearchInProgress => currentSearch != null && currentSearch.MatchesCount > 0;

		#region IBuildOutputContextProvider

		public bool IsShowingDiagnostics => showDiagnosticsButton.Active;

		public string SearchString => searchEntry.Entry.Text;

		#endregion

		public void FocusOnSearchEntry ()
		{
			searchEntry.Entry.GrabFocus ();
		}

		int currentIndex = -1;
		public Control CreatePathWidget (int index)
		{
			if (currentIndex != index) {
				currentIndex = index;
			}

			PathEntry [] path = CurrentPath;
			if (path == null || index < 0 || index >= path.Length)
				return null;

			var tag = path [index].Tag as BuildOutputNode;
			var window = new DropDownBoxListWindow (new DropDownWindowDataProvider (this,  tag));
			window.FixedRowHeight = 22;
			window.MaxVisibleRows = 14;
			if (path [index].Tag != null)
				window.SelectItem (path [index].Tag);
			return window;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing) {
				buttonSearchBackward.Clicked -= FindPrevious;
				buttonSearchForward.Clicked -= FindNext;
				searchEntry.Entry.Changed -= FindFirst;
				searchEntry.Entry.Activated -= FindNext;
				saveButton.Clicked -= SaveButtonClickedAsync;
				treeView.SelectionChanged -= TreeView_SelectionChanged;
				treeView.ButtonPressed -= TreeView_ButtonPressed;
			}

			base.Dispose (disposing);
		}

		class DropDownWindowDataProvider : DropDownBoxListWindow.IListDataProvider
		{
			IReadOnlyList<BuildOutputNode> list;
			BuildOutputWidget widget;
			BuildOutputDataSource DataSource => widget.treeView.DataSource as BuildOutputDataSource;

			public DropDownWindowDataProvider (BuildOutputWidget widget, BuildOutputNode node)
			{
				if (widget == null)
					throw new ArgumentNullException ("widget");
				this.widget = widget;
				Reset ();

				list = (node == null || node.Parent == null) ? DataSource.RootNodes : NodesWithChildren (node.Parent.Children);
			}

			IReadOnlyList<BuildOutputNode> NodesWithChildren(IEnumerable<BuildOutputNode> nodes)
			{
				var aux = new List<BuildOutputNode> ();
				foreach (var node in nodes) {
					if (node.HasChildren)
						aux.Add (node);
				}

				return aux;
			}

			public int IconCount => list.Count;

			public void ActivateItem (int n)
			{
				if (list [n].HasChildren)
					widget.FocusRow (list [n]);
			}

			public Xwt.Drawing.Image GetIcon (int n) => list [n].GetImage ();

			public string GetMarkup (int n) => list [n].Message;

			public object GetTag (int n) => list [n];

			public void Reset () => list = Array.Empty<BuildOutputNode> ();
		}
	}
}
