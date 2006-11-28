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
using System.Collections.Generic;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;

using Gtk;
using Gdk;

namespace MonoDevelop.Ide.Gui.Dialogs
{	
	public class GotoTypeDialog : Gtk.Dialog
	{
		protected Gtk.TreeView treeview1;
		private Dictionary<string, Pixbuf> icons;
		
		public GotoTypeDialog()
		{
			Stetic.Gui.Build(this, typeof(GotoTypeDialog));
			
			icons = new Dictionary<string, Pixbuf>();
			SetupTreeView();						       								
		}
		
		private void SetupTreeView()
		{
			ListStore listStore = new ListStore(new Type[] { typeof(Pixbuf), typeof(string), typeof(string), typeof(IRegion) });
			TreeModelSort treeModelSort = new TreeModelSort(listStore); 			
			treeview1.Model = treeModelSort;

			TreeViewColumn typeNameColumn = new TreeViewColumn();
			typeNameColumn.Title = "Type";			
			CellRendererPixbuf cellRendererPixbuf = new CellRendererPixbuf();
			typeNameColumn.PackStart(cellRendererPixbuf, false);
			typeNameColumn.AddAttribute(cellRendererPixbuf, "pixbuf", 0);			
			CellRendererText cellRenderer = new CellRendererText();
			typeNameColumn.PackStart(cellRenderer, true);
			typeNameColumn.AddAttribute(cellRenderer, "text", 1);			
			treeview1.AppendColumn(typeNameColumn);			
					
			foreach (CombineEntry combineEntry in IdeApp.ProjectOperations.CurrentOpenCombine.Entries)
			{
				AddTypesToTreeView(combineEntry, listStore);
			}					
			
			treeModelSort.SetSortColumnId(2, SortType.Ascending);
			treeModelSort.ChangeSortColumn();			
		}

		private void AddTypesToTreeView(CombineEntry entry, ListStore listStore)
		{
			if (entry is Combine)
			{
				foreach (CombineEntry ce in ((Combine)entry).Entries)
				{
					AddTypesToTreeView(ce, listStore);
				}
			}
			else if (entry is Project)
			{
				Project project = entry as Project;				
				IParserContext ctx = IdeApp.ProjectOperations.ParserDatabase.GetProjectParserContext(project);

				foreach (IClass c in ctx.GetProjectContents())
				{										
					Pixbuf pixbuf = GetIcon(Services.Icons.GetIcon(c));				
					listStore.AppendValues(pixbuf, c.Name + " (" + c.Namespace + " namespace)", c.Name, c.Region);
				}
			}
		}

		private Pixbuf GetIcon(string id)
		{					
			if (!icons.ContainsKey(id))
			{
				icons.Add(id, treeview1.RenderIcon(id, IconSize.Menu, "")); 
			}
		
			return icons[id];
		}

		private void GotoType()
		{	
			IRegion region;
			TreeIter iter;
			TreeModel model;			
			
			if (treeview1.Selection.GetSelected(out model, out iter))
			{
				region = (IRegion)model.GetValue(iter, 3);
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
			GotoType();
		}

		protected virtual void OkClicked(object sender, System.EventArgs e)
		{
			GotoType();
		}

		protected virtual void CancelClicked(object sender, System.EventArgs e)
		{
			Hide();
		}				
	}
}
