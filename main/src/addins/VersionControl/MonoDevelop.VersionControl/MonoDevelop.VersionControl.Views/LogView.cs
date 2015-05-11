using System;
using System.IO;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Linq;
using System.Collections.Generic;

namespace MonoDevelop.VersionControl.Views
{
	public class LogView : BaseView, IAttachableViewContent
	{
		LogWidget widget;
		VersionInfo vinfo;
		
		ListStore changedpathstore;
		
		public LogWidget LogWidget {
			get {
				return widget;
			}
		}

		public static bool CanShow (List<VersionControlItem> items, Revision since)
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
				vinfo   = this.info.Item.VersionInfo;
			};
			lw.History = this.info.History;
			vinfo   = this.info.Item.VersionInfo;
		
			if (WorkbenchWindow != null)
				widget.SetToolbar (WorkbenchWindow.GetToolbar (this));
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
