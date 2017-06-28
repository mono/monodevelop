using System;
using System.Threading;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using System.Linq;

namespace MonoDevelop.VersionControl.Views
{
	[System.ComponentModel.ToolboxItem (true)]
	public class ChangeSetView: ScrolledWindow
	{
		bool disposed;
		
		FileTreeView filelist;
		TreeViewColumn colCommit;
		TreeStore filestore;
		CellRendererDiff diffRenderer;
		
		class DiffData {
			public bool diffRequested = false;
			public bool diffRunning = false;
			public Exception diffException;
			public DiffInfo[] difs;
		};

		DiffData localDiff = new DiffData ();
		
		ChangeSet changeSet;
		bool firstLoad = true;
		
		const int ColIcon = 0;
		const int ColStatus = 1;
		const int ColPath = 2;
		const int ColCommit = 3;
		const int ColFilled = 4;
		const int ColFullPath = 5;
		const int ColShowStatus = 6;
		const int ColShowComment = 7;
		const int ColIconFile = 8;
		const int ColShowToggle = 9;
		const int ColStatusColor = 10;
		
		delegate void DiffDataHandler (DiffData diffdata);
		
		public delegate DiffInfo[] DiffLoaderDelegate (FilePath path);
		
		public DiffLoaderDelegate DiffLoader;
		
		/// <summary>
		/// Fired when content difference data is loaded
		/// </summary>
		event DiffDataHandler DiffDataLoaded;

		static Xwt.Drawing.Image commitImage = Xwt.Drawing.Image.FromResource ("commit-16.png");
		public ChangeSetView ()
		{
			ShadowType = Gtk.ShadowType.In;
			filelist = new FileTreeView();
			filelist.Selection.Mode = SelectionMode.Multiple;
			
			Add (filelist);
			HscrollbarPolicy = PolicyType.Automatic;
			VscrollbarPolicy = PolicyType.Automatic;
			filelist.RowActivated += OnRowActivated;
			filelist.DiffLineActivated += OnDiffLineActivated;
			
			CellRendererToggle cellToggle = new CellRendererToggle();
//			cellToggle.Toggled += new ToggledHandler(OnCommitToggledHandler);
			var crc = new CellRendererImage ();
			crc.StockId = "vc-comment";
			colCommit = new TreeViewColumn ();
			colCommit.Spacing = 2;
			colCommit.Widget = new Xwt.ImageView (commitImage).ToGtkWidget ();
			colCommit.Widget.Show ();
			colCommit.PackStart (cellToggle, false);
			colCommit.PackStart (crc, false);
			colCommit.AddAttribute (cellToggle, "active", ColCommit);
			colCommit.AddAttribute (cellToggle, "visible", ColShowToggle);
			colCommit.AddAttribute (crc, "visible", ColShowComment);
			colCommit.Visible = false;
			
			CellRendererText crt = new CellRendererText();
			var crp = new CellRendererImage ();
			TreeViewColumn colStatus = new TreeViewColumn ();
			colStatus.Title = GettextCatalog.GetString ("Status");
			colStatus.PackStart (crp, false);
			colStatus.PackStart (crt, true);
			colStatus.AddAttribute (crp, "image", ColIcon);
			colStatus.AddAttribute (crp, "visible", ColShowStatus);
			colStatus.AddAttribute (crt, "text", ColStatus);
			colStatus.AddAttribute (crt, "foreground", ColStatusColor);
			
			TreeViewColumn colFile = new TreeViewColumn ();
			colFile.Title = GettextCatalog.GetString ("File");
			colFile.Spacing = 2;
			crp = new CellRendererImage ();
			diffRenderer = new CellRendererDiff ();
			colFile.PackStart (crp, false);
			colFile.PackStart (diffRenderer, true);
			colFile.AddAttribute (crp, "image", ColIconFile);
			colFile.AddAttribute (crp, "visible", ColShowStatus);
			colFile.SetCellDataFunc (diffRenderer, new TreeCellDataFunc (SetDiffCellData));
			
			filelist.AppendColumn(colStatus);
			filelist.AppendColumn(colCommit);
			filelist.AppendColumn(colFile);
			
			filestore = new TreeStore (typeof (Xwt.Drawing.Image), typeof (string), typeof (string[]), typeof(bool), typeof(bool), typeof(string), typeof(bool), typeof (bool), typeof(Xwt.Drawing.Image), typeof(bool), typeof(string));
			filelist.Model = filestore;
			filelist.SearchColumn = -1; // disable the interactive search
			filelist.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
			
			ShowAll();
			
			filelist.Selection.Changed += new EventHandler(OnCursorChanged);
			
			filelist.HeadersClickable = true;
			filestore.SetSortFunc (0, CompareNodes);
			colStatus.SortColumnId = 0;
			filestore.SetSortFunc (1, CompareNodes);
			colCommit.SortColumnId = 1;
			filestore.SetSortFunc (2, CompareNodes);
			colFile.SortColumnId = 2;
			
			filestore.SetSortColumnId (2, Gtk.SortType.Ascending);
		}
		
