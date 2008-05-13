
using System;
using System.Reflection;
using System.Collections;

namespace Stetic.Editor
{
	public class IconSelectorItem: Gtk.EventBox
	{
		ArrayList icons = new ArrayList ();
		ArrayList labels = new ArrayList ();
		ArrayList names = new ArrayList ();
		int columns = 12;
		int iconSize = 16;
		int spacing = 3;
		int selIndex = -1;
		int sectionGap = 10;
		int lastSel = -1;
		int xmax;
		int ymax;
		string title;
		Gtk.Window tipWindow;
		bool inited;
		
		public IconSelectorItem (IntPtr ptr): base (ptr)
		{
		}
		
		public IconSelectorItem (string title)
		{
			this.title = title;
			
			int w, h;
			Gtk.Icon.SizeLookup (Gtk.IconSize.Menu, out w, out h);
			iconSize = w;
			
			this.Events |= Gdk.EventMask.PointerMotionMask;
		}
		
		protected override void OnSizeRequested (ref Gtk.Requisition req)
		{
			if (!inited) {
				CreateIcons ();
				inited = true;
			}
			
			base.OnSizeRequested (ref req);
			CalcSize ();
			
			Gtk.Requisition nr = new Gtk.Requisition ();
			nr.Width = xmax;
			nr.Height = ymax;
			req = nr;
		}
		
		protected virtual void CreateIcons ()
		{
		}
		
		public int SelectedIndex {
			get { return selIndex; }
		}
		
		public string SelectedIcon {
			get {
				if (selIndex != -1)
					return (string) names [selIndex];
				else
					return null;
			}
		}
		
		protected void AddIcon (string name, Gdk.Pixbuf pix, string label)
		{
			icons.Add (pix);
			labels.Add (label);
			names.Add (name);
		}
		
		protected void AddSeparator (string separator)
		{
			icons.Add (null);
			labels.Add (null);
			names.Add (separator);
		}
		
		protected override bool OnMotionNotifyEvent (Gdk.EventMotion ev)
		{
			ProcessMotionEvent ((int) ev.X, (int) ev.Y);
			return true;
		}
		
		internal void ProcessMotionEvent (int x, int y)
		{
			int ix, iy;
			GetIconIndex (x, y, out selIndex, out ix, out iy);
			if (selIndex != -1) {
				string name = labels [selIndex] as string;
				if (name == null || name.Length == 0)
					name = names [selIndex] as string;
				if (selIndex != lastSel) {
					HideTip ();
					ShowTip (ix, iy + iconSize + spacing*2, name);
				}
			} else
				HideTip ();
				
			lastSel = selIndex;
			
			QueueDraw ();
		}
		
		void ShowTip (int x, int y, string text)
		{
			if (GdkWindow == null)
				return;
			if (tipWindow == null) {
				tipWindow = new TipWindow ();
				Gtk.Label lab = new Gtk.Label (text);
				lab.Xalign = 0;
				lab.Xpad = 3;
				lab.Ypad = 3;
				tipWindow.Add (lab);
			}
			((Gtk.Label)tipWindow.Child).Text = text;
			int w = tipWindow.Child.SizeRequest().Width;
			int ox, oy;
			GdkWindow.GetOrigin (out ox, out oy);
			tipWindow.Move (ox + x - (w/2) + (iconSize/2), oy + y);
			tipWindow.ShowAll ();
		}
		
		void HideTip ()
		{
			if (tipWindow != null) {
				tipWindow.Destroy ();
				tipWindow = null;
			}
		}
		
		public override void Dispose ()
		{
			HideTip ();
			base.Dispose ();
		}
		
		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
			HideTip ();
			return base.OnLeaveNotifyEvent (ev);
		}
		
		internal void ProcessLeaveNotifyEvent (Gdk.EventCrossing ev)
		{
			HideTip ();
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			Draw ();
			return true;
		}
		
		void Draw ()
		{
			int a,b;
			Expose  (true, -1, -1, out a, out b);
		}
		
		void CalcSize ()
		{
			int a,b;
			Expose (false, -1, -1, out a, out b);
		}
		
