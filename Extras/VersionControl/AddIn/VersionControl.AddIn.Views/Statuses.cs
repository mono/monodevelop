using System;
using System.Collections;
using System.IO;
using System.Threading;
using System.Text;
using System.Collections.Specialized;

using Gtk;
using VersionControl.Service;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.SourceEditor.Gui;
using MonoDevelop.Ide.Gui;

namespace VersionControl.AddIn.Views 
{
	public class StatusView : BaseView 
	{
		string filepath;
		Repository vc;
		
		Widget widget;
		Toolbar commandbar;
		VBox main;
		Label status;
		Gtk.ToolButton showRemoteStatus;
		Gtk.ToolButton buttonCommit;
		
		TreeView filelist;
		TreeViewColumn colCommit, colRemote;
		TreeStore filestore;
		ScrolledWindow scroller;
		
		Box commitBox;
		TextView commitText;
		Gtk.Label labelCommit;
		
		VersionInfo[] statuses;
		Hashtable markedCommit = new Hashtable();
		
		bool remoteStatus = false;
		bool diffRequested = false;
		bool diffRunning = false;
		Exception diffException;
		DiffInfo[] difs;
		Hashtable comments = new Hashtable ();
		bool updatingComment;
		
		const int ColIcon = 0;
		const int ColStatus = 1;
		const int ColPath = 2;
		const int ColRemoteStatus = 3;
		const int ColCommit = 4;
		const int ColFilled = 5;
		const int ColFullPath = 6;
		const int ColShowToggle = 7;
		const int ColShowComment = 8;
		
		public static bool Show (Repository vc, string path, bool test)
		{
			if (vc.IsVersioned(path)) {
				if (test) return true;
				StatusView d = new StatusView(path, vc);
				MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (d, true);
				return true;
			}
			return false;
		}
		
