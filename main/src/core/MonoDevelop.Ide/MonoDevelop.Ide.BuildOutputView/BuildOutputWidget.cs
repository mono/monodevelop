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
using Gtk;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.BuildOutputView
{
	class BuildOutputWidget : VBox
	{
#if TEXT_EDITOR
		TextEditor editor;
#else
		TreeView treeView;
#endif
		CompactScrolledWindow scrolledWindow;
		CheckButton showDiagnosticsButton;
		Button saveButton;

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

#if TEXT_EDITOR
			editor.FileName = filePath;
#endif

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

		Button MakeButton (string image, string name, out Label label)
		{
			var btnBox = MakeHBox (image, out label);

			var btn = new Button { Name = name };
			btn.Child = btnBox;

			return btn;
		}

		HBox MakeHBox (string image, out Label label)
		{
			var btnBox = new HBox (false, 2);
			btnBox.Accessible.SetShouldIgnore (true);
			var imageView = new ImageView (image, Gtk.IconSize.Menu);
			imageView.Accessible.SetShouldIgnore (true);
			btnBox.PackStart (imageView);

			label = new Label ();
			label.Accessible.SetShouldIgnore (true);
			btnBox.PackStart (label);

			return btnBox;
		}

		void Initialize ()
		{
			showDiagnosticsButton = new CheckButton (GettextCatalog.GetString ("Show Diagnostics")) {
				BorderWidth = 0
			};
			showDiagnosticsButton.Accessible.Name = "BuildOutputWidget.ShowDiagnosticsButton";
			showDiagnosticsButton.TooltipText = GettextCatalog.GetString ("Show full (diagnostics enabled) or reduced log");
			showDiagnosticsButton.Accessible.Description = GettextCatalog.GetString ("Show Diagnostics");
			showDiagnosticsButton.Clicked += (sender, e) => ProcessLogs (showDiagnosticsButton.Active);

			Label saveLbl;

			saveButton = MakeButton (Gui.Stock.SaveIcon, GettextCatalog.GetString ("Save"), out saveLbl);
			saveButton.Accessible.Name = "BuildOutputWidget.SaveButton";
			saveButton.TooltipText = GettextCatalog.GetString ("Save build output");
			saveButton.Accessible.Description = GettextCatalog.GetString ("Save build output");

			saveLbl.Text = GettextCatalog.GetString ("Save");
			saveButton.Accessible.SetTitle (saveLbl.Text);
			saveButton.Clicked += async (sender, e) => {
				const string binLogExtension = "binlog";
				var dlg = new OpenFileDialog (GettextCatalog.GetString ("Save as..."), MonoDevelop.Components.FileChooserAction.Save) {
					TransientFor = IdeApp.Workbench.RootWindow,
					InitialFileName = string.IsNullOrEmpty (ViewContentName) ? editor.FileName.FileName : ViewContentName
				};
				if (dlg.Run ()) {
					var outputFile = dlg.SelectedFile;
					if (!outputFile.HasExtension (binLogExtension))
						outputFile = outputFile.ChangeExtension (binLogExtension);
					
					await BuildOutput.Save (outputFile);
					FileSaved?.Invoke (this, outputFile.FileName);
				}
			};

			var toolbar = new DocumentToolbar ();

			toolbar.AddSpace ();
			toolbar.Add (showDiagnosticsButton);
			toolbar.Add (saveButton);
			PackStart (toolbar.Container, expand: false, fill: true, padding: 0);

#if TEXT_EDITOR
			editor = TextEditorFactory.CreateNewEditor ();
			editor.IsReadOnly = true;
			editor.Options = new CustomEditorOptions (editor.Options) {
				ShowFoldMargin = true,
				TabsToSpaces = false
			};
#else
			treeView = new TreeView ();
			treeView.HeadersVisible = false;
			treeView.Accessible.Name = "BuildOutputWidget.TreeView";
			treeView.Accessible.Description = GettextCatalog.GetString ("Structured build output");

			TreeViewColumn col = new TreeViewColumn ();
			var crp = new CellRendererImage ();
			col.PackStart (crp, false);
			col.SetCellDataFunc (crp, PixbufCellDataFunc);
			var crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.SetCellDataFunc (crt, TextCellDataFunc);
			treeView.AppendColumn (col);

			treeView.ExpanderColumn = col;
#endif

			scrolledWindow = new CompactScrolledWindow ();
#if TEXT_EDITOR
			scrolledWindow.AddWithViewport (editor);
#else
			scrolledWindow.AddWithViewport (treeView);
#endif

			PackStart (scrolledWindow, expand: true, fill: true, padding: 0);
			ShowAll ();
		}

		static void PixbufCellDataFunc (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			var crp = (CellRendererImage)cell;
			var node = (BuildOutputNode)model.GetValue (iter, 0);
			switch (node.NodeType) {
			case BuildOutputNodeType.Build:
				crp.Image = ImageService.GetIcon (Ide.Gui.Stock.StatusBuild, IconSize.Menu);
				break;
			case BuildOutputNodeType.Error:
				crp.Image = ImageService.GetIcon (Ide.Gui.Stock.Error, IconSize.Menu);
				break;
			case BuildOutputNodeType.Project:
				crp.Image = ImageService.GetIcon (Ide.Gui.Stock.Project, IconSize.Menu);
				break;
			case BuildOutputNodeType.Warning:
				crp.Image = ImageService.GetIcon (Ide.Gui.Stock.Warning, IconSize.Menu);
				break;
			default:
				crp.Image = ImageService.GetIcon (Ide.Gui.Stock.Empty);
				break;
			}
		}

		static void TextCellDataFunc (TreeViewColumn col, CellRenderer cell, TreeModel model, TreeIter iter)
		{
			TreeIter parent;
			bool toplevel = !model.IterParent (out parent, iter);

			var crt = (CellRendererText)cell;
			var node = (BuildOutputNode)model.GetValue (iter, 0);

			if (toplevel) {
				crt.Markup = "<b>" + GLib.Markup.EscapeText (node.Message) + "</b>";
			} else {
				crt.Text = node.Message;
			}
		}

		protected override void OnDestroyed ()
		{
#if TEXT_EDITOR
			editor.Dispose ();
#else
			treeView.Dispose ();
#endif
			base.OnDestroyed ();
		}

#if TEXT_EDITOR
		void SetupTextEditor (string text, IList<IFoldSegment> segments)
		{
			editor.Text = text;
			if (segments != null) {
				editor.SetFoldings (segments);
			}
		}
#endif

#if TEXT_EDITOR
		CancellationTokenSource cts;
#endif

		static void ExpandChildrenWithErrors (TreeView tree, TreeStore store, TreeIter parent)
		{
			TreeIter child = TreeIter.Zero;
			var node = store.GetValue (parent, 0) as BuildOutputNode;
			if (node?.HasErrors ?? false && store.IterChildren (out child, parent)) {
				do {
					var childNode = store.GetValue (child, 0) as BuildOutputNode;
					if (childNode?.HasErrors ?? false) {
						tree.ExpandRow (store.GetPath (child), false);
						ExpandChildrenWithErrors (tree, store, child);
					}
				} while (store.IterNext (ref child));
			}
		}

		void ProcessLogs (bool showDiagnostics)
		{
#if TEXT_EDITOR
			cts?.Cancel ();
			cts = new CancellationTokenSource ();

			Task.Run (async () => {
				var (text, segments) = await BuildOutput.ToTextEditor (editor, showDiagnostics);

				if (Runtime.IsMainThread) {
					SetupTextEditor (text, segments);
				} else {
					await Runtime.RunInMainThread (() => SetupTextEditor (text, segments));
				}
			}, cts.Token);
#else
			var model = BuildOutput.ToTreeStore (showDiagnostics);
			treeView.Model = model;

			// Expand root nodes and nodes with errors
			TreeIter it;
			if (model.GetIterFirst (out it)) {
				do {
					treeView.ExpandRow (model.GetPath (it), false);
					ExpandChildrenWithErrors (treeView, model, it);
				} while (model.IterNext (ref it));
			}
#endif
		}
	}
}
