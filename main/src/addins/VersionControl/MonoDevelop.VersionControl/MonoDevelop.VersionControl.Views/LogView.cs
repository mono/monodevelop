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
	public interface ILogView : IAttachableViewContent
	{
	}
	
	public class LogView : BaseView, ILogView 
	{
		LogWidget widget;
		VersionInfo vinfo;
		
		ListStore changedpathstore;
		
		public LogWidget LogWidget {
			get {
				return this.widget;
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
			var lw = new LogWidget (info);
			
			widget = lw;
			info.Updated += delegate {
				lw.History = this.info.History;
				vinfo   = this.info.VersionInfo;
			};
			lw.History = this.info.History;
			vinfo   = this.info.VersionInfo;
		
			if (WorkbenchWindow != null)
				widget.SetToolbar (WorkbenchWindow.GetToolbar (this));
		}
		
		public LogView (string filepath, bool isDirectory, Revision [] history, Repository vc) 
			: base (Path.GetFileName (filepath) + " Log")
		{
			try {
				this.vinfo = vc.GetVersionInfo (filepath);
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

		protected override void OnWorkbenchWindowChanged (EventArgs e)
		{
			base.OnWorkbenchWindowChanged (e);
			if (WorkbenchWindow != null && widget != null)
				widget.SetToolbar (WorkbenchWindow.GetToolbar (this));
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

}
