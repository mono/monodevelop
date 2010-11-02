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
using System.IO;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Text;
using System.Threading;

namespace MonoDevelop.VersionControl.Views
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class LogWidget : Gtk.Bin
	{
		MonoDevelop.VersionControl.Revision[] history;
		public Revision[] History {
			get {
				return history;
			}
			set {
				history = value;
				UpdateHistory ();
			}
		}
		
		public Toolbar CommandBar {
			get {
				return commandBar;
			}
		}
		
		
		ListStore logstore = new ListStore (typeof (Revision));
		FileTreeView treeviewFiles;
		TreeStore changedpathstore;
		
		VersionControlDocumentInfo info;
		string preselectFile;
		CellRendererDiff diffRenderer = new CellRendererDiff ();
		CellRendererText messageRenderer = new CellRendererText ();
		CellRendererText textRenderer = new CellRendererText ();
		
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
					cr.Arc (cell_area.X + cell_area.Width / 2, cell_area.Y + cell_area.Height / 2, 5, 0, 2 * Math.PI);
					cr.Color = new Cairo.Color (0, 0, 0);
					cr.Stroke ();
					double h = (cell_area.Height - 10) / 2;
					if (!FirstNode) {
						cr.MoveTo (cell_area.X + cell_area.Width / 2, cell_area.Y - 1);
						cr.LineTo (cell_area.X + cell_area.Width / 2, cell_area.Y + h);
						cr.Stroke ();
					}
					
					if (!LastNode) {
						cr.MoveTo (cell_area.X + cell_area.Width / 2, cell_area.Y + cell_area.Height + 1);
						cr.LineTo (cell_area.X + cell_area.Width / 2, cell_area.Y + cell_area.Height - h);
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
				this.preselectFile = info.Document.FileName;
			
			messageRenderer.Ellipsize = Pango.EllipsizeMode.End;
			TreeViewColumn colRevMessage = new TreeViewColumn ();
			colRevMessage.Title = GettextCatalog.GetString ("Message");
			var graphRenderer = new RevisionGraphCellRenderer ();
			colRevMessage.PackStart (graphRenderer, false);
			colRevMessage.SetCellDataFunc (graphRenderer, GraphFunc);
			
			colRevMessage.PackStart (messageRenderer, true);
			colRevMessage.SetCellDataFunc (messageRenderer, MessageFunc);
			colRevMessage.Sizing = TreeViewColumnSizing.Autosize;
			
			colRevMessage.MinWidth = 350;
			colRevMessage.Resizable = true;
			treeviewLog.AppendColumn (colRevMessage);

			
			TreeViewColumn colRevDate = new TreeViewColumn (GettextCatalog.GetString ("Date"), textRenderer);
			colRevDate.SetCellDataFunc (textRenderer, DateFunc);
			colRevDate.Resizable = true;
			treeviewLog.AppendColumn (colRevDate);
			
			TreeViewColumn colRevAuthor = new TreeViewColumn (GettextCatalog.GetString ("Author"), textRenderer);
			colRevAuthor.SetCellDataFunc (textRenderer, AuthorFunc);
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
			
			changedpathstore = new TreeStore (typeof(Gdk.Pixbuf), typeof (string), // icon/file name
				typeof(Gdk.Pixbuf), typeof (string), // icon/operation
				typeof (string), // path
				typeof (string), // revision path (invisible)
				typeof (string[]) // diff
				);
			
			TreeViewColumn colChangedFile = new TreeViewColumn ();
			var crp = new CellRendererPixbuf ();
			var crt = new CellRendererText ();
			colChangedFile.Title = GettextCatalog.GetString ("File");
			colChangedFile.PackStart (crp, false);
			colChangedFile.PackStart (crt, true);
			colChangedFile.AddAttribute (crp, "pixbuf", 2);
			colChangedFile.AddAttribute (crt, "text", 3);
			treeviewFiles.AppendColumn (colChangedFile);
			
			TreeViewColumn colOperation = new TreeViewColumn ();
			colOperation.Title = GettextCatalog.GetString ("Operation");
			colOperation.PackStart (crp, false);
			colOperation.PackStart (crt, true);
			colOperation.AddAttribute (crp, "pixbuf", 0);
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
		}

		void HandleTreeviewFilesDiffLineActivated (object sender, EventArgs e)
		{
			TreePath[] paths = treeviewFiles.Selection.GetSelectedRows ();
			
			if (paths.Length != 1)
				return;
			
			TreeIter iter;
			changedpathstore.GetIter (out iter, paths[0]);
			
			string fileName = (string)changedpathstore.GetValue (iter, colPath);
			int line = diffRenderer.GetSelectedLine (paths[0]);
			var doc = IdeApp.Workbench.OpenDocument (fileName, line, 0, true);
			int i = 1;
			foreach (var content in doc.Window.SubViewContents) {
				DiffView diffView = content as DiffView;
				if (diffView != null) {
					doc.Window.SwitchView (i);
					diffView.ComparisonWidget.info.RunAfterUpdate (delegate {
						diffView.ComparisonWidget.SetRevision (diffView.ComparisonWidget.OriginalEditor, SelectedRevision.GetPrevious ());
						diffView.ComparisonWidget.SetRevision (diffView.ComparisonWidget.DiffEditor, SelectedRevision);
						
						diffView.ComparisonWidget.DiffEditor.Caret.Location = new Mono.TextEditor.DocumentLocation (line, 1);
						diffView.ComparisonWidget.DiffEditor.CenterToCaret ();
					});
					break;
				}
				i++;
			}
		}
		
		const int colPath = 5;
		const int colDiff = 6;
		
		void HandleTreeviewFilesTestExpandRow (object o, TestExpandRowArgs args)
		{
			string[] diff = changedpathstore.GetValue (args.Iter, colDiff) as string[];
			if (diff != null) {
				return;
			}
			TreeIter iter;
			if (changedpathstore.IterChildren (out iter, args.Iter)) {
				string path = (string)changedpathstore .GetValue (args.Iter, colPath);
				changedpathstore.SetValue (iter, colDiff, new string[] { GettextCatalog.GetString ("Loading data...") });
				var rev = SelectedRevision;
				ThreadPool.QueueUserWorkItem (delegate {
					string text;
					try {
						text = info.Repository.GetTextAtRevision (path, rev);
					} catch (Exception e) {
						Application.Invoke (delegate {
							LoggingService.LogError ("Error while getting revision text", e);
							MessageService.ShowError ("Error while getting revision text.", "The file may not be part of the working copy.");
						});
						return;
					}
					Revision prevRev = null;
					try {
						prevRev = rev.GetPrevious ();
					} catch (Exception e) {
						Application.Invoke (delegate {
							LoggingService.LogError ("Error while getting previous revision", e);
							MessageService.ShowException (e, "Error while getting previous revision.");
						});
						return;
					}
					string[] lines;
					var changedDocument = new Mono.TextEditor.Document (text);
					if (prevRev == null) {
						lines = new string[changedDocument.LineCount];
						for (int i = 0; i < changedDocument.LineCount; i++) {
							lines[i] = "+ " + changedDocument.GetLineText (i);
						}
						
					} else {
						string prevRevisionText = info.Repository.GetTextAtRevision (path, prevRev);
						
						var originalDocument = new Mono.TextEditor.Document (prevRevisionText);
						originalDocument.FileName = "Revision " + prevRev.ToString ();
						changedDocument.FileName = "Revision " + rev.ToString ();
						lines = Mono.TextEditor.Utils.Diff.GetDiffString (originalDocument, changedDocument).Split ('\n');
					}
					Application.Invoke (delegate {
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
			string path = (string)changedpathstore.GetValue (iter, 5);
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
		
		public override void Destroy ()
		{
			base.Destroy ();
			logstore.Dispose ();
			changedpathstore.Dispose ();
			
			diffRenderer.Dispose ();
			messageRenderer.Dispose ();
			textRenderer.Dispose ();
		}
		
		static void DateFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText renderer = (CellRendererText)cell;
			var rev = (Revision)model.GetValue (iter, 0);
			string day;
			var age = rev.Time - DateTime.Now;
			if (age.TotalDays == 0) {
				day = GettextCatalog.GetString ("Today");
			} else if (age.TotalDays == 1) {
				day = GettextCatalog.GetString ("Yesterday");
			} else {
				day = rev.Time.ToShortDateString ();
			}
			string time = rev.Time.ToString ("HH:MM");
			renderer.Text = day + " " + time;
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
		
		static void MessageFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText renderer = (CellRendererText)cell;
			var rev = (Revision)model.GetValue (iter, 0);
			if (string.IsNullOrEmpty (rev.Message)) {
				renderer.Text = GettextCatalog.GetString ("(No message)");
			} else {
				string message = BlameWidget.FormatMessage (rev.Message);
				int idx = message.IndexOf ('\n');
				if (idx > 0)
					message = message.Substring (0, idx);
				renderer.Text = message;
			}
		}
		
		static void AuthorFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText renderer = (CellRendererText)cell;
			var rev = (Revision)model.GetValue (iter, 0);
			string author = rev.Author;
			int idx = author.IndexOf ("<");
			if (idx >= 0 && idx < author.IndexOf (">"))
				author = author.Substring (0, idx).Trim ();
			renderer.Text = author;
		}
		
		static void RevisionFunc (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererText renderer = (CellRendererText)cell;
			var rev = (Revision)model.GetValue (iter, 0);
			renderer.Text = rev.ToString ();
		}
		
		void SetDiffCellData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererDiff rc = (CellRendererDiff)cell;
			string[] lines = (string[])changedpathstore.GetValue (iter, colDiff);
			if (lines == null)
				lines = new string[] { (string)changedpathstore.GetValue (iter, 4) };
			rc.InitCell (treeviewFiles, changedpathstore.IterDepth (iter) != 0, lines, changedpathstore.GetPath (iter));
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
			Gtk.TreeIter selectIter = Gtk.TreeIter.Zero;
			bool select = false;
			foreach (RevisionPath rp in d.ChangedFiles) {
				Gdk.Pixbuf actionIcon;
				string action = null;
				if (rp.Action == RevisionAction.Add) {
					action = GettextCatalog.GetString ("Add");
					actionIcon = ImageService.GetPixbuf (Gtk.Stock.Add, Gtk.IconSize.Menu);
				} else if (rp.Action == RevisionAction.Delete) {
					action = GettextCatalog.GetString ("Delete");
					actionIcon = ImageService.GetPixbuf (Gtk.Stock.Remove, Gtk.IconSize.Menu);
				} else if (rp.Action == RevisionAction.Modify) {
					action = GettextCatalog.GetString ("Modify");
					actionIcon = ImageService.GetPixbuf ("gtk-edit", Gtk.IconSize.Menu);
				} else if (rp.Action == RevisionAction.Replace) {
					action = GettextCatalog.GetString ("Replace");
					actionIcon = ImageService.GetPixbuf ("gtk-edit", Gtk.IconSize.Menu);
				} else {
					action = rp.ActionDescription;
					actionIcon = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.Empty, Gtk.IconSize.Menu);
				}
				Gdk.Pixbuf fileIcon = DesktopService.GetPixbufForFile (rp.Path, Gtk.IconSize.Menu);
				var iter = changedpathstore.AppendValues (actionIcon, action, fileIcon, System.IO.Path.GetFileName (rp.Path), System.IO.Path.GetDirectoryName (rp.Path), rp.Path, null);
				changedpathstore.AppendValues (iter, null, null, null, null, null, rp.Path, null);
				if (rp.Path == preselectFile) {
					selectIter = iter;
					select = true;
				}
			}
			StringBuilder sb = new StringBuilder ();
			sb.AppendLine (string.Format (GettextCatalog.GetString ("Author: {0}"), d.Author));
			sb.AppendLine (string.Format (GettextCatalog.GetString ("Date: {0}"), d.Time));
			sb.AppendLine (string.Format (GettextCatalog.GetString ("Revision: {0}"), d.ToString ()));
			sb.AppendLine ();
			sb.AppendLine (d.Message);
			
			textviewDetails.Buffer.Text = sb.ToString ();
			
			if (select)
				treeviewFiles.Selection.SelectIter (selectIter);
		}
		
		void UpdateHistory ()
		{
			var h = History;
			if (h == null)
				return;
			logstore.Clear ();
			foreach (var rev in h) {
				logstore.AppendValues (rev);
			}
		}
	}
}
