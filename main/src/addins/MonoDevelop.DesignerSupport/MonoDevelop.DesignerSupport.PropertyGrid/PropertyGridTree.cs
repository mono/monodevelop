//
// PropertyGridTree.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections;
using System.ComponentModel;

using Gtk;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.DesignerSupport.PropertyGrid.PropertyEditors;

namespace MonoDevelop.DesignerSupport.PropertyGrid
{
	internal class PropertyGridTree: Gtk.ScrolledWindow
	{
		Gtk.TreeStore store;
		InternalTree tree;
		TreeViewColumn editorColumn;
		Hashtable propertyRows;
		ArrayList collapseStatus = new ArrayList ();
		EditorManager editorManager;
		PropertyGrid parentGrid;
		
		public event EventHandler Changed;
		
		private System.Windows.Forms.PropertySort propertySort = System.Windows.Forms.PropertySort.Categorized;

		
		public PropertyGridTree (EditorManager editorManager, PropertyGrid parentGrid)
		{
			this.editorManager = editorManager;
			this.parentGrid = parentGrid;
			
			propertyRows = new Hashtable ();
			
			store = new TreeStore (typeof (string), typeof(object), typeof(bool), typeof(object));
			
			tree = new InternalTree (this, store);

			CellRendererText crt;
			
			TreeViewColumn col;

			col = new TreeViewColumn ();
			col.Title = GettextCatalog.GetString ("Property");
			crt = new CellRendererPropertyGroup (tree);
			crt.Xpad = 0;
			col.PackStart (crt, true);
			col.SetCellDataFunc (crt, new TreeCellDataFunc (GroupData));
			col.Resizable = true;
			col.Expand = false;
			col.Sizing = TreeViewColumnSizing.Fixed;
			col.FixedWidth = 180;
			tree.AppendColumn (col);
			
			editorColumn = new TreeViewColumn ();
			editorColumn.Title = GettextCatalog.GetString ("Value");
			
			CellRendererProperty crp = new CellRendererProperty (tree);
			
			editorColumn.PackStart (crp, true);
			editorColumn.SetCellDataFunc (crp, new TreeCellDataFunc (PropertyData));
			editorColumn.Sizing = TreeViewColumnSizing.Fixed;
			editorColumn.Resizable = false;
			editorColumn.Expand = true;
			tree.AppendColumn (editorColumn);
			
			tree.HeadersVisible = false;
			this.ShadowType = Gtk.ShadowType.In;
			
			this.HscrollbarPolicy = Gtk.PolicyType.Never;
			
			Add (tree);
			
			ShowAll ();
			
			tree.Selection.Changed += OnSelectionChanged;
		}
		
		public void SaveStatus ()
		{
			collapseStatus.Clear ();

			TreeIter iter;
			if (!tree.Model.GetIterFirst (out iter))
				return;
			
			do {
				if (!tree.GetRowExpanded (tree.Model.GetPath (iter))) {
					collapseStatus.Add (tree.Model.GetValue (iter, 0));
				}
			} while (tree.Model.IterNext (ref iter));
		}
		
		public void RestoreStatus ()
		{
			TreeIter iter;
			if (!tree.Model.GetIterFirst (out iter))
				return;
			
			// If the tree only has one group, show it always expanded
			TreeIter iter2 = iter;
			if (!tree.Model.IterNext (ref iter2)) {
				tree.ExpandRow (tree.Model.GetPath (iter), true);
				return;
			}
			
			do {
				object grp = tree.Model.GetValue (iter, 0);
				if (!collapseStatus.Contains (grp))
					tree.ExpandRow (tree.Model.GetPath (iter), false);
			} while (tree.Model.IterNext (ref iter));
		}
		
		public virtual void Clear ()
		{
			store.Clear ();
			propertyRows.Clear ();
		}
		
		public virtual void Update ()
		{
			// Just repaint the cells
			QueueDraw ();
		}
		
		public System.Windows.Forms.PropertySort PropertySort {
			get { return propertySort; }
			set { propertySort = value; }
		}
		
