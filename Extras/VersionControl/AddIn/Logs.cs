using System;
using System.Collections;
using System.IO;

using Gtk;
using VersionControl;

using MonoDevelop.Components;
using MonoDevelop.SourceEditor.Gui;

namespace VersionControlPlugin {
	public class LogView : BaseView {
		string filepath;
		Widget widget;
		RevisionDescription[] history;
		VersionControlSystem vc;
		RevisionPtr since;
		
		TreeView loglist;
		ListStore changedpathstore;
		
		public static bool Show(string filepath, bool isDirectory, RevisionPtr since, bool test) {
			foreach (VersionControlSystem vc in VersionControlService.Providers) {
				if (vc.IsHistoryAvailable(filepath)) {
					if (test) return true;
					new Worker(vc, filepath, isDirectory, since).Start();
					return true;
				}
			}
			return false;
		}
		
		private class Worker : Task {
			VersionControlSystem vc;
			string filepath;
			bool isDirectory;
			RevisionPtr since;
			RevisionDescription[] history;
						
			public Worker(VersionControlSystem vc, string filepath, bool isDirectory, RevisionPtr since) {
				this.vc = vc;
				this.filepath = filepath;
				this.isDirectory = isDirectory;
				this.since = since;
			}
			
			protected override string GetDescription() {
				return "Retrieving history for " + Path.GetFileName(filepath) + "...";
			}
			
			protected override void Run() {
				history = vc.GetHistory(filepath, since);
			}
		
			protected override void Finished() {
				if (history == null) return;
				LogView d = new LogView(filepath, isDirectory, history, vc);
				MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (d, true);
			}
		}
		
		public LogView(string filepath, bool isDirectory, RevisionDescription[] history, VersionControlSystem vc) 
			: base(Path.GetFileName(filepath) + " Log") {
			this.vc = vc;
			this.filepath = filepath;
			this.history = history;

			// Widget setup
			
			VBox box = new VBox(false, 5);
			
			widget = box;

			loglist = new TreeView();
			loglist.Selection.Changed += new EventHandler(TreeSelectionChanged);
			ScrolledWindow loglistscroll = new ScrolledWindow();
			loglistscroll.Add(loglist);
			loglistscroll.HscrollbarPolicy = PolicyType.Never;
			loglistscroll.VscrollbarPolicy = PolicyType.Always;
			box.PackStart(loglistscroll, true, true, 0);
			
			box.PackStart(new HSeparator(), false, false, 0);
			
			HBox commands = new HBox(false, 10);
			box.PackStart(commands, false, false, 5);
			if (!isDirectory) {
				Button viewdiff = new Button("View Changes");
				viewdiff.Clicked += new EventHandler(DiffButtonClicked);
				commands.Add(viewdiff);
				
				Button viewtext = new Button("View File");
				viewtext.Clicked += new EventHandler(ViewTextButtonClicked);
				commands.Add(viewtext);

				box.PackStart(new HSeparator(), false, false, 0);
			}
			
			TreeView changedPaths = new TreeView();
			ScrolledWindow changedPathsScroll = new ScrolledWindow();
			changedPathsScroll.HscrollbarPolicy = PolicyType.Never;
			changedPathsScroll.VscrollbarPolicy = PolicyType.Always;
			changedPathsScroll.Add(changedPaths);
			box.PackStart(changedPathsScroll, true, true, 0);

			widget.ShowAll();
			
			// Revision list setup
			
			CellRendererText textRenderer = new CellRendererText();
			textRenderer.Yalign = 0;
			
			TreeViewColumn colRevNum = new TreeViewColumn("Rev", textRenderer, "text", 0);
			TreeViewColumn colRevDate = new TreeViewColumn("Date", textRenderer, "text", 1);
			TreeViewColumn colRevAuthor = new TreeViewColumn("Author", textRenderer, "text", 2);
			TreeViewColumn colRevMessage = new TreeViewColumn("Message", textRenderer, "text", 3);
			
			loglist.AppendColumn(colRevNum);
			loglist.AppendColumn(colRevDate);
			loglist.AppendColumn(colRevAuthor);
			loglist.AppendColumn(colRevMessage);
			
			ListStore logstore = new ListStore (typeof (string), typeof (string), typeof (string), typeof (string));
			loglist.Model = logstore;
			 
			foreach (RevisionDescription d in history) {
				logstore.AppendValues(
					d.Revision.ToString(),
					d.Time.ToString(),
					d.Author,
					d.Message == "" ? "(No message.)" : d.Message);
			}

			// Changed paths list setup
			
			TreeViewColumn colChangedPath = new TreeViewColumn("Path", textRenderer, "text", 0);
			changedPaths.AppendColumn(colChangedPath);
			
			changedpathstore = new ListStore (typeof (string));
			changedPaths.Model = changedpathstore;
		}
		
