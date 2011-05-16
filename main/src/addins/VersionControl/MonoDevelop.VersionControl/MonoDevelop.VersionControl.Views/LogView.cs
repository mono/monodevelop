using System;
using System.IO;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Text;
using System.Linq;

namespace MonoDevelop.VersionControl.Views
{
	public class LogView : BaseView, IAttachableViewContent 
	{
		string filepath;
		LogWidget widget;
		Repository vc;
		VersionInfo vinfo;
		
		ListStore changedpathstore;
		
		public LogWidget LogWidget {
			get {
				return this.widget;
			}
		}
		
		public static void Show (VersionControlItemList items, Revision since)
		{
			foreach (VersionControlItem item in items) {
				if (!item.IsDirectory) {
					var document = IdeApp.Workbench.OpenDocument (item.Path, OpenDocumentOptions.Default | OpenDocumentOptions.OnlyInternalViewer);
					if (document != null) {
						DiffView.AttachViewContents (document, item);
						document.Window.SwitchView (document.Window.FindView (typeof(LogView)));
					} else {
						VersionControlDocumentInfo info = new VersionControlDocumentInfo (null, item, item.Repository);
						LogView logView = new LogView (info);
						info.Document = IdeApp.Workbench.OpenDocument (logView, true);
						logView.Selected ();
					}
				} else if (item.VersionInfo.CanLog) {
					new Worker (item.Repository, item.Path, item.IsDirectory, since).Start ();
				}
			}
		}
		
		class Worker : Task {
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
				IdeApp.Workbench.OpenDocument (d, true);
			}
		}
		
		public static bool CanShow (VersionControlItemList items, Revision since)
		{
			return items.All (i => i.VersionInfo.CanLog);
		}
		
		VersionControlDocumentInfo info;
		public LogView (VersionControlDocumentInfo info) : base (GettextCatalog.GetString ("Log"))
		{
			this.info = info;
		}
		
		void CreateControlFromInfo ()
		{
			this.vc = info.Item.Repository;
			this.filepath = info.Item.Path;
			var lw = new LogWidget (info);
			
			widget = lw;
			info.Updated += delegate {
				lw.History = this.info.History;
				vinfo   = this.info.VersionInfo;
			};
			lw.History = this.info.History;
			vinfo   = this.info.VersionInfo;
		
		}
		
		public LogView (string filepath, bool isDirectory, Revision [] history, Repository vc) 
			: base (Path.GetFileName (filepath) + " Log")
		{
			this.vc = vc;
			this.filepath = filepath;
			
			try {
				this.vinfo = vc.GetVersionInfo (filepath, false);
			}
			catch (Exception ex) {
				MessageService.ShowException (ex, GettextCatalog.GetString ("Version control command failed."));
			}
			
			// Widget setup
			VersionControlDocumentInfo info  =new VersionControlDocumentInfo (null, null, vc);
			info.History = history;
			info.VersionInfo = vinfo;
			var lw = new LogWidget (info);
			
			widget = lw;
			lw.History = history;
		}

		
		public override Gtk.Widget Control { 
			get {
				if (widget == null)
					CreateControlFromInfo ();
				return widget; 
			}
		}
		
		public override void Dispose ()
		{
			if (widget != null) {
				widget.Destroy ();
				widget = null;
			}
			if (changedpathstore != null) {
				changedpathstore.Dispose ();
				changedpathstore = null;
			}
			base.Dispose ();
		}

		
/*		internal class DiffWorker : Task {
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
				DiffView.Show (name + " (revision " + revision.ToString () + ")", DesktopService.GetMimeTypeForUri (revPath), text1, text2);
			}
		}*/
		
		/// Background worker to create a revision-specific diff for a directory
		internal class DirectoryDiffWorker: Task
		{
			FilePath path;
			Repository repo;
			Revision revision;
			string name;
			string patch;
			
			public DirectoryDiffWorker (FilePath path, Repository repo, Revision revision)
			{
				this.path = path;
				name = string.Format ("{0} (revision {1})", path.FileName, revision);
				this.repo = repo;
				this.revision = revision;
			}
			
			protected override string GetDescription ()
			{
				return GettextCatalog.GetString ("Retrieving changes in {0} ...", name, revision);
			}
			
			
			protected override void Run ()
			{
				DiffInfo[] diffs = repo.PathDiff (path, revision.GetPrevious (), revision);
				patch = repo.CreatePatch (diffs);
			}
			
			protected override void Finished ()
			{
				if (patch != null)
					IdeApp.Workbench.NewDocument (name, "text/x-diff", patch);
			}
		}
		
		#region IAttachableViewContent implementation
		public void Selected ()
		{
			if (info != null && !info.Started) {
				widget.ShowLoading ();
				info.Start ();
			}
		}

		public void Deselected ()
		{
		}

		public void BeforeSave ()
		{
		}

		public void BaseContentChanged ()
		{
		}
		#endregion
	}

	internal class HistoricalFileView
	{
		public static void Show (string name, string file, string text) {
			string mimeType = DesktopService.GetMimeTypeForUri (file);
			if (mimeType == null || mimeType.Length == 0)
				mimeType = "text/plain";
			Document doc = IdeApp.Workbench.NewDocument (name, mimeType, text);
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
				return GettextCatalog.GetString ("Retrieving content of {0} at revision {1}...", name, revision);
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
