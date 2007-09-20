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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Gui;

namespace MonoDevelop.Database.Components
{
	public class ProjectDirectoryComboBox : ComboBox
	{
		private TreeStore store;
		
		public ProjectDirectoryComboBox ()
		{
			store = new TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (Project), typeof (ProjectFile));
			
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
				if (this.GetActiveIter (out iter)) {
					ProjectFile file = store.GetValue (iter, 3) as ProjectFile;
					if (file != null)
						return file.FilePath;
					
					Project proj = store.GetValue (iter, 2) as Project;
					return proj.BaseDirectory;
				}
				
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
			IconService service = (IconService) ServiceManager.GetService (typeof(IconService));
			Combine cmb = IdeApp.ProjectOperations.CurrentOpenCombine;
			if (cmb != null) {
				CombineEntry selected = IdeApp.ProjectOperations.CurrentSelectedCombineEntry;
				TreeIter activeIter = TreeIter.Zero;

				foreach (Project proj in cmb.Entries) {
					Gdk.Pixbuf pixbuf = null;
					
					if (proj is DotNetProject && (proj as DotNetProject).LanguageBinding == null) {
						pixbuf = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Stock.DialogError);
					} else {
						string icon = service.GetImageForProjectType (proj.ProjectType);
						pixbuf = MonoDevelop.Core.Gui.Services.Resources.GetIcon (icon);
					}
					
					TreeIter iter = store.AppendValues (pixbuf, "<b>" + proj.Name + "</b>", proj, null);
					
					foreach (ProjectFile file in proj.ProjectFiles) {
						if (file.Subtype != Subtype.Directory)
							continue;
						
						pixbuf = MonoDevelop.Core.Gui.Services.Resources.GetIcon (Stock.Directory);
						store.AppendValues (iter, pixbuf, file.Name, proj, file);
					}
					
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
	}
}