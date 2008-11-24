
using System;
using System.Collections;
using Gtk;
using Gdk;

namespace Stetic.Editor
{
	public class WidgetSelector: ComboBox, IPropertyEditor
	{
		Gtk.Widget obj;
		ListStore store;
		Hashtable widgets = new Hashtable ();
		
		public void Initialize (PropertyDescriptor descriptor)
		{
			store = new ListStore (typeof(Pixbuf), typeof(string));
			Model = store;
			store.SetSortColumnId (1, SortType.Ascending);
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			CellRendererText crt = new CellRendererText ();
			PackStart (crp, false);
			PackStart (crt, true);
			SetAttributes (crp, "pixbuf", 0);
			SetAttributes (crt, "text", 1);
		}
		
		public void AttachObject (object obj)
		{
			this.obj = obj as Gtk.Widget;
			FillWidgets ();
		}
		
		void FillWidgets ()
		{
			store.Clear ();
			widgets.Clear ();
			
			Stetic.Wrapper.Widget widget = Stetic.Wrapper.Widget.Lookup (obj);
			if (widget == null)
				return;
				
			while (!widget.IsTopLevel)
				widget = widget.ParentWrapper;
			
			store.AppendValues (null, "(None)");
			FillWidgets (widget, 0);
		}
		
		void FillWidgets (Stetic.Wrapper.Widget widget, int level)
		{
			if (!widget.Unselectable) {
				TreeIter iter = store.AppendValues (widget.ClassDescriptor.Icon, widget.Wrapped.Name);
				widgets [widget.Wrapped.Name] = iter;
			}
			Gtk.Container cont = widget.Wrapped as Gtk.Container;
			if (cont != null && widget.ClassDescriptor.AllowChildren) {
				foreach (Gtk.Widget child in cont.AllChildren) {
					Stetic.Wrapper.Widget cwidget = Stetic.Wrapper.Widget.Lookup (child);
					if (cwidget != null)
						FillWidgets (cwidget, level+1);
				}
			}
		}
		
		
		public object Value {
			get {
				if (Active <= 0)
					return null;
				else {
					TreeIter iter;
					if (!GetActiveIter (out iter))
						return null;
					return (string) store.GetValue (iter, 1);
				}
			}
			set {
				if (value == null)
					Active = 0;
				else if (widgets.Contains ((string) value)) {
					TreeIter iter = (TreeIter) widgets [value];
					SetActiveIter (iter);
				}
			}
		}
		
		protected override void OnChanged ()
		{
			base.OnChanged ();
			if (ValueChanged != null)
				ValueChanged (this, EventArgs.Empty);
		}

		// To be fired when the edited value changes.
		public event EventHandler ValueChanged;
	}
}
