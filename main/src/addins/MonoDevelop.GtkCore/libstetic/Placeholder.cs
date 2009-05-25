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
			WidgetFlags |= WidgetFlags.AppPaintable;
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

		protected override void OnRealized ()
		{
			base.OnRealized ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			if (!IsDrawable)
				return false;

			int width, height;
			GdkWindow.GetSize (out width, out height);
			
			Gdk.Rectangle a = new Gdk.Rectangle (0,0,width,height);
			
			byte b1 = 210;
			byte b2 = 210;
			
			int ssLT = 12;
			int ssRB = 7;
			double grey = 0.6;
			double greyb1 = 0.9;
			double greyb2 = 0.6;
			
			Gdk.Rectangle rect = a;
			Cairo.Color back1 = new Cairo.Color (greyb1, greyb1, greyb1);
			Cairo.Color back2 = new Cairo.Color (greyb2, greyb2, greyb2);
			Cairo.Color cdark = new Cairo.Color (grey, grey, grey, 1);
			Cairo.Color clight = new Cairo.Color (grey, grey, grey, 0);
			using (Cairo.Context cr = Gdk.CairoHelper.Create (evt.Window)) {
			
				DrawGradient (cr, rect, 0, 0, 1, 1, back1, back2);
				
				rect.X = a.X;
				rect.Y = a.Y;
				rect.Height = ssLT;
				rect.Width = a.Width;
				DrawGradient (cr, rect, 0, 0, 0, 1, cdark, clight);

				rect.Y = a.Bottom - ssRB;
				rect.Height = ssRB;
				DrawGradient (cr, rect, 0, 0, 0, 1, clight, cdark);

				rect.X = a.X;
				rect.Y = a.Y;
				rect.Width = ssLT;
				rect.Height = a.Height;
				DrawGradient (cr, rect, 0, 0, 1, 0, cdark, clight);

				rect.X = a.Right - ssRB;
				rect.Width = ssRB;
				DrawGradient (cr, rect, 0, 0, 1, 0, clight, cdark);
				
				Gdk.GC gc = new Gdk.GC (GdkWindow);
				gc.RgbBgColor = new Gdk.Color (b2,b2,b2);
				gc.RgbFgColor = new Gdk.Color (b2,b2,b2);
				GdkWindow.DrawRectangle (gc, false, a.X, a.Y, a.Width, a.Height);
				gc.Dispose ();
			}

			return base.OnExposeEvent (evt);
		}
		
		void DrawGradient (Cairo.Context cr, Gdk.Rectangle rect, int fx, int fy, int fw, int fh, Cairo.Color c1, Cairo.Color c2)
		{
			cr.NewPath ();
			cr.MoveTo (rect.X, rect.Y);
			cr.RelLineTo (rect.Width, 0);
			cr.RelLineTo (0, rect.Height);
			cr.RelLineTo (-rect.Width, 0);
			cr.RelLineTo (0, -rect.Height);
			cr.ClosePath ();
			Cairo.LinearGradient pat = new Cairo.LinearGradient (rect.X + rect.Width*fx, rect.Y + rect.Height*fy, rect.X + rect.Width*fw, rect.Y + rect.Height*fh);
			pat.AddColorStop (0, c1);
			pat.AddColorStop (1, c2);
			cr.Pattern = pat;
			cr.FillPreserve ();
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
