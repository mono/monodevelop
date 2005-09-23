using System;
using System.Collections;
using System.IO;

using Gtk;
using VersionControl;

using MonoDevelop.Gui.Widgets;
using MonoDevelop.SourceEditor.Gui;

namespace VersionControlPlugin {
	public class LogView : BaseView {
		string filepath;
		Widget widget;
		Hashtable buttons = new Hashtable();
		VersionControlSystem vc;
		RevisionPtr since;
		
		private class RevItem {
			public RevisionPtr Rev;
			public object Path;
		}
	
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
				LogView d = new LogView(filepath, isDirectory, history, vc);
				MonoDevelop.Gui.WorkbenchSingleton.Workbench.ShowView(d, true);
			}
		}
		
		public LogView(string filepath, bool isDirectory, RevisionDescription[] history, VersionControlSystem vc) 
			: base(Path.GetFileName(filepath) + " Log") {
			this.vc = vc;
			this.filepath = filepath;
			
			ScrolledWindow scroller = new ScrolledWindow();
			Viewport viewport = new Viewport(); 
			VBox box = new VBox(false, 5);
			
			viewport.Add(box);
			scroller.Add(viewport);
			widget = scroller;
			 
			foreach (RevisionDescription d in history) {
				RevItem revitem = new RevItem();
				revitem.Path = d.RepositoryPath;
				revitem.Rev = d.Revision;
			
				VBox item = new VBox(false, 1);
				
				HBox header_row = new HBox(false, 2);
				item.PackStart(header_row, false, false, 0);

				Label header = new Label(d.Revision + " -- " + d.Time + " -- " + d.Author);
				header.Xalign = 0;
				header_row.Add(header);
				
				if (!isDirectory) {
					Button viewdiff = new Button("View Changes");
					viewdiff.Clicked += new EventHandler(DiffButtonClicked);
					header_row.Add(viewdiff);
					buttons[viewdiff] = revitem;
					
					Button viewtext = new Button("View File");
					viewtext.Clicked += new EventHandler(ViewTextButtonClicked);
					header_row.Add(viewtext);
					buttons[viewtext] = revitem;
				}
				
				TextView message = new TextView();
				message.Editable = false;
				message.WrapMode = Gtk.WrapMode.WordChar;
				message.Buffer.Text = d.Message == "" ? "No message." : d.Message;
				item.PackStart(message, false, false, 0);
				
				box.PackStart(item, false, false, 0);
			}
			
			widget.ShowAll();
		}
		
		void DiffButtonClicked(object src, EventArgs args) {
			RevItem item = (RevItem)buttons[src];
			new DiffWorker(Path.GetFileName(filepath), vc, item.Path, item.Rev).Start();
		}
		
		void ViewTextButtonClicked(object src, EventArgs args) {
			RevItem item = (RevItem)buttons[src];
			HistoricalFileView.Show(filepath, vc, item.Path, item.Rev);
		}
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		internal class DiffWorker : Task {
			VersionControlSystem vc;
			string name;
			object revPath;
			RevisionPtr revision;
			string text1, text2;
						
			public DiffWorker(string name, VersionControlSystem vc, object revPath, RevisionPtr revision) {
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
				DiffView.Show(name + " " + revision.ToString(), text1, text2);
			}
		}
		
	}

	public class HistoricalFileView : BaseView {
		SourceEditor widget;
	
		public static void Show(string name, string file, string text) {
			HistoricalFileView d = new HistoricalFileView(name, file, text);
			MonoDevelop.Gui.WorkbenchSingleton.Workbench.ShowView(d, true);
		}
			
		public static void Show(string file, VersionControlSystem vc, object revPath, RevisionPtr revision) {
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
			object revPath;
			RevisionPtr revision;
			string text;
						
			public Worker(string name, string file, VersionControlSystem vc, object revPath, RevisionPtr revision) {
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
				HistoricalFileView.Show(name, file, text);
			}
		}
	}

}
