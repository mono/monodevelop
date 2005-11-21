//
// AddinManagerDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using Gtk;
using Glade;
using MonoDevelop.Core.AddIns;
using MonoDevelop.Core.AddIns.Setup;

namespace MonoDevelop.Core.Gui.Dialogs
{
	class AddinManagerDialog : IDisposable
	{
		[Glade.Widget ("AddinManagerDialog")] Dialog dialog;
		[Glade.Widget] Gtk.TreeView addinTree;
		[Glade.Widget] Button btnEnable;
		[Glade.Widget] Button btnDisable;
		[Glade.Widget] Button btnUninstall;
		[Glade.Widget] Button btnInfo;
		[Glade.Widget] Image imageInstall;
		AddinTreeWidget tree;
		
		public AddinManagerDialog (Window parent)
		{
			new Glade.XML (null, "Base.glade", "AddinManagerDialog", null).Autoconnect (this);
			dialog.TransientFor = parent;
			imageInstall.Stock = "md-software-update";
			imageInstall.IconSize = (int)IconSize.Dialog;

			tree = new AddinTreeWidget (addinTree);
			LoadAddins ();
			UpdateButtons ();
		}
		
		public void Show ()
		{
			dialog.Show ();
		}
		
		public void Run ()
		{
			dialog.Show ();
			dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
			dialog.Dispose ();
		}
		
		protected void OnSelectionChanged (object sender, EventArgs args)
		{
			UpdateButtons ();
		}
		
		protected void OnInstall (object sender, EventArgs e)
		{
			using (AddinInstallDialog dlg = new AddinInstallDialog ()) {
				dlg.Run ();
				LoadAddins ();
			}
		}
		
		protected void OnUpdate (object sender, EventArgs e)
		{
		}
		
		protected void OnUninstall (object sender, EventArgs e)
		{
			AddinInfo info = (AddinInfo) tree.ActiveAddin;
			using (AddinInstallDialog dlg = new AddinInstallDialog ()) {
				dlg.SetUninstallMode (info);
				dlg.Run ();
				LoadAddins ();
			}
		}
		
		protected void OnEnable (object sender, EventArgs e)
		{
			AddinSetupInfo sinfo = (AddinSetupInfo) tree.ActiveAddinData;
			if (sinfo == null)
				return;
			sinfo.Enabled = true;
			LoadAddins ();
		}
		
		protected void OnDisable (object sender, EventArgs e)
		{
			AddinSetupInfo sinfo = (AddinSetupInfo) tree.ActiveAddinData;
			if (sinfo == null)
				return;
			sinfo.Enabled = false;
			LoadAddins ();
		}
		
		protected void OnShowInfo (object sender, EventArgs e)
		{
			AddinSetupInfo sinfo = (AddinSetupInfo) tree.ActiveAddinData;
			if (sinfo == null)
				return;

			using (AddinInfoDialog dlg = new AddinInfoDialog (sinfo.Addin)) {
				dlg.Run ();
			}
		}
		
		protected void OnManageRepos (object sender, EventArgs e)
		{
			using (ManageSitesDialog dlg = new ManageSitesDialog ()) {
				dlg.Run ();
			}
		}
		
		void LoadAddins ()
		{
			object s = tree.SaveStatus ();
			
			tree.Clear ();
			foreach (AddinSetupInfo ainfo in Runtime.SetupService.GetInstalledAddins ()) {
				string icon = ainfo.IsUserAddin ? "md-user-package" : "md-package";
				tree.AddAddin (ainfo.Addin, ainfo, ainfo.Enabled, icon);
			}
			
			tree.RestoreStatus (s);
			UpdateButtons ();
		}
		
		void UpdateButtons ()
		{
			AddinSetupInfo sinfo = (AddinSetupInfo) tree.ActiveAddinData;
			if (sinfo == null) {
				btnEnable.Sensitive = false;
				btnDisable.Sensitive = false;
				btnUninstall.Sensitive = false;
				btnInfo.Sensitive = false;
			} else {
				if (sinfo.Addin.Id == "MonoDevelop.Core") {
					btnEnable.Sensitive = false;
					btnDisable.Sensitive = false;
					btnUninstall.Sensitive = false;
				} else {
					btnEnable.Sensitive = !sinfo.Enabled;
					btnDisable.Sensitive = sinfo.Enabled;
					btnUninstall.Sensitive = true;
				}
				btnInfo.Sensitive = true;
			}
		}
	}
	
	class SetupApp: IApplication
	{
		public int Run (string[] args)
		{
			Application.Init ();
		
			using (AddinManagerDialog dlg = new AddinManagerDialog (null)) {
				dlg.Run ();
			}
			
			return 0;
		}
	}
}
