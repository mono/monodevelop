
using System;
using Stetic.Wrapper;

namespace Stetic.Editor
{
	abstract class ActionItem: Gtk.EventBox, IEditableObject
	{
		protected IMenuItemContainer parentMenu;
		protected ActionTreeNode node;
		protected Widget wrapper;
		protected bool localUpdate;
		protected bool editOnRelease;
		protected bool editing;
		protected uint itemSpacing;
		protected int minWidth;
		
		// To use in the action editor
		protected IDesignArea designArea;
		protected IProject project;
		bool disposed;
		
		public ActionItem (ActionTreeNode node, IMenuItemContainer parent, uint itemSpacing)
		{
			DND.SourceSet (this);
			this.parentMenu = parent;
			this.node = node;
			this.VisibleWindow = false;
			this.CanFocus = true;
			this.Events |= Gdk.EventMask.KeyPressMask;
			this.itemSpacing = itemSpacing;
			if (node.Action != null)
				node.Action.ObjectChanged += OnActionChanged;
		}
		
		public ActionTreeNode Node {
			get { return node; }
		}
		
		public uint ItemSpacing {
			get { return itemSpacing; }
			set { itemSpacing = value; }
		}
		
		public int MinWidth {
			get { return minWidth; }
			set { minWidth = value; }
		}
		
		public bool IsSelected {
			get {
				IDesignArea area = GetDesignArea ();
				return area.IsSelected (this);
			}
		}
		
		protected void UpdateSelectionStatus ()
		{
			IDesignArea area = GetDesignArea ();
			IObjectSelection sel = area.GetSelection ();
			sel.Disposed -= OnSelectionDisposed;
			sel.Drag -= HandleItemDrag;
			
			area.ResetSelection (this);
			
			sel = area.SetSelection (this, this);
			sel.Drag += HandleItemDrag;
			sel.Disposed += OnSelectionDisposed;
		}
		
		public virtual void Select ()
		{
			IDesignArea area = GetDesignArea ();
			if (area.IsSelected (this))
				return;
			IObjectSelection sel = area.SetSelection (this, node.Action != null ? node.Action.GtkAction : null);
			sel.Drag += HandleItemDrag;
			sel.Disposed += OnSelectionDisposed;
			GrabFocus ();
		}
		
		void OnSelectionDisposed (object ob, EventArgs a)
		{
			if (!disposed)
				EndEditing (Gdk.Key.Return);
		}
		
		protected virtual void EndEditing (Gdk.Key exitKey)
		{
		}
		
		public override void Dispose ()
		{
			disposed = true;
			base.Dispose ();
		}


		void HandleItemDrag (Gdk.EventMotion evt, int dx, int dy)
		{
			ProcessDragBegin (null, evt);
		}
		
		protected IDesignArea GetDesignArea ()
		{
			if (wrapper != null)
				return wrapper.GetDesignArea ();
			else
				return designArea;
		}
		
		protected IProject GetProject ()
		{
			if (wrapper != null)
				return wrapper.Project;
			else
				return project;
		}
		
		void OnActionChanged (object ob, ObjectWrapperEventArgs a)
		{
			if (!localUpdate)
				Refresh ();
		}
		
		public abstract void Refresh ();
		
		protected override bool OnButtonPressEvent (Gdk.EventButton ev)
		{
			return ProcessButtonPress (ev);
		}
		
		public bool ProcessButtonPress (Gdk.EventButton ev)
		{
			if (ev.Button == 1) {
				IDesignArea area = GetDesignArea ();
				if (area == null)
					return true;

				// Clicking a selected item starts the edit mode
				if (area.IsSelected (this)) {
					editOnRelease = true;
					return true;
				}
			} else if (ev.Button == 3) {
				parentMenu.ShowContextMenu (this);
			}
			
			Select ();
			return true;
		}
		
		protected override void OnDragBegin (Gdk.DragContext ctx)
		{
			ProcessDragBegin (ctx, null);
		}
		
		public virtual void ProcessDragBegin (Gdk.DragContext ctx, Gdk.EventMotion evt)
		{
			editOnRelease = false;
			ActionPaletteItem item = new ActionPaletteItem (node);
			if (ctx != null)
				DND.Drag (parentMenu.Widget, ctx, item);
			else
				DND.Drag (parentMenu.Widget, evt, item);
		}
		
		public bool CanCopy {
			get { return !editing; }
		}

		public bool CanCut {
			get { return false; }
		}

		public bool CanPaste {
			get { return false; }
		}

		public bool CanDelete {
			get { return !editing; }
		}
		
		public void Copy ()
		{
		}
		
		public void Cut ()
		{
		}

		public void Paste ()
		{
		}
		
		public void Delete ()
		{
			if (node.ParentNode != null)
				node.ParentNode.Children.Remove (node);
			Destroy ();
		}
		
		void IEditableObject.Delete ()
		{
			if (!editing)
				Delete ();
		}
	}
}