		public void Load (ChangeSet changeSet)
		{
			this.changeSet = changeSet;
			Update ();
		}
		
		public void Clear ()
		{
			this.changeSet = null;
			Update ();
		}
		
		int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			int col, val=0;
			SortType type;
			filestore.GetSortColumnId (out col, out type);
			
			switch (col) {
				case 0: val = ColStatus; break;
				case 1: val = ColCommit; break;
				case 2: val = ColPath; break;
			}
			object o1 = filestore.GetValue (a, val);
			object o2 = filestore.GetValue (b, val);
			if (o1 is string[]) o1 = ((string[])o1)[0];
			if (o2 is string[]) o2 = ((string[])o2)[0];
			
			if (o1 == null && o2 == null)
				return 0;
			else if (o1 == null)
				return 1;
			else if (o2 == null)
				return -1;
			
			return ((IComparable)o1).CompareTo (o2);
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			
			disposed = true;
			if (colCommit != null) {
				colCommit.Destroy ();
				colCommit = null;
			}

			if (this.diffRenderer != null) {
				this.diffRenderer.Destroy ();
				this.diffRenderer = null;
			}
		}
		
		void UpdateControlStatus ()
		{
		}
		
		void Update ()
		{
			localDiff.diffRequested = false;
			localDiff.difs = null;
			
			filestore.Clear();
			diffRenderer.Reset ();
			
			if (changeSet == null) {
				Sensitive = false;
				return;
			}
			
			Sensitive = true;
			
			foreach (ChangeSetItem n in changeSet.Items)
				AppendFileInfo (n);
			
			UpdateControlStatus ();
			if (firstLoad) {
				TreeIter it;
				if (filestore.GetIterFirst (out it))
					filelist.Selection.SelectIter (it);
				firstLoad = false;
			}
		}
		
