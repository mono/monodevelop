//  CombineStartupPanel.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA
using System;
using System.Collections;
using System.ComponentModel;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui.Dialogs;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;

using Gtk;
using MonoDevelop.Components;

namespace MonoDevelop.Projects.Gui.Dialogs.OptionPanels
{
	internal class CombineStartupPanel : AbstractOptionPanel
	{
		CombineStartupPanelWidget widget;

		public override void LoadPanelContents()
		{
			Add (widget = new  CombineStartupPanelWidget ((Properties) CustomizationObject));
		}

		public override bool StorePanelContents()
		{
		        bool success = widget.Store ();
 			return success;			
       	}	
	}

	partial class CombineStartupPanelWidget : Gtk.Bin 
	{
		public ListStore store;

		Combine combine;

		public CombineStartupPanelWidget (Properties CustomizationObject)
		{
			Build ();
			
			this.combine = ((Properties)CustomizationObject).Get<Combine> ("Combine");

			singleRadioButton.Active = combine.SingleStartupProject;
			singleRadioButton.Clicked += new EventHandler(OnSingleRadioButtonClicked);
			multipleRadioButton.Active = !combine.SingleStartupProject;
			//singleRadioButton.Clicked += new EventHandler(OptionsChanged);

			// Setting up OptionMenus
			ListStore tmpStore = new ListStore (typeof (string));
			int active = -1;
			for (int i = 0;  i < combine.Entries.Count; i++)  {
				CombineEntry entry = (CombineEntry) combine.Entries[i];
				tmpStore.AppendValues (entry.Name);

				if (combine.StartupEntry == entry)
					active = i;
			}
			singleCombo.Model = tmpStore;

			CellRendererText cr = new CellRendererText ();
			singleCombo.PackStart (cr, true);
			singleCombo.AddAttribute (cr, "text", 0);
			singleCombo.Active = active;

			tmpStore = new ListStore (typeof (string));
			tmpStore.AppendValues (GettextCatalog.GetString ("None"));
			tmpStore.AppendValues (GettextCatalog.GetString ("Execute"));
			actionCombo.Model = tmpStore;
			actionCombo.PackStart (cr, true);
			actionCombo.AddAttribute (cr, "text", 0);
			actionCombo.Changed += new EventHandler(OptionsChanged);

			// Populating entryTreeView					
				store = new ListStore (typeof(string), typeof(string), typeof(CombineExecuteDefinition) );
			entryTreeView.Model = store;
			
				string entryHeader = StringParserService.Parse(GettextCatalog.GetString ("Entry"));
				entryTreeView.AppendColumn (entryHeader, new CellRendererText (), "text", 0);
				string actionHeader = StringParserService.Parse(GettextCatalog.GetString ("Action"));
				entryTreeView.AppendColumn (actionHeader, new CellRendererText (), "text", 1);
			
			// sanity check to ensure we had a proper execture definitions save last time rounf
			if(combine.CombineExecuteDefinitions.Count == combine.Entries.Count) {
				// add the previously saved execute definitions to the treeview list
				for (int n = 0; n < combine.CombineExecuteDefinitions.Count; n++) {
					CombineExecuteDefinition edef = (CombineExecuteDefinition)combine.CombineExecuteDefinitions[n];
					if (edef == null || edef.Entry == null) {
						// Invalid
						store.Clear ();
						AddEmptyExecuteDefinitions ();
						break;
					}
					string action = edef.Type == EntryExecuteType.None ? GettextCatalog.GetString ("None") : GettextCatalog.GetString ("Execute");
					store.AppendValues (edef.Entry.Name, action, edef);
				}
			} else {
				AddEmptyExecuteDefinitions ();
			}
				
				entryTreeView.Selection.Changed += new EventHandler(SelectedEntryChanged);
			entryTreeView.Selection.SelectPath(new TreePath ("0"));
			
			// Setting up Buttons
			moveUpButton.Clicked += new EventHandler(OnMoveUpButtonClicked);
			moveDownButton.Clicked += new EventHandler(OnMoveDownButtonClicked);

			OnSingleRadioButtonClicked(null, null);				
		}

		void AddEmptyExecuteDefinitions ()
		{
			for (int n = 0; n < combine.Entries.Count; n++) {
				CombineExecuteDefinition edef = new CombineExecuteDefinition ((CombineEntry) combine.Entries[n],EntryExecuteType.None);
				string action = edef.Type == EntryExecuteType.None ? GettextCatalog.GetString ("None") : GettextCatalog.GetString ("Execute");
				store.AppendValues (edef.Entry.Name, action, edef);
			}
			
			// tell the user we encountered and worked around an issue
			Gtk.Application.Invoke (delegate {
				Services.MessageService.ShowError (null, MonoDevelop.Core.GettextCatalog.GetString ("The Solution Execute Definitions for this Solution were invalid. A new empty set of Execute Definitions has been created."), (Gtk.Window) Toplevel, true);
			});
		}
				