		RevisionDescription GetSelectedRev() {
			TreePath path;
			TreeViewColumn col;
			loglist.GetCursor(out path, out col);
			if (path == null) return null;
			return history[ path.Indices[0] ];
		}
		
		void TreeSelectionChanged(object o, EventArgs args) {
			RevisionDescription d = GetSelectedRev();
			changedpathstore.Clear();
			foreach (string n in d.ChangedFiles)
				changedpathstore.AppendValues(n);
		}
		
		void DiffButtonClicked(object src, EventArgs args) {
			RevisionDescription d = GetSelectedRev();
			if (d == null) return;
			new DiffWorker(Path.GetFileName(filepath), vc, d.RepositoryPath, d.Revision).Start();
		}
		
		void ViewTextButtonClicked(object src, EventArgs args) {
			RevisionDescription d = GetSelectedRev();
			if (d == null) return;
			HistoricalFileView.Show(filepath, vc, d.RepositoryPath, d.Revision);
		}
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		internal class DiffWorker : Task {
			VersionControlSystem vc;
			string name;
			RepositoryPath revPath;
			RevisionPtr revision;
			string text1, text2;
						
			public DiffWorker(string name, VersionControlSystem vc, RepositoryPath revPath, RevisionPtr revision) {
				this.name = name;
				this.vc = vc;
				this.revPath = revPath;
				this.revision = revision;
			}
			
			protected override string GetDescription() {
				return "Retreiving changes in " + name + " at " + revision + "...";
			}
			
			protected override void Run() {
				Log("Getting text of " + revPath + " at " + revision.GetPrevious() + "...");
				try {
					text1 = vc.GetTextAtRevision(revPath, revision.GetPrevious());
				} catch (Exception e) {
					// If the file was added in this revision, no previous
					// text exists.
					text1 = "";
				}
				Log("Getting text of " + revPath + " at " + revision + "...");
				text2 = vc.GetTextAtRevision(revPath, revision);
			}
		
			protected override void Finished() {
				if (text1 == null || text2 == null) return;
				DiffView.Show(name + " " + revision.ToString(), text1, text2);
			}
		}
		
	}

	public class HistoricalFileView : BaseView {
		SourceEditor widget;
	
		public static void Show(string name, string file, string text) {
			HistoricalFileView d = new HistoricalFileView(name, file, text);
			MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (d, true);
		}
			
		public static void Show(string file, VersionControlSystem vc, RepositoryPath revPath, RevisionPtr revision) {
			new Worker(Path.GetFileName(file) + " " + revision.ToString(),
				file, vc, revPath, revision).Start();
		}
		
			
		public HistoricalFileView(string name, string file, string text) 
			: base(name) {
			
			// How do I get it to recognize the language of the file?
			widget = new SourceEditor(null);
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
			VersionControlSystem vc;
			string name, file;
			RepositoryPath revPath;
			RevisionPtr revision;
			string text;
						
			public Worker(string name, string file, VersionControlSystem vc, RepositoryPath revPath, RevisionPtr revision) {
				this.name = name;
				this.file = file;
				this.vc = vc;
				this.revPath = revPath;
				this.revision = revision;
			}
			
			protected override string GetDescription() {
				return "Retreiving content of " + name + " at " + revision + "...";
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
