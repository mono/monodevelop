using System;
using System.IO;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
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
				return widget;
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
			info.Updated += OnInfoUpdated;
			lw.History = this.info.History;
			vinfo   = this.info.VersionInfo;
		
			if (WorkbenchWindow != null)
				widget.SetToolbar (WorkbenchWindow.GetToolbar (this));
		}

		void OnInfoUpdated (object sender, EventArgs e)
		{
			widget.History = this.info.History;
			vinfo   = this.info.VersionInfo;
		}

		[Obsolete]
		public LogView (string filepath, bool isDirectory, Revision [] history, Repository vc) 
			: base (Path.GetFileName (filepath) + " Log")
		{
			try {
				this.vinfo = vc.GetVersionInfo (filepath, VersionInfoQueryFlags.IgnoreCache);
			}
			catch (Exception ex) {
				MessageService.ShowError (GettextCatalog.GetString ("Version control command failed."), ex);
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
			if (info != null) {
				info.Updated -= OnInfoUpdated;
				info = null;
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

		[CommandHandler (MonoDevelop.Ide.Commands.EditCommands.Copy)]
		protected void OnCopy ()
		{
			string data = widget.DiffText;
			if (data == null)
				return;

			var clipboard = Clipboard.Get (Gdk.Atom.Intern ("CLIPBOARD", false));
			clipboard.Text = data;
			clipboard = Clipboard.Get (Gdk.Atom.Intern ("PRIMARY", false));
			clipboard.Text = data;
		}
	}

}
