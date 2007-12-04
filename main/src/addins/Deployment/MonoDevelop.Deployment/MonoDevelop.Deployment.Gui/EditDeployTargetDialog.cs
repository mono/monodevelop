//
// EditDeployTargetDialog.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Deployment;
using MonoDevelop.Deployment.Gui;

namespace MonoDevelop.Projects.Gui
{
	public class EditDeployTargetDialog: IDisposable
	{
		[Glade.Widget] Gtk.Button okbutton;
		[Glade.Widget] Gtk.Entry entryName;
		[Glade.Widget] Gtk.Image iconHandler;
		[Glade.Widget] Gtk.Label labelHandler;
		[Glade.Widget] Gtk.VBox targetBox;
		[Glade.Widget ("EditDeployTargetDialog")] Gtk.Dialog dialog;
		PackageBuilder target;
		
		public EditDeployTargetDialog (PackageBuilder target, CombineEntry entry)
		{
			Glade.XML glade = new Glade.XML (null, "Base.glade", "EditDeployTargetDialog", null);
			glade.Autoconnect (this);
			this.target = target;
			
			labelHandler.Markup = "<b>" + target.Description + "</b>";
			iconHandler.Pixbuf = MonoDevelop.Core.Gui.Services.Resources.GetIcon (target.Icon, Gtk.IconSize.Menu);
			entryName.Text = target.Name;
			
			targetBox.PackStart (new PackageBuilderEditor (target, entry), true, true, 0);
			
			dialog.DefaultWidth = 500;
			dialog.DefaultHeight = 400;
			dialog.ShowAll ();
			dialog.Resize (500, 400);
		}
		
		protected void OnNameChanged (object s, EventArgs args)
		{
			target.Name = entryName.Text;
			okbutton.Sensitive = entryName.Text.Length > 0;
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
		
		public int Run ()
		{
			return dialog.Run ();
		}
	}
}
