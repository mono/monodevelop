/*
Copyright (C) 2006  Jacob Ilsø Christensen

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using System;
using System.Collections;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

using Gtk;

namespace MonoDevelop.Ide.Gui.Dialogs
{	
	public class GotoClassDialog : Gtk.Dialog
	{
		protected Gtk.TreeView treeview1;
		
		public GotoClassDialog()
		{
			Stetic.Gui.Build(this, typeof(GotoClassDialog));
			SetupTreeView();						       								
		}
		
		private void SetupTreeView()
		{
			ListStore listStore = new ListStore(new Type[] { typeof(string), typeof(string), typeof(IRegion) });
			TreeModelSort treeModelSort = new TreeModelSort(listStore); 			
			treeview1.Model = treeModelSort;

			TreeViewColumn classNameColumn = new TreeViewColumn();
			classNameColumn.Title = "Class";
			CellRendererText cellRenderer = new CellRendererText();
			classNameColumn.PackStart(cellRenderer, true);
			classNameColumn.AddAttribute(cellRenderer, "text", 0);			
			treeview1.AppendColumn(classNameColumn);
			
			TreeViewColumn nameSpaceColumn = new TreeViewColumn();
			nameSpaceColumn.Title = "Namespace";
			cellRenderer = new CellRendererText();
			nameSpaceColumn.PackStart(cellRenderer, true);
			nameSpaceColumn.AddAttribute(cellRenderer, "text", 1);	
			treeview1.AppendColumn(nameSpaceColumn);								
			
			foreach (CombineEntry combineEntry in IdeApp.ProjectOperations.CurrentOpenCombine.Entries)
			{
				AddClassesToTreeView(combineEntry, listStore);
			}					
			
			treeModelSort.SetSortColumnId(0, SortType.Ascending);
			treeModelSort.ChangeSortColumn();			
		}

		private void AddClassesToTreeView(CombineEntry entry, ListStore listStore)
		{
			if (entry is Combine)
			{
				foreach (CombineEntry ce in ((Combine)entry).Entries)
				{
					AddClassesToTreeView(ce, listStore);
				}
			}
			else if (entry is Project)
			{
				Project project = entry as Project;				
				IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext(project);

				foreach (IClass c in ctx.GetProjectContents())
				{					
					listStore.AppendValues(c.Name, c.Namespace, c.Region);
				}
			}
		}

		private void GotoClass()
		{	
			IRegion region;
			TreeIter iter;
			TreeModel model;			
			
			if (treeview1.Selection.GetSelected(out model, out iter))
			{
				region = (IRegion)model.GetValue(iter, 2);                        
			}
			else                
			{
				return;
			}
                					  		    		   
			if (region.FileName == null)
			{
				return;
			}
		    
			IdeApp.Workbench.OpenDocument(region.FileName, Math.Max(1, region.BeginLine), 1, true);
			
			Hide();
		}

		protected virtual void RowActivated(object o, Gtk.RowActivatedArgs args)
		{
			GotoClass();
		}

		protected virtual void OkClicked(object sender, System.EventArgs e)
		{
			GotoClass();
		}

		protected virtual void CancelClicked(object sender, System.EventArgs e)
		{
			Hide();
		}				
	}
}
