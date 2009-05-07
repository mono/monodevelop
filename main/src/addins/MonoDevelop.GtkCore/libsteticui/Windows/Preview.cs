using System;
using System.Collections.Generic;
using System.Text;
using Gtk;
using Gdk;

namespace Stetic.Windows
{
	class Preview: Bin
	{
		Gtk.Widget child;
		WindowsTheme wtheme;
		string caption;

		public Preview ( )
		{
			DoubleBuffered = false;
			AppPaintable = true;
		}

		public static Preview Create (TopLevelWindow window)
		{
			try {
				Preview p = new Preview ();
				p.Add (window);
				return p;
			}
			catch {
				return null;
			}
		}

		public string Title {
			get { return caption; }
			set {
				caption = value;
				QueueDraw ();
			}
		}

		protected override void OnDestroyed ( )
		{
			if (wtheme != null)
				wtheme.Dipose ();
			base.OnDestroyed ();
		}

		protected override void OnAdded (Widget widget)
		{
			base.OnAdded (widget);
			child = widget;
			if (child is TopLevelWindow) {
				((TopLevelWindow) child).PropertyChanged += Preview_TitleChanged;
				Title = ((TopLevelWindow) child).Title;
			}
		}

		void Preview_TitleChanged (object sender, EventArgs e)
		{
			Title = ((TopLevelWindow) child).Title;
		}

		protected override void OnRemoved (Widget widget)
		{
			base.OnRemoved (widget);
			if (widget == child) {
				if (child is TopLevelWindow)
					((TopLevelWindow) child).PropertyChanged -= Preview_TitleChanged;
				child = null;
			}
		}

		protected override void OnSizeRequested (ref Requisition requisition)
		{
			if (child != null)
				requisition = child.SizeRequest ();
		}

		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			base.OnSizeAllocated (allocation);
			if (child != null) {
				if (wtheme != null)
					child.Allocation = wtheme.GetWindowClientArea (allocation);
				else
					child.Allocation = allocation;
			}
		}

		protected override void OnRealized ( )
		{
			base.OnRealized ();
			wtheme = new WindowsTheme (GdkWindow);
			if (child != null)
				child.Allocation = wtheme.GetWindowClientArea (Allocation);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			Gdk.Rectangle rect = new Gdk.Rectangle (Allocation.X+50, Allocation.Y+50, Allocation.Width - 1, Allocation.Height - 1);
			wtheme.DrawWindowFrame (this, caption, Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);
			foreach (Widget child in Children)
				PropagateExpose (child, evnt);
			return false;
		}
	}
}