		internal void Populate (PropertyDescriptorCollection properties, object instance)
		{
			bool categorised = PropertySort == System.Windows.Forms.PropertySort.Categorized;
		
			//transcribe browsable properties
			ArrayList sorted = new ArrayList();

			foreach (PropertyDescriptor descriptor in properties)
				if (descriptor.IsBrowsable)
					sorted.Add (descriptor);
			
			if (sorted.Count == 0)
				return;
			
			InstanceData idata = new InstanceData (instance);
			
			if (!categorised) {
				sorted.Sort(new SortByName ());
				foreach (PropertyDescriptor pd in sorted)
					AppendProperty (TreeIter.Zero, pd, idata);
			}
			else {
				sorted.Sort (new SortByCat ());
				string oldCat = null;
				TreeIter catIter = TreeIter.Zero;
				
				foreach (PropertyDescriptor pd in sorted) {
					if (pd.Category != oldCat) {
						catIter = store.AppendValues (pd.Category, null, true, idata);
						oldCat = pd.Category;
					}
					AppendProperty (catIter, pd, idata);
				}
			}
		}
		
		internal void Update (PropertyDescriptorCollection properties, object instance)
		{
			foreach (PropertyDescriptor pd in properties) {
				TreeIter it;
				if (!store.GetIterFirst (out it))
					continue;
				
				UpdateProperty (pd, it, instance);
			}
		}
		
		bool UpdateProperty (PropertyDescriptor pd, TreeIter it, object instance)
		{
			do {
				PropertyDescriptor prop = (PropertyDescriptor) store.GetValue (it, 1);
				InstanceData idata = (InstanceData) store.GetValue (it, 3);
				if (prop != null && idata != null && prop.Name == pd.Name && idata.Instance == instance) {
					// Don't update the current editing node, since it may cause tree update problems
					if (!store.GetPath (tree.EditingIter).Equals (store.GetPath (it)))
						store.SetValue (it, 1, pd);
					return true;
				}
				TreeIter ci;
				if (store.IterChildren (out ci, it)) {
					if (UpdateProperty (pd, ci, instance))
						return true;
				}
			}
			while (store.IterNext (ref it));
			return false;
		}
	
		protected void AppendProperty (PropertyDescriptor prop, object instance)
		{
			AppendProperty (TreeIter.Zero, prop, new InstanceData (instance));
		}
		
		protected void AppendProperty (TreeIter piter, PropertyDescriptor prop, object instance)
		{
			AppendProperty (piter, prop, new InstanceData (instance));
		}
		
		void AppendProperty (TreeIter piter, PropertyDescriptor prop, InstanceData idata)
		{
			TreeIter iter;
			
			if (piter.Equals (TreeIter.Zero))
				iter = store.AppendValues (prop.DisplayName, prop, false, idata);
			else
				iter = store.AppendValues (piter, prop.DisplayName, prop, false, idata);
			propertyRows [prop] = store.GetStringFromIter (iter);
			
			TypeConverter tc = prop.Converter;
			if (typeof (ExpandableObjectConverter).IsAssignableFrom (tc.GetType ())) {
				object cob = prop.GetValue (idata.Instance);
				foreach (PropertyDescriptor cprop in TypeDescriptor.GetProperties (cob))
					AppendProperty (iter, cprop, cob);
			}
		}
		
		protected virtual void OnObjectChanged ()
		{
			// Delay the notification of the change. There may be problems if the
			// handler of this event starts its own gui loop.
			GLib.Timeout.Add (0, delegate {
				if (Changed != null)
					Changed (this, EventArgs.Empty);
				return false;
			});
		}
		
		void OnSelectionChanged (object s, EventArgs a)
		{
			TreePath[] rows = tree.Selection.GetSelectedRows ();
			if (!tree.dragging && rows != null && rows.Length > 0) {
				tree.SetCursor (rows[0], editorColumn, true);
			}
			TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				PropertyDescriptor prop = (PropertyDescriptor) store.GetValue (iter, 1);
				if (prop != null)
					parentGrid.SetHelp (prop.DisplayName, prop.Description);
				else
					parentGrid.SetHelp (string.Empty, string.Empty);
			} else {
				parentGrid.SetHelp (string.Empty, string.Empty);
			}
		}
		