		TreeIter AppendFileInfo (ChangeSetItem n)
		{
			Xwt.Drawing.Image statusicon = VersionControlService.LoadIconForStatus(n.Status);
			string lstatus = VersionControlService.GetStatusLabel (n.Status);
			
			string scolor = null;
			
			string localpath = n.LocalPath.ToRelative (changeSet.BaseLocalPath);
			if (localpath.Length > 0 && localpath[0] == System.IO.Path.DirectorySeparatorChar) localpath = localpath.Substring(1);
			if (localpath == "") { localpath = "."; } // not sure if this happens
			
			bool hasComment = false; //GetCommitMessage (n.LocalPath).Length > 0;
			bool commit = true; //changeSet.ContainsFile (n.LocalPath);
			
			Xwt.Drawing.Image fileIcon;
			if (n.IsDirectory)
				fileIcon = ImageService.GetIcon (MonoDevelop.Ide.Gui.Stock.ClosedFolder, Gtk.IconSize.Menu);
			else
				fileIcon = DesktopService.GetIconForFile (n.LocalPath, Gtk.IconSize.Menu);
			
			TreeIter it = filestore.AppendValues (statusicon, lstatus, GLib.Markup.EscapeText (localpath).Split ('\n'), commit, false, n.LocalPath.ToString (), true, hasComment, fileIcon, n.HasLocalChanges, scolor);
			if (!n.IsDirectory)
				filestore.AppendValues (it, statusicon, "", new string[0], false, true, n.LocalPath.ToString (), false, false, fileIcon, false, null);
			return it;
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
		
		void UpdateSelectionStatus ()
		{
		}
		
		void OnCursorChanged (object o, EventArgs args)
		{
			UpdateSelectionStatus ();
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			OnOpen ();
		}
		
		void OnDiffLineActivated (object o, EventArgs a)
		{
			OnOpen ();
		}
		
		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) filestore.GetValue (args.Iter, ColFilled);
			if (!filled) {
				filestore.SetValue (args.Iter, ColFilled, true);
				TreeIter iter;
				filestore.IterChildren (out iter, args.Iter);
				string fileName = (string) filestore.GetValue (args.Iter, ColFullPath);
				SetFileDiff (iter, fileName);
			}
		}
		
		void OnOpen ()
		{
			string[] files = GetCurrentFiles ();
			if (files.Length == 0)
				return;
			else if (files.Length == 1) {
				TreePath[] rows = filelist.Selection.GetSelectedRows ();
				int line = 1;
				if (rows.Length == 1 && rows [0].Depth == 2) {
					line = diffRenderer.GetSelectedLine (rows [0]);
					if (line == -1)
						line = 1;
				}
				var proj = IdeApp.Workspace.GetProjectsContainingFile (files [0]).FirstOrDefault ();
				IdeApp.Workbench.OpenDocument (files [0], proj, line, 0);
			}
			else {
				AlertButton openAll = new AlertButton (GettextCatalog.GetString ("_Open All")); 
				if (MessageService.AskQuestion (GettextCatalog.GetString ("Do you want to open all {0} files?", files.Length), AlertButton.Cancel, openAll) == openAll) {
					for (int n=0; n<files.Length; n++) {
						var proj = IdeApp.Workspace.GetProjectsContainingFile (files [n]).FirstOrDefault ();
						IdeApp.Workbench.OpenDocument (files [n], proj, n == 0);
					}
				}
			}
		}
		
		DiffData GetDiffData ()
		{
			return localDiff;
		}
		
		void LoadDiffs ()
		{
			DiffData ddata = GetDiffData ();
			if (ddata.diffRunning)
				return;

			// Diff not yet requested. Do it now.
			ddata.diffRunning = true;
			
			// Run the diff in a separate thread and update the tree when done
			ThreadPool.QueueUserWorkItem (
				delegate {
					ddata.diffException = null;
					try {
						ddata.difs = DiffLoader (changeSet.BaseLocalPath);
					} catch (Exception ex) {
						ddata.diffException = ex;
					} finally {
						ddata.diffRequested = true;
						ddata.diffRunning = false;
						if (null != DiffDataLoaded) {
							Gtk.Application.Invoke ((o, args) => {
								DiffDataLoaded (ddata);
								DiffDataLoaded = null;
							});
						}
					}
				}
			);
		}
		
		void SetFileDiff (TreeIter iter, string file)
		{
			// If diff information is already loaded, just look for the
			// diff chunk of the file and fill the tree

			DiffData ddata = GetDiffData ();
			if (ddata.diffRequested) {
				FillDiffInfo (iter, file, ddata);
				return;
			}

			filestore.SetValue (iter, ColPath, new string[] { GettextCatalog.GetString ("Loading data...") });
			
			if (ddata.diffRunning)
				return;

			DiffDataLoaded += FillDifs;
			LoadDiffs ();
		}
		
		void FillDiffInfo (TreeIter iter, string file, DiffData ddata)
		{
			if (ddata.difs != null) {
				foreach (DiffInfo di in ddata.difs) {
					if (di.FileName == file) {
						filestore.SetValue (iter, ColPath, di.Content.Split ('\n'));
						return;
					}
				}
			}
			filestore.SetValue (iter, ColPath, new string[] { GettextCatalog.GetString ("No differences found") });
		}
		
		void FillDifs (DiffData ddata)
		{
			if (disposed)
				return;

			diffRenderer.Reset ();

			if (ddata.diffException != null) {
				MessageService.ShowError (GettextCatalog.GetString ("Could not get diff information. "), ddata.diffException);
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
					FillDiffInfo (citer, fileName, GetDiffData ());
				}
			}
			while (filestore.IterNext (ref it));
		}
		
		void SetDiffCellData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			if (disposed)
				return;
			CellRendererDiff rc = (CellRendererDiff)cell;
			string[] lines = (string[])filestore.GetValue (iter, ColPath);
			TreePath path = filestore.GetPath (iter);
			if (filestore.IterDepth (iter) == 0) {
				rc.InitCell (filelist, false, lines, path);
			} else {
				rc.InitCell (filelist, true, lines, path);
			}
		}
	}
}