		void GetIconIndex (int x, int y, out int index, out int ix, out int iy)
		{
			index = Expose (false, x, y, out ix, out iy);
		}
		
		int Expose (bool draw, int testx, int testy, out int ix, out int iy)
		{
			int x = spacing;
			int y = spacing;
			int sx = spacing;
			int maxx = columns * (iconSize + spacing) + spacing;
			bool calcSize = (testx == -1);
			
			Pango.Layout layout = new Pango.Layout (this.PangoContext);
			Pango.FontDescription des = this.Style.FontDescription.Copy();
			des.Size = 10 * (int) Pango.Scale.PangoScale;
			layout.FontDescription = des;
			layout.SetMarkup (title);
			layout.Width = -1;
			int w, h;
			int tborder = 1;
			layout.GetPixelSize (out w, out h);
			if (draw) {
				GdkWindow.DrawRectangle (this.Style.DarkGC (Gtk.StateType.Normal), true, x, y, Allocation.Width + tborder*2, h + tborder*2);
				GdkWindow.DrawLayout (this.Style.ForegroundGC (Gtk.StateType.Normal), x + tborder + 2, y + tborder, layout);
			}
			
			if (calcSize)
				xmax = 0;

			y += h + spacing*2 + tborder*2;
			
			for (int n=0; n<icons.Count; n++) {
				string cmd = names [n] as string;
				Gdk.Pixbuf pix = icons [n] as Gdk.Pixbuf;
				
				if (cmd == "-") {
					if (x > sx) {
						y += iconSize + spacing;
					}
					x = sx;
					y -= spacing;
					if (draw) {
						Gdk.Rectangle rect = new Gdk.Rectangle (0, y+(sectionGap/2), Allocation.Width - x, 1);
						Gtk.Style.PaintHline (this.Style, this.GdkWindow, Gtk.StateType.Normal, rect, this, "", rect.X, rect.Right, rect.Y);
					}
					y += sectionGap;
					continue;
				}
				
				if (cmd == "|") {
					if (x == sx)
						continue;
					x += spacing;
					if (draw) {
						Gdk.Rectangle rect = new Gdk.Rectangle (x, y, 1, iconSize);
						Gtk.Style.PaintVline (this.Style, this.GdkWindow, Gtk.StateType.Normal, rect, this, "", rect.Y, rect.Bottom, rect.X);
					}
					x += spacing*2;
					continue;
				}
				
				if (testx != -1 && testx >= (x - spacing/2) && testx < (x + iconSize + spacing) && testy >= (y - spacing/2) && testy < (y + iconSize + spacing)) {
					ix = x;
					iy = y;
					return n;
				}
					
				if (draw) {
					Gtk.StateType state = (n == selIndex) ? Gtk.StateType.Selected : Gtk.StateType.Normal;
					if (n == selIndex)
						GdkWindow.DrawRectangle (this.Style.BackgroundGC (state), true, x-spacing, y-spacing, iconSize + spacing*2, iconSize + spacing*2);
					GdkWindow.DrawPixbuf (this.Style.ForegroundGC (state), pix, 0, 0, x, y, pix.Width, pix.Height, Gdk.RgbDither.None, 0, 0);
				}
				
				x += (iconSize + spacing);
				if (calcSize && x > xmax)
					xmax = x;
					
				if (x >= maxx) {
					x = sx;
					y += iconSize + spacing;
				}
			}
			if (calcSize) {
				if (x > sx)
					y += iconSize + spacing;
				ymax = y;
			}
			
			ix = iy = 0;
			return -1;
		}
	}
	
	class TipWindow: Gtk.Window
	{
		public TipWindow (): base (Gtk.WindowType.Popup)
		{
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose ev)
		{
			base.OnExposeEvent (ev);
			Gtk.Requisition req = SizeRequest ();
			Gtk.Style.PaintFlatBox (this.Style, this.GdkWindow, Gtk.StateType.Normal, Gtk.ShadowType.Out, Gdk.Rectangle.Zero, this, "tooltip", 0, 0, req.Width, req.Height);
			return true;
		}
	}
}
