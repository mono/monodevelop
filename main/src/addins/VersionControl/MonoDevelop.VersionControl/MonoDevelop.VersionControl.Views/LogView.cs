using System;
using System.IO;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Linq;

namespace MonoDevelop.VersionControl.Views
{
	public interface ILogView
	{
	}
	
	class LogView : BaseView, ILogView
	{
		LogWidget widget;
		VersionInfo vinfo;
		
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
		public LogView (VersionControlDocumentInfo info) : base (GettextCatalog.GetString ("Log"), GettextCatalog.GetString ("Shows the source control log for the current file"))
		{
			this.info = info;
		}
		
		void CreateControlFromInfo ()
		{
			var lw = new LogWidget (info);
			
			widget = lw;
			info.Updated += OnInfoUpdated;
			lw.History = this.info.History;
			vinfo   = this.info.Item.VersionInfo;
		
			if (WorkbenchWindow != null)
				widget.SetToolbar (WorkbenchWindow.GetToolbar (this));
		}

		void OnInfoUpdated (object sender, EventArgs e)
		{
			widget.History = this.info.History;
			vinfo   = this.info.Item.VersionInfo;
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
			info.Item.VersionInfo = vinfo;
			var lw = new LogWidget (info);
			
			widget = lw;
			lw.History = history;
		}

		
		public override Control Control { 
			get {
				if (widget == null)
					CreateControlFromInfo ();
				return widget; 
			}
		}

		protected override void OnWorkbenchWindowChanged ()
		{
			base.OnWorkbenchWindowChanged ();
			if (WorkbenchWindow != null && widget != null)
				widget.SetToolbar (WorkbenchWindow.GetToolbar (this));
		}
		
		public override void Dispose ()
		{
			if (widget != null) {
				widget.Destroy ();
				widget = null;
			}
			if (info != null) {
				info.Updated -= OnInfoUpdated;
				info = null;
			}
			base.Dispose ();
		}

		public void Init ()
		{
			if (info != null && !info.Started) {
				widget.ShowLoading ();
				info.Start ();
			}
		}

		protected override void OnSelected ()
		{
			Init ();
		}

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
