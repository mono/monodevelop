using System;
using System.Collections;
using System.IO;

using Gtk;
using VersionControl;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Codons;
using MonoDevelop.Core.AddIns.Conditions;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Dialogs;
using MonoDevelop.SourceEditor.Gui;
using MonoDevelop.Services;

namespace VersionControlPlugin {
	public class StatusView : BaseView {
		string filepath;
		VersionControlSystem vc;
		
		Widget widget;
		HBox commandbar;
		VBox box, main;
		Label status;
		Table table;
		Button showRemoteStatus;
		Button buttonCommit;
		
		bool commitShown = false;
		Hashtable checkCommit;
		VBox boxCommit;
		TextView textCommitMessage;
		Button buttonCommitCancel;
		Button buttonCommitCommit;
		
		Node[] statuses;
		
		bool remoteStatus = false;
		
		Hashtable buttonsShowLog;
		Hashtable buttonsShowDiff;
		
		private class RevItem {
			public RevisionPtr BaseRev;
			public string Path;
		}
	
		public static bool Show(string path, bool test) {
			foreach (VersionControlSystem vc in VersionControlService.Providers) {
				if (vc.IsDirectoryStatusAvailable(path)) {
					if (test) return true;
					StatusView d = new StatusView(path, vc);
					MonoDevelop.Gui.WorkbenchSingleton.Workbench.ShowView(d, true);
					return true;
				}
			}
			return false;
		}
		
		public StatusView(string filepath, VersionControlSystem vc) 
			: base(Path.GetFileName(filepath) + " Status") {
			this.vc = vc;
			this.filepath = filepath;
			
			main = new VBox(false, 5);
			widget = main;
			main.Show();
			
			commandbar = new HBox(false, 5);
			main.PackStart(commandbar, false, false, 5);
			
			showRemoteStatus = new Button("Show Remote Status");
			commandbar.PackEnd(showRemoteStatus, false, false, 0);
			showRemoteStatus.Clicked += new EventHandler(OnShowRemoteStatusClicked);
			
			buttonCommit = new Button("Commit...");
			commandbar.PackEnd(buttonCommit, false, false, 0);
			buttonCommit.Clicked += new EventHandler(OnCommitClicked);

			boxCommit = new VBox(false, 2);
			textCommitMessage = new TextView();
			HBox boxCommitButtons = new HBox(false, 2);
			buttonCommitCancel = new Button("Cancel");
			buttonCommitCommit = new Button("Commit");
			textCommitMessage.Show();
			buttonCommitCancel.Show();
			buttonCommitCommit.Show();
			boxCommit.PackStart(textCommitMessage, true, true, 0);
			boxCommit.PackStart(boxCommitButtons, false, false, 0);
			boxCommitButtons.PackEnd(buttonCommitCancel, false, false, 0);
			boxCommitButtons.PackEnd(buttonCommitCommit, false, false, 0);
			buttonCommitCancel.Clicked += new EventHandler(OnCommitCancelClicked);
			buttonCommitCommit.Clicked += new EventHandler(OnCommitCommitClicked);
			
			ScrolledWindow scroller = new ScrolledWindow();
			Viewport viewport = new Viewport(); 
			box = new VBox(false, 5);
			main.Add(scroller);
			
			viewport.Add(box);
			scroller.Add(viewport);
			
			main.ShowAll();
			
			StartUpdate();
		}
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		private void StartUpdate() {
			if (table != null) {
				box.Remove(table);
				table.Destroy();
				table = null;
			}
			
			if (status == null) {
				status = new Label();
				box.Add(status);
				status.Show();
			}
			if (!remoteStatus)
				status.Text = "Scanning for changes...";
			else
				status.Text = "Scanning for local and remote changes...";
			
			showRemoteStatus.Sensitive = false;
			buttonCommit.Sensitive = false;
			
			new Worker(vc, filepath, remoteStatus, this).Start();
		}
		
		private class HeaderLabel : Label {
			public HeaderLabel(string text) : base() {
				Markup = "<b>" + text + "</b>";
				Show();
			}
		}
	