		internal void NotifyChanged ()
		{
			OnObjectChanged ();
		}
		
		void PropertyData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererProperty rc = (CellRendererProperty) cell;
			bool group = (bool) model.GetValue (iter, 2);
			if (group) {
				rc.SetData (null, null, null);
			} else {
				PropertyDescriptor prop = (PropertyDescriptor) model.GetValue (iter, 1);
				PropertyEditorCell propCell = editorManager.GetEditor (prop);
				InstanceData idata = (InstanceData) model.GetValue (iter, 3);
				propCell.Initialize (tree, editorManager, prop, idata.Instance);
				rc.SetData (idata.Instance, prop, propCell);
			}
		}
		
		void GroupData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererPropertyGroup rc = (CellRendererPropertyGroup) cell;
			rc.IsGroup = (bool) model.GetValue (iter, 2);
			rc.Text = (string) model.GetValue (iter, 0);
			
			PropertyDescriptor prop = (PropertyDescriptor) model.GetValue (iter, 1);
			if (prop != null)
				rc.SensitiveProperty = !prop.IsReadOnly;
			else
				rc.SensitiveProperty = true;
		}
		
		private class SortByCat : IComparer
		{
			public int Compare (object x, object y)
			{
				int catcomp = ((PropertyDescriptor)x).Category.CompareTo (((PropertyDescriptor)y).Category);

				if (catcomp == 0)
					return ((PropertyDescriptor)x).DisplayName.CompareTo (((PropertyDescriptor)y).DisplayName);
				else
					return catcomp;
			}
		}

		private class SortByName : IComparer
		{
			public int Compare(object x, object y)
			{
				return ((PropertyDescriptor)x).DisplayName.CompareTo (((PropertyDescriptor)y).DisplayName);
			}
		}
	}
	
	class InternalTree: TreeView
	{
		internal ArrayList Groups = new ArrayList ();
		Pango.Layout layout;
		bool editing;
		TreeIter editingIter;
		PropertyGridTree tree;
		internal bool dragging;
		int dragPos;
		Gdk.Cursor resizeCursor = new Gdk.Cursor (CursorType.SbHDoubleArrow);
		
		public InternalTree (PropertyGridTree tree, TreeModel model): base (model)
		{
			this.tree = tree;
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			Pango.FontDescription des = this.Style.FontDescription.Copy();
			layout.FontDescription = des;
		}
		
		public bool Editing {
			get { return editing; }
			set { editing = value; }
		}
		
		public TreeIter EditingIter {
			get { return editingIter; }
			set { editingIter = value; }
		}
		
		public PropertyGridTree PropertyTree {
			get { return tree; }
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			Groups.Clear ();
			
			bool res = base.OnExposeEvent (e);
			
			foreach (TreeGroup grp in Groups) {
				layout.SetMarkup ("<b>" + grp.Group + "</b>");
				e.Window.DrawLayout (this.Style.TextGC (grp.State), grp.X, grp.Y, layout);
			}
			
			return res;
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (resizeCursor != null) {
				resizeCursor.Dispose ();
				resizeCursor = null;
			}
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			if (dragging) {
				int nw = (int)(evnt.X) + dragPos;
				if (nw <= 40) nw = 40;
				GLib.Idle.Add (delegate {
					Columns[0].FixedWidth = nw;
					return false;
				});
			} else {
				int w = Columns[0].Width;
				if (Math.Abs (w - evnt.X) < 5)
					this.GdkWindow.Cursor = resizeCursor;
				else
					this.GdkWindow.Cursor = null;
			}
			return base.OnMotionNotifyEvent (evnt);
		}
		
		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			int w = Columns[0].Width;
			if (Math.Abs (w - evnt.X) < 5) {
				TreePath[] rows = Selection.GetSelectedRows ();
				if (rows != null && rows.Length > 0)
					SetCursor (rows[0], Columns[0], false);
				dragging = true;
				dragPos = w - (int) evnt.X;
				this.GdkWindow.Cursor = resizeCursor;
			}
			return base.OnButtonPressEvent (evnt);
		}
		
		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			if (dragging) {
				this.GdkWindow.Cursor = null;
				dragging = false;
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		public virtual void Update ()
		{
		}
	}
	
	class TreeGroup
	{
		public string Group;
		public int X;
		public int Y;
		public StateType State;
	}
	
	class CellRendererProperty: CellRenderer
	{
		TreeView tree;
		PropertyDescriptor property;
		object instance;
		int rowHeight;
		PropertyEditorCell editorCell;
		bool sensitive = true;
		bool visible = true;
		
		public CellRendererProperty (TreeView tree)
		{
			this.tree = tree;
			Xalign = 0;
			Xpad = 3;
			
			Mode |= Gtk.CellRendererMode.Editable;
			Entry dummyEntry = new Gtk.Entry ();
			dummyEntry.HasFrame = false;
			rowHeight = dummyEntry.SizeRequest ().Height;
		}
		
		public void SetData (object instance, PropertyDescriptor property, PropertyEditorCell editor)
		{
			this.instance = instance;
			this.property = property;
			if (property == null) {
				this.CellBackgroundGdk = tree.Style.MidColors [(int) Gtk.StateType.Normal];
				sensitive = true;
			}
			else {
				this.CellBackground = null;
				sensitive = !property.IsReadOnly;
			}
			
			editorCell = editor;
		}

		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			if (editorCell != null)
				editorCell.GetSize ((int)(cell_area.Width - this.Xpad * 2), out width, out height);
			else {
				width = height = 0;
			}
			
			width += (int) this.Xpad * 2;
			height += (int) this.Ypad * 2;

			x_offset = 0;
			y_offset = 0;
			
			if (height < rowHeight)
				height = rowHeight;
		}

		protected override void Render (Drawable window, Widget widget, Rectangle background_area, Rectangle cell_area, Rectangle expose_area, CellRendererState flags)
		{
			if (instance == null || !visible)
				return;
			int width = 0, height = 0;
			int iwidth = cell_area.Width - (int) this.Xpad * 2;
			
			if (editorCell != null)
				editorCell.GetSize ((int)(cell_area.Width - this.Xpad * 2), out width, out height);

			Rectangle bounds = new Rectangle ();
			bounds.Width = width > iwidth ? iwidth : width;
			bounds.Height = height;
			bounds.X = (int) (cell_area.X + this.Xpad);
			bounds.Y = cell_area.Y + (cell_area.Height - height) / 2;
			
			StateType state = GetState (flags);
				
			if (editorCell != null)
				editorCell.Render (window, bounds, state);
		}
		
		public override CellEditable StartEditing (Gdk.Event ev, Widget widget, string path, Gdk.Rectangle background_area, Gdk.Rectangle cell_area, CellRendererState flags)
		{
			if (property == null || editorCell == null || !sensitive)
				return null;

			StateType state = GetState (flags);
			EditSession session = editorCell.StartEditing (cell_area, state);
			if (session == null)
				return null;
			Gtk.Widget propEditor = (Gtk.Widget) session.Editor;
			propEditor.Show ();
			HackEntry e = new HackEntry (session, propEditor);
			e.Show ();
			session.Changed += delegate {
				((InternalTree)widget).PropertyTree.NotifyChanged ();
			};
			TreeIter it;
			((InternalTree)widget).Model.GetIterFromString (out it, path);
			((InternalTree)widget).EditingIter = it;
			return e;
		}
		
		StateType GetState (CellRendererState flags)
		{
			if (!sensitive)
				return StateType.Insensitive;
			else if ((flags & CellRendererState.Selected) != 0)
				return StateType.Selected;
			else
				return StateType.Normal;
		}
	}

	class CellRendererPropertyGroup: CellRendererText
	{
		TreeView tree;
		Pango.Layout layout;
		bool isGroup;
		bool sensitive = true;
		
		public bool IsGroup {
			get { return isGroup; }
			set { 
				isGroup = value;
				if (value)
					this.CellBackgroundGdk = tree.Style.MidColors [(int) Gtk.StateType.Normal];
				else
					this.CellBackground = null;
			}
		}
		
		public bool SensitiveProperty {
			get { return sensitive; }
			set { sensitive = value; }
		}
		
		public CellRendererPropertyGroup (TreeView tree)
		{
			this.tree = tree;
			layout = new Pango.Layout (tree.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			
			Pango.FontDescription des = tree.Style.FontDescription.Copy();
			layout.FontDescription = des;
		}
		
		protected void GetCellSize (Widget widget, int availableWidth, out int width, out int height)
		{
			layout.SetMarkup (Text);
			layout.Width = -1;
			layout.GetPixelSize (out width, out height);
		}
		
		public override void GetSize (Widget widget, ref Rectangle cell_area, out int x_offset, out int y_offset, out int width, out int height)
		{
			GetCellSize (widget, (int)(cell_area.Width - this.Xpad * 2), out width, out height);
			width += (int) this.Xpad * 2;
			height += (int) this.Ypad * 2;
			
			x_offset = y_offset = 0;
			
			if (IsGroup)
				width = 0;
		}

		protected override void Render (Drawable window, Widget widget, Rectangle background_area, Rectangle cell_area, Rectangle expose_area, CellRendererState flags)
		{
			int width, height;
			GetCellSize (widget, (int)(cell_area.Width - this.Xpad * 2), out width, out height);

			int x = (int) (cell_area.X + this.Xpad);
			int y = cell_area.Y + (cell_area.Height - height) / 2;

			StateType state;
			if (!sensitive)
				state = StateType.Insensitive;
			else if ((flags & CellRendererState.Selected) != 0)
				state = StateType.Selected;
			else
				state = StateType.Normal;

			if (IsGroup) {
				TreeGroup grp = new TreeGroup ();
				grp.X = x;
				grp.Y = y;
				grp.Group = Text;
				grp.State = state;
				InternalTree tree = (InternalTree) widget;
				tree.Groups.Add (grp);
			} else {
				window.DrawLayout (widget.Style.TextGC (state), x, y, layout);
				int bx = background_area.X + background_area.Width - 1;
				Gdk.GC gc = new Gdk.GC (window);
		   		gc.RgbFgColor = tree.Style.MidColors [(int) Gtk.StateType.Normal];
				window.DrawLine (gc, bx, background_area.Y, bx, background_area.Y + background_area.Height);
			}
		}
	}

	class HackEntry: Entry
	{
		EventBox box;
		EditSession session;
		
		public HackEntry (EditSession session, Gtk.Widget child)
		{
			this.session = session;
			box = new EventBox ();
			box.ButtonPressEvent += new ButtonPressEventHandler (OnClickBox);
			box.ModifyBg (StateType.Normal, Style.White);
			box.Add (child);
		}
		
		[GLib.ConnectBefore]
		void OnClickBox (object s, ButtonPressEventArgs args)
		{
			// Avoid forwarding the button press event to the
			// tree, since it would hide the cell editor.
			args.RetVal = true;
		}
		
		protected override void OnParentSet (Gtk.Widget parent)
		{
			base.OnParentSet (parent);
			
			if (Parent != null) {
				if (this.ParentWindow != null)
					box.ParentWindow = this.ParentWindow;
				box.Parent = Parent;
				box.Show ();
				((InternalTree)Parent).Editing = true;
			}
			else {
				session.Dispose ();
				((InternalTree)parent).Editing = false;
				box.Unparent ();
			}
		}
		
		protected override void OnShown ()
		{
			// Do nothing.
		}
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			box.SizeRequest ();
			box.Allocation = allocation;
		}
	}
	
	class InstanceData 
	{
		public InstanceData (object instance) 
		{
			Instance = instance;
		}
		
		public object Instance;
	}
}
