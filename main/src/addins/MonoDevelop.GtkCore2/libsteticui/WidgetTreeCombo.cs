
using System;
using Gtk;

namespace Stetic
{
	internal class WidgetTreeCombo: Button
	{
		Label label;
		Image image;
		Stetic.Wrapper.Widget rootWidget;
		Stetic.IProject project;
		Stetic.Wrapper.Widget selection;
		WidgetTreePopup popup;
		
		public WidgetTreeCombo ()
		{
			label = new Label ();
			image = new Gtk.Image ();
			
			HBox bb = new HBox ();
			bb.PackStart (image, false, false, 3);
			bb.PackStart (label, true, true, 3);
			label.Xalign = 0;
			label.HeightRequest = 18;
			bb.PackStart (new VSeparator (), false, false, 1);
			bb.PackStart (new Arrow (ArrowType.Down, ShadowType.None), false, false, 1);
			Child = bb;
			this.WidthRequest = 300;
			Sensitive = false;
		}
		
		public override void Dispose ()
		{
			if (popup != null)
				popup.Destroy ();
			base.Dispose ();
		}

		
		public Stetic.Wrapper.Widget RootWidget {
			get { return rootWidget; }
			set {
				rootWidget = value;
				Sensitive = rootWidget != null;
				if (rootWidget != null)
					project = rootWidget.Project;
				Update ();
			}
		}
		
		public void SetSelection (Stetic.Wrapper.Widget widget) 
		{
			selection = widget;
			Update ();
		}
		
		void Update ()
		{
			if (selection != null) {
				label.Text = selection.Wrapped.Name;
				image.Pixbuf = selection.ClassDescriptor.Icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear);
				image.Show ();
			} else {
				label.Text = "             ";
				image.Hide ();
			}
		}
		
		protected override void OnPressed ()
		{
			base.OnPressed ();
			if (popup == null) {
				popup = new WidgetTreePopup (project);
				popup.WidgetActivated += OnPopupActivated;
				popup.SetDefaultSize (Allocation.Width, 500);
			}
			else if (popup.Visible) {
				popup.Hide ();
				return;
			}
			
			int x, y;
			ParentWindow.GetOrigin (out x, out y);
			x += Allocation.X;
			y += Allocation.Y + Allocation.Height;
			
			popup.Fill (RootWidget.Wrapped);
			popup.Move (x, y);
			popup.ShowAll ();
		}
		
		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			if (popup != null)
				popup.Hide ();
			return base.OnFocusOutEvent (evnt);
		}

		
		void OnPopupActivated (object s, EventArgs a)
		{
			popup.Hide ();
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (popup.Selection);
			GLib.Timeout.Add (10, delegate {
				wrapper.Select ();
				return false;
			});
		}
	}
	
	class WidgetTreePopup: Window
	{
		TreeView tree;
		TreeStore store;
		Gtk.Widget selection;
		Stetic.IProject project;
		
		public event EventHandler WidgetActivated;
		
		public WidgetTreePopup (Stetic.IProject project): base (WindowType.Popup)
		{
			this.project = project;
			Gtk.Frame frame = new Frame ();
			frame.ShadowType = Gtk.ShadowType.Out;
			Add (frame);
			
			Gtk.ScrolledWindow sc = new ScrolledWindow ();
			sc.VscrollbarPolicy = PolicyType.Automatic;
			sc.HscrollbarPolicy = PolicyType.Automatic;
			frame.Add (sc);
			
			tree = new TreeView ();
			store = new TreeStore (typeof(Gdk.Pixbuf), typeof(string), typeof(Widget));
			tree.Model = store;
			tree.HeadersVisible = false;
			
			TreeViewColumn col = new TreeViewColumn ();
			CellRendererPixbuf cr = new CellRendererPixbuf ();
			col.PackStart (cr, false);
			col.AddAttribute (cr, "pixbuf", 0);
			
			CellRendererText tr = new CellRendererText ();
			col.PackStart (tr, true);
			col.AddAttribute (tr, "markup", 1);
			
			tree.AppendColumn (col);
			
			tree.ButtonReleaseEvent += OnButtonRelease;
			tree.RowActivated += OnButtonRelease;
			sc.Add (tree);
			tree.GrabFocus ();
			
		}
		
		public void Fill (Gtk.Widget widget)
		{
			store.Clear ();
			Fill (TreeIter.Zero, widget);
		}
		
		public void Fill (TreeIter iter, Gtk.Widget widget)
		{
			Stetic.Wrapper.Widget wrapper = Stetic.Wrapper.Widget.Lookup (widget);
			if (wrapper == null) return;
			
			if (!wrapper.Unselectable) {
				
				Gdk.Pixbuf icon = wrapper.ClassDescriptor.Icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear);
				string txt;
				if (widget == project.Selection)
					txt = "<b>" + GLib.Markup.EscapeText (widget.Name) + "</b>";
				else
					txt = GLib.Markup.EscapeText (widget.Name);
				
				if (!iter.Equals (TreeIter.Zero))
					iter = store.AppendValues (iter, icon, txt, widget);
				else
					iter = store.AppendValues (icon, txt, widget);
			}
			
			Gtk.Container cc = widget as Gtk.Container;
			if (cc != null && wrapper.ClassDescriptor.AllowChildren) {
				foreach (Gtk.Widget child in cc.Children)
					Fill (iter, child);
			}
			if (widget == project.Selection) {
				tree.ExpandToPath (store.GetPath (iter));
				tree.ExpandRow (store.GetPath (iter), false);
				tree.Selection.SelectIter (iter);
			}
		}
		
		public Gtk.Widget Selection {
			get { return selection; }
		}
		
		void OnButtonRelease (object s, EventArgs a)
		{
			NotifyActivated ();
		}
		
		void NotifyActivated ()
		{
			TreeIter iter;
			if (!tree.Selection.GetSelected (out iter))
				return;
			selection = (Gtk.Widget) store.GetValue (iter, 2);
			if (WidgetActivated != null)
				WidgetActivated (this, EventArgs.Empty);
		}
	}
}
