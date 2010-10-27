//
// BindDesignDialog.cs
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
using System.Collections;

using Glade;
using Gtk;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Ide;

namespace MonoDevelop.GtkCore.Dialogs
{
	class BindDesignDialog: IDisposable
	{
		[Glade.Widget ("BindDesignDialog")] protected Gtk.Dialog dialog;
		[Glade.Widget] protected Gtk.Label labelMessage;
		[Glade.Widget] protected Gtk.ComboBox comboClasses;
		[Glade.Widget] protected Gtk.Entry entryClassName;
		[Glade.Widget] protected Gtk.Entry entryNamespace;
		[Glade.Widget] protected Gtk.RadioButton radioSelect;
		[Glade.Widget] protected Gtk.RadioButton radioCreate;
		[Glade.Widget] protected Gtk.Table tableNewClass;
		[Glade.Widget] protected Gtk.Button okButton;
		[Glade.Widget] protected Gtk.EventBox fileEntryBox;
		
		FolderEntry fileEntry;
		
		ListStore store;
		static string lastNamespace = "";
		
		public BindDesignDialog (string id, ArrayList validClasses, string baseFolder)
		{
			XML glade = new XML (null, "gui.glade", "BindDesignDialog", null);
			glade.Autoconnect (this);
			labelMessage.Text = GettextCatalog.GetString ("The widget design {0} is not currently bound to a class.", id);
			
			fileEntry = new FolderEntry ();
			fileEntryBox.Add (fileEntry);
			fileEntry.ShowAll ();
			
			if (validClasses.Count > 0) {
			
				store = new ListStore (typeof (string));
				foreach (string cname in validClasses)
					store.AppendValues (cname);
				comboClasses.Model = store;
				CellRendererText cr = new CellRendererText ();
				comboClasses.PackStart (cr, true);
				comboClasses.AddAttribute (cr, "text", 0);
				comboClasses.Active = 0;
				
			} else {
				radioSelect.Sensitive = false;
				radioCreate.Active = true;
			}
			
			fileEntry.Path = baseFolder;
			
			// Initialize the class name using the widget name
			int i = id.IndexOf ('.');
			if (i != -1) {
				entryClassName.Text = id.Substring (i+1);
				entryNamespace.Text = id.Substring (0,i);
			} else {
				entryClassName.Text = id;
				entryNamespace.Text = lastNamespace;
			}
			
			dialog.Response += new Gtk.ResponseHandler (OnResponse);
			UpdateStatus ();
		}
		
		void OnResponse (object ob, Gtk.ResponseArgs args)
		{
			dialog.Response -= new Gtk.ResponseHandler (OnResponse);
			if (args.ResponseId == ResponseType.Ok && radioCreate.Active)
				lastNamespace = Namespace;
		}
		
		public bool Run ()
		{
			dialog.TransientFor = IdeApp.Workbench.RootWindow;
			return dialog.Run () == (int) ResponseType.Ok;
		}
		
		public bool CreateNew {
			get { return radioCreate.Active; }
		}
		
		public string ClassName {
			get {
				if (radioCreate.Active) {
					return entryClassName.Text;
				} else {
					Gtk.TreeIter it;
					if (!comboClasses.GetActiveIter (out it))
						return "";
					string s = (string) store.GetValue (it, 0);
					int i = s.IndexOf ('.');
					if (i != -1)
						return s.Substring (i+1);
					else
						return s;
				}
			}
		}
		
		public string Namespace {
			get {
				if (radioCreate.Active) {
					return entryNamespace.Text;
				} else {
					Gtk.TreeIter it;
					if (!comboClasses.GetActiveIter (out it))
						return "";
					string s = (string) store.GetValue (it, 0);
					int i = s.IndexOf ('.');
					if (i != -1)
						return s.Substring (0, i);
					else
						return "";
				}
			}
		}
		
		public string Folder {
			get { return fileEntry.Path; }
		}
		
		protected void OnSelectToggled (object ob, EventArgs args)
		{
			UpdateStatus ();
		}
		
		protected void OnEntryChanged (object ob, EventArgs a)
		{
			UpdateStatus ();
		}
		
		void UpdateStatus ()
		{
			if (radioSelect.Active) {
				tableNewClass.Sensitive = false;
				comboClasses.Sensitive = true;
				okButton.Sensitive = true;
			} else {
				tableNewClass.Sensitive = true;
				comboClasses.Sensitive = false;
				okButton.Sensitive = ClassName != "" && Folder != "";
			}
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
	}
	
}
