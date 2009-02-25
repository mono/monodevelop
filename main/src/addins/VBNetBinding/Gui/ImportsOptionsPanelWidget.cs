// 
// ImportsOptionPanelWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using System;
using System.Collections.Generic;
using System.IO;

using Mono.Addins;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core;
using MonoDevelop.Components;


namespace MonoDevelop.VBNetBinding
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ImportsOptionsPanelWidget : Gtk.Bin
	{
		Gtk.ListStore imports = new Gtk.ListStore (typeof (String));
		List<Import> currentImports;
		DotNetProject project;
		
		public ImportsOptionsPanelWidget (MonoDevelop.Projects.Project project)
		{
			this.Build();

			this.project = (DotNetProject) project;
			currentImports = new List<Import> (project.Items.GetAll<Import> ());
			
			treeview1.AppendColumn ("Import", new Gtk.CellRendererText (), "text", 0);
			treeview1.Model = imports;
			imports.SetSortColumnId (0, Gtk.SortType.Ascending);
			LoadImports ();
		}
		
		public void StorePanelContents ()
		{
			List<Import> oldImports = new List<Import> (project.Items.GetAll<Import> ());
			foreach (Import i in oldImports)
				project.Items.Remove (i);
			foreach (Import i in currentImports)
				project.Items.Add (i);
		}
		
		protected virtual void OnCmdAddClicked (object sender, System.EventArgs e)
		{
			bool exists = false;
			
			foreach (Import import in currentImports) {
				if (import.Include == txtImport.Text) {
					exists = true;
					break;
				}
			}

			if (!exists) {
				currentImports.Add (new Import (txtImport.Text));
				LoadImports ();
			}
		}

		protected virtual void OnCmdRemoveClicked (object sender, System.EventArgs e)
		{
			bool removed = false;

			Console.WriteLine ("OnCmdRemoveClicked");
			treeview1.Selection.SelectedForeach (delegate (Gtk.TreeModel model, Gtk.TreePath path, Gtk.TreeIter iter) 
			{
				string import;
				GLib.Value value = new GLib.Value ();
				
				model.GetValue (iter, 0, ref value);

				import = value.Val as string;

				if (string.IsNullOrEmpty (import))
					return;
				
				foreach (Import im in currentImports) {
					if (im.Include == import) {
						currentImports.Remove (im);
						removed = true;
						break;
					}
				}
				
			});
			if (removed)
				LoadImports ();
		}

		private void LoadImports ()
		{
			imports.Clear ();
			foreach (Import import in currentImports) {
				imports.AppendValues (import.Include);
			}
		}

		protected virtual void OnTxtImportChanged (object sender, System.EventArgs e)
		{
			cmdAdd.Sensitive = !string.IsNullOrEmpty (txtImport.Text);
		}
	}
}