		public StatusView (string filepath, Repository vc) 
			: base(Path.GetFileName(filepath) + " Status") 
		{
			this.vc = vc;
			this.filepath = filepath;
			
			main = new VBox(false, 6);
			widget = main;
			
			commandbar = new Toolbar ();
			commandbar.ToolbarStyle = Gtk.ToolbarStyle.BothHoriz;
			commandbar.IconSize = Gtk.IconSize.Menu;
			main.PackStart(commandbar, false, false, 0);
			
			buttonCommit = new Gtk.ToolButton (new Gtk.Image ("vc-commit", Gtk.IconSize.Menu), "Commit...");
			buttonCommit.IsImportant = true;
			buttonCommit.Clicked += new EventHandler(OnCommitClicked);
			commandbar.Insert (buttonCommit, -1);
			
			showRemoteStatus = new Gtk.ToolButton (new Gtk.Image ("vc-remote-status", Gtk.IconSize.Menu), "Show Remote Status");
			showRemoteStatus.IsImportant = true;
			showRemoteStatus.Clicked += new EventHandler(OnShowRemoteStatusClicked);
			commandbar.Insert (showRemoteStatus, -1);
			
			Gtk.ToolButton btnExpand = new Gtk.ToolButton (null, "Expand All");
			btnExpand.IsImportant = true;
			btnExpand.Clicked += new EventHandler (OnExpandAll);
			commandbar.Insert (btnExpand, -1);
			
			Gtk.ToolButton btnCollapse = new Gtk.ToolButton (null, "Collapse All");
			btnCollapse.IsImportant = true;
			btnCollapse.Clicked += new EventHandler (OnCollapseAll);
			commandbar.Insert (btnCollapse, -1);
			
			status = new Label("");
			main.PackStart(status, false, false, 0);
			
			scroller = new ScrolledWindow();
			scroller.ShadowType = Gtk.ShadowType.In;
			filelist = new TreeView();
			filelist.Selection.Mode = SelectionMode.Multiple;
			scroller.Add(filelist);
			scroller.HscrollbarPolicy = PolicyType.Automatic;
			scroller.VscrollbarPolicy = PolicyType.Automatic;
			filelist.RowActivated += new RowActivatedHandler(OnRowActivated);
			
			CellRendererToggle cellToggle = new CellRendererToggle();
			cellToggle.Toggled += new ToggledHandler(OnCommitToggledHandler);
			CellRendererPixbuf crc = new CellRendererPixbuf();
			crc.StockId = "vc-comment";
			colCommit = new TreeViewColumn ();
			colCommit.Spacing = 2;
			colCommit.Widget = new Gtk.Image ("vc-commit", Gtk.IconSize.Menu);
			colCommit.Widget.Show ();
			colCommit.PackStart (cellToggle, false);
			colCommit.PackStart (crc, false);
			colCommit.AddAttribute (cellToggle, "active", ColCommit);
			colCommit.AddAttribute (cellToggle, "visible", ColShowToggle);
			colCommit.AddAttribute (crc, "visible", ColShowComment);
			
			CellRendererText crt = new CellRendererText();
			CellRendererPixbuf crp = new CellRendererPixbuf();
			TreeViewColumn colStatus = new TreeViewColumn ();
			colStatus.Title = GettextCatalog.GetString ("Status");
			colStatus.PackStart (crp, false);
			colStatus.PackStart (crt, true);
			colStatus.AddAttribute (crp, "pixbuf", ColIcon);
			colStatus.AddAttribute (crt, "text", ColStatus);
			
			TreeViewColumn colFile = new TreeViewColumn ();
			colFile.Title = GettextCatalog.GetString ("File");
			CellRendererDiff cdif = new CellRendererDiff ();
			colFile.PackStart (cdif, true);
			colFile.SetCellDataFunc (cdif, new TreeCellDataFunc (SetDiffCellData));
			
			colRemote = new TreeViewColumn("Remote Status", new CellRendererText(), "text", ColRemoteStatus);
			
			filelist.AppendColumn(colStatus);
			filelist.AppendColumn(colRemote);
			filelist.AppendColumn(colCommit);
			filelist.AppendColumn(colFile);
			
			colRemote.Visible = false;

			filestore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string), typeof (string), typeof(bool), typeof(bool), typeof(string), typeof(bool), typeof (bool));
			filelist.Model = filestore;
			filelist.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
			
			commitBox = new VBox ();
			labelCommit = new Gtk.Label (GettextCatalog.GetString ("Commit message:"));
			labelCommit.Xalign = 0;
			commitBox.PackStart (labelCommit, false, false, 0);
			Gtk.ScrolledWindow frame = new Gtk.ScrolledWindow ();
			frame.ShadowType = ShadowType.In;
			commitText = new TextView ();
			commitText.WrapMode = WrapMode.WordChar;
			commitText.Buffer.Changed += OnCommitTextChanged;
			frame.Add (commitText);
			commitBox.PackStart (frame, true, true, 6);
			
			VPaned paned = new VPaned ();
			paned.Pack1 (scroller, true, true);
			paned.Pack2 (commitBox, false, false);
			main.PackStart (paned, true, true, 0);
			
			main.ShowAll();
			status.Visible = false;
			
			filelist.Selection.Changed += new EventHandler(OnCursorChanged);
			
