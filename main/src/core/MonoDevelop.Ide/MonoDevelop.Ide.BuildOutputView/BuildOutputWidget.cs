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
using System.Linq;
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
		const int PathBarTopPadding = 5;

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
		MDSpinner loadingSpinner;

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

		public event EventHandler<string> FileNameChanged;
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

			BuildOutput.OutputChanged += OnOutputChanged;
			BuildOutput.ProjectStarted += OnProjectStarted;
			BuildOutput.ProjectFinished += OnProjectFinished;
			ProcessLogs (false);

			pathBar = new PathBar (this.CreatePathWidget, PathBarTopPadding) {
				DrawBottomBorder = false
			};
			pathBar.Show ();

			box.PackStart (pathBar, true, true, 2);
			box.ReorderChild (pathBar, 0);
			box.Show ();
		}

		void OnOutputChanged (object sender, EventArgs args)
		{
			ProcessLogs (showDiagnosticsButton.Active);
		}

		void OnProjectStarted (object sender, EventArgs args)
		{
			SetSpinnerVisibility (true);
		}

		void OnProjectFinished (object sender, EventArgs args)
		{
			SetSpinnerVisibility (false);
		}

		void Initialize (DocumentToolbar toolbar)
		{
			Spacing = 0;

			// FIXME: DocumentToolbar does not support native widgets
			// Toolbar items must use Gtk, for now
			Xwt.Toolkit.Load (ToolkitType.Gtk).Invoke (() => {
				showDiagnosticsButton = new CheckBox (GettextCatalog.GetString ("Diagnostic log verbosity"));
				showDiagnosticsButton.HeightRequest = 17;
				showDiagnosticsButton.Accessible.Identifier = "BuildOutputWidget.ShowDiagnosticsButton";
				showDiagnosticsButton.TooltipText = GettextCatalog.GetString ("Show full (diagnostics enabled) or reduced log");
				showDiagnosticsButton.Accessible.Description = GettextCatalog.GetString ("Diagnostic log verbosity");
				showDiagnosticsButton.Clicked += (sender, e) => ProcessLogs (showDiagnosticsButton.Active);

				saveButton = new Button (GettextCatalog.GetString ("Save"));
				saveButton.HeightRequest = 17;
				saveButton.Accessible.Identifier = "BuildOutputWidget.SaveButton";
				saveButton.TooltipText = GettextCatalog.GetString ("Save build output");
				saveButton.Accessible.Description = GettextCatalog.GetString ("Save build output");

				saveButton.Clicked += SaveButtonClickedAsync;

				searchEntry = new SearchEntry ();
				searchEntry.Accessible.SetLabel (GettextCatalog.GetString ("Search"));
				searchEntry.Accessible.Name = "BuildOutputWidget.Search";
				searchEntry.Accessible.Description = GettextCatalog.GetString ("Search the build log");
				searchEntry.WidthRequest = 200;
				searchEntry.Entry.HeightRequest = 17;
				searchEntry.Visible = true;
				searchEntry.EmptyMessage = GettextCatalog.GetString ("Search Build Output");

				resultInformLabel = new Label ();
				resultInformLabel.HeightRequest = 17;
				searchEntry.AddLabelWidget ((Gtk.Label)resultInformLabel.ToGtkWidget ());

				searchEntry.Entry.Changed += FindFirst;
				searchEntry.Entry.Activated += FindNext;

				buttonSearchBackward = new Button ();
				buttonSearchForward = new Button ();
				buttonSearchBackward.Clicked += FindPrevious;
				buttonSearchForward.Clicked += FindNext;
				buttonSearchForward.TooltipText = GettextCatalog.GetString ("Find next {0}", GetShortcut (SearchCommands.FindNext, true));
				buttonSearchBackward.TooltipText = GettextCatalog.GetString ("Find previous {0}", GetShortcut (SearchCommands.FindPrevious, true));
				buttonSearchForward.HeightRequest = buttonSearchBackward.HeightRequest = 17;
				SetSearchButtonsSensitivity (false);
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
			treeView.AnimationsEnabled = false;
			treeView.BorderVisible = false;
			treeView.UseAlternatingRowColors = true;
			treeView.SelectionMode = Xwt.SelectionMode.Single;
			treeView.Accessible.Identifier = "BuildOutputWidget.TreeView";
			treeView.Accessible.Description = GettextCatalog.GetString ("Structured build output");
			treeView.HorizontalScrollPolicy = ScrollPolicy.Never;
			treeView.SelectionChanged += TreeView_SelectionChanged;
			treeView.ButtonPressed += TreeView_ButtonPressed;

			var treeColumn = new ListViewColumn {
				CanResize = false,
				Expands = true
			};

			cellView = new BuildOutputTreeCellView (this);
			treeColumn.Views.Add (cellView, true);
			treeView.Columns.Add (treeColumn);

			// HACK: this should not be required, atomic cell calculation should depend on the final column size.
			// This workaround causes the node information to float in a weird way when the tab is being resized.
			// FIXME: Xwt.XamMac does not raise the TreeView.BoundsChanged event, however it's ok to use the container instead.
			BoundsChanged += (s, e) => cellView.OnBoundsChanged (s, e);
			cellView.GoToTask += (s, e) => GoToTask (e);

			cellView.ExpandWarnings += (s, e) => ExpandErrorOrWarningsNodes (treeView, true);
			cellView.ExpandErrors += (s, e) => ExpandErrorOrWarningsNodes (treeView, false);

			PackStart (treeView, expand: true, fill: true);

			loadingSpinner = new MDSpinner (Gtk.IconSize.Button) {
				Visible = false
			};
			PackStart (loadingSpinner, expand: true, vpos: WidgetPlacement.Center, hpos: WidgetPlacement.Center);
		}

		static void ExpandErrorOrWarningsNodes (TreeView treeView, bool warnings)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null) {
				return;
			}
			ExpandErrorOrWarningsNodes (treeView, dataSource, warnings);
		}

		static void ExpandErrorOrWarningsNodes (TreeView treeView, BuildOutputDataSource dataSource, bool warnings)
		{
			BuildOutputNode firstNodeFound = null;

			int rootsCount = dataSource.GetChildrenCount (null);
			for (int i = 0; i < rootsCount; i++) {
				var root = dataSource.GetChild (null, i) as BuildOutputNode;
				treeView.ExpandRow (root, false);
				firstNodeFound = ExpandChildrenWithErrorsOrWarnings (treeView, dataSource, root, warnings, firstNodeFound);
			}

			if (firstNodeFound != null) {
				treeView.ScrollToRow (firstNodeFound);
				treeView.SelectRow (firstNodeFound);
			}
		}

		static BuildOutputNode ExpandChildrenWithErrorsOrWarnings (TreeView tree, BuildOutputDataSource dataSource, BuildOutputNode parent, bool expandWarnings, BuildOutputNode firstNode)
		{
			int totalChildren = dataSource.GetChildrenCount (parent);
			for (int i = 0; i < totalChildren; i++) {
				var child = dataSource.GetChild (parent, i) as BuildOutputNode;
				var containNodes = expandWarnings ? (child?.HasWarnings ?? false) : (child?.HasErrors ?? false);
				if (containNodes) {
					tree.ExpandToRow (child);
					if (child.NodeType == (expandWarnings ? BuildOutputNodeType.Warning : BuildOutputNodeType.Error) && firstNode == null) {
						firstNode = child;
					}
					firstNode = ExpandChildrenWithErrorsOrWarnings (tree, dataSource, child, expandWarnings, firstNode);
				} else if (child.NodeType == BuildOutputNodeType.Project) {
					tree.ExpandToRow (child);
				}
			}

			return firstNode;
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
				ViewContentName = outputFile.FileName;
				FileNameChanged?.Invoke (this, outputFile);
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

			if (e.Button == PointerButton.Left) {
				if (!cellView.IsViewClickable (selectedNode, e.Position)) {
					return;
				}

				if (e.MultiplePress == 1) {
					cellView.ClearSelection ();
				} else if (e.MultiplePress == 2) {

					if (selectedNode.NodeType == BuildOutputNodeType.Warning || selectedNode.NodeType == BuildOutputNodeType.Error) {
						GoToTask (selectedNode);
						return;
					}
					if (treeView.IsRowExpanded (selectedNode)) {
						treeView.CollapseRow (selectedNode);
					} else {
						treeView.ExpandRow (selectedNode, false);
					}
					return;
				}
			}

			if (e.IsContextMenuTrigger) {
				CommandEntrySet cset = IdeApp.CommandService.CreateCommandEntrySet ("/MonoDevelop/BuildOutput/ContextMenu");
				IdeApp.CommandService.ShowContextMenu (treeView, (int)e.X, (int)e.Y, cset, this);
			}
		}

		[CommandHandler (EditCommands.Copy)]
		public void Copy ()
		{
			ClipboardCopy ();
		}

		[CommandUpdateHandler (EditCommands.Copy)]
		public void UpdateCopyHandler (CommandInfo cinfo)
		{
			cinfo.Enabled = CanClipboardCopy ();
		}

		[CommandHandler (BuildOutputCommands.ExpandAll)]
		public void ExpandAll ()
		{
			treeView.ExpandAll ();
		}

		[CommandHandler (BuildOutputCommands.CollapseAll)]
		public void CollapseAll ()
		{
			var dataSource = (BuildOutputDataSource)treeView.DataSource;
			if (dataSource != null) {
				foreach (var root in dataSource.RootNodes) {
					treeView.CollapseRow (root);
				}
			}
		}

		[CommandUpdateHandler (BuildOutputCommands.ExpandAll)]
		[CommandUpdateHandler (BuildOutputCommands.CollapseAll)]
		public void UpdateExpandCollapseAllHandler (CommandInfo cinfo)
		{
			cinfo.Enabled = (treeView?.DataSource as BuildOutputDataSource) != null;
		}

		[CommandHandler (BuildOutputCommands.JumpTo)]
		public void JumpTo ()
		{
			var selectedNode = treeView.SelectedRow as BuildOutputNode;
			GoToTask (selectedNode);
		}

		[CommandUpdateHandler (BuildOutputCommands.JumpTo)]
		public void UpdateJumpToHandler (CommandInfo cinfo)
		{
			var selectedNode = treeView.SelectedRow as BuildOutputNode;
			cinfo.Visible = cinfo.Enabled = selectedNode?.NodeType == BuildOutputNodeType.Warning || selectedNode?.NodeType == BuildOutputNodeType.Error;
			if (cinfo.Visible)
				cinfo.Text = GettextCatalog.GetString ("_Jump To {0}", selectedNode.NodeType.ToString ());
		}

		void GoToTask (BuildOutputNode selectedNode)
		{
			var path = System.IO.Path.Combine (System.IO.Path.GetDirectoryName (selectedNode.Project), selectedNode.File);
			IdeApp.Workbench.OpenDocument (new FilePath (path),
			                               null,
			                               Math.Max (1, selectedNode.LineNumber),
			                               Math.Max (1, 0)
			                              );
		}

		public void GoToFirstNode ()
		{
			var dataSource = (BuildOutputDataSource)treeView.DataSource;
			if (dataSource != null) {
				if (dataSource.RootNodes.Count > 0) {
					treeView.FocusedRow = dataSource.RootNodes [0];
				}
			}
		}

		public void GoToLastNode ()
		{
			var dataSource = (BuildOutputDataSource)treeView.DataSource;
			if (dataSource != null) {
				if (dataSource.RootNodes.Count > 0) {
					treeView.FocusedRow = dataSource.RootNodes [dataSource.RootNodes.Count - 1];
				}
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
			var currentRow = treeView.SelectedRow as BuildOutputNode;

			var cellSelection = cellView.TextSelection;
			if (cellSelection?.IsShown (currentRow) ?? false) {
				Clipboard.SetText (cellSelection.Content.Message.Substring (cellSelection.Index, cellSelection.Length));
			} else {
				ClipboardCopy (currentRow);
			}
		}

		private void ClipboardCopy (BuildOutputNode selectedNode)
		{
			if (selectedNode != null) {
				Clipboard.SetText (selectedNode.ToString (true));
			}
		}

		static void RefreshSearchMatches (BuildOutputDataSource dataSource, BuildOutputDataSearch search)
		{
			if (search.IsCanceled) {
				// If search was canceled, we never highlighted matches for it,
				// so avoid doing anything
				return;
			}

			foreach (var match in search.AllMatches) {
				dataSource.RaiseNodeChanged (match);
			}
		}

		async void FindFirst (object sender, EventArgs args)
		{
			if (!(treeView.DataSource is BuildOutputDataSource dataSource))
				return;

			using (Counters.SearchBuildLog.BeginTiming ()) {
				// Cleanup previous search
				if (currentSearch != null) {
					currentSearch.Cancel ();
					RefreshSearchMatches (dataSource, currentSearch);
					Counters.SearchBuildLog.Trace ("Cleared previous search matches");
				}

				currentSearch = new BuildOutputDataSearch (dataSource.RootNodes);
				var firstMatch = await currentSearch.FirstMatch (searchEntry.Entry.Text);
				if (firstMatch != null && !currentSearch.IsCanceled) {
					RefreshSearchMatches (dataSource, currentSearch);
					Find (firstMatch);
				}
			}
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

			SetSearchButtonsSensitivity (currentSearch?.MatchesCount > 0);
		}

		static string GetShortcut (object commandId, bool includeParen)
		{
			var key = IdeApp.CommandService.GetCommand (commandId).AccelKey;
			if (string.IsNullOrEmpty (key))
				return "";
			var nextShortcut = KeyBindingManager.BindingToDisplayLabel (key, false);
			return includeParen ? "(" + nextShortcut + ")" : nextShortcut;
		}

		void SetSearchButtonsSensitivity (bool sensitive)
		{
			buttonSearchForward.Sensitive = buttonSearchBackward.Sensitive = sensitive;
			buttonSearchForward.Image = ImageService.GetIcon ("gtk-go-down", Gtk.IconSize.Menu).WithStyles (sensitive ? "" : "disabled");
			buttonSearchBackward.Image = ImageService.GetIcon ("gtk-go-up", Gtk.IconSize.Menu).WithStyles (sensitive ? "" : "disabled");
		}

		Task SetSpinnerVisibility (bool visible)
		{
			return InvokeAsync (() => {
				loadingSpinner.Visible = loadingSpinner.Animate = visible;
				loadingSpinner.TooltipText = visible ? GettextCatalog.GetString ("Loading build log\u2026") : String.Empty;
				treeView.Visible = !visible;
			});
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
				await SetSpinnerVisibility (true);

				try {
					var metadata = new BuildOutputCounterMetadata ();
					using (Counters.ProcessBuildLog.BeginTiming (metadata)) {
						BuildOutput.ProcessProjects (showDiagnostics, metadata);

						await InvokeAsync (() => {
							currentSearch = null;
							searchEntry.Entry.Text = String.Empty;
							Find (null);

							var buildOutputDataSource = new BuildOutputDataSource (BuildOutput.GetRootNodes (showDiagnostics));
							(treeView.Columns [0].Views [0] as BuildOutputTreeCellView).BuildOutputNodeField = buildOutputDataSource.BuildOutputNodeField;

							treeView.DataSource = buildOutputDataSource;
							cellView.OnDataSourceChanged ();

							// Expand root nodes and nodes with errors
							ExpandErrorOrWarningsNodes (treeView, buildOutputDataSource, false);
							processingCompletion.TrySetResult (null);

							ViewContentName = filePathLocation.IsEmpty ?
							                                  GettextCatalog.GetString ("Build Output {0}.binlog", DateTime.Now.ToString ("h:mm tt yyyy-MM-dd")) :
							                                  (string)filePathLocation;
							FileNameChanged?.Invoke (this, ViewContentName);
						});
					}
				} catch (Exception ex) {
					processingCompletion.TrySetException (ex);
				}

				await SetSpinnerVisibility (false);
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
			if (BuildOutput != null) {
				BuildOutput.OutputChanged -= OnOutputChanged;
				BuildOutput.ProjectStarted -= OnProjectStarted;
				BuildOutput.ProjectFinished -= OnProjectFinished;
				BuildOutput = null;
			}

			if (buttonSearchBackward != null) {
				buttonSearchBackward.Clicked -= FindPrevious;
				buttonSearchBackward = null;
			}

			if (buttonSearchForward != null) {
				buttonSearchForward.Clicked -= FindNext;
				buttonSearchForward = null;
			}

			if (searchEntry != null) {
				searchEntry.Entry.Changed -= FindFirst;
				searchEntry.Entry.Activated -= FindNext;
				searchEntry = null;
			}

			if (saveButton != null) {
				saveButton.Clicked -= SaveButtonClickedAsync;
				saveButton = null;
			}

			if (treeView != null) {
				treeView.SelectionChanged -= TreeView_SelectionChanged;
				treeView.ButtonPressed -= TreeView_ButtonPressed;
				treeView = null;
			}

			pathBar = null;

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

				list = (node == null || node.Parent == null) ?
					DataSource?.RootNodes?.Where (x => x.NodeType != BuildOutputNodeType.BuildSummary).ToList () :
				    NodesWithChildren (node.Parent.Children);
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

			public int IconCount => list?.Count ?? 0;

			public void ActivateItem (int n)
			{
				if (list [n].HasChildren || list [n].NodeType == BuildOutputNodeType.BuildSummary)
					widget.FocusRow (list [n]);
			}

			public Xwt.Drawing.Image GetIcon (int n) => list [n].GetImage ();

			public string GetMarkup (int n) => list [n].Message;

			public object GetTag (int n) => list [n];

			public void Reset () => list = Array.Empty<BuildOutputNode> ();
		}
	}
}
