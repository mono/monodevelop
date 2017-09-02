using Gtk;
using Gdk;
using System;
using System.Collections;
using System.Reflection;

namespace Stetic {

	// This is the base class for palette items. Implements the basic
	// functionality for showing the icon and label of the item.
	
	internal class PaletteItemFactory : EventBox 
	{
		public PaletteItemFactory ()
		{
		}
		
		public virtual void Initialize (string name, Gdk.Pixbuf icon)
		{
			DND.SourceSet (this);
			AboveChild = true;

			Gtk.HBox hbox = new HBox (false, 6);
			hbox.BorderWidth = 3;

			if (icon != null) {
				icon = icon.ScaleSimple (16, 16, Gdk.InterpType.Bilinear);
				hbox.PackStart (new Gtk.Image (icon), false, false, 0);
			}

			Gtk.Label label = new Gtk.Label ("<span font='11'>" + name + "<span>");
			label.UseMarkup = true;
			label.Justify = Justification.Left;
			label.Xalign = 0;
			hbox.PackEnd (label, true, true, 0);

			Add (hbox);
		}

		protected override void OnDragBegin (Gdk.DragContext ctx)
		{
			Gtk.Widget ob = CreateItemWidget ();
			if (ob != null)
				DND.Drag (this, ctx, ob);
		}
		
		protected virtual Gtk.Widget CreateItemWidget ()
		{
			return null;
		}
		
		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing ev)
		{
			this.State = Gtk.StateType.Prelight;
			return base.OnEnterNotifyEvent (ev);
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
			this.State = Gtk.StateType.Normal;
			return base.OnLeaveNotifyEvent (ev);
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose e)
		{
			base.OnExposeEvent (e);
			if (State == Gtk.StateType.Prelight)
				Gtk.Style.PaintShadow (this.Style, this.GdkWindow, State, Gtk.ShadowType.Out, e.Area, this, "", e.Area.X, e.Area.Y, e.Area.Width, e.Area.Height);
			return false;
		}
	}


	// Palette item factory which creates a widget.
	internal class WidgetFactory : PaletteItemFactory {

		protected ProjectBackend project;
		protected ClassDescriptor klass;

		public WidgetFactory (ProjectBackend project, ClassDescriptor klass)
		{
			this.project = project;
			this.klass = klass;
			Initialize (klass.Label, klass.Icon);
			if (project == null)
				Sensitive = false;
		}
		
		protected override Gtk.Widget CreateItemWidget ()
		{
			return klass.NewInstance (project) as Widget;
		}
	}

	internal class WindowFactory : WidgetFactory
	{
		public WindowFactory (ProjectBackend project, ClassDescriptor klass) : base (project, klass) 
		{
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evt)
		{
			Gtk.Window win = klass.NewInstance (project) as Gtk.Window;
			project.AddWindow (win, true);
			return true;
		}

		public override void Initialize (string name, Gdk.Pixbuf icon)
		{
			base.Initialize (name, icon);
			DND.SourceUnset (this);
		}
	}
	
	// Palette item factory which allows dragging an already existing object.
	internal class InstanceWidgetFactory : PaletteItemFactory 
	{
		Gtk.Widget instance;
		
		public InstanceWidgetFactory (string name, Gdk.Pixbuf icon, Gtk.Widget instance)
		{
			this.instance = instance;
			Initialize (name, icon);
		}

		protected override Gtk.Widget CreateItemWidget ()
		{
			return instance;
		}
		
		public override void Dispose ()
		{
			base.Dispose ();
		}
	}
}
