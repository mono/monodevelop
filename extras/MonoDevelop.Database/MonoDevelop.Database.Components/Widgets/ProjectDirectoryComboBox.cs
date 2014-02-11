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
using System.IO;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Components;

namespace MonoDevelop.Database.Components
{
	[System.ComponentModel.Category("widget")]
	[System.ComponentModel.ToolboxItem(true)]
	public class ProjectDirectoryComboBox : ComboBox
	{
		private TreeStore store;
		
		public ProjectDirectoryComboBox ()
		{
			store = new TreeStore (typeof (Xwt.Drawing.Image), typeof (string), typeof (Project), typeof (string));
			
			var pixbuf = new CellRendererImage ();
			CellRendererText text = new CellRendererText ();

			this.PackStart (pixbuf, false);
			this.PackStart (text, false);
			this.AddAttribute (pixbuf, "image", 0);
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
				foreach (Project entry in IdeApp.Workspace.GetAllProjects ()) {
					if (!(entry is DotNetProject))
						continue;
				
					DotNetProject proj = (DotNetProject)entry;
					Xwt.Drawing.Image pixbuf = null;
					
					if (proj is DotNetProject && (proj as DotNetProject).LanguageBinding == null) {
						pixbuf = ImageService.GetIcon (Gtk.Stock.DialogError, IconSize.Menu);
					} else {
						pixbuf = ImageService.GetIcon (proj.StockIcon, IconSize.Menu);
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
				DirectoryInfo info = new DirectoryInfo (dir);
				
				
				//TODO: use the ProjectFile information
				if (name == "gtk-gui" || name == "bin" || info.Attributes.ToString ().Contains ("Hidden"))
					continue;
				
				var pixbuf = ImageService.GetIcon (Gtk.Stock.Directory, IconSize.Menu);
				TreeIter iter = store.AppendValues (parent, pixbuf, name, project, dir);
						
				PopulateCombo (iter, dir, project);
			}
		}
	}
}
