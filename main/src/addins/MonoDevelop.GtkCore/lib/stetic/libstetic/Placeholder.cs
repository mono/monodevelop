using Gtk;
using System;

namespace Stetic {

	public class Placeholder : Gtk.DrawingArea, IEditableObject
	{
		// This id is used by the undo methods to identify a child of a container.
		string undoId;
		
		public Placeholder ()
		{
			undoId = WidgetUtils.GetUndoId ();
			DND.DestSet (this, true);
			Events |= Gdk.EventMask.ButtonPressMask;
		}
		
		internal string UndoId {
			get { return undoId; }
			set { undoId = value; }
		}

		const int minSize = 10;

		protected override void OnSizeRequested (ref Requisition req)
		{
			base.OnSizeRequested (ref req);
			if (req.Width <= 0)
				req.Width = minSize;
			if (req.Height <= 0)
				req.Height = minSize;
		}

		static private string[] placeholder_xpm = {
			"8 8 2 1",
			"  c #bbbbbb",
			". c #d6d6d6",
			"   ..   ",
			"  .  .  ",
			" .    . ",
			".      .",
			".      .",
			" .    . ",
			"  .  .  ",
			"   ..   "
		};

		Gdk.Pixmap pixmap;
		
		protected override void OnRealized ()
		{
			base.OnRealized ();

			Gdk.Pixmap mask;
			pixmap = Gdk.Pixmap.CreateFromXpmD (GdkWindow, out mask, new Gdk.Color (99, 99, 99), placeholder_xpm);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			if (!IsDrawable)
				return false;

			int width, height;
			GdkWindow.GetSize (out width, out height);

			Gdk.GC light, dark;
			light = Style.LightGC (StateType.Normal);
			dark = Style.DarkGC (StateType.Normal);

			// Looks like GdkWindow.SetBackPixmap doesn't work very well,
			// so draw the pixmap manually.
			light.Fill = Gdk.Fill.Tiled;
			light.Tile = pixmap;
			GdkWindow.DrawRectangle (light, true, 0, 0, width, height);
			light.Fill = Gdk.Fill.Solid;

			GdkWindow.DrawLine (light, 0, 0, width - 1, 0);
			GdkWindow.DrawLine (light, 0, 0, 0, height - 1);
			GdkWindow.DrawLine (dark, 0, height - 1, width - 1, height - 1);
			GdkWindow.DrawLine (dark, width - 1, 0, width - 1, height - 1);

			return base.OnExposeEvent (evt);
		}

		bool IEditableObject.CanDelete {
			get { return true; }
		}

		bool IEditableObject.CanPaste {
			get { return true; }
		}

		bool IEditableObject.CanCut {
			get { return false; }
		}

		bool IEditableObject.CanCopy {
			get { return false; }
		}

		void IEditableObject.Delete ()
		{
			Stetic.Wrapper.Container wc = Stetic.Wrapper.Container.LookupParent (this);
			if (wc != null)
				wc.Delete (this);
		}

		void IEditableObject.Paste ()
		{
			Clipboard.Paste (this);
		}

		void IEditableObject.Cut ()
		{
		}

		void IEditableObject.Copy ()
		{
		}
	}
}
