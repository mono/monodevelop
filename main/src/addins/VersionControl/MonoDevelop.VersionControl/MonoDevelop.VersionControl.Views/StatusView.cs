using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Mono.Addins;

using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Views
{
	internal class StatusView : BaseView 
	{
		string filepath;
		Repository vc;
		bool disposed;
		
		Widget widget;
		Toolbar commandbar;
		VBox main;
		Label status;
		Gtk.ToolButton showRemoteStatus;
		Gtk.ToolButton buttonCommit;
		Gtk.ToolButton buttonRevert;
		
		FileTreeView filelist;
		TreeViewColumn colCommit, colRemote;
		TreeStore filestore;
		ScrolledWindow scroller;
		CellRendererDiff diffRenderer;
		
		Box commitBox;
		TextView commitText;
		Gtk.Label labelCommit;
		
		List<VersionInfo> statuses;
		bool remoteStatus = false;

		class DiffData {
			public bool diffRequested = false;
			public bool diffRunning = false;
			public Exception diffException;
			public DiffInfo[] difs;
		};

		DiffData localDiff = new DiffData ();
		DiffData remoteDiff = new DiffData ();
		
		bool updatingComment;
		ChangeSet changeSet;
		bool firstLoad = true;
		
		const int ColIcon = 0;
		const int ColStatus = 1;
		const int ColPath = 2;
		const int ColRemoteStatus = 3;
		const int ColCommit = 4;
		const int ColFilled = 5;
		const int ColFullPath = 6;
		const int ColShowStatus = 7;
		const int ColShowComment = 8;
		const int ColIconFile = 9;
		const int ColShowToggle = 10;
		const int ColRemoteIcon = 11;
		const int ColStatusColor = 12;
		const int ColStatusRemoteDiff = 13;
		
		delegate void DiffDataHandler (DiffData diffdata);
		
		/// <summary>
		/// Fired when content difference data is loaded
		/// </summary>
		event DiffDataHandler DiffDataLoaded;
		
		public static bool Show (VersionControlItemList items, bool test)
		{
			if (items.Count != 1)
				return false;

			VersionControlItem item = items [0];
			if (item.Repository.IsVersioned (item.Path)) {
				if (test) return true;
				StatusView d = new StatusView (item.Path, item.Repository);
				IdeApp.Workbench.OpenDocument (d, true);
				return true;
			}
			return false;
		}
		
		public StatusView (string filepath, Repository vc) 
			: base(Path.GetFileName(filepath) + " Status") 
		{
			this.vc = vc;
			this.filepath = filepath;
			changeSet = vc.CreateChangeSet (filepath);
			
			main = new VBox(false, 6);
			widget = main;
			
			commandbar = new Toolbar ();
			commandbar.ToolbarStyle = Gtk.ToolbarStyle.BothHoriz;
			commandbar.IconSize = Gtk.IconSize.Menu;
			main.PackStart (commandbar, false, false, 0);
			
			buttonCommit = new Gtk.ToolButton (new Gtk.Image ("vc-commit", Gtk.IconSize.Menu), GettextCatalog.GetString ("Commit..."));
			buttonCommit.IsImportant = true;
			buttonCommit.Clicked += new EventHandler (OnCommitClicked);
			commandbar.Insert (buttonCommit, -1);
			
			Gtk.ToolButton btnRefresh = new Gtk.ToolButton (new Gtk.Image (Gtk.Stock.Refresh, IconSize.Menu), GettextCatalog.GetString ("Refresh"));
			btnRefresh.IsImportant = true;
			btnRefresh.Clicked += new EventHandler (OnRefresh);
			commandbar.Insert (btnRefresh, -1);
			
			buttonRevert = new Gtk.ToolButton (new Gtk.Image ("vc-revert-command", Gtk.IconSize.Menu), GettextCatalog.GetString ("Revert"));
			buttonRevert.IsImportant = true;
			buttonRevert.Clicked += new EventHandler (OnRevert);
			commandbar.Insert (buttonRevert, -1);
			
			showRemoteStatus = new Gtk.ToolButton (new Gtk.Image ("vc-remote-status", Gtk.IconSize.Menu), GettextCatalog.GetString ("Show Remote Status"));
			showRemoteStatus.IsImportant = true;
			showRemoteStatus.Clicked += new EventHandler(OnShowRemoteStatusClicked);
			commandbar.Insert (showRemoteStatus, -1);
			
			Gtk.ToolButton btnCreatePatch = new Gtk.ToolButton (new Gtk.Image ("vc-diff", Gtk.IconSize.Menu), GettextCatalog.GetString ("Create Patch"));
			btnCreatePatch.IsImportant = true;
			btnCreatePatch.Clicked += new EventHandler (OnCreatePatch);
			commandbar.Insert (btnCreatePatch, -1);
			
			commandbar.Insert (new Gtk.SeparatorToolItem (), -1);
			
			Gtk.ToolButton btnOpen = new Gtk.ToolButton (new Gtk.Image (Gtk.Stock.Open, IconSize.Menu), GettextCatalog.GetString ("Open"));
			btnOpen.IsImportant = true;
			btnOpen.Clicked += new EventHandler (OnOpen);
			commandbar.Insert (btnOpen, -1);
			
			commandbar.Insert (new Gtk.SeparatorToolItem (), -1);
			
			Gtk.ToolButton btnExpand = new Gtk.ToolButton (null, GettextCatalog.GetString ("Expand All"));
			btnExpand.IsImportant = true;
			btnExpand.Clicked += new EventHandler (OnExpandAll);
			commandbar.Insert (btnExpand, -1);
			
			Gtk.ToolButton btnCollapse = new Gtk.ToolButton (null, GettextCatalog.GetString ("Collapse All"));
			btnCollapse.IsImportant = true;
			btnCollapse.Clicked += new EventHandler (OnCollapseAll);
			commandbar.Insert (btnCollapse, -1);
			
			commandbar.Insert (new Gtk.SeparatorToolItem (), -1);
			
			Gtk.ToolButton btnSelectAll = new Gtk.ToolButton (null, GettextCatalog.GetString ("Select All"));
			btnSelectAll.IsImportant = true;
			btnSelectAll.Clicked += new EventHandler (OnSelectAll);
			commandbar.Insert (btnSelectAll, -1);
			
			Gtk.ToolButton btnSelectNone = new Gtk.ToolButton (null, GettextCatalog.GetString ("Select None"));
			btnSelectNone.IsImportant = true;
			btnSelectNone.Clicked += new EventHandler (OnSelectNone);
			commandbar.Insert (btnSelectNone, -1);
			
			status = new Label("");
			main.PackStart(status, false, false, 0);
			
			scroller = new ScrolledWindow();
			scroller.ShadowType = Gtk.ShadowType.In;
			filelist = new FileTreeView();
			filelist.Selection.Mode = SelectionMode.Multiple;
			
			scroller.Add(filelist);
			scroller.HscrollbarPolicy = PolicyType.Automatic;
			scroller.VscrollbarPolicy = PolicyType.Automatic;
			filelist.RowActivated += OnRowActivated;
			filelist.DiffLineActivated += OnDiffLineActivated;
			
			CellRendererToggle cellToggle = new CellRendererToggle();
			cellToggle.Toggled += new ToggledHandler(OnCommitToggledHandler);
			var crc = new CellRendererIcon ();
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
			var crp = new CellRendererPixbuf ();
			TreeViewColumn colStatus = new TreeViewColumn ();
			colStatus.Title = GettextCatalog.GetString ("Status");
			colStatus.PackStart (crp, false);
			colStatus.PackStart (crt, true);
			colStatus.AddAttribute (crp, "pixbuf", ColIcon);
			colStatus.AddAttribute (crp, "visible", ColShowStatus);
			colStatus.AddAttribute (crt, "text", ColStatus);
			colStatus.AddAttribute (crt, "foreground", ColStatusColor);
			
			TreeViewColumn colFile = new TreeViewColumn ();
			colFile.Title = GettextCatalog.GetString ("File");
			colFile.Spacing = 2;
			crp = new CellRendererPixbuf ();
			diffRenderer = new CellRendererDiff ();
			colFile.PackStart (crp, false);
			colFile.PackStart (diffRenderer, true);
			colFile.AddAttribute (crp, "pixbuf", ColIconFile);
			colFile.AddAttribute (crp, "visible", ColShowStatus);
			colFile.SetCellDataFunc (diffRenderer, new TreeCellDataFunc (SetDiffCellData));
			
			crt = new CellRendererText();
			crp = new CellRendererPixbuf ();
			colRemote = new TreeViewColumn ();
			colRemote.Title = GettextCatalog.GetString ("Remote Status");
			colRemote.PackStart (crp, false);
			colRemote.PackStart (crt, true);
			colRemote.AddAttribute (crp, "pixbuf", ColRemoteIcon);
			colRemote.AddAttribute (crt, "text", ColRemoteStatus);
			colRemote.AddAttribute (crt, "foreground", ColStatusColor);
			
			filelist.AppendColumn(colStatus);
			filelist.AppendColumn(colRemote);
			filelist.AppendColumn(colCommit);
			filelist.AppendColumn(colFile);
			
			colRemote.Visible = false;

			filestore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string[]), typeof (string), typeof(bool), typeof(bool), typeof(string), typeof(bool), typeof (bool), typeof(Gdk.Pixbuf), typeof(bool), typeof (Gdk.Pixbuf), typeof(string), typeof(bool));
			filelist.Model = filestore;
			filelist.TestExpandRow += new Gtk.TestExpandRowHandler (OnTestExpandRow);
			
			commitBox = new VBox ();
			
			HBox labBox = new HBox ();
			labelCommit = new Gtk.Label (GettextCatalog.GetString ("Commit message:"));
			labelCommit.Xalign = 0;
			labBox.PackStart (new Gtk.Image ("vc-comment", Gtk.IconSize.Menu), false, false, 0);
			labBox.PackStart (labelCommit, true, true, 3);
			
			commitBox.PackStart (labBox, false, false, 0);
			
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
			VersionControlService.FileStatusChanged += OnFileStatusChanged;
			
			filelist.HeadersClickable = true;
			filestore.SetSortFunc (0, CompareNodes);
			colStatus.SortColumnId = 0;
			filestore.SetSortFunc (1, CompareNodes);
			colRemote.SortColumnId = 1;
			filestore.SetSortFunc (2, CompareNodes);
			colCommit.SortColumnId = 2;
			filestore.SetSortFunc (3, CompareNodes);
			colFile.SortColumnId = 3;
			
			filestore.SetSortColumnId (3, Gtk.SortType.Ascending);
			
			filelist.ShowContextMenu += OnPopupMenu;
			
			StartUpdate();
		}
		
		public override string StockIconId {
			get { return "vc-status"; }
		}

		int CompareNodes (Gtk.TreeModel model, Gtk.TreeIter a, Gtk.TreeIter b)
		{
			int col, val=0;
			SortType type;
			filestore.GetSortColumnId (out col, out type);
			
			switch (col) {
				case 0: val = ColStatus; break;
				case 1: val = ColRemoteStatus; break;
				case 2: val = ColCommit; break;
				case 3: val = ColPath; break;
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
		
		public override void Dispose ()
		{
			disposed = true;
			if (colCommit != null) {
				colCommit.Destroy ();
				colCommit = null;
			}
			
			if (colRemote != null) {
				colRemote.Destroy ();
				colRemote = null;
			}
			if (filestore != null) {
				filestore.Dispose ();
				filestore = null;
			}
			if (this.diffRenderer != null) {
				this.diffRenderer.Destroy ();
				this.diffRenderer = null;
			}
			VersionControlService.FileStatusChanged -= OnFileStatusChanged;
			if (widget != null) {
				widget.Destroy ();
				widget = null;
			}
			base.Dispose ();
		}
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		private void StartUpdate ()
		{
			if (!remoteStatus)
				status.Text = GettextCatalog.GetString ("Scanning for changes...");
			else
				status.Text = GettextCatalog.GetString ("Scanning for local and remote changes...");
			
			status.Visible = true;
			scroller.Visible = false;
			commitBox.Visible = false;
			
			showRemoteStatus.Sensitive = false;
			buttonCommit.Sensitive = false;
			
			ThreadPool.QueueUserWorkItem (delegate {
				List<VersionInfo> newList = new List<VersionInfo> ();
				newList.AddRange (vc.GetDirectoryVersionInfo(filepath, remoteStatus, true));
				DispatchService.GuiDispatch (delegate {
					if (!disposed)
						LoadStatus (newList);
				});
			});
		}
		
		void LoadStatus (List<VersionInfo> newList)
		{
			statuses = newList;
			
			// Remove from the changeset files/folders which have been deleted
			var toRemove = new List<ChangeSetItem> ();
			foreach (ChangeSetItem item in changeSet.Items) {
				bool found = false;
				foreach (VersionInfo vi in statuses) {
					if (vi.LocalPath == item.LocalPath) {
						found = true;
						break;
					}
				}
				if (!found)
					toRemove.Add (item);
			}
			foreach (var item in toRemove) 
				changeSet.RemoveItem (item);
			
			Update();
		}
		
		void UpdateControlStatus ()
		{
			// Set controls to the correct state according to the changes found
			showRemoteStatus.Sensitive = !remoteStatus;
			TreeIter it;
			
			if (!filestore.GetIterFirst (out it)) {
				commitBox.Visible = false;
				buttonCommit.Sensitive = false;
				scroller.Visible = false;
				status.Visible = true;
				if (!remoteStatus)
					status.Text = GettextCatalog.GetString ("No files have local modifications.");
				else
					status.Text = GettextCatalog.GetString ("No files have local or remote modifications.");
			} else {
				status.Visible = false;
				scroller.Visible = true;
				commitBox.Visible = true;
				colRemote.Visible = remoteStatus;
				
				try {
					if (vc.CanCommit(filepath))
						buttonCommit.Sensitive = true;
				} catch (Exception ex) {
					LoggingService.LogError (ex.ToString ());
					buttonCommit.Sensitive = true;
				}
			}
			UpdateSelectionStatus ();
		}
		
		void UpdateSelectionStatus ()
		{
			buttonRevert.Sensitive = filelist.Selection.CountSelectedRows () != 0;
			buttonCommit.Sensitive = !changeSet.IsEmpty;
			commitBox.Visible = filelist.Selection.CountSelectedRows () != 0;
		}
		
		private void Update ()
		{
			localDiff.diffRequested = false;
			remoteDiff.diffRequested = false;
			localDiff.difs = null;
			remoteDiff.difs = null;
			
			filestore.Clear();
			diffRenderer.Reset ();
			
			if (statuses.Count > 0) {
				foreach (VersionInfo n in statuses) {
					if (FileVisible (n)) {
						if (firstLoad)
							changeSet.AddFile (n);
						AppendFileInfo (n);
					}
				}
			}
			UpdateControlStatus ();
			if (firstLoad) {
				TreeIter it;
				if (filestore.GetIterFirst (out it))
					filelist.Selection.SelectIter (it);
				firstLoad = false;
			}
		}
		
		TreeIter AppendFileInfo (VersionInfo n)
		{
			Gdk.Pixbuf statusicon = VersionControlService.LoadIconForStatus(n.Status);
			string lstatus = VersionControlService.GetStatusLabel (n.Status);
			
			Gdk.Pixbuf rstatusicon = VersionControlService.LoadIconForStatus(n.RemoteStatus);
			string rstatus = VersionControlService.GetStatusLabel (n.RemoteStatus);

			string scolor = n.HasLocalChanges && n.HasRemoteChanges ? "red" : null;
			
			string localpath = n.LocalPath.ToRelative (filepath);
			if (localpath.Length > 0 && localpath[0] == Path.DirectorySeparatorChar) localpath = localpath.Substring(1);
			if (localpath == "") { localpath = "."; } // not sure if this happens
			
			bool hasComment = GetCommitMessage (n.LocalPath).Length > 0;
			bool commit = changeSet.ContainsFile (n.LocalPath);
			
			Gdk.Pixbuf fileIcon;
			if (n.IsDirectory)
				fileIcon = ImageService.GetPixbuf (MonoDevelop.Ide.Gui.Stock.ClosedFolder, Gtk.IconSize.Menu);
			else
				fileIcon = DesktopService.GetPixbufForFile (n.LocalPath, Gtk.IconSize.Menu);

			
			
			TreeIter it = filestore.AppendValues (statusicon, lstatus, GLib.Markup.EscapeText (localpath).Split ('\n'), rstatus, commit, false, n.LocalPath.ToString (), true, hasComment, fileIcon, n.HasLocalChanges, rstatusicon, scolor, n.HasRemoteChange (VersionStatus.Modified));
			if (!n.IsDirectory)
				filestore.AppendValues (it, statusicon, "", new string[0], "", false, true, n.LocalPath.ToString (), false, false, fileIcon, false, null, null, false);
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
		
		void OnCursorChanged (object o, EventArgs args)
		{
			UpdateSelectionStatus ();
			
			string[] files = GetCurrentFiles ();
			if (files.Length > 0) {
				commitBox.Visible = true;
				updatingComment = true;
				if (files.Length == 1)
					labelCommit.Text = GettextCatalog.GetString ("Commit message for file '{0}':", Path.GetFileName (files[0]));
				else
					labelCommit.Text = GettextCatalog.GetString ("Commit message (multiple selection):");
				
				// If all selected files have the same message,
				// then show it so it can be modified. If not, show
				// a blank message
				string msg = GetCommitMessage (files [0]);
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
				commitText.Buffer.Text = String.Empty;
				updatingComment = false;
				commitBox.Visible = false;
			}
		}
		
		void OnCommitTextChanged (object o, EventArgs args)
		{
			if (updatingComment)
				return;
				
			string msg = commitText.Buffer.Text;
			
			// Update the comment in all selected files
			string[] files = GetCurrentFiles ();
			foreach (string file in files)
				SetCommitMessage (file, msg);

			// Make the comment icon visible in all selected rows
			TreePath[] paths = filelist.Selection.GetSelectedRows ();
			foreach (TreePath path in paths) {
				TreeIter iter;
				filestore.GetIter (out iter, path);
				if (filestore.IterDepth (iter) != 0)
					filestore.IterParent (out iter, iter);
				
				bool curv = (bool) filestore.GetValue (iter, ColShowComment);
				if (curv != (msg.Length > 0))
					filestore.SetValue (iter, ColShowComment, msg.Length > 0);

				string fp = (string) filestore.GetValue (iter, ColFullPath);
				filestore.SetValue (iter, ColCommit, changeSet.ContainsFile (fp));
			}
			UpdateSelectionStatus ();
		}
		
		string GetCommitMessage (string file)
		{
			string txt = VersionControlService.GetCommitComment (file);
			return txt != null ? txt : String.Empty;
		}
		
		void SetCommitMessage (string file, string text)
		{
			if (text.Length > 0) {
				ChangeSetItem item = changeSet.GetFileItem (file);
				if (item == null)
					item = changeSet.AddFile (file);
				item.Comment = text;
			} else {
				VersionControlService.SetCommitComment (file, text, true);
			}
		}
		
		void OnRowActivated (object o, RowActivatedArgs args)
		{
			OnOpen (null, null);
		}
		
		void OnDiffLineActivated (object o, EventArgs a)
		{
			OnOpen (null, null);
		}
		
		void OnCommitToggledHandler(object o, ToggledArgs args) {
			TreeIter pos;
			if (!filestore.GetIterFromString (out pos, args.Path))
				return;

			string localpath = (string) filestore.GetValue (pos, ColFullPath);
			
			if (changeSet.ContainsFile (localpath)) {
				changeSet.RemoveFile (localpath);
			} else {
				VersionInfo vi = GetVersionInfo (localpath);
				if (vi != null)
					changeSet.AddFile (vi);
			}
			filestore.SetValue (pos, ColCommit, changeSet.ContainsFile (localpath));
			UpdateSelectionStatus ();
		}
		
		VersionInfo GetVersionInfo (string file)
		{
			foreach (VersionInfo vi in statuses)
				if (vi.LocalPath == file)
					return vi;
			return null;
		}
		
		private void OnShowRemoteStatusClicked(object src, EventArgs args) {
			remoteStatus = true;
			StartUpdate ();
		}
		
		private void OnCommitClicked (object src, EventArgs args)
		{
			// Nothing to commit
			if (changeSet.IsEmpty)
				return;

			int comments = changeSet.CommentsCount;
			if ((comments > 0) && (changeSet.Count > comments)) {
				if (MessageService.AskQuestion (
				  GettextCatalog.GetString ("Some of the files in this commit do not have ChangeLog messages."),
				  GettextCatalog.GetString ("You may have forgotten to unselect items."),
				  AlertButton.Cancel, AlertButton.Proceed) != AlertButton.Proceed)
					return;
			}
			
			if (!CommitCommand.Commit (vc, changeSet.Clone (), false))
				return;
		}

		
		private void OnTestExpandRow (object sender, Gtk.TestExpandRowArgs args)
		{
			bool filled = (bool) filestore.GetValue (args.Iter, ColFilled);
			if (!filled) {
				filestore.SetValue (args.Iter, ColFilled, true);
				TreeIter iter;
				filestore.IterChildren (out iter, args.Iter);
				string fileName = (string) filestore.GetValue (args.Iter, ColFullPath);
				bool remoteDiff = (bool) filestore.GetValue (args.Iter, ColStatusRemoteDiff);
				SetFileDiff (iter, fileName, remoteDiff);
			}
		}
		
		void OnPopupMenu (object o, EventArgs args)
		{
			object commandChain = this;
			CommandEntrySet opset = new CommandEntrySet ();
			VersionControlItemList items = GetSelectedItems ();
			
			foreach (object ob in AddinManager.GetExtensionNodes ("/MonoDevelop/VersionControl/StatusViewCommands")) {
				if (ob is TypeExtensionNode) {
					TypeExtensionNode node = (TypeExtensionNode) ob;
					opset.AddItem (ParseCommandId (node));
					VersionControlCommandHandler handler = node.CreateInstance () as VersionControlCommandHandler;
					if (handler == null) {
						LoggingService.LogError ("Invalid type specified in extension point 'MonoDevelop/VersionControl/StatusViewCommands'. Subclass of 'VersionControlCommandHandler' expected.");
						continue;
					}
					handler.Init (items);
					CommandRouter rt = new CommandRouter (handler);
					rt.Next = commandChain;
					commandChain = rt;
				} else
					opset.AddSeparator ();
			}
			IdeApp.CommandService.ShowContextMenu (opset, commandChain);
		}
		
		public VersionControlItemList GetSelectedItems ()
		{
			string[] files = GetCurrentFiles ();
			VersionControlItemList items = new VersionControlItemList ();
			foreach (string file in files) {
				Project prj = IdeApp.Workspace.GetProjectContainingFile (file);
				items.Add (new VersionControlItem (vc, prj, file, Directory.Exists (file)));
			}
			return items;
		}
		
		class CommandRouter: ICommandDelegatorRouter
		{
			object handler;
			public object Next;
			
			public CommandRouter (object handler)
			{
				this.handler = handler;
			}
			
			public object GetNextCommandTarget ()
			{
				return Next;
			}
			
			public object GetDelegatedCommandTarget ()
			{
				return handler;
			}
		}

		internal static object ParseCommandId (ExtensionNode codon)
		{
			string id = codon.Id;
			if (id.StartsWith ("@"))
				return id.Substring (1);
			else
				return id;
		}
		
		void OnExpandAll (object s, EventArgs args)
		{
			filelist.ExpandAll ();
		}
		
		void OnCollapseAll (object s, EventArgs args)
		{
			filelist.CollapseAll ();
		}
		
		void OnSelectAll (object s, EventArgs args)
		{
			TreeIter pos;
			if (filestore.GetIterFirst (out pos)) {
				do {
					string localpath = (string) filestore.GetValue (pos, ColFullPath);
					if (!changeSet.ContainsFile (localpath)) {
						VersionInfo vi = GetVersionInfo (localpath);
						changeSet.AddFile (vi);
					}
					filestore.SetValue (pos, ColCommit, changeSet.ContainsFile (localpath));
				} while (filestore.IterNext (ref pos));
			}
			UpdateSelectionStatus ();
		}
		
		void OnSelectNone (object s, EventArgs args)
		{
			TreeIter pos;
			if (filestore.GetIterFirst (out pos)) {
				do {
					string localpath = (string) filestore.GetValue (pos, ColFullPath);
					if (changeSet.ContainsFile (localpath)) {
						changeSet.RemoveFile (localpath);
					} 
					filestore.SetValue (pos, ColCommit, changeSet.ContainsFile (localpath));
				} while (filestore.IterNext (ref pos));
			}
			UpdateSelectionStatus ();
		}
		
		/// <summary>
		/// Handler for "Create Patch" toolbar button click. 
		/// </summary>
		void OnCreatePatch (object s, EventArgs args)
		{
			CreatePatchCommand.CreatePatch (changeSet, false);
		}
		
		void OnRefresh (object s, EventArgs args)
		{
			StartUpdate ();
		}
		
		void OnRevert (object s, EventArgs args)
		{
			RevertCommand.Revert (GetSelectedItems (), false);
		}
		
		void OnOpen (object s, EventArgs args)
		{
			string[] files = GetCurrentFiles ();
			if (files.Length == 0)
				return;
			else if (files.Length == 1) {
				TreePath[] rows = filelist.Selection.GetSelectedRows ();
				int line = -1;
				if (rows.Length == 1 && rows [0].Depth == 2)
					line = diffRenderer.GetSelectedLine (rows[0]);
				IdeApp.Workbench.OpenDocument (files [0], line, 0, true);
			}
			else {
				AlertButton openAll = new AlertButton (GettextCatalog.GetString ("_Open All")); 
				if (MessageService.AskQuestion (GettextCatalog.GetString ("Do you want to open all {0} files?", files.Length), AlertButton.Cancel, openAll) == openAll) {
					for (int n=0; n<files.Length; n++)
						IdeApp.Workbench.OpenDocument (files[n], n==0);
				}
			}
		}
		
		void OnFileStatusChanged (object s, FileUpdateEventArgs args)
		{
			if (!args.FilePath.IsChildPathOf (filepath) && args.FilePath != filepath)
				return;
				
			if (args.IsDirectory) {
				StartUpdate ();
				return;
			}
			
			bool found = false;
			int oldStatusIndex;
			TreeIter oldStatusIter = TreeIter.Zero;
			
			// Locate the file in the status object list
			for (oldStatusIndex=0; oldStatusIndex<statuses.Count; oldStatusIndex++) {
				if (statuses [oldStatusIndex].LocalPath == args.FilePath) {
					found = true;
					break;
				}
			}

			// Locate the file in the treeview
			if (found) {
				found = false;
				if (filestore.GetIterFirst (out oldStatusIter)) {
					do {
						if (args.FilePath == (string) filestore.GetValue (oldStatusIter, ColFullPath)) {
							found = true;
							break;
						}
					} while (filestore.IterNext (ref oldStatusIter));
				}
			}

			VersionInfo newInfo;
			try {
				// Reuse remote status from old version info
				newInfo = vc.GetVersionInfo (args.FilePath, false);
				if (found && newInfo != null) {
					VersionInfo oldInfo = statuses [oldStatusIndex];
					if (oldInfo != null) {
						newInfo.RemoteStatus = oldInfo.RemoteStatus;
						newInfo.RemoteRevision = oldInfo.RemoteRevision;
					}
				}
			}
			catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
				return;
			}
			
			if (found) {
				if (!FileVisible (newInfo)) {
					// Just remove the file from the change set
					changeSet.RemoveFile (args.FilePath);
					statuses.RemoveAt (oldStatusIndex);
					filestore.Remove (ref oldStatusIter);
					UpdateControlStatus ();
					return;
				}
				
				statuses [oldStatusIndex] = newInfo;
				
				// Update the tree
				AppendFileInfo (newInfo);
				filestore.Remove (ref oldStatusIter);
			}
			else {
				if (FileVisible (newInfo)) {
					statuses.Add (newInfo);
					changeSet.AddFile (newInfo);
					AppendFileInfo (newInfo);
				}
			}
			UpdateControlStatus ();
		}
		
		bool FileVisible (VersionInfo vinfo)
		{
			return vinfo != null && (vinfo.HasLocalChanges || vinfo.HasRemoteChanges);
		}

		DiffData GetDiffData (bool remote)
		{
			if (remote)
				return remoteDiff;
			else
				return localDiff;
		}
		
		/// <summary>
		/// Loads diff information from a version control provider.
		/// </summary>
		/// <param name="remote">
		/// A <see cref="System.Boolean"/>: Whether the information 
		/// should be loaded from the remote server.
		/// </param>
		void LoadDiffs (bool remote)
		{
			DiffData ddata = GetDiffData (remote);
			if (ddata.diffRunning)
				return;

			// Diff not yet requested. Do it now.
			ddata.diffRunning = true;
			
			// Run the diff in a separate thread and update the tree when done
			ThreadPool.QueueUserWorkItem (
				delegate {
					ddata.diffException = null;
					try {
						ddata.difs = vc.PathDiff (filepath, null, remote);
					} catch (Exception ex) {
						ddata.diffException = ex;
					} finally {
						ddata.diffRequested = true;
						ddata.diffRunning = false;
						if (null != DiffDataLoaded) {
							Gtk.Application.Invoke (delegate {
								DiffDataLoaded (ddata);
								DiffDataLoaded = null;
							});
						}
					}
				}
			);
		}
		
		void SetFileDiff (TreeIter iter, string file, bool remote)
		{
			// If diff information is already loaded, just look for the
			// diff chunk of the file and fill the tree

			DiffData ddata = GetDiffData (remote);
			if (ddata.diffRequested) {
				FillDiffInfo (iter, file, ddata);
				return;
			}

			filestore.SetValue (iter, ColPath, new string[] { GettextCatalog.GetString ("Loading data...") });
			
			if (ddata.diffRunning)
				return;

			DiffDataLoaded += FillDifs;
			LoadDiffs (remote);
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
				MessageService.ShowException (ddata.diffException, GettextCatalog.GetString ("Could not get diff information. ") + ddata.diffException.Message);
			}
			
			TreeIter it;
			if (!filestore.GetIterFirst (out it))
				return;
				
			do {
				bool filled = (bool) filestore.GetValue (it, ColFilled);
				if (filled) {
					string fileName = (string) filestore.GetValue (it, ColFullPath);
					bool remoteDiff = (bool) filestore.GetValue (it, ColStatusRemoteDiff);
					TreeIter citer;
					filestore.IterChildren (out citer, it);
					FillDiffInfo (citer, fileName, GetDiffData (remoteDiff));
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

	class FileTreeView: TreeView
	{
		protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
		{
			bool keepPos = false;
			double vpos = 0;
			
			TreePath path, cpath;
			GetPathAtPos ((int)evnt.X, (int)evnt.Y, out path);
			
			TreeViewColumn col;
			GetCursor (out cpath, out col);
			
			if (path != null && path.Depth == 2) {
				vpos = Vadjustment.Value;
				keepPos = true;
				if (Selection.PathIsSelected (path) && Selection.GetSelectedRows ().Length == 1 && evnt.Button == 1) {
					if (evnt.Type == Gdk.EventType.TwoButtonPress && DiffLineActivated != null)
						DiffLineActivated (this, EventArgs.Empty);
					return true;
				}
			}
			
			bool res = true;
			bool withModifider = (evnt.State & Gdk.ModifierType.ShiftMask) != 0 || (evnt.State & Gdk.ModifierType.ControlMask) != 0;
			if (!IsClickedNodeSelected ((int)evnt.X, (int)evnt.Y) || (Selection.GetSelectedRows ().Length <= 1) || withModifider || evnt.Button != 3)
				res = base.OnButtonPressEvent (evnt);
			
			if (evnt.Button == 3) {
				if (ShowContextMenu != null)
					ShowContextMenu (this, EventArgs.Empty);
			}
			if (keepPos)
				Vadjustment.Value = vpos;
			return res;
		}
		
		bool IsClickedNodeSelected (int x, int y)
		{
			Gtk.TreePath path;
			if (GetPathAtPos (x, y, out path))
				return Selection.PathIsSelected (path);
			else
				return false;
		}

		protected override bool OnPopupMenu()
		{
			if (ShowContextMenu != null)
				ShowContextMenu (this, EventArgs.Empty);
			return true;
		}
		
		internal Gdk.Point? CursorLocation { get; private set; }
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			TreePath path;
			GetPathAtPos ((int)evnt.X, (int)evnt.Y, out path);
			
			// Diff cells need to be redrawn so they can show the updated selected line
			if (path != null && path.Depth == 2) {
				CursorLocation = new Gdk.Point ((int)evnt.X, (int)evnt.Y);
				//FIXME: we should optimize these draws
				QueueDraw ();
			} else if (CursorLocation.HasValue) {
				CursorLocation = null;
				QueueDraw ();
			}
			
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			if (CursorLocation.HasValue) {
				CursorLocation = null;
				QueueDraw ();
			}
			return base.OnLeaveNotifyEvent (evnt);
		}
		
		protected override bool OnScrollEvent (Gdk.EventScroll evnt)
		{
			QueueDraw ();
			return base.OnScrollEvent (evnt);
		}

		public event EventHandler ShowContextMenu;
		public event EventHandler DiffLineActivated;
	}
}
