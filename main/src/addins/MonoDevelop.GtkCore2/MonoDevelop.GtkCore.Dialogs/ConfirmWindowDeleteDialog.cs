//
// ConfirmWindowDeleteDialog.cs
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
using Glade;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore.Dialogs
{
	class ConfirmWindowDeleteDialog: IDisposable
	{
		[Glade.Widget ("ConfirmWindowDeleteDialog")] protected Gtk.Dialog dialog;
		[Glade.Widget] protected Gtk.Label label;
		[Glade.Widget] protected Gtk.CheckButton checkbox;
		
		public ConfirmWindowDeleteDialog (string windowName, string fileName, Stetic.ProjectItemInfo obj)
		{
			XML glade = new XML (null, "gui.glade", "ConfirmWindowDeleteDialog", null);
			glade.Autoconnect (this);
			
			if (obj is Stetic.WidgetInfo && ((Stetic.WidgetInfo)obj).IsWindow) {
				label.Text = GettextCatalog.GetString ("Are you sure you want to delete the window '{0}'?", windowName);
			} else if (obj is Stetic.WidgetInfo) {
				label.Text = GettextCatalog.GetString ("Are you sure you want to delete the widget '{0}'?", windowName);
			} else if (obj is Stetic.ActionGroupInfo) {
				label.Text = GettextCatalog.GetString ("Are you sure you want to delete the action group '{0}'?", windowName);
			} else
				label.Text = GettextCatalog.GetString ("Are you sure you want to delete '{0}'?", windowName);
			
			if (fileName != null) {
				checkbox.Label = string.Format (checkbox.Label, fileName);
				checkbox.Active = true;
			} else
				checkbox.Hide ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
		
		public int Run ()
		{
			dialog.TransientFor = IdeApp.Workbench.RootWindow;
			return dialog.Run ();
		}
		
		public bool DeleteFile {
			get { return checkbox.Active; }
		}
	}	
}
