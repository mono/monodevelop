// 
// ProjectSelectorWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using System.Linq;
using MonoDevelop.Projects;
using Gtk;
using System.Collections.Generic;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Components
{
	[System.ComponentModel.ToolboxItem(true)]
	public partial class ProjectSelectorWidget : Gtk.Bin
	{
		TreeStore store;
		bool showCheckboxes;
		IBuildTarget rootItem;
		IBuildTarget currentSelection;
		HashSet<IBuildTarget> activeItems = new HashSet<IBuildTarget> ();
		HashSet<Type> selectableTypes = new HashSet<Type> ();
		Func<IBuildTarget,bool> selectableFilter;
		
		public event EventHandler SelectionChanged;
		public event EventHandler ActiveChanged;
		
		public ProjectSelectorWidget ()
		{
			this.Build();
			
			store = new TreeStore (typeof(string), typeof(string), typeof(object), typeof(bool), typeof(bool));
			tree.Model = store;
			
			tree.HeadersVisible = false;
			TreeViewColumn col = new TreeViewColumn ();
			Gtk.CellRendererToggle ctog = new CellRendererToggle ();
			ctog.Toggled += OnToggled;
			col.PackStart (ctog, false);
			var cr = new CellRendererImage ();
			col.PackStart (cr, false);
			Gtk.CellRendererText crt = new Gtk.CellRendererText();
			crt.Mode &= ~CellRendererMode.Activatable;
			col.PackStart (crt, true);
			col.AddAttribute (cr, "stock-id", 0);
			col.AddAttribute (crt, "markup", 1);
			col.AddAttribute (ctog, "active", 3);
			col.AddAttribute (ctog, "visible", 4);
			tree.AppendColumn (col);
			
			tree.Selection.Changed += HandleTreeSelectionChanged;
		}

		void HandleTreeSelectionChanged (object sender, EventArgs e)
		{
			TreeIter it;
			if (tree.Selection.GetSelected (out it))
				currentSelection = (IBuildTarget) store.GetValue (it, 2);
			else
				currentSelection = null;
			
			if (SelectionChanged != null)
				SelectionChanged (this, EventArgs.Empty);
		}

		public Func<IBuildTarget,bool> SelectableFilter {
			get {
				return selectableFilter;
			}
			set {
				selectableFilter = value;
				Fill ();
			}
		}
		
		public IBuildTarget SelectedItem {
			get {
				if (currentSelection != null && !IsSelectable (currentSelection))
					return null;
				else
					return currentSelection;
			}
			set {
				currentSelection = value;
				SetSelection (currentSelection, null);
			}
		}
		
		public IEnumerable<IBuildTarget> ActiveItems {
			get {
				return activeItems;
			}
			set {
				activeItems = new HashSet<IBuildTarget> ();
				activeItems.UnionWith (value);
				SetSelection (currentSelection, activeItems);
			}
		}
		
		public IEnumerable<Type> SelectableItemTypes {
			get {
				return selectableTypes;
			}
			set {
				selectableTypes = new HashSet<Type> ();
				selectableTypes.UnionWith (value);
				Fill ();
			}
		}
		
		public bool ShowCheckboxes {
			get { return showCheckboxes; }
			set { showCheckboxes = value; Fill (); }
		}
		
		public bool CascadeCheckboxSelection { get; set; }
		
		public IBuildTarget RootItem {
			get {
				return this.rootItem;
			}
			set {
				rootItem = value;
				Fill ();
			}
		}
		
		void Fill ()
		{
			IBuildTarget sel = SelectedItem;
			store.Clear ();
			if (rootItem is RootWorkspace) {
				foreach (var item in ((RootWorkspace)rootItem).Items)
					AddEntry (TreeIter.Zero, item);
				SelectedItem = sel;
			}
			else if (rootItem != null) {
				AddEntry (TreeIter.Zero, rootItem);
				SelectedItem = sel;
			}
		}
		
		void AddEntry (TreeIter iter, IBuildTarget item)
		{
			if (!IsVisible (item))
				return;
			
			string icon;
			if (item is Solution)
				icon = MonoDevelop.Ide.Gui.Stock.Solution;
			else if (item is SolutionFolder)
				icon = MonoDevelop.Ide.Gui.Stock.SolutionFolderClosed;
			else if (item is WorkspaceItem)
				icon = MonoDevelop.Ide.Gui.Stock.Workspace;
			else if (item is Project)
				icon = ((Project)item).StockIcon;
			else
				icon = MonoDevelop.Ide.Gui.Stock.Project;
			
			bool checkVisible = IsCheckboxVisible (item);
			bool selected = activeItems.Contains (item);
			bool isRoot = iter.Equals (TreeIter.Zero);
			
			if (!isRoot)
				iter = store.AppendValues (iter, icon, item.Name, item, selected && checkVisible, checkVisible);
			else
				iter = store.AppendValues (icon, item.Name, item, selected && checkVisible, checkVisible);

			if (selected)
				tree.ExpandToPath (store.GetPath (iter));
			
			foreach (IBuildTarget ce in GetChildren (item))
				AddEntry (iter, ce);

			// Expand all root items by default
			if (isRoot)
				tree.ExpandRow (store.GetPath (iter), false);
		}
		
		void SetSelection (IBuildTarget selected, HashSet<IBuildTarget> active)
		{
			TreeIter it;
			if (store.GetIterFirst (out it))
				SetSelection (it, selected, active);
		}
		
		bool SetSelection (TreeIter it, IBuildTarget selected, HashSet<IBuildTarget> active)
		{
			do {
				IBuildTarget item = (IBuildTarget) store.GetValue (it, 2);
				if (selected != null && item == selected) {
					tree.Selection.SelectIter (it);
					tree.ExpandToPath (store.GetPath (it));
					tree.ScrollToCell (store.GetPath (it), tree.Columns[0], false, 0, 0);
					if (active == null)
						return true;
				}
				bool val = (bool) store.GetValue (it, 3);
				bool newVal = active != null ? active.Contains (item) : val;
				if (val != newVal)
					store.SetValue (it, 3, newVal);
				
				TreeIter ci;
				if (store.IterChildren (out ci, it)) {
					if (SetSelection (ci, selected, active))
						return true;
				}
				
			} while (store.IterNext (ref it));
			
			return false;
		}
		
		void OnToggled (object sender, Gtk.ToggledArgs args)
		{
			TreeIter iter;
			store.GetIterFromString (out iter, args.Path);
			IBuildTarget ob = (IBuildTarget) store.GetValue (iter, 2);
			if (activeItems.Contains (ob)) {
				activeItems.Remove (ob);
				if (CascadeCheckboxSelection) {
					foreach (var i in GetAllChildren (ob))
						activeItems.Remove (i);
					SetSelection (iter, null, new HashSet<IBuildTarget> ());
				} else {
					store.SetValue (iter, 3, false);
				}
			} else {
				activeItems.Add (ob);
				if (CascadeCheckboxSelection) {
					foreach (var i in GetAllChildren (ob))
						activeItems.Add (i);
					SetSelection (iter, null, activeItems);
				}
				else {
					store.SetValue (iter, 3, true);
				}
			}
			if (ActiveChanged != null)
				ActiveChanged (this, EventArgs.Empty);
		}
		
		IEnumerable<IBuildTarget> GetAllChildren (IBuildTarget item)
		{
			IEnumerable<IBuildTarget> res = GetChildren (item);
			return res.Concat (res.SelectMany (i => GetAllChildren (i)));
		}
		
		IEnumerable<IBuildTarget> GetChildren (IBuildTarget item)
		{
			if (item is SolutionFolder) {
				return ((SolutionFolder)item).Items;
			} else if (item is Solution) {
				return ((Solution)item).RootFolder.Items;
			} else if (item is Workspace) {
				return ((Workspace)item).Items;
			} else
				return new IBuildTarget [0];
		}
		
		protected bool IsVisible (IBuildTarget item)
		{
			return true;
		}
		
		protected bool IsCheckboxVisible (IBuildTarget item)
		{
			if (!ShowCheckboxes)
				return false;
			return IsSelectable (item);
		}

		bool IsSelectable (IBuildTarget item)
		{
			if (SelectableFilter != null && !SelectableFilter (item))
				return false;
			if (selectableTypes.Count > 0)
				return selectableTypes.Any (t => t.IsAssignableFrom (item.GetType ()));
			return true;
		}
	}
}

