//
// AddDeployTargetDialog.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Projects.Deployment;
using MonoDevelop.Projects.Gui.Deployment;

namespace MonoDevelop.Projects.Gui
{
	public class AddDeployTargetDialog: IDisposable
	{
		[Glade.Widget] Gtk.Button okbutton;
		[Glade.Widget] Gtk.Entry entryName;
		[Glade.Widget] Gtk.ComboBox comboHandlers;
		[Glade.Widget] Gtk.VBox targetOptionsBox;
		[Glade.Widget ("AddDeployTargetDialog")] Gtk.Dialog dialog;
		
		DeployHandler[] handlers;
		DeployHandler currentHandler;
		Gtk.Widget currentEditor;
		DeployTarget target;
		CombineEntry entry;
		
		public AddDeployTargetDialog (CombineEntry entry)
		{
			Glade.XML glade = new Glade.XML (null, "Base.glade", "AddDeployTargetDialog", null);
			glade.Autoconnect (this);
			
			this.entry = entry;
			
			ListStore store = new ListStore (typeof(Gdk.Pixbuf), typeof(string));
			comboHandlers.Model = store;
			
			Gtk.CellRenderer cr = new Gtk.CellRendererPixbuf();
			comboHandlers.PackStart (cr, false);
			comboHandlers.SetAttributes (cr, "pixbuf", 0);
			
			cr = new Gtk.CellRendererText();
			comboHandlers.PackStart (cr, true);
			comboHandlers.SetAttributes (cr, "text", 1);
			
			handlers = MonoDevelop.Projects.Services.DeployService.GetDeployHandlers (entry);
			foreach (DeployHandler handler in handlers) {
				Gdk.Pixbuf pix = MonoDevelop.Core.Gui.Services.Resources.GetIcon (handler.Icon, Gtk.IconSize.Menu);
				store.AppendValues (pix, handler.Description);
			}
			
			comboHandlers.Changed += OnSelectionChanged;
			comboHandlers.Active = 0;
			dialog.Resize (500, 300);
			dialog.ShowAll ();
		}
		
		public int Run ()
		{
			return dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
		
		protected void OnNameChanged (object s, EventArgs args)
		{
			okbutton.Sensitive = entryName.Text.Length > 0;
		}
		
		protected void OnSelectionChanged (object s, EventArgs args)
		{
			if (currentEditor != null) {
				targetOptionsBox.Remove (currentEditor);
				currentEditor.Destroy ();
			}
			
			if (comboHandlers.Active == -1)
				return;

			bool hasDefaultName = target == null || entryName.Text == target.GetDefaultName (entry) || entryName.Text.Length == 0;
			
			currentHandler = handlers [comboHandlers.Active];
			target = currentHandler.CreateTarget (entry);
			currentEditor = new DeployTargetEditor (target);
			targetOptionsBox.PackStart (currentEditor, false, false, 0);
			targetOptionsBox.ShowAll ();
			
			if (hasDefaultName)
				entryName.Text = target.GetDefaultName (entry);
		}
		
		public DeployTarget NewTarget {
			get {
				target.Name = entryName.Text;
				return target; 
			}
		}
	}
}
