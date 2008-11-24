
using System;
using System.Collections;
using Gtk;
using Gdk;
using Mono.Unix;

namespace Stetic
{
	public class PropertyTree: Gtk.ScrolledWindow
	{
		Gtk.TreeStore store;
		InternalTree tree;
		TreeViewColumn editorColumn;
		Hashtable propertyRows;
		Hashtable sensitives, invisibles;
		ArrayList expandStatus = new ArrayList ();
		
		public PropertyTree ()
		{
			propertyRows = new Hashtable ();
			sensitives = new Hashtable ();
			invisibles = new Hashtable ();
			
			store = new TreeStore (typeof (string), typeof(PropertyDescriptor), typeof(bool), typeof(InstanceData));
			
			tree = new InternalTree (this, store);

			CellRendererText crt;
			
			TreeViewColumn col;

			col = new TreeViewColumn ();
			col.Title = Catalog.GetString ("Property");
			crt = new CellRendererPropertyGroup (tree);
			col.PackStart (crt, true);
			col.SetCellDataFunc (crt, new TreeCellDataFunc (GroupData));
			col.Resizable = true;
			col.Expand = false;
			col.Sizing = TreeViewColumnSizing.Fixed;
			col.FixedWidth = 150;
			tree.AppendColumn (col);
			
			editorColumn = new TreeViewColumn ();
			editorColumn.Title = Catalog.GetString ("Value");
			
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
		
		public void AddProperties (ItemGroupCollection itemGroups, object instance, string targetGtkVersion)
		{
			foreach (ItemGroup igroup in itemGroups)
				AddGroup (igroup, instance, targetGtkVersion);
		}
		
		public void SaveStatus ()
		{
			expandStatus.Clear ();

			TreeIter iter;
			if (!tree.Model.GetIterFirst (out iter))
				return;
			
			do {
				if (tree.GetRowExpanded (tree.Model.GetPath (iter))) {
					expandStatus.Add (tree.Model.GetValue (iter, 0));
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
				if (expandStatus.Contains (grp))
					tree.ExpandRow (tree.Model.GetPath (iter), true);
			} while (tree.Model.IterNext (ref iter));
		}
		
		public virtual void Clear ()
		{
			store.Clear ();
			propertyRows.Clear ();
			sensitives.Clear ();
			invisibles.Clear ();
		}
		
		public virtual void Update ()
		{
			// Just repaint the cells
			QueueDraw ();
		}
		
		public void AddGroup (ItemGroup igroup, object instance, string targetGtkVersion)
		{
			ArrayList props = new ArrayList ();
			foreach (ItemDescriptor item in igroup) {
				if (item.IsInternal)
					continue;
				if (item is PropertyDescriptor && item.SupportsGtkVersion (targetGtkVersion))
					props.Add (item);
			}
			
			if (props.Count == 0)
				return;
			
			InstanceData idata = new InstanceData (instance);
			TreeIter iter = store.AppendValues (igroup.Label, null, true, idata);
			foreach (PropertyDescriptor item in props)
				AppendProperty (iter, (PropertyDescriptor)item, idata);
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
				iter = store.AppendValues (prop.Label, prop, false, idata);
			else
				iter = store.AppendValues (piter, prop.Label, prop, false, idata);
			if (prop.HasDependencies)
				sensitives[prop] = prop;
			if (prop.HasVisibility)
				invisibles[prop] = prop;
			propertyRows [prop] = store.GetStringFromIter (iter);
		}
		
		protected virtual void OnObjectChanged ()
		{
		}
		
		void OnSelectionChanged (object s, EventArgs a)
		{
			TreePath[] rows = tree.Selection.GetSelectedRows ();
			if (!tree.dragging && rows != null && rows.Length > 0) {
				tree.SetCursor (rows[0], editorColumn, true);
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
				PropertyEditorCell propCell = PropertyEditorCell.GetPropertyCell (prop);
				InstanceData idata = (InstanceData) model.GetValue (iter, 3);
				propCell.Initialize (tree, prop, idata.Instance);
				rc.SetData (idata.Instance, prop, propCell);
			}
		}
		
		void GroupData (Gtk.TreeViewColumn tree_column, Gtk.CellRenderer cell, Gtk.TreeModel model, Gtk.TreeIter iter)
		{
			CellRendererPropertyGroup rc = (CellRendererPropertyGroup) cell;
			rc.IsGroup = (bool) model.GetValue (iter, 2);
			rc.Text = (string) model.GetValue (iter, 0);
			
			PropertyDescriptor prop = (PropertyDescriptor) model.GetValue (iter, 1);
			if (prop != null) {
				InstanceData idata = (InstanceData) model.GetValue (iter, 3);
				rc.SensitiveProperty = prop.EnabledFor (idata.Instance) && prop.VisibleFor (idata.Instance);
			} else
				rc.SensitiveProperty = true;
		}
	}
	
	class InternalTree: TreeView
	{
		internal ArrayList Groups = new ArrayList ();
		Pango.Layout layout;
		bool editing;
		PropertyTree tree;
		internal bool dragging;
		int dragPos;
		Gdk.Cursor resizeCursor = new Gdk.Cursor (CursorType.SbHDoubleArrow);
		
		public InternalTree (PropertyTree tree, TreeModel model): base (model)
		{
			this.tree = tree;
			layout = new Pango.Layout (this.PangoContext);
			layout.Wrap = Pango.WrapMode.Char;
			Pango.FontDescription des = this.Style.FontDescription.Copy();
			layout.FontDescription = des;
		}
		
		public bool Editing {
			get { return editing; }
			set { editing = value; Update (); tree.NotifyChanged (); }
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			Groups.Clear ();
			
			bool res = base.OnExposeEvent (e);
			
			foreach (TreeGroup grp in Groups) {
				layout.SetMarkup ("<b>" + GLib.Markup.EscapeText (grp.Group) + "</b>");
				e.Window.DrawLayout (this.Style.TextGC (grp.State), grp.X, grp.Y, layout);
			}
			
			return res;
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
		PropertyDescriptor property;
		object instance;
		int rowHeight;
		PropertyEditorCell editorCell;
		bool sensitive;
		bool visible;
		TreeView tree;
		
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
			if (property == null)
				this.CellBackgroundGdk = tree.Style.MidColors [(int)Gtk.StateType.Normal];
			else
				this.CellBackground = null;
			
			visible = property != null ? property.VisibleFor (instance): true;
			sensitive = property != null ? property.EnabledFor (instance) && property.VisibleFor (instance): true;
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
			HackEntry e = new HackEntry (propEditor, session);
			e.Show ();
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
		bool sensitive;
		
		public bool IsGroup {
			get { return isGroup; }
			set { 
				isGroup = value;
				if (value)
					this.CellBackgroundGdk = tree.Style.MidColors [(int)Gtk.StateType.Normal];
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
		   		gc.RgbFgColor = tree.Style.MidColors [(int)Gtk.StateType.Normal];
				window.DrawLine (gc, bx, background_area.Y, bx, background_area.Y + background_area.Height);
			}
		}
	}

	class HackEntry: Entry
	{
		EventBox box;
		EditSession session;
		
		public HackEntry (Gtk.Widget child, EditSession session)
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
