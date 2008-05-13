using Gtk;
using System;
using Mono.Unix;
using System.Collections.Generic;

namespace Stetic {

	internal class ProjectViewBackend : ScrolledWindow 
	{
		ProjectViewBackendNodeView nodeView;
		
		public ProjectViewBackend (ProjectViewFrontend frontend)
		{
			ShadowType = Gtk.ShadowType.In;
			nodeView = new ProjectViewBackendNodeView (frontend);
			Add (nodeView);
			ShowAll ();
		}
		
		public event Wrapper.WidgetEventHandler WidgetActivated {
			add { nodeView.WidgetActivated += value; }
			remove { nodeView.WidgetActivated -= value; }
		}
		
		public void Bind (WidgetEditSession newSession) 
		{
			nodeView.Bind (newSession);
		}
	}
	
	internal class ProjectViewBackendNodeView : TreeView 
	{
		ProjectViewFrontend frontend;
		TreeStore store;
		WidgetEditSession editSession;
		
		const int ColIcon = 0;
		const int ColName = 1;
		const int ColWrapper = 2;
		const int ColFilled = 3;
		
		public event Wrapper.WidgetEventHandler WidgetActivated;
		
		public ProjectViewBackendNodeView (ProjectViewFrontend frontend)
		{
			this.frontend = frontend;
			HeadersVisible = false;
			
			store = new TreeStore (typeof(Gdk.Pixbuf), typeof(string), typeof(ObjectWrapper), typeof(bool));
			Model = store;
			
			TreeViewColumn col;
			CellRenderer renderer;

			col = new TreeViewColumn ();

			renderer = new CellRendererPixbuf ();
			col.PackStart (renderer, false);
			col.AddAttribute (renderer, "pixbuf", 0);

			renderer = new CellRendererText ();
			col.PackStart (renderer, true);
			col.AddAttribute (renderer, "text", 1);

			AppendColumn (col);

			Selection.Mode = SelectionMode.Single;
			Selection.Changed += RowSelected;
			TestExpandRow += OnTestExpandRow;
			ShowAll ();
		}
		
		public void Bind (WidgetEditSession newSession) 
		{
			if (editSession != null) {
				editSession.SelectionChanged -= WidgetSelected;
				editSession.EditingBackend.ProjectReloaded -= OnProjectReloaded;
				editSession.EditingBackend.WidgetNameChanged -= OnWidgetNameChanged;
				editSession.EditingBackend.WidgetContentsChanged -= OnContentsChanged;
			}
			editSession = newSession;
			if (editSession != null) {
				editSession.SelectionChanged += WidgetSelected;
				editSession.EditingBackend.ProjectReloaded += OnProjectReloaded;
				editSession.EditingBackend.WidgetNameChanged += OnWidgetNameChanged;
				editSession.EditingBackend.WidgetContentsChanged += OnContentsChanged;
			}
			LoadProject ();
		}
		
		public override void Dispose ()
		{
			if (editSession != null) {
				editSession.SelectionChanged -= WidgetSelected;
				editSession.EditingBackend.ProjectReloaded -= OnProjectReloaded;
				editSession.EditingBackend.WidgetNameChanged -= OnWidgetNameChanged;
				editSession.EditingBackend.WidgetContentsChanged -= OnContentsChanged;
			}
			base.Dispose ();
		}
		
		public void LoadProject ()
		{
			Clear ();
			if (editSession == null || editSession.RootWidget == null)
				return;
			
			AddNode (TreeIter.Zero, editSession.RootWidget.Wrapped);
		}
		
		public void AddNode (TreeIter iter, Gtk.Widget widget)
		{
			Stetic.Wrapper.Widget wrapper = GetVisibleWrapper (widget);
			if (wrapper == null)
				return;
			
			Gdk.Pixbuf icon = wrapper.ClassDescriptor.Icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear);
			string txt = widget.Name;
			
			if (!iter.Equals (TreeIter.Zero))
				iter = store.AppendValues (iter, icon, txt, wrapper, true);
			else
				iter = store.AppendValues (icon, txt, wrapper, true);
			
			FillChildren (iter, wrapper);
		}
		
		void FillChildren (TreeIter it, Wrapper.Widget wrapper)
		{
			Stetic.Wrapper.Container container = wrapper as Wrapper.Container;
			if (container != null) {
				foreach (Gtk.Widget w in container.RealChildren) {
					Stetic.Wrapper.Widget ww = GetVisibleWrapper (w);
					if (ww != null) {
						// Add a dummy node to allow lazy loading
						store.SetValue (it, ColFilled, false);
						store.AppendValues (it, null, "", null, false);
						return;
					}
				}
			}
			store.SetValue (it, ColFilled, true);
		}
		
