using System;
using System.Collections;
using System.IO;

using Gtk;
using VersionControl;

using MonoDevelop.Core.AddIns;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.SourceEditor.Gui;

namespace VersionControlPlugin {
	public class StatusView : BaseView {
		string filepath;
		VersionControlSystem vc;
		
		Widget widget;
		HBox commandbar;
		VBox main;
		Label status;
		Button showRemoteStatus;
		Button buttonCommit;
		
		TreeView filelist;
		TreeViewColumn colCommit, colRemote;
		ListStore filestore;
		
		bool commitShown = false;
		VBox boxCommit;
		TextView textCommitMessage;
		Button buttonCommitCancel;
		Button buttonCommitCommit;
		
		Node[] statuses;
		Hashtable markedCommit = new Hashtable();
		
		bool remoteStatus = false;
		
		public static bool Show(string path, bool test) {
			foreach (VersionControlSystem vc in VersionControlService.Providers) {
				if (vc.IsStatusAvailable(path)) {
					if (test) return true;
					StatusView d = new StatusView(path, vc);
					MonoDevelop.Ide.Gui.IdeApp.Workbench.OpenDocument (d, true);
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
			textCommitMessage.HeightRequest = 100;
			buttonCommitCancel.Show();
			buttonCommitCommit.Show();
			boxCommit.PackStart(boxCommitButtons, false, false, 0);
			boxCommit.PackStart(textCommitMessage, true, false, 0);
			boxCommitButtons.PackStart(new Label("Select Files and Enter Commit Message..."), true, false, 0);
			boxCommitButtons.PackEnd(buttonCommitCancel, false, false, 0);
			boxCommitButtons.PackEnd(buttonCommitCommit, false, false, 0);
			buttonCommitCancel.Clicked += new EventHandler(OnCommitCancelClicked);
			buttonCommitCommit.Clicked += new EventHandler(OnCommitCommitClicked);
			
			status = new Label("");
			main.PackStart(status, false, false, 0);
			
			ScrolledWindow scroller = new ScrolledWindow();
			main.PackStart(scroller, true, true, 5);
			filelist = new TreeView();
			scroller.Add(filelist);
			scroller.HscrollbarPolicy = PolicyType.Never;
			scroller.VscrollbarPolicy = PolicyType.Always;

			filelist.RowActivated += new RowActivatedHandler(OnRowActivated);
			CellRendererToggle cellToggle = new CellRendererToggle();
			cellToggle.Toggled += new ToggledHandler(OnCommitToggledHandler);
			TreeViewColumn colIcon = new TreeViewColumn("", new CellRendererPixbuf(), "pixbuf", 0);
			colCommit = new TreeViewColumn("Commit", cellToggle, "active", 4);
			TreeViewColumn colStatus = new TreeViewColumn("Status", new CellRendererText(), "text", 1);
			TreeViewColumn colFile = new TreeViewColumn("File", new CellRendererText(), "text", 2);
			colRemote = new TreeViewColumn("Remote Status", new CellRendererText(), "text", 3);
			
			filelist.AppendColumn(colIcon);
			filelist.AppendColumn(colCommit);
			filelist.AppendColumn(colStatus);
			filelist.AppendColumn(colFile);
			filelist.AppendColumn(colRemote);
			
			colCommit.Visible = false;
			colRemote.Visible = false;

			filestore = new ListStore(typeof (Gdk.Pixbuf), typeof (string), typeof (string), typeof (string), typeof(bool));
			filelist.Model = filestore;

			main.ShowAll();
			
			StartUpdate();
		}
		
		public override Gtk.Widget Control { 
			get {
				return widget;
			}
		}
		
		private void StartUpdate() {
			if (!remoteStatus)
				status.Text = "Scanning for changes...";
			else
				status.Text = "Scanning for local and remote changes...";
			
			status.Visible = true;
			filelist.Visible = false;
			
			showRemoteStatus.Sensitive = false;
			buttonCommit.Sensitive = false;
			
			new Worker(vc, filepath, remoteStatus, this).Start();
		}
		
		private void Update() {
			showRemoteStatus.Sensitive = !remoteStatus;
			
			if (statuses.Length == 0) {
				if (!remoteStatus)
					status.Text = "No files have local modifications.";
				else
					status.Text = "No files have local or remote modifications.";
				return;
			}
			
			status.Visible = false;
			filelist.Visible = true;
			
			if (vc.CanCommit(filepath))
				buttonCommit.Sensitive = true;
						
			filestore.Clear();

			colCommit.Visible = commitShown;
			colRemote.Visible = remoteStatus;
		
			for (int i = 0; i < statuses.Length; i++) {
				Node n = statuses[i];
				
				Gdk.Pixbuf statusicon = VersionControlService.LoadIconForStatus(n.Status);
				
				string lstatus = n.Status.ToString();
				
				string localpath = n.LocalPath.Substring(filepath.Length);
				if (localpath.Length > 0 && localpath[0] == Path.DirectorySeparatorChar) localpath = localpath.Substring(1);
				if (localpath == "") { localpath = "."; } // not sure if this happens
				
				string rstatus = n.RemoteStatus.ToString();
				
				bool commitStatus = markedCommit.ContainsKey(n.LocalPath);
				
				filestore.AppendValues(statusicon, lstatus, localpath, rstatus, commitStatus);
			}
			
			buttonCommitCommit.Sensitive = (markedCommit.Count > 0);
		}
		
		void OnRowActivated(object o, RowActivatedArgs args) {
			int index = args.Path.Indices[0];
			Node node = statuses[index];
			DiffView.Show(node.LocalPath, false);
		}
		
		void OnCommitToggledHandler(object o, ToggledArgs args) {
			TreeIter pos;
			if (!filestore.GetIterFromString(out pos, args.Path))
				return;

			int index = int.Parse(args.Path); // why is it a string?
			string localpath = ((Node)statuses[index]).LocalPath;
			
			if (markedCommit.ContainsKey(localpath)) {
				markedCommit.Remove(localpath);
			} else {
				markedCommit[localpath] = markedCommit;
			}
			filestore.SetValue(pos, 4, markedCommit.ContainsKey(localpath));
			buttonCommitCommit.Sensitive = (markedCommit.Count > 0);
		}
		
		private void OnShowRemoteStatusClicked(object src, EventArgs args) {
			if (commitShown)
				buttonCommitCancel.Click();
		
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
		
		private void OnCommitClicked(object src, EventArgs args) {
			buttonCommit.Sensitive = false;
			main.PackEnd(boxCommit, false, false, 5);
			boxCommit.ShowAll();
			commitShown = true;
			colCommit.Visible = true;
		}
		
		private void OnCommitCommitClicked(object src, EventArgs args) {
			ArrayList paths = new ArrayList(markedCommit.Keys);
			if (paths.Count == 0)
				return;
			
			new CommitWorker(
				vc,
				(string[])paths.ToArray(typeof(string)),
				textCommitMessage.Buffer.Text,
				this).Start();
				
			OnCommitCancelClicked(null, null);
		}
		
		private void OnCommitCancelClicked(object src, EventArgs args) {
			colCommit.Visible = false;
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
				if (view.statuses == null) return;
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
