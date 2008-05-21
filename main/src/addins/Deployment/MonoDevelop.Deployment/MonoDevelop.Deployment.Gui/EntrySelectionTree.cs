
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.Deployment.Gui
{
	internal partial class EntrySelectionTree : Gtk.Bin
	{
		TreeStore store;
		Dictionary<SolutionItem,SolutionItem> selectedEntries = new Dictionary<SolutionItem,SolutionItem> ();
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
			Gtk.CellRendererPixbuf cr = new Gtk.CellRendererPixbuf();
			col.PackStart (cr, false);
			Gtk.CellRendererText crt = new Gtk.CellRendererText();
			col.PackStart (crt, true);
			col.AddAttribute (cr, "stock-id", 0);
			col.AddAttribute (crt, "markup", 1);
			col.AddAttribute (ctog, "active", 3);
			col.AddAttribute (ctog, "visible", 4);
			tree.AppendColumn (col);
		}
		
		public void Fill (PackageBuilder builder, SolutionItem selection)
		{
			store.Clear ();
			
			this.builder = builder;
			if (selection is SolutionFolder) {
				foreach (SolutionItem e in ((SolutionFolder)selection).GetAllItems ()) {
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
					ReadOnlyCollection<Solution> items = IdeApp.ProjectOperations.CurrentSelectedWorkspaceItem.GetAllSolutions ();
					if (items.Count > 0)
						solution = items [0];
					else
						return;
				}
			}
			AddEntry (TreeIter.Zero, solution.RootFolder);
		}
		
		void AddEntry (TreeIter iter, SolutionItem entry)
		{
			string icon;
			if (entry.ParentFolder == null)
				icon = MonoDevelop.Core.Gui.Stock.Solution;
			else if (entry is SolutionFolder)
				icon = MonoDevelop.Core.Gui.Stock.SolutionFolderClosed;
			else if (entry is Project)
				icon = IdeApp.Services.Icons.GetImageForProjectType (((Project)entry).ProjectType);
			else
				icon = MonoDevelop.Core.Gui.Stock.Project;
			
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
				foreach (SolutionItem ce in ((SolutionFolder)entry).Items) {
					AddEntry (iter, ce);
				}
			}
		}
		
		public void SetSelection (SolutionItem rootEntry, SolutionItem[] childEntries)
		{
			selectedEntries.Clear ();
			selectedEntries [rootEntry] = rootEntry;
			foreach (SolutionItem e in childEntries)
				selectedEntries [e] = e;
			UpdateSelectionChecks (TreeIter.Zero, true);
		}
		
		public SolutionItem GetSelectedEntry ()
		{
			return GetCommonSolutionItem ();
		}
		
		public SolutionItem[] GetSelectedChildren ()
		{
			// The first entry is the root entry
			SolutionItem common = GetCommonSolutionItem ();
			if (common == null)
				return null;
			ArrayList list = new ArrayList ();
			foreach (SolutionItem e in selectedEntries.Keys)
				if (e != common)
					list.Add (e);
			return (SolutionItem[]) list.ToArray (typeof(SolutionItem));
		}
		
		void OnToggled (object sender, Gtk.ToggledArgs args)
		{
			TreeIter iter;
			store.GetIterFromString (out iter, args.Path);
			SolutionItem ob = (SolutionItem) store.GetValue (iter, 2);
			if (selectedEntries.ContainsKey (ob)) {
				selectedEntries.Remove (ob);
				store.SetValue (iter, 3, false);
				if (ob is SolutionFolder) {
					foreach (SolutionItem e in ((SolutionFolder)ob).GetAllItems ())
						selectedEntries.Remove (e);
					UpdateSelectionChecks (TreeIter.Zero, false);
				}
			} else {
				selectedEntries [ob] = ob;
				store.SetValue (iter, 3, true);
				if (ob is SolutionFolder) {
					foreach (SolutionItem e in ((SolutionFolder)ob).GetAllItems ()) {
						if (builder.CanBuild (e))
							selectedEntries [e] = e;
					}
					UpdateSelectionChecks (TreeIter.Zero, false);
				}
				SelectCommonCombine ((SolutionItem)ob);
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
				bool sel = selectedEntries.ContainsKey ((SolutionItem) store.GetValue (iter, 2));
				store.SetValue (iter, 3, sel);
				if (sel)
					tree.ExpandToPath (store.GetPath (iter));
				UpdateSelectionChecks (iter, expandSelected);
			}
			while (store.IterNext (ref iter));
		}
		
		void SelectCommonCombine (SolutionItem e)
		{
			SolutionItem common = GetCommonSolutionItem ();
			if (common == null)
				return;
			selectedEntries [common] = common;
			SolutionItem[] entries = new SolutionItem [selectedEntries.Count];
			selectedEntries.Keys.CopyTo (entries, 0);
			foreach (SolutionItem se in entries) {
				SolutionItem ce = se;
				while (ce != null && ce != common) {
					selectedEntries [ce] = ce;
					ce = ce.ParentFolder;
				}
			}
			UpdateSelectionChecks (TreeIter.Zero, false);
		}
		
		SolutionItem GetCommonSolutionItem ()
		{
			return PackageBuilder.GetCommonSolutionItem (selectedEntries.Keys);
		}
	}
}