		Wrapper.Widget GetVisibleWrapper (Gtk.Widget w)
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (w);
			if (wrapper == null || wrapper.Unselectable)
				return null;
			else
				return wrapper;
		}
		
		void EnsureFilled (TreeIter it)
		{
			bool filled = (bool) store.GetValue (it, ColFilled);
			if (filled)
				return;
			
			// Remove the dummy node
			TreeIter cit;
			store.IterChildren (out cit, it);
			store.Remove (ref cit);
			
			// Add the real children
			Stetic.Wrapper.Container container = store.GetValue (it, ColWrapper) as Wrapper.Container;
			if (container != null) {
				foreach (Gtk.Widget w in container.RealChildren)
					AddNode (it, w);
			}
			
			store.SetValue (it, ColFilled, true);
		}
		
		void OnTestExpandRow (object s, Gtk.TestExpandRowArgs args)
		{
			EnsureFilled (args.Iter);
			args.RetVal = false;
		}
		
		public void Clear ()
		{
			TreeIter it;
			while (store.GetIterFirst (out it)) {
				store.Remove (ref it);
			}
		}
		
		void RefreshNode (TreeIter it)
		{
			Wrapper.Widget wrapper = (Wrapper.Widget) store.GetValue (it, ColWrapper);
			store.SetValue (it, ColName, wrapper.Wrapped.Name);
			TreeIter cit;
			while (store.IterChildren (out cit, it)) {
				store.Remove (ref cit);
			}
			FillChildren (it, wrapper);
		}
		
		void OnWidgetNameChanged (object s, Wrapper.WidgetNameChangedArgs args)
		{
			TreeIter? node = FindNode (args.WidgetWrapper, false);
			if (node == null)
				return;
			store.SetValue (node.Value, ColName, args.NewName);
		}
			
		void OnContentsChanged (object s, Wrapper.WidgetEventArgs args)
		{
			TreeIter? node = FindNode (args.WidgetWrapper, false);
			if (node != null)
				RefreshNode (node.Value);
		}
		
		void OnProjectReloaded (object o, EventArgs a)
		{
			LoadProject ();
		}
		
		Stetic.Wrapper.Widget SelectedWrapper {
			get {
				TreeIter iter;
				if (!Selection.GetSelected (out iter))
					return null;
				Wrapper.Widget w = (Wrapper.Widget) store.GetValue (iter, ColWrapper);
				return w;
			}
		}

		bool syncing = false;

		void RowSelected (object obj, EventArgs args)
		{
			if (!syncing) {
				syncing = true;
				Stetic.Wrapper.Widget selection = SelectedWrapper;
				if (selection != null)
					selection.Select ();
				syncing = false;
				NotifySelectionChanged (selection);
			}
		}

		void WidgetSelected (object s, Wrapper.WidgetEventArgs args)
		{
			if (!syncing) {
				syncing = true;
				if (args.Widget != null) {
					Wrapper.Widget w = Wrapper.Widget.Lookup (args.Widget);
					if (w != null) {
						TreeIter? it = FindNode (w, true);
						if (it != null) {
							ExpandToPath (store.GetPath (it.Value));
							Selection.SelectIter (it.Value);
							ScrollToCell (store.GetPath (it.Value), Columns[0], false, 0, 0);
							NotifySelectionChanged (w);
						}
					}
				}
				else {
					Selection.UnselectAll ();
					NotifySelectionChanged (null);
				}
				syncing = false;
			}
		}
		
		TreeIter? FindNode (Wrapper.Widget w, bool loadBranches)
		{
			Wrapper.Widget parent = w.ParentWrapper;
			TreeIter it;
			
			if (parent == null) {
				if (!store.GetIterFirst (out it))
					return null;
			} else {
				TreeIter? pi = FindNode (parent, loadBranches);
				if (pi == null)
					return null;
				if (loadBranches)
					EnsureFilled (pi.Value);
				if (!store.IterChildren (out it, pi.Value))
					return null;
			}
			
			do {
				Wrapper.Widget cw = (Wrapper.Widget) store.GetValue (it, ColWrapper);
				if (cw == w)
					return it;
			} while (store.IterNext (ref it));
			
			return null;
		}
		
		void NotifySelectionChanged (Stetic.Wrapper.Widget w)
		{
			if (frontend == null)
				return;
			if (w != null)
				frontend.NotifySelectionChanged (Component.GetSafeReference (w), w.Wrapped.Name, w.ClassDescriptor.Name);
			else
				frontend.NotifySelectionChanged (null, null, null);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evt)
		{
			if (evt.Button == 3 && evt.Type == Gdk.EventType.ButtonPress)
				return OnPopupMenu ();
			return base.OnButtonPressEvent (evt);
		}

		protected override bool OnPopupMenu ()
		{
			Stetic.Wrapper.Widget selection = SelectedWrapper;

			if (selection != null) {
				Menu m = new ContextMenu (selection);
				if (selection.IsTopLevel) {
					// Allow deleting top levels from the project view
					ImageMenuItem item = new ImageMenuItem (Gtk.Stock.Delete, null);
					item.Activated += delegate (object obj, EventArgs args) {
						selection.Delete ();
					};
					item.Show ();
					m.Add (item);
				}
				m.Popup ();
				return true;
			} else
				return base.OnPopupMenu ();
		}

		protected override void OnRowActivated (TreePath path, TreeViewColumn col)
		{
			base.OnRowActivated (path, col);
			Stetic.Wrapper.Widget w = SelectedWrapper;
			if (w != null) {
				if (frontend != null)
					frontend.NotifyWidgetActivated (Component.GetSafeReference (w), w.Wrapped.Name, w.ClassDescriptor.Name);
				if (WidgetActivated != null)
					WidgetActivated (this, new Wrapper.WidgetEventArgs (w));
			}
		}
	}
}
