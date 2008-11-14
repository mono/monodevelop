using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl.Views
{
	internal class LogView : BaseView
	{
		string filepath;
		Widget widget;
		Revision [] history;
		Repository vc;
		VersionInfo vinfo;
		Gtk.ToolButton revertButton, revertToButton;
		
		TreeView loglist;
		ListStore changedpathstore;
		Toolbar commandbar;
		
		public static bool Show (VersionControlItemList items, Revision since, bool test)
		{
			bool found = false;
			foreach (VersionControlItem item in items) {
				if (item.Repository.IsHistoryAvailable (item.Path)) {
					if (test)
						return true;
					found = true;
					new Worker (item.Repository, item.Path, item.IsDirectory, since).Start ();
				}
			}
			return found;
		}
		
		private class Worker : Task {
			Repository vc;
			string filepath;
			bool isDirectory;
			Revision since;
			Revision [] history;
						
			public Worker (Repository vc, string filepath, bool isDirectory, Revision since) {
				this.vc = vc;
				this.filepath = filepath;
				this.isDirectory = isDirectory;
				this.since = since;
			}
			
			protected override string GetDescription () {
				return GettextCatalog.GetString ("Retrieving history for {0}...", Path.GetFileName (filepath));
			}
			
			protected override void Run () {
				history = vc.GetHistory (filepath, since);
			}
		
			protected override void Finished() {
				if (history == null)
					return;
				LogView d = new LogView (filepath, isDirectory, history, vc);
				MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (d, true);
			}
		}
		
		public LogView (string filepath, bool isDirectory, Revision [] history, Repository vc) 
			: base (Path.GetFileName (filepath) + " Log")
		{
			this.vc = vc;
			this.filepath = filepath;
			this.history = history;
			
			try {
				this.vinfo = vc.GetVersionInfo (filepath, false);
			}
			catch (Exception ex) {
				MessageService.ShowException (ex, GettextCatalog.GetString ("Version control command failed."));
			}

			// Widget setup
			
			VBox box = new VBox (false, 6);
			
			widget = box;

			// Create the toolbar
			commandbar = new Toolbar ();
			commandbar.ToolbarStyle = Gtk.ToolbarStyle.BothHoriz;
			commandbar.IconSize = Gtk.IconSize.Menu;
			box.PackStart (commandbar, false, false, 0);
				
			if (!isDirectory && vinfo != null) {
				Gtk.ToolButton button = new Gtk.ToolButton (new Gtk.Image ("vc-diff", Gtk.IconSize.Menu), GettextCatalog.GetString ("View Changes"));
				button.IsImportant = true;
				button.Clicked += new EventHandler (DiffButtonClicked);
				commandbar.Insert (button, -1);
				
				button = new Gtk.ToolButton (new Gtk.Image (Gtk.Stock.Open, Gtk.IconSize.Menu), GettextCatalog.GetString ("View File"));
				button.IsImportant = true;
				button.Clicked += new EventHandler (ViewTextButtonClicked);
				commandbar.Insert (button, -1);
			}
			
			revertButton = new Gtk.ToolButton (new Gtk.Image ("vc-revert-command", Gtk.IconSize.Menu), GettextCatalog.GetString ("Revert changes from this revision"));
			revertButton.IsImportant = true;
			revertButton.Sensitive = false;
			revertButton.Clicked += new EventHandler (RevertRevisionClicked);
			commandbar.Insert (revertButton, -1);
			
			revertToButton = new Gtk.ToolButton (new Gtk.Image ("vc-revert-command", Gtk.IconSize.Menu), GettextCatalog.GetString ("Revert to this revision"));
			revertToButton.IsImportant = true;
			revertToButton.Sensitive = false;
			revertToButton.Clicked += new EventHandler (RevertToRevisionClicked);
			commandbar.Insert (revertToButton, -1);

			
			// A paned with two trees
			
			Gtk.VPaned paned = new Gtk.VPaned ();
			box.PackStart (paned, true, true, 0);
			
			// Create the log list
			
			loglist = new TreeView ();
			ScrolledWindow loglistscroll = new ScrolledWindow ();
			loglistscroll.ShadowType = Gtk.ShadowType.In;
			loglistscroll.Add (loglist);
			loglistscroll.HscrollbarPolicy = PolicyType.Automatic;
			loglistscroll.VscrollbarPolicy = PolicyType.Automatic;
			paned.Add1 (loglistscroll);
			((Paned.PanedChild)paned [loglistscroll]).Resize = true;
			
			TreeView changedPaths = new TreeView ();
			ScrolledWindow changedPathsScroll = new ScrolledWindow ();
			changedPathsScroll.ShadowType = Gtk.ShadowType.In;
			changedPathsScroll.HscrollbarPolicy = PolicyType.Automatic;
			changedPathsScroll.VscrollbarPolicy = PolicyType.Automatic;
			changedPathsScroll.Add (changedPaths);
			paned.Add2 (changedPathsScroll);
			((Paned.PanedChild)paned [changedPathsScroll]).Resize = false;

			widget.ShowAll ();
			
			// Revision list setup
			
			CellRendererText textRenderer = new CellRendererText ();
			textRenderer.Yalign = 0;
			
			TreeViewColumn colRevNum = new TreeViewColumn (GettextCatalog.GetString ("Revision"), textRenderer, "text", 0);
			TreeViewColumn colRevDate = new TreeViewColumn (GettextCatalog.GetString ("Date"), textRenderer, "text", 1);
			TreeViewColumn colRevAuthor = new TreeViewColumn (GettextCatalog.GetString ("Author"), textRenderer, "text", 2);
			TreeViewColumn colRevMessage = new TreeViewColumn (GettextCatalog.GetString ("Message"), textRenderer, "text", 3);
			
			loglist.AppendColumn (colRevNum);
			loglist.AppendColumn (colRevDate);
			loglist.AppendColumn (colRevAuthor);
			loglist.AppendColumn (colRevMessage);
			
			ListStore logstore = new ListStore (typeof (string), typeof (string), typeof (string), typeof (string));
			loglist.Model = logstore;
			 
			foreach (Revision d in history) {
				logstore.AppendValues(
					d.ToString (),
					d.Time.ToString (),
					d.Author,
					d.Message == String.Empty ? GettextCatalog.GetString ("(No message)") : d.Message);
			}

			// Changed paths list setup
			
			changedpathstore = new ListStore (typeof(string), typeof (string), typeof(string), typeof (string));
			changedPaths.Model = changedpathstore;
			
			TreeViewColumn colOperation = new TreeViewColumn ();
			CellRendererText crt = new CellRendererText ();
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			colOperation.Title = GettextCatalog.GetString ("Operation");
			colOperation.PackStart (crp, false);
			colOperation.PackStart (crt, true);
			colOperation.AddAttribute (crp, "stock-id", 0);
			colOperation.AddAttribute (crt, "text", 1);
			changedPaths.AppendColumn (colOperation);
			
			TreeViewColumn colChangedPath = new TreeViewColumn ();
			crp = new CellRendererPixbuf ();
			crt = new CellRendererText ();
			colChangedPath.Title = GettextCatalog.GetString ("File Path");
			colChangedPath.PackStart (crp, false);
			colChangedPath.PackStart (crt, true);
			colChangedPath.AddAttribute (crp, "stock-id", 2);
			colChangedPath.AddAttribute (crt, "text", 3);
			changedPaths.AppendColumn (colChangedPath);
			
			loglist.Selection.Changed += new EventHandler (TreeSelectionChanged);
		}

		Revision GetSelectedRev ()
		{
			int [] indices;
			return GetSelectedRev (out indices);
		}
		
		Revision GetSelectedRev (out int [] indices)
		{
			indices = null;
			TreePath path;
			TreeViewColumn col;
			
			loglist.GetCursor (out path, out col);
			if (path == null)
				return null;

			indices = path.Indices;
			return history [indices [0]];
		}
		
		void TreeSelectionChanged (object o, EventArgs args) {
			int [] indices;
			Revision d = GetSelectedRev (out indices);
			
			revertButton.Sensitive = (d != null);
			revertToButton.Sensitive = ((d != null) &&
			                            (indices.Length == 1) && //no sense to revert to *many* revs
			                            (indices [0] != 0)); //no sense to revert to *current* rev
			
			changedpathstore.Clear ();
			foreach (RevisionPath rp in d.ChangedFiles) 
			{
				string actionIcon;
				string action = null;
				if (rp.Action == RevisionAction.Add) {
					action = GettextCatalog.GetString ("Add");
					actionIcon = Gtk.Stock.Add;
				}
				else if (rp.Action == RevisionAction.Delete) {
					action = GettextCatalog.GetString ("Delete");
					actionIcon = Gtk.Stock.Remove;
				}
				else if (rp.Action == RevisionAction.Modify) {
					action = GettextCatalog.GetString ("Modify");
					actionIcon = "gtk-edit";
				}
				else if (rp.Action == RevisionAction.Replace) {
					action = GettextCatalog.GetString ("Replace");
					actionIcon = "gtk-edit";
				} else {
					action = rp.ActionDescription;
					actionIcon = MonoDevelop.Core.Gui.Stock.Empty;
				}
				
				string fileIcon;
/*				if (n.IsDirectory)
					fileIcon = MonoDevelop.Core.Gui.Stock.ClosedFolder;
				else
*/					fileIcon = IdeApp.Services.Icons.GetImageForFile (rp.Path);
				
				changedpathstore.AppendValues (actionIcon, action, fileIcon, rp.Path);
			}
		}
		
		void DiffButtonClicked (object src, EventArgs args) {
			Revision d = GetSelectedRev ();
			if (d == null)
				return;
			new DiffWorker (Path.GetFileName (filepath), vc, vinfo.RepositoryPath, d).Start ();
		}
		
		void ViewTextButtonClicked (object src, EventArgs args) {
			Revision d = GetSelectedRev ();
			if (d == null)
				return;
			HistoricalFileView.Show (filepath, vc, vinfo.RepositoryPath, d);
		}
		
		void RevertToRevisionClicked (object src, EventArgs args) {
			Revision d = GetSelectedRev ();
			if (RevertRevisionsCommands.RevertToRevision (vc, filepath, d, false))
				VersionControlService.SetCommitComment (filepath, 
				                                        String.Format ("(Revert to revision {0})", d.ToString ()), true);
		}
		
		void RevertRevisionClicked (object src, EventArgs args) {
			Revision d = GetSelectedRev ();
			if (RevertRevisionsCommands.RevertRevision (vc, filepath, d, false))
				VersionControlService.SetCommitComment (filepath, 
				                                        String.Format ("(Revert to revision {0})", d.ToString ()), true);
		}
		
		public override Gtk.Widget Control { 
			get { return widget; }
		}
		
		public override void Dispose ()
		{
			widget.Destroy ();
			base.Dispose ();
		}

		
		internal class DiffWorker : Task {
			Repository vc;
			string name;
			Revision revision;
			string text1, text2;
			string revPath;
						
			public DiffWorker (string name, Repository vc, string revPath, Revision revision) {
				this.name = name;
				this.vc = vc;
				this.revPath = revPath;
				this.revision = revision;
			}
			
			protected override string GetDescription () {
				return GettextCatalog.GetString ("Retrieving changes in {0} at revision {1}...", name, revision);
			}
			
			protected override void Run () {
				Log (GettextCatalog.GetString ("Getting text of {0} at revision {1}...", revPath, revision.GetPrevious ()));
				try {
					text1 = vc.GetTextAtRevision (revPath, revision.GetPrevious ());
				} catch {
					// If the file was added in this revision, no previous
					// text exists.
					text1 = String.Empty;
				}
				Log (GettextCatalog.GetString ("Getting text of {0} at revision {1}...", revPath, revision));
				text2 = vc.GetTextAtRevision (revPath, revision);
			}
		
			protected override void Finished () {
				if (text1 == null || text2 == null) return;
				DiffView.Show (name + " (revision " + revision.ToString () + ")", text1, text2);
			}
		}
		
	}

	internal class HistoricalFileView
	{
		public static void Show (string name, string file, string text) {
			string mimeType = IdeApp.Services.PlatformService.GetMimeTypeForUri (file);
			if (mimeType == null || mimeType.Length == 0)
				mimeType = "text/plain";
			Document doc = MonoDevelop.Ide.Gui.IdeApp.Workbench.NewDocument (name, mimeType, text);
			doc.IsDirty = false;
		}
			
		public static void Show (string file, Repository vc, string revPath, Revision revision) {
			new Worker (Path.GetFileName (file) + " (revision " + revision.ToString () + ")",
				file, vc, revPath, revision).Start ();
		}
		
			
		internal class Worker : Task {
			Repository vc;
			string name, file;
			string revPath;
			Revision revision;
			string text;
						
			public Worker (string name, string file, Repository vc, string revPath, Revision revision) {
				this.name = name;
				this.file = file;
				this.vc = vc;
				this.revPath = revPath;
				this.revision = revision;
			}
			
			protected override string GetDescription () {
				return GettextCatalog.GetString ("Retreiving content of {0} at revision {1}...", name, revision);
			}
			
			protected override void Run () {
				text = vc.GetTextAtRevision (revPath, revision);
			}
		
			protected override void Finished () {
				if (text == null)
					return;
				HistoricalFileView.Show (name, file, text);
			}
		}
	}

}
