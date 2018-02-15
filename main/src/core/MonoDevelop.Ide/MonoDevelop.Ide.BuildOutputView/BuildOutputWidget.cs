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
	class BuildOutputWidget : VBox, IPathedDocument
	{
		const string binLogExtension = "binlog";

		TreeView treeView;
		ScrollView scrolledWindow;
		CheckBox showDiagnosticsButton;
		Button saveButton;
		SearchEntry searchEntry;
		Gtk.VBox box;
		DocumentToolbar toolbar;
		PathBar pathBar;
		Button buttonSearchBackward;
		Button buttonSearchForward;
		Label resultInformLabel;
		List<BuildOutputNode> treeBuildOutputNodes;
		BuildOutputDataSearch search;

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

		public event EventHandler<string> FileSaved;
		public event EventHandler<DocumentPathChangedEventArgs> PathChanged;

		public BuildOutputWidget (BuildOutput output, string viewContentName)
		{
			Initialize ();
			ViewContentName = viewContentName;
			SetupBuildOutput (output);
			filePathLocation = FilePath.Empty;
		}

		public BuildOutputWidget (FilePath filePath)
		{
			Initialize ();

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

		void Initialize ()
		{
			Spacing = 0;

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

			resultInformLabel = new Label ();
			searchEntry.AddLabelWidget ((Gtk.Label) resultInformLabel.ToGtkWidget());

			searchEntry.Entry.Changed += FindFirst;
			searchEntry.Entry.Activated += FindNext;

			buttonSearchBackward = new Button ();
			buttonSearchForward = new Button ();
			buttonSearchBackward.Clicked += FindPrevious;
			buttonSearchForward.Clicked += FindNext;
			buttonSearchForward.TooltipText = GettextCatalog.GetString ("Find next {0}", GetShortcut (SearchCommands.FindNext));
			buttonSearchBackward.TooltipText = GettextCatalog.GetString ("Find previous {0}", GetShortcut (SearchCommands.FindPrevious));
			buttonSearchBackward.Image = ImageService.GetIcon ("gtk-go-up", Gtk.IconSize.Menu);
			buttonSearchForward.Image = ImageService.GetIcon ("gtk-go-down", Gtk.IconSize.Menu);

			toolbar = new DocumentToolbar ();

			box = new Gtk.VBox ();
			box.Spacing = 0;
			toolbar.Add (box, true);

			toolbar.AddSpace ();
			toolbar.Add (showDiagnosticsButton.ToGtkWidget ());
			toolbar.Add (saveButton.ToGtkWidget ());
			toolbar.AddSpace ();
			toolbar.Add (searchEntry, false);
			toolbar.Add (buttonSearchBackward.ToGtkWidget ());
			toolbar.Add (buttonSearchForward.ToGtkWidget());

			PackStart (toolbar.Container, expand: false, fill: true);

			treeView = new TreeView ();
			treeView.HeadersVisible = false;
			treeView.Accessible.Identifier = "BuildOutputWidget.TreeView";
			treeView.Accessible.Description = GettextCatalog.GetString ("Structured build output");
			treeView.SelectionChanged += TreeView_SelectionChanged;
			var treeColumn = new ListViewColumn {
				CanResize = false,
				Expands = true
			};
			var imageCell = new ImageCellView ();
			var textCell = new TextCellView ();
			treeColumn.Views.Add (imageCell);
			treeColumn.Views.Add (textCell);
			treeView.Columns.Add (treeColumn);

			scrolledWindow = new ScrollView { BorderVisible = false };
			scrolledWindow.Content = treeView;

			PackStart (scrolledWindow, expand: true, fill: true);
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
			await processingCompletion.Task;
			var projectNode = treeBuildOutputNodes.SearchFirstNode (BuildOutputNodeType.Project, project);
			var node = projectNode.SearchFirstNode (nodeType, message);
			FocusRow (node);
		}

		void FocusRow (BuildOutputNode match)
		{
			treeView.ExpandToRow (match);
			treeView.FocusedRow = match;
		}

		async void SaveButtonClickedAsync (object sender, EventArgs e) => await Save ();

		public async Task Save ()
		{
			if (filePathLocation == FilePath.Empty) {
				await SaveAs ();
			} else {
				await Save (filePathLocation);
			}
		}

		async Task Save (FilePath outputFile)
		{
			if (!outputFile.HasExtension (binLogExtension))
				outputFile = outputFile.ChangeExtension (binLogExtension);

			await BuildOutput.Save (outputFile);
			FileSaved?.Invoke (this, outputFile.FileName);
			filePathLocation = outputFile;
			IsDirty = false;
		}

		FilePath filePathLocation;
		async Task SaveAs ()
		{
			var dlg = new Gui.Dialogs.OpenFileDialog (GettextCatalog.GetString ("Save as..."), MonoDevelop.Components.FileChooserAction.Save) {
				TransientFor = IdeApp.Workbench.RootWindow,
				InitialFileName = ViewContentName
			};
			if (dlg.Run ()) {
				await Save (dlg.SelectedFile);
			}
		}

		void IndexChanged (int newIndex)
		{
			if (newIndex >= CurrentPath.Length)
				return;

			if (CurrentPath [newIndex].Tag != null) {
				FocusRow (CurrentPath [newIndex].Tag as BuildOutputNode);
			}
		}

		void TreeView_SelectionChanged (object sender, EventArgs e)
		{
			var selectedNode = treeView.SelectedRow as BuildOutputNode;
			if (selectedNode == null)
				return;

			var stack = new Stack<BuildOutputNode> ();

			stack.Push (selectedNode);
			var parent = selectedNode.Parent;

			while (parent != null) {
				stack.Push (parent);
				parent = parent.Parent;
			}

			var entries = new PathEntry [stack.Count];
			var index = 0;
			while (stack.Count > 0) {
				var node = stack.Pop ();
				var dataSource = treeView.DataSource as BuildOutputDataSource;
				var pathEntry = new PathEntry (dataSource.GetValue (node, 0) as Xwt.Drawing.Image, node.Message);
				pathEntry.Tag = node;
				entries [index] = pathEntry;
				index++;
			}

			UpdatePathBarEntries (entries);
		}

		void UpdatePathBarEntries (PathEntry[] entries)
		{
			var previousPathEntry = CurrentPath;
			pathBar.SetPath (entries);
			CurrentPath = pathBar.Path;
			PathChanged?.Invoke (this, new DocumentPathChangedEventArgs (CurrentPath));
		}

		void FindFirst (object sender, EventArgs args)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null)
				return;

			Find (search.FirstMatch (searchEntry.Entry.Text));
		}

		public void FindNext (object sender, EventArgs args)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null)
				return;

			Find (search.NextMatch ());

			if (search.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (
					Gtk.Stock.Find, GettextCatalog.GetString ("Reached top, continued from bottom"));
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
		}

		public void FindPrevious (object sender, EventArgs e)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null)
				return;

			Find (search.PreviousMatch ());

			if (search.SearchWrapped) {
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
			
			IsSearchInProgress = node != null;

			if (node != null) {
				FocusRow (node);

				resultInformLabel.Text = GettextCatalog.GetString ("{0} of {1}", search.CurrentAbsoluteMatchIndex, search.MatchesCount);
				resultInformLabel.TextColor = searchEntry.Style.Foreground (Gtk.StateType.Insensitive).ToXwtColor();
			} else if (string.IsNullOrEmpty (searchEntry.Entry.Text)) {
				resultInformLabel.Text = string.Empty;
				IdeApp.Workbench.StatusBar.ShowReady ();
			} else {
				resultInformLabel.Text = GettextCatalog.GetString ("0 of 0");
				resultInformLabel.TextColor = Ide.Gui.Styles.Editor.SearchErrorForegroundColor;
			}
			resultInformLabel.Show ();

			buttonSearchForward.Sensitive = dataSource.MatchesCount > 0;
			buttonSearchBackward.Sensitive = dataSource.MatchesCount > 0; 
		}

		static string GetShortcut (object commandId)
		{
			var key = IdeApp.CommandService.GetCommand (commandId).AccelKey;
			if (string.IsNullOrEmpty (key))
				return "";
			var nextShortcut = KeyBindingManager.BindingToDisplayLabel (key, false);
			return "(" + nextShortcut + ")";
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

					await Runtime.RunInMainThread (() => {
					
						treeBuildOutputNodes = BuildOutput.GetRootNodes (showDiagnostics);
						search = new BuildOutputDataSearch (treeBuildOutputNodes);

						var buildOutputDataSource = new BuildOutputDataSource (treeBuildOutputNodes);
						treeView.DataSource = buildOutputDataSource;

						(treeView.Columns [0].Views [0] as ImageCellView).ImageField = buildOutputDataSource.ImageField;
						(treeView.Columns [0].Views [1] as TextCellView).MarkupField = buildOutputDataSource.LabelField;

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

		public bool IsSearchInProgress { get; private set; } = false;

		public void FocusOnSearchEntry ()
		{
			searchEntry.Entry.GrabFocus ();
		}

		int currentIndex = -1;
		public Control CreatePathWidget (int index)
		{
			if (currentIndex != index) {
				IndexChanged (index);
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

		protected override void Dispose(bool disposing)
		{
			buttonSearchBackward.Clicked -= FindPrevious;
			buttonSearchForward.Clicked -= FindNext;
			searchEntry.Entry.Changed -= FindFirst;
			searchEntry.Entry.Activated -= FindNext;
			saveButton.Clicked -= SaveButtonClickedAsync;

			base.Dispose(disposing);
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

				list = (node == null || node.Parent == null) ? DataSource.RootNodes : node.Parent.Children;
			}

			public int IconCount => list.Count;

			public void ActivateItem (int n) => widget.FocusRow (list [n]);

			public Xwt.Drawing.Image GetIcon (int n) => DataSource.GetValue (list [n], 0) as Xwt.Drawing.Image;

			public string GetMarkup (int n) => list [n].Message;

			public object GetTag (int n) => list [n];

			public void Reset () => list = Array.Empty<BuildOutputNode> ();
		}
	}
}
