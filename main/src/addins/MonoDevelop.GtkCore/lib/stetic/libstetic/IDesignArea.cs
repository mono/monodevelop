
using System;

namespace Stetic
{
	public interface IDesignArea
	{
		IObjectSelection SetSelection (Gtk.Widget widget, object selectedInstance);
		IObjectSelection SetSelection (Gtk.Widget widget, object selectedInstance, bool allowDrag);
		void ResetSelection (Gtk.Widget widget);
		bool IsSelected (Gtk.Widget widget);
		IObjectSelection GetSelection ();
		IObjectSelection GetSelection (Gtk.Widget widget);

		void AddWidget (Gtk.Widget w, int x, int y);
		void RemoveWidget (Gtk.Widget w);
		void MoveWidget (Gtk.Widget w, int x, int y);
		Gdk.Rectangle GetCoordinates (Gtk.Widget w);
		
		event EventHandler SelectionChanged;
	}
	
	public delegate void DragDelegate (Gdk.EventMotion evt, int dx, int dy);

	public interface IObjectViewer
	{
		object TargetObject { get; set; }
	}
	
	public interface IObjectSelection: IDisposable
	{
		Gtk.Widget Widget { get; }
		object DataObject { get; }
		bool AllowDrag {get; set; }
		
		event DragDelegate Drag;
		event EventHandler Disposed;
	}
}