		private void Update() {
			showRemoteStatus.Sensitive = !remoteStatus;
			
			if (statuses.Length == 0) {
				if (!remoteStatus)
					this.status.Text = "No files have local modifications.";
				else
					this.status.Text = "No files have local or remote modifications.";
				return;
			}
			
			buttonsShowLog = new Hashtable();
			buttonsShowDiff = new Hashtable();
			
			box.Remove(this.status);
			this.status = null;
			
			if (vc.CanCommit(filepath))
				buttonCommit.Sensitive = true;
			checkCommit = new Hashtable();
						
			table = new Table((uint)statuses.Length+1, (uint)5 + (uint)(remoteStatus ? 2 : 0), false);
			box.Add(table);

			uint row = 0;		
		
			table.Attach(new HeaderLabel("Status"), 0, 3, row, row+1, AttachOptions.Shrink, AttachOptions.Shrink, 2, 2);
			table.Attach(new HeaderLabel("Path"), 3, 4, row, row+1, AttachOptions.Shrink, AttachOptions.Shrink, 2, 2);
			
			if (remoteStatus)
				table.Attach(new HeaderLabel("Remote Status"), 4, 6, row, row+1, AttachOptions.Shrink, AttachOptions.Shrink, 2, 2);
					
			for (int i = 0; i < statuses.Length; i++) {
				Node n = statuses[i];
				
				RevItem item = new RevItem();
				item.Path = n.LocalPath;
				item.BaseRev = n.BaseRevision;
				
				uint col = 0;
				row++;
				
				CheckButton check = new CheckButton();
				checkCommit[check] = item;
				table.Attach(check, col, ++col, row, row+1, AttachOptions.Shrink, AttachOptions.Shrink, 2, 2);
				check.Visible = false;
					
				Gdk.Pixbuf statusicon = VersionControlService.LoadIconForStatus(n.Status);
				if (n.Status == NodeStatus.Modified) {
					Button b = new Button();
					if (statusicon != null) {
						Image img = new Image(statusicon);
						img.Show();
						b.Add(img);
					} else {
						b.Label = "Diff";
					}
					
					b.Relief = ReliefStyle.Half;
					buttonsShowDiff[b] = item;
					table.Attach(b, col, ++col, row, row+1, AttachOptions.Shrink, AttachOptions.Shrink, 2, 2);
					b.Clicked += new EventHandler(OnShowDiffClicked);
					b.Show();
				} else if (statusicon != null) {
					Image img = new Image(statusicon);
					img.Show();
					table.Attach(img, col, ++col, row, row+1, AttachOptions.Shrink, AttachOptions.Fill, 2, 2);
				} else {
					++col;
				}
				
				Label status = new Label(n.Status.ToString());
				status.Show();				
				table.Attach(status, col, ++col, row, row+1, AttachOptions.Shrink, AttachOptions.Shrink, 2, 2);

				Label name = new Label(); // I can't get this to left align!
				name.Justify = Justification.Left;
				name.Layout.Alignment = Pango.Alignment.Left;
				name.Xalign = 0;
				
				string localpath = n.LocalPath.Substring(filepath.Length);
				if (localpath.Length > 0 && localpath[0] == Path.DirectorySeparatorChar) localpath = localpath.Substring(1);
				if (localpath == "") { localpath = "."; } // not sure if this happens
				name.Text = localpath;
				name.Show();
				table.Attach(name, col, ++col, row, row+1, AttachOptions.Expand, AttachOptions.Shrink, 2, 2);
				
				if (remoteStatus) {
					Label rstatus = new Label(n.RemoteStatus.ToString());
					rstatus.Show();
				
					table.Attach(rstatus, col, ++col, row, row+1, AttachOptions.Shrink, AttachOptions.Shrink, 2, 2);
					
					if (n.RemoteStatus == NodeStatus.Modified) {
						Button b = new Button("View");
						b.Relief = ReliefStyle.Half;
						buttonsShowLog[b] = item;
						table.Attach(b, col, ++col, row, row+1, AttachOptions.Shrink, AttachOptions.Shrink, 2, 2);
						b.Clicked += new EventHandler(OnShowLogClicked);
						b.Show();
					}
				}
			}

			table.Show();
		}
		
		private void OnShowRemoteStatusClicked(object src, EventArgs args) {
			if (commitShown)
				buttonCommitCancel.Click();
		
			remoteStatus = true;
			StartUpdate();
		}
		
		private void OnShowLogClicked(object src, EventArgs args) {
			RevItem file = (RevItem)buttonsShowLog[src];
			LogView.Show(file.Path, false, file.BaseRev, false);
		}
		
		private void OnShowDiffClicked(object src, EventArgs args) {
			RevItem file = (RevItem)buttonsShowDiff[src];
			DiffView.Show(file.Path, false);
		}
		
		private void OnCommitClicked(object src, EventArgs args) {
			buttonCommit.Sensitive = false;
			main.Add(boxCommit);
			boxCommit.ShowAll();
			commitShown = true;
			foreach (CheckButton check in checkCommit.Keys)
				check.Visible = true;
		}
		
		private void OnCommitCommitClicked(object src, EventArgs args) {
			ArrayList paths = new ArrayList();
			foreach (CheckButton check in checkCommit.Keys) {
				if (check.Active) {
					RevItem file = (RevItem)checkCommit[check];
					paths.Add(file.Path);
				}
			}
			
			if (paths.Count == 0)
				return; // TODO: Show message.
			
			new CommitWorker(
				vc,
				(string[])paths.ToArray(typeof(string)),
				textCommitMessage.Buffer.Text,
				this).Start();
				
			OnCommitCancelClicked(null, null);
		}
		
		private void OnCommitCancelClicked(object src, EventArgs args) {
			foreach (CheckButton check in checkCommit.Keys)
				check.Visible = false;
			commitShown = false;
			buttonCommit.Sensitive = true;
			main.Remove(boxCommit);
		}
		
		private class Worker : Task {
			StatusView view;
			VersionControlSystem vc;
			string filepath;
			bool remoteStatus;
						
			public Worker(VersionControlSystem vc, string filepath, bool remoteStatus, StatusView view) {
				this.vc = vc;
				this.filepath = filepath;
				this.view = view;
				this.remoteStatus = remoteStatus;
			}
			
			protected override string GetDescription() {
				return "Retrieving status for " + Path.GetFileName(filepath) + "...";
			}
			
			protected override void Run() {
				view.statuses = vc.GetDirectoryStatus(filepath, remoteStatus, true);
			}
		
			protected override void Finished() {
				view.Update();
			}
		}
		
		private class CommitWorker : Task {
			StatusView view;
			VersionControlSystem vc;
			string[] paths;
			string message;
						
			public CommitWorker(VersionControlSystem vc, string[] paths, string message, StatusView view) {
				this.vc = vc;
				this.paths = paths;
				this.message = message;
				this.view = view;
			}
			
			protected override string GetDescription() {
				return "Committing changes...";
			}
			
			protected override void Run() {
				vc.Commit(paths, message, new UpdateCallback(Callback));
			}
		
			protected override void Finished() {
				view.StartUpdate();
			}
			
			void Callback(string path, string action) {
				Log(action + "\t" + path);
			}
		}
	}

}
