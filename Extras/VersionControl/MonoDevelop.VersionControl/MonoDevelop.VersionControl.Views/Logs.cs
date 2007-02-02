using System;
using System.Collections;
using System.IO;

using Gtk;

using MonoDevelop.Components;
using MonoDevelop.SourceEditor.Gui;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.VersionControl.Views
{
	public class LogView : BaseView
	{
		string filepath;
		Widget widget;
		Revision[] history;
		Repository vc;
		VersionInfo vinfo;
		
		TreeView loglist;
		ListStore changedpathstore;
		Toolbar commandbar;
		
		public static bool Show (Repository vc, string filepath, bool isDirectory, Revision since, bool test)
		{
			if (vc.IsHistoryAvailable (filepath)) {
				if (test) return true;
				new Worker(vc, filepath, isDirectory, since).Start();
				return true;
			}
			return false;
		}
		
		private class Worker : Task {
			Repository vc;
			string filepath;
			bool isDirectory;
			Revision since;
			Revision[] history;
						
			public Worker (Repository vc, string filepath, bool isDirectory, Revision since) {
				this.vc = vc;
				this.filepath = filepath;
				this.isDirectory = isDirectory;
				this.since = since;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Retrieving history for {0}...", Path.GetFileName(filepath));
			}
			
			protected override void Run() {
				history = vc.GetHistory (filepath, since);
			}
		
			protected override void Finished() {
				if (history == null) return;
				LogView d = new LogView(filepath, isDirectory, history, vc);
				MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (d, true);
			}
		}
		
		public LogView (string filepath, bool isDirectory, Revision[] history, Repository vc) 
			: base(Path.GetFileName(filepath) + " Log")
		{
			this.vc = vc;
			this.filepath = filepath;
			this.history = history;
			
			try {
				this.vinfo = vc.GetVersionInfo (filepath, false);
			}
			catch (Exception ex) {
				IdeApp.Services.MessageService.ShowError (ex, GettextCatalog.GetString ("Version control command failed."));
			}

			// Widget setup
			
			VBox box = new VBox(false, 6);
			
			widget = box;
			if (vinfo == null)
				widget.Sensitive = false;

			// Create the toolbar
			
			if (!isDirectory) {
				commandbar = new Toolbar ();
				commandbar.ToolbarStyle = Gtk.ToolbarStyle.BothHoriz;
				commandbar.IconSize = Gtk.IconSize.Menu;
				box.PackStart (commandbar, false, false, 0);
				
				Gtk.ToolButton button = new Gtk.ToolButton (new Gtk.Image ("vc-diff", Gtk.IconSize.Menu), GettextCatalog.GetString ("View Changes"));
				button.IsImportant = true;
				button.Clicked += new EventHandler(DiffButtonClicked);
				commandbar.Insert (button, -1);
				
				button = new Gtk.ToolButton (new Gtk.Image (Gtk.Stock.Open, Gtk.IconSize.Menu), GettextCatalog.GetString ("View File"));
				button.IsImportant = true;
				button.Clicked += new EventHandler (ViewTextButtonClicked);
				commandbar.Insert (button, -1);
			}
			
			// A paned with two trees
			
			Gtk.VPaned paned = new Gtk.VPaned ();
			box.PackStart (paned, true, true, 0);
			
			// Create the log list
			
			loglist = new TreeView ();
			ScrolledWindow loglistscroll = new ScrolledWindow();
			loglistscroll.ShadowType = Gtk.ShadowType.In;
			loglistscroll.Add (loglist);
			loglistscroll.HscrollbarPolicy = PolicyType.Automatic;
			loglistscroll.VscrollbarPolicy = PolicyType.Automatic;
			paned.Add1 (loglistscroll);
			((Paned.PanedChild)paned [loglistscroll]).Resize = true;
			
			TreeView changedPaths = new TreeView();
			ScrolledWindow changedPathsScroll = new ScrolledWindow();
			changedPathsScroll.ShadowType = Gtk.ShadowType.In;
			changedPathsScroll.HscrollbarPolicy = PolicyType.Automatic;
			changedPathsScroll.VscrollbarPolicy = PolicyType.Automatic;
			changedPathsScroll.Add (changedPaths);
			paned.Add2 (changedPathsScroll);
			((Paned.PanedChild)paned [changedPathsScroll]).Resize = false;

			widget.ShowAll();
			
			// Revision list setup
			
			CellRendererText textRenderer = new CellRendererText();
			textRenderer.Yalign = 0;
			
			TreeViewColumn colRevNum = new TreeViewColumn(GettextCatalog.GetString ("Revision"), textRenderer, "text", 0);
			TreeViewColumn colRevDate = new TreeViewColumn(GettextCatalog.GetString ("Date"), textRenderer, "text", 1);
			TreeViewColumn colRevAuthor = new TreeViewColumn(GettextCatalog.GetString ("Author"), textRenderer, "text", 2);
			TreeViewColumn colRevMessage = new TreeViewColumn(GettextCatalog.GetString ("Message"), textRenderer, "text", 3);
			
			loglist.AppendColumn(colRevNum);
			loglist.AppendColumn(colRevDate);
			loglist.AppendColumn(colRevAuthor);
			loglist.AppendColumn(colRevMessage);
			
			ListStore logstore = new ListStore (typeof (string), typeof (string), typeof (string), typeof (string));
			loglist.Model = logstore;
			 
			foreach (Revision d in history) {
				logstore.AppendValues(
					d.ToString(),
					d.Time.ToString(),
					d.Author,
					d.Message == "" ? GettextCatalog.GetString ("(No message)") : d.Message);
			}

			// Changed paths list setup
			
			changedpathstore = new ListStore (typeof(string), typeof (string), typeof(string), typeof (string));
			changedPaths.Model = changedpathstore;
			
			TreeViewColumn colOperation = new TreeViewColumn ();
			CellRendererText crt = new CellRendererText();
			CellRendererPixbuf crp = new CellRendererPixbuf();
			colOperation.Title = GettextCatalog.GetString ("Operation");
			colOperation.PackStart (crp, false);
			colOperation.PackStart (crt, true);
			colOperation.AddAttribute (crp, "stock-id", 0);
			colOperation.AddAttribute (crt, "text", 1);
			changedPaths.AppendColumn (colOperation);
			
			TreeViewColumn colChangedPath = new TreeViewColumn ();
			crp = new CellRendererPixbuf();
			crt = new CellRendererText();
			colChangedPath.Title = GettextCatalog.GetString ("File Path");
			colChangedPath.PackStart (crp, false);
			colChangedPath.PackStart (crt, true);
			colChangedPath.AddAttribute (crp, "stock-id", 2);
			colChangedPath.AddAttribute (crt, "text", 3);
			changedPaths.AppendColumn (colChangedPath);
			
			loglist.Selection.Changed += new EventHandler(TreeSelectionChanged);
		}
		
		Revision GetSelectedRev()
		{
			TreePath path;
			TreeViewColumn col;
			loglist.GetCursor(out path, out col);
			if (path == null) return null;
			return history [path.Indices[0]];
		}
		
		void TreeSelectionChanged(object o, EventArgs args) {
			Revision d = GetSelectedRev();
			changedpathstore.Clear();
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
		
		void DiffButtonClicked(object src, EventArgs args) {
			Revision d = GetSelectedRev();
			if (d == null) return;
			new DiffWorker(Path.GetFileName(filepath), vc, vinfo.RepositoryPath, d).Start();
		}
		
		void ViewTextButtonClicked(object src, EventArgs args) {
			Revision d = GetSelectedRev();
			if (d == null) return;
			HistoricalFileView.Show(filepath, vc, vinfo.RepositoryPath, d);
		}
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		internal class DiffWorker : Task {
			Repository vc;
			string name;
			Revision revision;
			string text1, text2;
			string revPath;
						
			public DiffWorker(string name, Repository vc, string revPath, Revision revision) {
				this.name = name;
				this.vc = vc;
				this.revPath = revPath;
				this.revision = revision;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Retreiving changes in {0} at revision {1}...", name, revision);
			}
			
			protected override void Run() {
				Log (GettextCatalog.GetString ("Getting text of {0} at revision {1}...", revPath, revision.GetPrevious()));
				try {
					text1 = vc.GetTextAtRevision(revPath, revision.GetPrevious());
				} catch {
					// If the file was added in this revision, no previous
					// text exists.
					text1 = "";
				}
				Log (GettextCatalog.GetString ("Getting text of {0} at revision {1}...", revPath, revision));
				text2 = vc.GetTextAtRevision(revPath, revision);
			}
		
			protected override void Finished() {
				if (text1 == null || text2 == null) return;
				DiffView.Show(name + " " + revision.ToString(), text1, text2);
			}
		}
		
	}

	public class HistoricalFileView : BaseView 
	{
		MonoDevelop.SourceEditor.Gui.SourceEditor widget;
	
		public static void Show(string name, string file, string text) {
			HistoricalFileView d = new HistoricalFileView(name, file, text);
			MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (d, true);
		}
			
		public static void Show(string file, Repository vc, string revPath, Revision revision) {
			new Worker(Path.GetFileName(file) + " " + revision.ToString(),
				file, vc, revPath, revision).Start();
		}
		
			
		public HistoricalFileView(string name, string file, string text) 
			: base(name) {
			
			// How do I get it to recognize the language of the file?
			widget = new MonoDevelop.SourceEditor.Gui.SourceEditor(null);
			widget.Text = text;
			widget.View.Editable = false;
			widget.ShowAll();
		}
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
	
		internal class Worker : Task {
			Repository vc;
			string name, file;
			string revPath;
			Revision revision;
			string text;
						
			public Worker(string name, string file, Repository vc, string revPath, Revision revision) {
				this.name = name;
				this.file = file;
				this.vc = vc;
				this.revPath = revPath;
				this.revision = revision;
			}
			
			protected override string GetDescription() {
				return GettextCatalog.GetString ("Retreiving content of {0} at revision {1}...", name, revision);
			}
			
			protected override void Run() {
				text = vc.GetTextAtRevision(revPath, revision);
			}
		
			protected override void Finished() {
				if (text == null) return;
				HistoricalFileView.Show(name, file, text);
			}
		}
	}

}
