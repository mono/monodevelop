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

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutputWidget : VBox
	{
		TreeView treeView;
		ScrollView scrolledWindow;
		CheckBox showDiagnosticsButton;
		Button saveButton;
		SearchEntry searchEntry;
		Button buttonSearchBackward;
		Button buttonSearchForward;
		Gtk.Label resultInformLabel;

		public string ViewContentName { get; private set; }
		public BuildOutput BuildOutput { get; private set; }

		public event EventHandler<string> FileSaved;

		public BuildOutputWidget (BuildOutput output, string viewContentName)
		{
			Initialize ();
			ViewContentName = viewContentName;
			SetupBuildOutput (output);
		}

		public BuildOutputWidget (FilePath filePath)
		{
			Initialize ();

			ViewContentName = filePath;
			var output = new BuildOutput ();
			output.Load (filePath.FullPath, false);
			SetupBuildOutput (output);
		}

		void SetupBuildOutput (BuildOutput output)
		{
			BuildOutput = output;
			ProcessLogs (false);

			BuildOutput.OutputChanged += (sender, e) => ProcessLogs (showDiagnosticsButton.Active);
		}

		void Initialize ()
		{
			showDiagnosticsButton = new CheckBox (GettextCatalog.GetString ("Diagnostic log verbosity"));
			showDiagnosticsButton.Accessible.Identifier = "BuildOutputWidget.ShowDiagnosticsButton";
			showDiagnosticsButton.TooltipText = GettextCatalog.GetString ("Show full (diagnostics enabled) or reduced log");
			showDiagnosticsButton.Accessible.Description = GettextCatalog.GetString ("Diagnostic log verbosity");
			showDiagnosticsButton.Clicked += (sender, e) => ProcessLogs (showDiagnosticsButton.Active);

			saveButton = new Button (GettextCatalog.GetString ("Save"));
			saveButton.Accessible.Identifier = "BuildOutputWidget.SaveButton";
			saveButton.TooltipText = GettextCatalog.GetString ("Save build output");
			saveButton.Accessible.Description = GettextCatalog.GetString ("Save build output");

			saveButton.Clicked += async (sender, e) => {
				const string binLogExtension = "binlog";
				var dlg = new Gui.Dialogs.OpenFileDialog(GettextCatalog.GetString ("Save as..."), MonoDevelop.Components.FileChooserAction.Save) {
					TransientFor = IdeApp.Workbench.RootWindow,
					InitialFileName = ViewContentName
				};
				if (dlg.Run ()) {
					var outputFile = dlg.SelectedFile;
					if (!outputFile.HasExtension (binLogExtension))
						outputFile = outputFile.ChangeExtension (binLogExtension);
					
					await BuildOutput.Save (outputFile);
					FileSaved?.Invoke (this, outputFile.FileName);
				}
			};

			searchEntry = new SearchEntry ();
			searchEntry.Accessible.SetLabel (GettextCatalog.GetString ("Search"));
			searchEntry.Accessible.Name = "BuildOutputWidget.Search";
			searchEntry.Accessible.Description = GettextCatalog.GetString ("Search the build log");
			searchEntry.WidthRequest = 200;
			searchEntry.Visible = true;

			resultInformLabel = new Gtk.Label ();
			searchEntry.AddLabelWidget (resultInformLabel);

			searchEntry.Entry.Changed += FindFirst;

			var searchBox = new Gtk.HBox ();
			searchBox.Add (searchEntry);
			buttonSearchBackward = new Button ();
			buttonSearchForward = new Button ();
			buttonSearchBackward.Clicked += FindPrevious;
			buttonSearchForward.Clicked += FindNext;
			buttonSearchForward.TooltipText = GettextCatalog.GetString ("Find next {0}", GetShortcut (SearchCommands.FindNext));
			buttonSearchBackward.TooltipText = GettextCatalog.GetString ("Find previous {0}", GetShortcut (SearchCommands.FindPrevious));
			buttonSearchBackward.Image = ImageService.GetIcon (Ide.Gui.Stock.FindPrevIcon, Gtk.IconSize.Menu);
			buttonSearchForward.Image = ImageService.GetIcon (Ide.Gui.Stock.FindNextIcon, Gtk.IconSize.Menu);
			searchBox.Add (buttonSearchBackward.ToGtkWidget());
			searchBox.Add (buttonSearchForward.ToGtkWidget ());
			searchBox.Show ();

			var toolbar = new DocumentToolbar ();

			toolbar.AddSpace ();
			toolbar.Add (showDiagnosticsButton.ToGtkWidget ());
			toolbar.Add (saveButton.ToGtkWidget ());
			toolbar.AddSpace ();
			toolbar.Add (searchBox, false);

			PackStart (toolbar.Container, expand: false, fill: true);

			treeView = new TreeView ();
			treeView.HeadersVisible = false;
			treeView.Accessible.Identifier = "BuildOutputWidget.TreeView";
			treeView.Accessible.Description = GettextCatalog.GetString ("Structured build output");

			var treeColumn = new ListViewColumn {
				CanResize = false,
				Expands = true
			};
			var imageCell = new ImageCellView ();
			var textCell = new TextCellView ();
			treeColumn.Views.Add (imageCell);
			treeColumn.Views.Add (textCell);
			treeView.Columns.Add (treeColumn);

			scrolledWindow = new ScrollView ();
			scrolledWindow.Content = treeView;

			PackStart (scrolledWindow, expand: true, fill: true);
		}

		void FindFirst (object sender, EventArgs args)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null)
				return;

			Find (dataSource.FirstMatch (searchEntry.Entry.Text));
		}

		void FindNext (object sender, EventArgs args)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null)
				return;

			Find (dataSource.NextMatch ());

			if (dataSource.SearchWrapped) {
				IdeApp.Workbench.StatusBar.ShowMessage (
					Gtk.Stock.Find, GettextCatalog.GetString ("Reached top, continued from bottom"));
			} else {
				IdeApp.Workbench.StatusBar.ShowReady ();
			}
		}

		void FindPrevious (object sender, EventArgs args)
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource == null)
				return;

			Find (dataSource.PreviousMatch ());

			if (dataSource.SearchWrapped) {
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
				MoveToMatch (node);

				resultInformLabel.Text = String.Format (GettextCatalog.GetString ("{0} of {1}"), dataSource.CurrentAbsoluteMatchIndex, dataSource.MatchesCount);
				resultInformLabel.Xpad = 2;
				resultInformLabel.ModifyFg (Gtk.StateType.Normal, searchEntry.Style.Foreground (Gtk.StateType.Insensitive));
			} else if (!string.IsNullOrEmpty (searchEntry.Entry.Text)) {
				resultInformLabel.Text = string.Empty;
				IdeApp.Workbench.StatusBar.ShowReady ();
			} else {
				resultInformLabel.Text = GettextCatalog.GetString ("Not found");
				resultInformLabel.ModifyFg (Gtk.StateType.Normal, Ide.Gui.Styles.Editor.SearchErrorForegroundColor.ToGdkColor ());
			}
			resultInformLabel.Show ();
		}

		static string GetShortcut (object commandId)
		{
			var key = IdeApp.CommandService.GetCommand (commandId).AccelKey;
			if (string.IsNullOrEmpty (key))
				return "";
			var nextShortcut = KeyBindingManager.BindingToDisplayLabel (key, false);
			return "(" + nextShortcut + ")";
		}

		CancellationTokenSource cts;

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

		void MoveToMatch (BuildOutputNode match)
		{
			if (match != null) {
				treeView.ExpandToRow (match);
				treeView.ScrollToRow (match);
				treeView.SelectRow (match);
			}
		}

		void ProcessLogs (bool showDiagnostics)
		{
			cts?.Cancel ();
			cts = new CancellationTokenSource ();

			Task.Run (async () => {
				await Runtime.RunInMainThread (() => {
					var dataSource = BuildOutput.ToTreeDataSource (showDiagnostics);
					treeView.DataSource = dataSource;
					(treeView.Columns [0].Views [0] as ImageCellView).ImageField = dataSource.ImageField;
					(treeView.Columns [0].Views [1] as TextCellView).MarkupField = dataSource.LabelField;

					// Expand root nodes and nodes with errors
					int rootsCount = dataSource.GetChildrenCount (null);
					for (int i = 0; i < rootsCount; i++) {
						var root = dataSource.GetChild (null, i) as BuildOutputNode;
						treeView.ExpandRow (root, false);
						ExpandChildrenWithErrors (treeView, dataSource, root);
					}
				});
			}, cts.Token);
		}

		public void Find ()
		{
			searchEntry.Entry.GrabFocus ();
		}

		public void FindPrevious ()
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource != null) {
				var match = dataSource.PreviousMatch ();
				if (match != null) {
					MoveToMatch (match);
				}
			}
		}

		public void FindNext ()
		{
			var dataSource = treeView.DataSource as BuildOutputDataSource;
			if (dataSource != null) {
				var match = dataSource.NextMatch ();
				if (match != null) {
					MoveToMatch (match);
				}
			}
		}
	}
}