			StartUpdate();
		}
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		private void StartUpdate ()
		{
			if (!remoteStatus)
				status.Text = "Scanning for changes...";
			else
				status.Text = "Scanning for local and remote changes...";
			
			status.Visible = true;
			scroller.Visible = false;
			
			showRemoteStatus.Sensitive = false;
			buttonCommit.Sensitive = false;
			
			new Worker(vc, filepath, remoteStatus, this).Start();
		}
		
		private void Update ()
		{
			showRemoteStatus.Sensitive = !remoteStatus;
			
			if (statuses.Length == 0) {
				if (!remoteStatus)
					status.Text = "No files have local modifications.";
				else
					status.Text = "No files have local or remote modifications.";
				return;
			}
			
			status.Visible = false;
			scroller.Visible = true;
			
			if (vc.CanCommit(filepath))
				buttonCommit.Sensitive = true;
						
			filestore.Clear();

			colRemote.Visible = remoteStatus;
		
			for (int i = 0; i < statuses.Length; i++) {
				VersionInfo n = statuses[i];
				if (n.Status == VersionStatus.Unversioned)
					continue;
				
				Gdk.Pixbuf statusicon = VersionControlProjectService.LoadIconForStatus(n.Status);
				string lstatus = VersionControlProjectService.GetStatusLabel (n.Status);
				
				string localpath = n.LocalPath.Substring(filepath.Length);
				if (localpath.Length > 0 && localpath[0] == Path.DirectorySeparatorChar) localpath = localpath.Substring(1);
				if (localpath == "") { localpath = "."; } // not sure if this happens
				
				string rstatus = n.RemoteStatus.ToString();
				bool hasComment = GetCommitMessage (n.LocalPath).Length > 0;
				markedCommit [localpath] = true;
				
				TreeIter it = filestore.AppendValues (statusicon, lstatus, GLib.Markup.EscapeText (localpath), rstatus, true, false, n.LocalPath, true, hasComment);
				filestore.AppendValues (it, null, "", "", "", false, true, n.LocalPath, false, hasComment);
			}
		}
		
		string[] GetCurrentFiles ()
		{
			TreePath[] paths = filelist.Selection.GetSelectedRows ();
			string[] files = new string [paths.Length];
			
			for (int n=0; n<paths.Length; n++) {
				TreeIter iter;
				filestore.GetIter (out iter, paths [n]);
				files [n] = (string) filestore.GetValue (iter, ColFullPath);
			}
			return files;
		}
		
		void OnCursorChanged (object o, EventArgs args)
		{
			string[] files = GetCurrentFiles ();
			if (files.Length > 0) {
				commitBox.Visible = true;
				updatingComment = true;
				if (files.Length == 1)
					labelCommit.Text = GettextCatalog.GetString ("Commit message:");
				else
					labelCommit.Text = GettextCatalog.GetString ("Commit message (multiple selection):");
				
				// If all selected files have the same message,
				// then show it so it can be modified. If not, show
				// a blank message
				string msg = GetCommitMessage (files[0]);
				foreach (string file in files) {
					if (msg != GetCommitMessage (file)) {
						commitText.Buffer.Text = "";
						updatingComment = false;
						return;
					}
				}
				commitText.Buffer.Text = msg;
				updatingComment = false;
			} else {
				updatingComment = true;
				commitText.Buffer.Text = "";
				updatingComment = false;
				commitBox.Visible = false;
			}
		}
		
		void OnCommitTextChanged (object o, EventArgs args)
		{
			if (updatingComment)
				return;
				
			string msg = commitText.Buffer.Text;
			
			string[] files = GetCurrentFiles ();
			foreach (string file in files)
				SetCommitMessage (file, msg);

			TreePath[] paths = filelist.Selection.GetSelectedRows ();
			foreach (TreePath path in paths) {
				TreeIter iter;
				filestore.GetIter (out iter, path);
				if (msg.Length > 0)
					filestore.SetValue (iter, ColShowComment, true);
				else
					filestore.SetValue (iter, ColShowComment, false);
			}
		}
		
		string GetCommitMessage (string file)
		{
			string txt = (string) comments [file];
			return txt != null ? txt : "";
		}
		
		void SetCommitMessage (string file, string text)
		{
			comments [file] = text;
		}
		
		void OnRowActivated(object o, RowActivatedArgs args) {
			int index = args.Path.Indices[0];
			VersionInfo node = statuses[index];
			DiffView.Show (vc, node.LocalPath, false);
		}
		
		void OnCommitToggledHandler(object o, ToggledArgs args) {
			TreeIter pos;
			if (!filestore.GetIterFromString(out pos, args.Path))
				return;

			int index = int.Parse(args.Path); // why is it a string?
			string localpath = ((VersionInfo)statuses[index]).LocalPath;
			
			if (markedCommit.ContainsKey(localpath)) {
				markedCommit.Remove(localpath);
			} else {
				markedCommit[localpath] = markedCommit;
			}
			filestore.SetValue (pos, ColCommit, markedCommit.ContainsKey(localpath));
		}
		
		private void OnShowRemoteStatusClicked(object src, EventArgs args) {
			remoteStatus = true;
			StartUpdate();
		}
		
		/*private void OnShowLogClicked(object src, EventArgs args) {
			RevItem file = (RevItem)buttonsShowLog[src];
			LogView.Show(file.Path, false, file.BaseRev, false);
		}
		
		private void OnShowDiffClicked(object src, EventArgs args) {
			RevItem file = (RevItem)buttonsShowDiff[src];
			DiffView.Show(file.Path, false);
		}*/
		
		private void OnCommitClicked(object src, EventArgs args)
		{
			// Get a list of files to commit
			StringCollection list = new StringCollection ();
			ArrayList comms = new ArrayList ();
			TreeIter iter;
			if (filestore.GetIterFirst (out iter)) {
				do {
					if ((bool) filestore.GetValue (iter, ColCommit)) {
						string file = (string) filestore.GetValue (iter, ColFullPath);
						list.Add (file);
						string msg = GetCommitMessage (file);
						
						// Put files with the same commit message together
						// For the commit message, use relative path names
						file = (string) filestore.GetValue (iter, ColPath);
						bool found = false;
						if (msg.Length > 0) {
							foreach (object[] com in comms) {
								if (((string)com[0]) == msg) {
									com[1] = ((string)com[1]) + ", " + file;
									found = true;
									break;
								}
							}
						}
						if (!found) {
							comms.Add (new object[] { msg, file });
						}
					}
				}
				while (filestore.IterNext (ref iter));
			}
			
			StringBuilder message = new StringBuilder ();
			foreach (object[] com in comms) {
				string msg = (string) com[1] + ": " + (string) com[0];
				if (message.Length > 0)
					message.Append ('\n');
				message.Append ("* " + FormatText (msg, 0, 2, 75));
			}
			
			// Nothing to commit
			if (list.Count == 0)
				return;
				
			if (!CommitCommand.Commit (vc, filepath, list, message.ToString (), false))
				return;
				
			// Remove all entries which have been committed
			if (filestore.GetIterFirst (out iter)) {
				while (!iter.Equals (TreeIter.Zero)) {
					if (list.Contains ((string) filestore.GetValue (iter, ColFullPath)))
						filestore.Remove (ref iter);
					else if (!filestore.IterNext (ref iter))
						break;
				}
			}
		}
		
		static string FormatText (string text, int initialLeftMargin, int leftMargin, int maxCols)
		{
			int n = 0;
			int margin = initialLeftMargin;
			
			if (text == "")
				return "";
			
			StringBuilder outs = new StringBuilder ();
			while (n < text.Length)
			{
				int col = margin;
				int lastWhite = -1;
				int sn = n;
				while ((col < maxCols || lastWhite==-1) && n < text.Length) {
					if (char.IsWhiteSpace (text[n]))
						lastWhite = n;
					if (text[n] == '\n') {
						lastWhite = n;
						n++;
						break;
					}
					col++;
					n++;
				}
				
				if (lastWhite == -1 || col < maxCols)
					lastWhite = n;
				else if (col >= maxCols)
					n = lastWhite + 1;
				
				if (outs.Length > 0) outs.Append ('\n');
				
				outs.Append (new String (' ', margin) + text.Substring (sn, lastWhite - sn));
				margin = leftMargin;
			}
			return outs.ToString ();
		}
		
		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) filestore.GetValue (args.Iter, ColFilled);
			if (!filled) {
				filestore.SetValue (args.Iter, ColFilled, true);
				TreeIter iter;
				filestore.IterChildren (out iter, args.Iter);
				SetFileDiff (iter, (string) filestore.GetValue (args.Iter, ColFullPath));
			}
		}
		
		void OnExpandAll (object s, EventArgs args)
		{
			filelist.ExpandAll ();
		}
		
		void OnCollapseAll (object s, EventArgs args)
		{
			filelist.CollapseAll ();
		}
		
		void SetFileDiff (TreeIter iter, string file)
		{
			// If diff information is already loaded, just look for the
			// diff chunk of the file and fill the tree
			if (diffRequested) {
				FillDiffInfo (iter, file);
				return;
			}
			
			filestore.SetValue (iter, ColPath, GLib.Markup.EscapeText (GettextCatalog.GetString ("Loading data...")));
			
			if (diffRunning)
				return;

			// Diff not yet requested. Do it now.
			diffRunning = true;
			
			// Run the diff in a separate thread and update the tree when done
			
			Thread t = new Thread (
				delegate () {
					diffException = null;
					try {
						difs = vc.PathDiff (filepath, null);
					} catch (Exception ex) {
						diffException = ex;
					} finally {
						Gtk.Application.Invoke (OnFillDifs);
					}
				}
			);
			t.IsBackground = true;
			t.Start ();
		}
		
		void FillDiffInfo (TreeIter iter, string file)
		{
			if (difs != null) {
				foreach (DiffInfo di in difs) {
					if (di.FileName == file) {
						filestore.SetValue (iter, ColPath, Colorize (di.Content));
						return;
					}
				}
			}
			filestore.SetValue (iter, ColPath, GLib.Markup.EscapeText (GettextCatalog.GetString ("No differences found")));
		}
		
		string Colorize (string txt)
		{
			txt = GLib.Markup.EscapeText (txt);
			StringReader sr = new StringReader (txt);
			StringBuilder sb = new StringBuilder ();
			string line;
			while ((line = sr.ReadLine ()) != null) {
				if (line.Length > 0) {
					char c = line [0];
					if (c == '-') {
						line = "<span foreground='red'>" + line + "</span>";
					} else if (c == '+')
						line = "<span foreground='blue'>" + line + "</span>";
				}
				sb.Append (line).Append ('\n');
			}
			return sb.ToString ();
		}
		
		void OnFillDifs (object s, EventArgs a)
		{
			diffRequested = true;
			diffRunning = false;
			
			if (diffException != null) {
				IdeApp.Services.MessageService.ShowError (diffException, GettextCatalog.GetString ("Could not get diff information. ") + diffException.Message);
			}
			
			TreeIter it;
			if (!filestore.GetIterFirst (out it))
				return;
				
			do {
				bool filled = (bool) filestore.GetValue (it, ColFilled);
				if (filled) {
					string fileName = (string) filestore.GetValue (it, ColFullPath);
					TreeIter citer;
					filestore.IterChildren (out citer, it);
					FillDiffInfo (citer, fileName);
				}
			}
			while (filestore.IterNext (ref it));
		}
		
		void SetDiffCellData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererDiff rc = (CellRendererDiff) cell;
			string text = (string) filestore.GetValue (iter, ColPath);
			if (filestore.IterDepth (iter) == 0) {
				rc.InitCell (filelist, false, text);
			} else {
				rc.InitCell (filelist, true, text);
			}
		}
		
		private class Worker : Task {
			StatusView view;
			Repository vc;
			string filepath;
			bool remoteStatus;
						
			public Worker(Repository vc, string filepath, bool remoteStatus, StatusView view) {
				this.vc = vc;
				this.filepath = filepath;
				this.view = view;
				this.remoteStatus = remoteStatus;
			}
			
			protected override string GetDescription() {
				return "Retrieving status for " + Path.GetFileName(filepath) + "...";
			}
			
			protected override void Run() {
				System.Console.WriteLine(filepath);
				view.statuses = vc.GetDirectoryVersionInfo(filepath, remoteStatus, true);
			}
		
			protected override void Finished() {
				if (view.statuses == null) return;
				view.Update();
			}
		}
	}

}
