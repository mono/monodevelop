//
// AddinManagerDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using Mono.Addins.Setup;
using Mono.Addins;

namespace Mono.Addins.Gui
{
	partial class AddinManagerDialog : Dialog, IDisposable
	{
		AddinTreeWidget tree;
		SetupService service = new SetupService ();
		
		internal bool AllowInstall
		{
			set {
				this.btnInstall.Visible = value;
				this.btnRepositories.Visible = value;
				this.hseparator4.Visible = value;
				this.btnUninstall.Visible = value;
			}
		}
		
		public AddinManagerDialog (Window parent)
		{
			Build ();
			TransientFor = parent;

			tree = new AddinTreeWidget (addinTree);
			LoadAddins ();
			UpdateButtons ();
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
			Destroy ();
		}
		
		internal void OnSelectionChanged (object sender, EventArgs args)
		{
			UpdateButtons ();
		}
		
		internal void OnInstall (object sender, EventArgs e)
		{
			AddinInstallDialog dlg = new AddinInstallDialog (service);
			try {
				dlg.Run ();
				LoadAddins ();
			} finally {
				dlg.Destroy ();
			}
		}
		
		internal void OnUpdate (object sender, EventArgs e)
		{
		}
		
		internal void OnUninstall (object sender, EventArgs e)
		{
			AddinHeader info = (AddinHeader) tree.ActiveAddin;
			AddinInstallDialog dlg = new AddinInstallDialog (service);
			try {
				dlg.SetUninstallMode (info);
				dlg.Run ();
				LoadAddins ();
			} finally {
				dlg.Destroy ();
			}
		}
		
		internal void OnEnable (object sender, EventArgs e)
		{
			try {
				Addin sinfo = (Addin) tree.ActiveAddinData;
				if (sinfo == null)
					return;
				sinfo.Enabled = true;
				LoadAddins ();
			}
			catch (Exception ex) {
				Services.ShowError (ex, null, this, true);
			}
		}
		
		internal void OnDisable (object sender, EventArgs e)
		{
			try {
				Addin sinfo = (Addin) tree.ActiveAddinData;
				if (sinfo == null)
					return;
				sinfo.Enabled = false;
				LoadAddins ();
			}
			catch (Exception ex) {
				Services.ShowError (ex, null, this, true);
			}
		}
		
		internal void OnShowInfo (object sender, EventArgs e)
		{
			Addin sinfo = (Addin) tree.ActiveAddinData;
			if (sinfo == null)
				return;

			AddinInfoDialog dlg = new AddinInfoDialog (SetupService.GetAddinHeader (sinfo));
			try {
				dlg.Run ();
			} finally {
				dlg.Destroy ();
			}
		}
		
		internal void OnManageRepos (object sender, EventArgs e)
		{
			ManageSitesDialog dlg = new ManageSitesDialog (service);
			dlg.TransientFor = this;
			try {
				dlg.Run ();
			} finally {
				dlg.Destroy ();
			}
		}
		
		void LoadAddins ()
		{
			object s = tree.SaveStatus ();
			
			tree.Clear ();
			foreach (Addin ainfo in AddinManager.Registry.GetAddins ()) {
				if (Services.InApplicationNamespace (service, ainfo.Id))
					tree.AddAddin (SetupService.GetAddinHeader (ainfo), ainfo, ainfo.Enabled, ainfo.IsUserAddin);
			}
			
			tree.RestoreStatus (s);
			UpdateButtons ();
		}
		
		void UpdateButtons ()
		{
			Addin sinfo = (Addin) tree.ActiveAddinData;
			if (sinfo == null) {
				btnEnable.Sensitive = false;
				btnDisable.Sensitive = false;
				btnUninstall.Sensitive = false;
				btnInfo.Sensitive = false;
			} else {
				btnEnable.Sensitive = !sinfo.Enabled;
				btnDisable.Sensitive = sinfo.Enabled;
				btnUninstall.Sensitive = true;
				btnInfo.Sensitive = true;
			}
		}
	}
}
