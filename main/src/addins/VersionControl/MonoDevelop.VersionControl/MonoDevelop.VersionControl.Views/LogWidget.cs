//
// LogWidget.cs
//
// Author:
//       Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Text;
using System.Threading;
using MonoDevelop.Components;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Fonts;

namespace MonoDevelop.VersionControl.Views
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class LogWidget : Gtk.Bin
	{
		Revision[] history;
		public Revision[] History {
			get {
				return history;
			}
			set {
				history = value;
				UpdateHistory ();
			}
		}
		
		ListStore logstore = new ListStore (typeof (Revision), typeof(string));
		FileTreeView treeviewFiles;
		TreeStore changedpathstore;
		DocumentToolButton revertButton, revertToButton, refreshButton;
		SearchEntry searchEntry;
		string currentFilter;
		
		VersionControlDocumentInfo info;
		string preselectFile;
		CellRendererDiff diffRenderer = new CellRendererDiff ();
		CellRendererText messageRenderer = new CellRendererText ();
		CellRendererText textRenderer = new CellRendererText ();
		CellRendererImage pixRenderer = new CellRendererImage ();
		
		bool currentRevisionShortened;
		
		class RevisionGraphCellRenderer : Gtk.CellRenderer
		{
			public bool FirstNode {
				get;
				set;
			}

			public bool LastNode {
				get;
				set;
			}
			
			public override void GetSize (Widget widget, ref Gdk.Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
			{
				x_offset = y_offset = 0;
				width = 16;
				height = cell_area.Height;
			}
			
			protected override void Render (Gdk.Drawable window, Widget widget, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, Gdk.Rectangle expose_area, CellRendererState flags)
			{
				using (Cairo.Context cr = Gdk.CairoHelper.Create (window)) {
					cr.LineWidth = 2.0;
					double center_x = cell_area.X + Math.Round ((double) (cell_area.Width / 2d));
					double center_y = cell_area.Y + Math.Round ((double) (cell_area.Height / 2d));
					cr.Arc (center_x, center_y, 5, 0, 2 * Math.PI);
					var state = StateType.Normal;
					if (!base.Sensitive)
						state = StateType.Insensitive;
					else if (flags.HasFlag (CellRendererState.Selected)) {
						if (widget.HasFocus)
							state = StateType.Selected;
						else
							state = StateType.Active;
					}
					else if (flags.HasFlag (CellRendererState.Prelit))
						state = StateType.Prelight;
					else if (widget.State == StateType.Insensitive)
						state = StateType.Insensitive;

					cr.SetSourceColor (widget.Style.Text (state).ToCairoColor ());
					cr.Stroke ();
					if (!FirstNode) {
						cr.MoveTo (center_x, cell_area.Y - 2);
						cr.LineTo (center_x, center_y - 5);
						cr.Stroke ();
					}
					
					if (!LastNode) {
						cr.MoveTo (center_x, cell_area.Y + cell_area.Height + 2);
						cr.LineTo (center_x, center_y + 5);
						cr.Stroke ();
					}
				}
			}
		}
		
		public LogWidget (VersionControlDocumentInfo info)
		{
			this.Build ();
			this.info = info;
			if (info.Document != null)
				this.preselectFile = info.Item.Path;

			var separator = new HeaderBox ();
			separator.SetMargins (1, 0, 0, 0);
			separator.HeightRequest = 4;
			separator.ShowAll ();
			
			hpaned1 = hpaned1.ReplaceWithWidget (new HPanedThin (), true);
			vpaned1 = vpaned1.ReplaceWithWidget (new VPanedThin () { HandleWidget = separator }, true);

			revertButton = new DocumentToolButton ("vc-revert-command", GettextCatalog.GetString ("Revert changes from this revision"));
			revertButton.GetNativeWidget<Gtk.Widget> ().Sensitive = false;
			revertButton.Clicked += new EventHandler (RevertRevisionClicked);

			revertToButton = new DocumentToolButton ("vc-revert-command", GettextCatalog.GetString ("Revert to this revision"));
			revertToButton.GetNativeWidget<Gtk.Widget> ().Sensitive = false;
			revertToButton.Clicked += new EventHandler (RevertToRevisionClicked);

			refreshButton = new DocumentToolButton (Gtk.Stock.Refresh, GettextCatalog.GetString ("Refresh"));
			refreshButton.Clicked += new EventHandler (RefreshClicked);

			searchEntry = new SearchEntry ();
			searchEntry.WidthRequest = 200;
			searchEntry.ForceFilterButtonVisible = true;
			searchEntry.EmptyMessage = GettextCatalog.GetString ("Search");
			searchEntry.Changed += HandleSearchEntryFilterChanged;
			searchEntry.Ready = true;
			searchEntry.Show ();

			messageRenderer.Ellipsize = Pango.EllipsizeMode.End;
			TreeViewColumn colRevMessage = new TreeViewColumn ();
			colRevMessage.Title = GettextCatalog.GetString ("Message");
			var graphRenderer = new RevisionGraphCellRenderer ();
			colRevMessage.PackStart (graphRenderer, false);
			colRevMessage.SetCellDataFunc (graphRenderer, GraphFunc);
			
			colRevMessage.PackStart (messageRenderer, true);
			colRevMessage.SetCellDataFunc (messageRenderer, MessageFunc);
			colRevMessage.Sizing = TreeViewColumnSizing.Autosize;
			
			treeviewLog.AppendColumn (colRevMessage);
			colRevMessage.MinWidth = 350;
			colRevMessage.Resizable = true;

			TreeViewColumn colRevDate = new TreeViewColumn (GettextCatalog.GetString ("Date"), textRenderer);
			colRevDate.SetCellDataFunc (textRenderer, DateFunc);
			colRevDate.Resizable = true;
			treeviewLog.AppendColumn (colRevDate);
			
			TreeViewColumn colRevAuthor = new TreeViewColumn ();
			colRevAuthor.Title = GettextCatalog.GetString ("Author");
			colRevAuthor.PackStart (pixRenderer, false);
			colRevAuthor.PackStart (textRenderer, true);
			colRevAuthor.SetCellDataFunc (textRenderer, AuthorFunc);
			colRevAuthor.SetCellDataFunc (pixRenderer, AuthorIconFunc);
			colRevAuthor.Resizable = true;
			treeviewLog.AppendColumn (colRevAuthor);

			TreeViewColumn colRevNum = new TreeViewColumn (GettextCatalog.GetString ("Revision"), textRenderer);
			colRevNum.SetCellDataFunc (textRenderer, RevisionFunc);
			colRevNum.Resizable = true;
			treeviewLog.AppendColumn (colRevNum);

			treeviewLog.Model = logstore;
			treeviewLog.Selection.Changed += TreeSelectionChanged;
			
			treeviewFiles = new FileTreeView ();
			treeviewFiles.DiffLineActivated += HandleTreeviewFilesDiffLineActivated;
			scrolledwindowFiles.Child = treeviewFiles;
			scrolledwindowFiles.ShowAll ();
			
			changedpathstore = new TreeStore (typeof(Xwt.Drawing.Image), typeof (string), // icon/file name
			                                  typeof(Xwt.Drawing.Image), typeof (string), // icon/operation
				typeof (string), // path
				typeof (string), // revision path (invisible)
				typeof (string []) // diff
				);
			
			TreeViewColumn colChangedFile = new TreeViewColumn ();
			var crp = new CellRendererImage ();
			var crt = new CellRendererText ();
			colChangedFile.Title = GettextCatalog.GetString ("File");
			colChangedFile.PackStart (crp, false);
			colChangedFile.PackStart (crt, true);
			colChangedFile.AddAttribute (crp, "image", 2);
			colChangedFile.AddAttribute (crt, "text", 3);
			treeviewFiles.AppendColumn (colChangedFile);
			
			TreeViewColumn colOperation = new TreeViewColumn ();
			colOperation.Title = GettextCatalog.GetString ("Operation");
			colOperation.PackStart (crp, false);
			colOperation.PackStart (crt, true);
			colOperation.AddAttribute (crp, "image", 0);
			colOperation.AddAttribute (crt, "text", 1);
			treeviewFiles.AppendColumn (colOperation);
			
			TreeViewColumn colChangedPath = new TreeViewColumn ();
			colChangedPath.Title = GettextCatalog.GetString ("Path");

			diffRenderer.DrawLeft = true;
			colChangedPath.PackStart (diffRenderer, true);
			colChangedPath.SetCellDataFunc (diffRenderer, SetDiffCellData);
			treeviewFiles.AppendColumn (colChangedPath);
			treeviewFiles.Model = changedpathstore;
			treeviewFiles.TestExpandRow += HandleTreeviewFilesTestExpandRow;
			treeviewFiles.Events |= Gdk.EventMask.PointerMotionMask;
			
			textviewDetails.WrapMode = Gtk.WrapMode.Word;

			labelAuthor.Text = "";
			labelDate.Text = "";
			labelRevision.Text = "";

			vbox2.Remove (scrolledwindow1);
			HeaderBox tb = new HeaderBox ();
			tb.Show ();
			tb.SetMargins (1, 0, 0, 0);
			tb.ShowTopShadow = true;
			tb.ShadowSize = 4;
			tb.SetPadding (8, 8, 8, 8);
			tb.UseChildBackgroundColor = true;
			tb.Add (scrolledwindow1);
			vbox2.PackStart (tb, true, true, 0);

			UpdateStyle ();
			Ide.Gui.Styles.Changed += HandleStylesChanged;
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
			UpdateStyle ();
		}

		void HandleStylesChanged (object sender, EventArgs e)
		{
			UpdateStyle ();
		}

		void UpdateStyle ()
		{
			var c = Style.Base (StateType.Normal).ToXwtColor ();
			c.Light *= 0.8;
			commitBox.ModifyBg (StateType.Normal, c.ToGdkColor ());

			var tcol = Styles.LogView.CommitDescBackgroundColor.ToGdkColor ();
			textviewDetails.ModifyBase (StateType.Normal, tcol);
			scrolledwindow1.ModifyBase (StateType.Normal, tcol);
		}

		internal void SetToolbar (DocumentToolbar toolbar)
		{
			if (info.Repository.SupportsRevertRevision)
				toolbar.Add (revertButton);

			if (info.Repository.SupportsRevertToRevision)
				toolbar.Add (revertToButton);
			toolbar.Add (refreshButton);

			Gtk.HBox a = new Gtk.HBox ();
			a.PackEnd (searchEntry, false, false, 0);
			toolbar.Add (a, true);

			toolbar.ShowAll ();
		}

		static void SetLogSearchFilter (ListStore store, string filter)
		{
			TreeIter iter;
			if (store.GetIterFirst (out iter))
				store.SetValue (iter, 1, filter);
		}
		
		bool filtering;
		void HandleSearchEntryFilterChanged (object sender, EventArgs e)
		{
			if (filtering)
				return;
			filtering = true;
			GLib.Timeout.Add (100, delegate {
				filtering = false;
				currentFilter = searchEntry.Entry.Text;
				SetLogSearchFilter (logstore, currentFilter);
				UpdateHistory ();
				return false;
			});
		}
		
		public void ShowLoading ()
		{
			scrolledLoading.Show ();
			scrolledLog.Hide ();
		}
		
		void RevertToRevisionClicked (object src, EventArgs args)
		{
			Revision d = SelectedRevision;
			if (RevertRevisionsCommands.RevertToRevision (info.Repository, info.Item.Path, d, false))
				VersionControlService.SetCommitComment (info.Item.Path, 
				  GettextCatalog.GetString ("(Revert to revision {0})", d.ToString ()), true);
		}
		
		void RevertRevisionClicked (object src, EventArgs args)
		{
			Revision d = SelectedRevision;
			if (RevertRevisionsCommands.RevertRevision (info.Repository, info.Item.Path, d, false))
				VersionControlService.SetCommitComment (info.Item.Path, 
				  GettextCatalog.GetString ("(Revert revision {0})", d.ToString ()), true);
		}

		void RefreshClicked (object src, EventArgs args)
		{
			ShowLoading ();
			info.Start (true);
			revertButton.GetNativeWidget<Gtk.Widget> ().Sensitive = revertToButton.GetNativeWidget<Gtk.Widget> ().Sensitive = false;
		}

		async void HandleTreeviewFilesDiffLineActivated (object sender, EventArgs e)
		{
			TreePath[] paths = treeviewFiles.Selection.GetSelectedRows ();
			
			if (paths.Length != 1)
				return;
			
			TreeIter iter;
			changedpathstore.GetIter (out iter, paths[0]);
			
			string fileName = (string)changedpathstore.GetValue (iter, colPath);
			int line = diffRenderer.GetSelectedLine (paths[0]);
			if (line == -1)
				line = 1;

			var proj = IdeApp.Workspace.GetProjectsContainingFile (fileName).FirstOrDefault ();
			var doc = await IdeApp.Workbench.OpenDocument (fileName, proj, line, 0, OpenDocumentOptions.Default | OpenDocumentOptions.OnlyInternalViewer);
			int i = 1;
			foreach (var content in doc.Window.SubViewContents) {
				DiffView diffView = content as DiffView;
				if (diffView != null) {
					doc.Window.SwitchView (i);
					diffView.ComparisonWidget.info.RunAfterUpdate (delegate {
						diffView.ComparisonWidget.SetRevision (diffView.ComparisonWidget.OriginalEditor, SelectedRevision.GetPrevious ());
						diffView.ComparisonWidget.SetRevision (diffView.ComparisonWidget.DiffEditor, SelectedRevision);
						
						diffView.ComparisonWidget.DiffEditor.Caret.Location = new DocumentLocation (line, 1);
						diffView.ComparisonWidget.DiffEditor.CenterToCaret ();
					});
					break;
				}
				i++;
			}
		}

		const int colOperation = 4;
		const int colPath = 5;
		const int colDiff = 6;
		
		void HandleTreeviewFilesTestExpandRow (object o, TestExpandRowArgs args)
		{
			TreeIter iter;
			if (changedpathstore.IterChildren (out iter, args.Iter)) {
				string[] diff = changedpathstore.GetValue (iter, colDiff) as string[];
				if (diff != null)
					return;

				string path = (string)changedpathstore.GetValue (args.Iter, colPath);

				changedpathstore.SetValue (iter, colDiff, new string[] { GettextCatalog.GetString ("Loading data...") });
				var rev = SelectedRevision;
				ThreadPool.QueueUserWorkItem (delegate {
					string text = "";
					try {
						text = info.Repository.GetTextAtRevision (path, rev);
					} catch (Exception e) {
						Application.Invoke ((o2, a2) => {
							LoggingService.LogError ("Error while getting revision text", e);
							MessageService.ShowError (
								GettextCatalog.GetString ("Error while getting revision text."),
								GettextCatalog.GetString ("The file may not be part of the working copy.")
							);
						});
						return;
					}
					Revision prevRev = null;
					try {
						prevRev = rev.GetPrevious ();
					} catch (Exception e) {
						Application.Invoke ((o2, a2) => {
							MessageService.ShowError (GettextCatalog.GetString ("Error while getting previous revision."), e);
						});
						return;
					}
					string[] lines;
					// Indicator that the file was binary
					if (text == null) {
						lines = new [] { GettextCatalog.GetString (" Binary files differ") };
					} else {
						var changedDocument = Mono.TextEditor.TextDocument.CreateImmutableDocument (text);
						if (prevRev == null) {
							lines = new string [changedDocument.LineCount];
							for (int i = 0; i < changedDocument.LineCount; i++) {
								lines[i] = "+ " + changedDocument.GetLineText (i + 1).TrimEnd ('\r','\n');
							}
						} else {
							string prevRevisionText = "";
							try {
								prevRevisionText = info.Repository.GetTextAtRevision (path, prevRev);
							} catch (Exception e) {
								Application.Invoke ((o2, a2) => {
									LoggingService.LogError ("Error while getting revision text", e);
									MessageService.ShowError (
										GettextCatalog.GetString ("Error while getting revision text."),
										GettextCatalog.GetString ("The file may not be part of the working copy.")
									);
								});
								return;
							}

							if (String.IsNullOrEmpty (text)) {
								if (!String.IsNullOrEmpty (prevRevisionText)) {
									lines = new string [changedDocument.LineCount];
									for (int i = 0; i < changedDocument.LineCount; i++) {
										lines [i] = "- " + changedDocument.GetLineText (i + 1).TrimEnd ('\r','\n');
									}
								}
							}

							var originalDocument = Mono.TextEditor.TextDocument.CreateImmutableDocument (prevRevisionText);
							originalDocument.FileName = GettextCatalog.GetString ("Revision {0}", prevRev);
							changedDocument.FileName = GettextCatalog.GetString ("Revision {0}", rev);
							lines = Mono.TextEditor.Utils.Diff.GetDiffString (originalDocument, changedDocument).Split ('\n');
						}
					}
					Application.Invoke ((o2, a2) => {
						changedpathstore.SetValue (iter, colDiff, lines);
					});
				});
			}
		}

/*		void FileSelectionChanged (object sender, EventArgs e)
		{
			Revision rev = SelectedRevision;
			if (rev == null) {
				diffWidget.ComparisonWidget.OriginalEditor.Text = "";
				diffWidget.ComparisonWidget.DiffEditor.Text = "";
				return;
			}
			TreeIter iter;
			if (!treeviewFiles.Selection.GetSelected (out iter))
				return;
			string path = (string)changedpathstore.GetValue (iter, colPath);
			ThreadPool.QueueUserWorkItem (delegate {
				string text = info.Repository.GetTextAtRevision (path, rev);
				string prevRevision = text; // info.Repository.GetTextAtRevision (path, rev.GetPrevious ());
				
				Application.Invoke (delegate {
					diffWidget.ComparisonWidget.MimeType = DesktopService.GetMimeTypeForUri (path);
					diffWidget.ComparisonWidget.OriginalEditor.Text = prevRevision;
					diffWidget.ComparisonWidget.DiffEditor.Text = text;
					diffWidget.ComparisonWidget.CreateDiff ();
				});
			});
		}*/

		protected override void OnDestroyed ()
		{
			revertButton.Clicked -= RevertRevisionClicked;
			revertToButton.Clicked -= RevertToRevisionClicked;
			refreshButton.Clicked -= RefreshClicked;
			Ide.Gui.Styles.Changed -= HandleStylesChanged;

			diffRenderer.Dispose ();
			messageRenderer.Dispose ();
			textRenderer.Dispose ();
			treeviewFiles.Dispose ();

			base.OnDestroyed ();
		}
		
		static void DateFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText renderer = (CellRendererText)cell;
			var rev = (Revision)model.GetValue (iter, 0);
			string day;

			// Grab today's day and the start of tomorrow's day to make Today/Yesterday calculations.
			var now = DateTime.Now;
			var age = new DateTime (now.Year, now.Month, now.Day).AddDays(1) - rev.Time;
			if (age.Days >= 0 && age.Days < 1) { // Check whether it's a commit that's less than a day away. Also discard future commits.
				day = GettextCatalog.GetString ("Today");
			} else if (age.Days < 2) { // Check whether it's a commit from yesterday.
				day = GettextCatalog.GetString ("Yesterday");
			} else {
				day = rev.Time.ToShortDateString ();
			}
			renderer.Text = string.Format ("{0} {1:HH:mm}", day, rev.Time);
		}	
		
		static void GraphFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			var renderer = (RevisionGraphCellRenderer)cell;
			Gtk.TreeIter node;
			model.GetIterFirst (out node);
			
			renderer.FirstNode = node.Equals (iter);
			model.IterNthChild (out node, model.IterNChildren () - 1);
			renderer.LastNode =  node.Equals (iter);
		}

		static string GetCurrentFilter (Gtk.TreeModel model)
		{
			TreeIter filterIter;
			string filter = string.Empty;
			if (model.GetIterFirst (out filterIter))
				filter = (string)model.GetValue (filterIter, 1);

			return filter;
		}
		
		static void MessageFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			string filter = GetCurrentFilter (model);

			CellRendererText renderer = (CellRendererText)cell;
			var rev = (Revision)model.GetValue (iter, 0);
			if (string.IsNullOrEmpty (rev.Message)) {
				renderer.Text = GettextCatalog.GetString ("(No message)");
			} else {
				string message = Revision.FormatMessage (rev.Message);
				int idx = message.IndexOf ('\n');
				if (idx > 0)
					message = message.Substring (0, idx);
				if (string.IsNullOrEmpty (filter))
					renderer.Text = message;
				else
					renderer.Markup = EscapeWithFilterMarker (message, filter);
			}
		}
		
		static void AuthorFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			string filter = GetCurrentFilter (model);

			CellRendererText renderer = (CellRendererText)cell;
			var rev = (Revision)model.GetValue (iter, 0);
			string author = rev.Author;
			if (string.IsNullOrEmpty (author))
				return;
			int idx = author.IndexOf ("<", StringComparison.Ordinal);
			if (idx >= 0 && idx < author.IndexOf (">", StringComparison.Ordinal))
				author = author.Substring (0, idx).Trim ();
			if (string.IsNullOrEmpty (filter))
				renderer.Text = author;
			else
				renderer.Markup = EscapeWithFilterMarker (author, filter);
		}
		
		static void AuthorIconFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererImage renderer = (CellRendererImage)cell;
			var rev = (Revision)model.GetValue (iter, 0);
			if (string.IsNullOrEmpty (rev.Email))
				return;
			ImageLoader img = ImageService.GetUserIcon (rev.Email, 16);

			renderer.Image = img.Image;
			if (img.Downloading) {
				img.Completed += (sender, e) => {
					renderer.Image = img.Image;
					if (((ListStore)model).IterIsValid (iter))
						model.EmitRowChanged (model.GetPath (iter), iter);
				};
			}
		}
		
		static void RevisionFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			string filter = GetCurrentFilter (model);

			CellRendererText renderer = (CellRendererText)cell;
			var rev = model.GetValue (iter, 0).ToString ();
			if (string.IsNullOrEmpty (filter))
				renderer.Text = rev;
			else
				renderer.Markup = EscapeWithFilterMarker (rev, filter);
		}
		
		static void SetDiffCellData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererDiff rc = (CellRendererDiff)cell;
			string[] lines = (string[])model.GetValue (iter, colDiff);
			if (lines == null)
				lines = new string[] { (string)model.GetValue (iter, colOperation) };

			rc.InitCell (tree_column.TreeView, ((TreeStore)model).IterDepth (iter) != 0, lines, model.GetPath (iter));
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			var old = Allocation;
			base.OnSizeAllocated (allocation);
			if (old.Width != allocation.Width || old.Height != allocation.Height) {
				hpaned1.Position = allocation.Width - 380;
				vpaned1.Position = allocation.Height / 2;
			}
		}
		
		public Revision SelectedRevision {
			get {
				TreeIter iter;
				if (!treeviewLog.Selection.GetSelected (out iter))
					return null;
				return (Revision)logstore.GetValue (iter, 0);
			}
			set {
				TreeIter iter;
				if (!treeviewLog.Model.GetIterFirst (out iter))
					return;
				do {
					var rev = (Revision)logstore.GetValue (iter, 0);
					if (rev.ToString () == value.ToString ()) {
						treeviewLog.Selection.SelectIter (iter);
						TreePath path = logstore.GetPath (iter);
						treeviewLog.ScrollToCell (path, treeviewLog.Columns[0], true, 0, 0);
						treeviewLog.SetCursorOnCell (path, treeviewLog.Columns[0], textRenderer, true);
						return;
					}
				} while (treeviewLog.Model.IterNext (ref iter));
			}
		}
		
		void TreeSelectionChanged (object o, EventArgs args)
		{
			Revision d = SelectedRevision;
			changedpathstore.Clear ();
			textviewDetails.Buffer.Clear ();
			
			if (d == null)
				return;

			revertButton.GetNativeWidget<Gtk.Widget> ().Sensitive = revertToButton.GetNativeWidget<Gtk.Widget> ().Sensitive = true;
			Gtk.TreeIter selectIter = Gtk.TreeIter.Zero;
			bool select = false;
			foreach (RevisionPath rp in info.Repository.GetRevisionChanges (d)) {
				Xwt.Drawing.Image actionIcon;
				string action = null;
				if (rp.Action == RevisionAction.Add) {
					action = GettextCatalog.GetString ("Add");
					actionIcon = ImageService.GetIcon (Gtk.Stock.Add, Gtk.IconSize.Menu);
				} else if (rp.Action == RevisionAction.Delete) {
					action = GettextCatalog.GetString ("Delete");
					actionIcon = ImageService.GetIcon (Gtk.Stock.Remove, Gtk.IconSize.Menu);
				} else if (rp.Action == RevisionAction.Modify) {
					action = GettextCatalog.GetString ("Modify");
					actionIcon = ImageService.GetIcon ("gtk-edit", Gtk.IconSize.Menu);
				} else if (rp.Action == RevisionAction.Replace) {
					action = GettextCatalog.GetString ("Replace");
					actionIcon = ImageService.GetIcon ("gtk-edit", Gtk.IconSize.Menu);
				} else {
					action = rp.ActionDescription;
					actionIcon = ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.Empty, Gtk.IconSize.Menu);
				}
				Xwt.Drawing.Image fileIcon = DesktopService.GetIconForFile (rp.Path, Gtk.IconSize.Menu);
				var iter = changedpathstore.AppendValues (actionIcon, action, fileIcon, System.IO.Path.GetFileName (rp.Path), System.IO.Path.GetDirectoryName (rp.Path), rp.Path, null);
				changedpathstore.AppendValues (iter, null, null, null, null, null, rp.Path, null);
				if (rp.Path == preselectFile) {
					selectIter = iter;
					select = true;
				}
			}
			if (!string.IsNullOrEmpty (d.Email)) {
				imageUser.Show ();
				imageUser.LoadUserIcon (d.Email, 32);
			}
			else
				imageUser.Hide ();
			
			labelAuthor.Text = d.Author;
			labelDate.Text = d.Time.ToString ();
			string rev = d.Name;
			if (rev.Length > 15) {
				currentRevisionShortened = true;
				rev = d.ShortName;
			} else
				currentRevisionShortened = false;
			
			labelRevision.Text = GettextCatalog.GetString ("Revision: {0}", rev);
			textviewDetails.Buffer.Text = d.Message;
			
			if (select) {
				treeviewFiles.Selection.SelectIter (selectIter);
				treeviewFiles.ExpandRow (treeviewFiles.Model.GetPath (selectIter), true);
			}
		}
		
		void UpdateHistory ()
		{
			scrolledLoading.Hide ();
			scrolledLog.Show ();
			treeviewLog.FreezeChildNotify ();
			logstore.Clear ();
			var h = History;
			if (h == null)
				return;
			foreach (var rev in h) {
				if (MatchesFilter (rev))
					logstore.InsertWithValues (-1, rev, string.Empty);
			}
			SetLogSearchFilter (logstore, currentFilter);
			treeviewLog.ThawChildNotify ();
		}
		
		bool MatchesFilter (Revision rev)
		{
			if (string.IsNullOrEmpty (currentFilter))
				return true;
			if (rev.Author.IndexOf (currentFilter,StringComparison.CurrentCultureIgnoreCase) != -1)
				return true;
			if (rev.Email.IndexOf (currentFilter,StringComparison.CurrentCultureIgnoreCase) != -1)
				return true;
			if (rev.Message.IndexOf (currentFilter,StringComparison.CurrentCultureIgnoreCase) != -1)
				return true;
			if (rev.Name.IndexOf (currentFilter,StringComparison.CurrentCultureIgnoreCase) != -1)
				return true;
			if (rev.ShortName.IndexOf (currentFilter,StringComparison.CurrentCultureIgnoreCase) != -1)
				return true;
			return false;
		}
		
		static string EscapeWithFilterMarker (string txt, string filter)
		{
			if (string.IsNullOrEmpty (filter))
				return GLib.Markup.EscapeText (txt);
			
			int i = txt.IndexOf (filter, StringComparison.CurrentCultureIgnoreCase);
			if (i == -1)
				return GLib.Markup.EscapeText (txt);
			
			StringBuilder sb = new StringBuilder ();
			int last = 0;
			while (i != -1) {
				sb.Append (GLib.Markup.EscapeText (txt.Substring (last, i - last)));
				sb.Append ("<span color='").Append (Styles.LogView.SearchSnippetTextColor).Append ("'>").Append (txt, i, filter.Length).Append ("</span>");
				last = i + filter.Length;
				i = txt.IndexOf (filter, last, StringComparison.CurrentCultureIgnoreCase);
			}
			if (last < txt.Length)
				sb.Append (GLib.Markup.EscapeText (txt.Substring (last, txt.Length - last)));
			return sb.ToString ();
		}
		
		[GLib.ConnectBeforeAttribute]
		protected virtual void OnLabelRevisionButtonPressEvent (object o, Gtk.ButtonPressEventArgs args)
		{
			if (currentRevisionShortened) {
				Revision d = SelectedRevision;
				labelRevision.Text = GettextCatalog.GetString ("Revision: {0}", d.Name);
				currentRevisionShortened = false;
			}
		}

		internal string DiffText {
			get {
				TreeIter iter;
				if (treeviewFiles.Selection.GetSelected (out iter)) {
					string [] items = changedpathstore.GetValue (iter, colDiff) as string [];
					if (items != null)
						return String.Join (Environment.NewLine, items);
				}
				return null;
			}
		}
	}
}
