// RegistrySelector.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using MonoDevelop.Core;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class RegistrySelector : Gtk.Bin
	{
		string registryPath;
		string startupPath;
		
		public event EventHandler Changed;
		
		public RegistrySelector()
		{
			this.Build();
		}
		
		public string RegistryPath {
			get {
				return registryPath; 
			}
			set {
				registryPath = value;
				UpdateLabel ();
			}
		}

		public string StartupPath {
			get {
				return startupPath;
			}
			set {
				startupPath = value;
				UpdateLabel ();
			}
		}

		protected virtual void OnButtonBrowseClicked (object sender, System.EventArgs e)
		{
			SelectRepositoryDialog dlg = new SelectRepositoryDialog (registryPath, startupPath);
			dlg.TransientFor = this.Toplevel as Gtk.Window;
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				registryPath = dlg.RegistryPath;
				startupPath = dlg.StartupPath;
				UpdateLabel ();
				if (Changed != null)
					Changed (this, EventArgs.Empty);
			}
			dlg.Destroy ();
		}
		
		void UpdateLabel ()
		{
			if (registryPath != null)
				label.Text = AddinAuthoringService.GetRegistryName (registryPath);
			else
				label.Text = AddinManager.CurrentLocalizer.GetString ("(No selection)");
		}
	}
}
