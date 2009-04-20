//
// Authors:
//   Ben Motmans  <ben.motmans@gmail.com>
//
// Copyright (c) 2007 Ben Motmans
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

using Gtk;
using System;
using System.IO;
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui;

namespace MonoDevelop.Database.Components
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public class ProjectDirectoryComboBox : ComboBox
	{
		private TreeStore store;
		
		public ProjectDirectoryComboBox ()
		{
			store = new TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (Project), typeof (string));
			
			CellRendererPixbuf pixbuf = new CellRendererPixbuf ();
			CellRendererText text = new CellRendererText ();

			this.PackStart (pixbuf, false);
			this.PackStart (text, false);
			this.AddAttribute (pixbuf, "pixbuf", 0);
			this.AddAttribute (text, "markup", 1);
			
			this.Model = store;
			
			PopulateCombo ();
		}
		
		public string SelectedDirectory {
			get {
				TreeIter iter;
				if (this.GetActiveIter (out iter))
					return store.GetValue (iter, 3) as string;
				return null;
			}
		}
		
		public Project SelectedProject {
			get {
				TreeIter iter;
				if (this.GetActiveIter (out iter))
					return store.GetValue (iter, 2) as Project;
				return null;
			}
		}
		
		private void PopulateCombo ()
		{			
			Solution cmb = IdeApp.ProjectOperations.CurrentSelectedSolution;
			if (cmb != null) {
				SolutionItem selected = IdeApp.ProjectOperations.CurrentSelectedSolutionItem;
				TreeIter activeIter = TreeIter.Zero;

				//TODO: add support for recursive combines
				foreach (SolutionItem entry in cmb.Items) {
					if (!(entry is DotNetProject))
						continue;
				
					DotNetProject proj = (DotNetProject)entry;
					Gdk.Pixbuf pixbuf = null;
					
					if (proj is DotNetProject && (proj as DotNetProject).LanguageBinding == null) {
						pixbuf = MonoDevelop.Core.Gui.PixbufService.GetPixbuf (Gtk.Stock.DialogError);
					} else {
						pixbuf = MonoDevelop.Core.Gui.PixbufService.GetPixbuf (proj.StockIcon, IconSize.Menu);
					}
					
					TreeIter iter = store.AppendValues (pixbuf, "<b>" + proj.Name + "</b>", proj, proj.BaseDirectory);
					PopulateCombo (iter, proj.BaseDirectory, proj);

					if (proj == selected)
						activeIter = iter;
				}
				
				if (activeIter.Equals (TreeIter.Zero)) {
					 if (store.GetIterFirst (out activeIter))
						this.SetActiveIter (activeIter);
				} else {
					this.SetActiveIter (activeIter);
				}
			}
		}
		
		private void PopulateCombo (TreeIter parent, string parentDir, Project project)
		{
			foreach (string dir in Directory.GetDirectories (parentDir)) {
				string name = System.IO.Path.GetFileName (dir);
				
				//TODO: use the ProjectFile information
				if (name == "gtk-gui" || name == "bin")
					continue;
				
				Gdk.Pixbuf pixbuf = MonoDevelop.Core.Gui.PixbufService.GetPixbuf (Gtk.Stock.Directory);
				TreeIter iter = store.AppendValues (parent, pixbuf, name, project, dir);
						
				PopulateCombo (iter, dir, project);
			}
		}
	}
}