		protected void OnMoveUpButtonClicked(object sender, EventArgs e)
		{
			if(entryTreeView.Selection.CountSelectedRows() == 1)
			{
				TreeIter selectedItem;
				TreeModel ls;				
				((ListStore)entryTreeView.Model).GetIter(
					out selectedItem, (TreePath) entryTreeView.Selection.GetSelectedRows(out ls)[0]);
				// we know we have a selected item so get it's index
				// use that to get the path of the item before it, and swap the two
				int index = GetSelectedIndex(entryTreeView);
				// only swap if at the top
				if(index > 0)
				{
					TreeIter prev; 
					if(entryTreeView.Model.GetIterFromString(out prev, (index - 1).ToString()))
					{
						((ListStore)ls).Swap(selectedItem, prev);
					}
				}
			}
		}
		

		protected void OnMoveDownButtonClicked(object sender, EventArgs e)
		{
			if(entryTreeView.Selection.CountSelectedRows() == 1)
			{
				TreeIter selectedItem;
				TreeModel ls;				
				((ListStore)entryTreeView.Model).GetIter(
					out selectedItem, (TreePath) entryTreeView.Selection.GetSelectedRows(out ls)[0]);
				// swap it with the next one
				TreeIter toSwap = selectedItem;
				if(ls.IterNext(ref toSwap))
				{
					((ListStore)ls).Swap(selectedItem, toSwap);
				}
			}
		}
		
		void OnSingleRadioButtonClicked(object sender, EventArgs e)
		{
			multipleBox.Sensitive = multipleRadioButton.Active;
			singleCombo.Sensitive = singleRadioButton.Active;
		}
		
	       	void OptionsChanged (object sender, EventArgs e)
		{
			TreeIter iter;
			TreeModel model;				
			ComboBox combo = sender as ComboBox;
			
			if (entryTreeView.Selection.GetSelected (out model, out iter))
			{
				CombineExecuteDefinition edef = (CombineExecuteDefinition) model.GetValue (iter, 2);
				switch (combo.Active) {
					case 0:
						edef.Type = EntryExecuteType.None;
						break;
					case 1:
						edef.Type = EntryExecuteType.Execute;
						break;
					default:
						break;
				}

				model.SetValue (iter, 2, edef);
				string action = edef.Type == EntryExecuteType.None ? GettextCatalog.GetString ("None") : GettextCatalog.GetString ("Execute");
				model.SetValue (iter, 1, action);
			}
		}
		
		void SelectedEntryChanged(object sender, EventArgs e)
		{
			TreeIter iter;
			TreeModel model;				
			TreeSelection selection = sender as TreeSelection;

			if (selection.GetSelected (out model, out iter))
			{
				string txt = (string) model.GetValue (iter, 1);
				if (txt == GettextCatalog.GetString ("None"))
					actionCombo.Active = 0;
				else
					actionCombo.Active = 1;
			}
		}
		
		// added this event to get the last select row index from gtk TreeView
		int GetSelectedIndex(Gtk.TreeView tv)
		{
			if(entryTreeView.Selection.CountSelectedRows() == 1)
			{
				TreeIter selectedIter;
				TreeModel lv;				
				((ListStore)entryTreeView.Model).GetIter(
					out selectedIter, (TreePath) entryTreeView.Selection.GetSelectedRows(out lv)[0]);
				
				// return index of first level node (since only 1 level list model)
				return lv.GetPath(selectedIter).Indices[0];
			}
			else
			{
				return -1;
			}
		}

		public bool Store()
		{
			if (singleCombo.Active != -1)
				combine.StartupEntry = (CombineEntry) combine.Entries [singleCombo.Active];
			else
				combine.StartupEntry = null;
			combine.SingleStartupProject   = singleRadioButton.Active;
			
			// write back new combine execute definitions
			combine.CombineExecuteDefinitions.Clear();
			TreeIter first;
			store.GetIterFirst(out first);
			TreeIter current = first;
			for (int i = 0; i < store.IterNChildren() ; ++i) {
				
				CombineExecuteDefinition edef = (CombineExecuteDefinition) store.GetValue(current, 2);					
				combine.CombineExecuteDefinitions.Add(edef);
				
				store.IterNext(ref current);	
			}
			return true;
		}		
	}

}

