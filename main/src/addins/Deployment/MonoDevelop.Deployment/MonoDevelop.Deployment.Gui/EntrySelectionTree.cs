
using Gtk;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using System.Linq;

namespace MonoDevelop.Deployment.Gui
{
	[System.ComponentModel.Category("MonoDevelop.Deployment")]
	[System.ComponentModel.ToolboxItem(true)]
	internal partial class EntrySelectionTree : Gtk.Bin
	{
		TreeStore store;
		Dictionary<SolutionFolderItem,SolutionFolderItem> selectedEntries = new Dictionary<SolutionFolderItem,SolutionFolderItem> ();
		PackageBuilder builder;
		Solution solution;
		
		public event EventHandler SelectionChanged;
		
		public EntrySelectionTree ()
		{
			this.Build();
			
			store = new TreeStore (typeof(string), typeof(string), typeof(object), typeof(bool), typeof(bool));
			tree.Model = store;
			
			tree.HeadersVisible = false;
			TreeViewColumn col = new TreeViewColumn ();
			Gtk.CellRendererToggle ctog = new CellRendererToggle ();
			ctog.Toggled += OnToggled;
			col.PackStart (ctog, false);
			CellRendererImage cr = new CellRendererImage();
			col.PackStart (cr, false);
			Gtk.CellRendererText crt = new Gtk.CellRendererText();
			col.PackStart (crt, true);
			col.AddAttribute (cr, "stock-id", 0);
			col.AddAttribute (crt, "markup", 1);
			col.AddAttribute (ctog, "active", 3);
			col.AddAttribute (ctog, "visible", 4);
			tree.AppendColumn (col);
		}
		
		public void Fill (PackageBuilder builder, SolutionFolderItem selection)
		{
			store.Clear ();
			
			this.builder = builder;
			if (selection is SolutionFolder) {
				foreach (SolutionFolderItem e in ((SolutionFolder)selection).GetAllItems ()) {
					if (builder.CanBuild (e))
						selectedEntries [e] = e;
				}
			}
			else if (selection != null) {
				selectedEntries [selection] = selection;
			}
			
			if (selection != null)
				solution = selection.ParentSolution;
			else {
				solution = IdeApp.ProjectOperations.CurrentSelectedSolution;
				if (solution == null) {
					solution = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem.GetAllItems<Solution> ().FirstOrDefault();
					if (solution == null)
						return;
				}
			}
			AddEntry (TreeIter.Zero, solution.RootFolder);
		}
		
		void AddEntry (TreeIter iter, SolutionFolderItem entry)
		{
			string icon;
			if (entry.ParentFolder == null)
				icon = MonoDevelop.Ide.Gui.Stock.Solution;
			else if (entry is SolutionFolder)
				icon = MonoDevelop.Ide.Gui.Stock.SolutionFolderClosed;
			else if (entry is Project)
				icon = ((Project)entry).StockIcon;
			else
				icon = MonoDevelop.Ide.Gui.Stock.Project;
			
			bool visible = builder.CanBuild (entry);
			bool selected = selectedEntries.ContainsKey (entry);
			
			if (!(entry is SolutionFolder) && !visible)
				return;
			
			if (!iter.Equals (TreeIter.Zero))
				iter = store.AppendValues (iter, icon, entry.Name, entry, selected && visible, visible);
			else
				iter = store.AppendValues (icon, entry.Name, entry, selected && visible, visible);
			
			if (selected)
				tree.ExpandToPath (store.GetPath (iter));
			
			if (entry is SolutionFolder) {
				foreach (SolutionFolderItem ce in ((SolutionFolder)entry).Items) {
					AddEntry (iter, ce);
				}
			}
		}
		
		public void SetSelection (SolutionFolderItem rootEntry, SolutionFolderItem[] childEntries)
		{
			selectedEntries.Clear ();
			selectedEntries [rootEntry] = rootEntry;
			foreach (SolutionFolderItem e in childEntries)
				selectedEntries [e] = e;
			UpdateSelectionChecks (TreeIter.Zero, true);
		}
		
		public SolutionFolderItem GetSelectedEntry ()
		{
			return GetCommonSolutionItem ();
		}
		
		public SolutionFolderItem[] GetSelectedChildren ()
		{
			// The first entry is the root entry
			SolutionFolderItem common = GetCommonSolutionItem ();
			if (common == null)
				return null;
			ArrayList list = new ArrayList ();
			foreach (SolutionFolderItem e in selectedEntries.Keys)
				if (e != common)
					list.Add (e);
			return (SolutionFolderItem[]) list.ToArray (typeof(SolutionFolderItem));
		}
		
		void OnToggled (object sender, Gtk.ToggledArgs args)
		{
			TreeIter iter;
			store.GetIterFromString (out iter, args.Path);
			SolutionFolderItem ob = (SolutionFolderItem) store.GetValue (iter, 2);
			if (selectedEntries.ContainsKey (ob)) {
				selectedEntries.Remove (ob);
				store.SetValue (iter, 3, false);
				if (ob is SolutionFolder) {
					foreach (SolutionFolderItem e in ((SolutionFolder)ob).GetAllItems ())
						selectedEntries.Remove (e);
					UpdateSelectionChecks (TreeIter.Zero, false);
				}
			} else {
				selectedEntries [ob] = ob;
				store.SetValue (iter, 3, true);
				if (ob is SolutionFolder) {
					foreach (SolutionFolderItem e in ((SolutionFolder)ob).GetAllItems ()) {
						if (builder.CanBuild (e))
							selectedEntries [e] = e;
					}
					UpdateSelectionChecks (TreeIter.Zero, false);
				}
				SelectCommonCombine ((SolutionFolderItem)ob);
			}
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}
		
		void UpdateSelectionChecks (TreeIter iter, bool expandSelected)
		{
			if (iter.Equals (TreeIter.Zero)) {
				if (!store.GetIterFirst (out iter))
					return;
			}
			else {
				if (!store.IterChildren (out iter, iter))
					return;
			}
			do {
				bool sel = selectedEntries.ContainsKey ((SolutionFolderItem) store.GetValue (iter, 2));
				store.SetValue (iter, 3, sel);
				if (sel)
					tree.ExpandToPath (store.GetPath (iter));
				UpdateSelectionChecks (iter, expandSelected);
			}
			while (store.IterNext (ref iter));
		}
		
		void SelectCommonCombine (SolutionFolderItem e)
		{
			SolutionFolderItem common = GetCommonSolutionItem ();
			if (common == null)
				return;
			selectedEntries [common] = common;
			SolutionFolderItem[] entries = new SolutionFolderItem [selectedEntries.Count];
			selectedEntries.Keys.CopyTo (entries, 0);
			foreach (SolutionFolderItem se in entries) {
				SolutionFolderItem ce = se;
				while (ce != null && ce != common) {
					selectedEntries [ce] = ce;
					ce = ce.ParentFolder;
				}
			}
			UpdateSelectionChecks (TreeIter.Zero, false);
		}
		
		SolutionFolderItem GetCommonSolutionItem ()
		{
			return PackageBuilder.GetCommonSolutionItem (selectedEntries.Keys);
		}
	}
}
