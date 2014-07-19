using System;

namespace GitHub.Issues
{
	/// <summary>
	/// Layout utilities which aid in common layout tasks when creating the UI
	/// </summary>
	public static class LayoutUtilities
	{
		#region Alignments

		/// <summary>
		/// Lefts align the widget
		/// </summary>
		/// <returns>The aligned widget</returns>
		/// <param name="widget">Widget.</param>
		public static Gtk.Alignment LeftAlign(Gtk.Widget widget)
		{
			Gtk.Alignment alignment = new Gtk.Alignment (0, 0, 0, 0);
			alignment.Add (widget);

			return alignment;
		}

		/// <summary>
		/// Right align the widget
		/// </summary>
		/// <returns>The aligned widget</returns>
		/// <param name="widget">Widget.</param>
		public static Gtk.Alignment RightAlign(Gtk.Widget widget)
		{
			Gtk.Alignment alignment = new Gtk.Alignment (1, 0, 0, 0);
			alignment.Add (widget);

			return alignment;
		}

		/// <summary>
		/// Tops the align.
		/// </summary>
		/// <returns>The align.</returns>
		/// <param name="alignment">Alignment.</param>
		public static Gtk.Alignment TopAlign(Gtk.Alignment alignment)
		{
			alignment.Yalign = 0;
			alignment.Yscale = 1;

			return alignment;
		}

		/// <summary>
		/// Centers the horizontal align.
		/// </summary>
		/// <returns>The horizontal align.</returns>
		/// <param name="widget">Widget.</param>
		public static Gtk.Alignment CenterHorizontalAlign(Gtk.Widget widget)
		{
			Gtk.Alignment alignment = new Gtk.Alignment (0.5f, 0, 1, 0);
			alignment.Add (widget);

			return alignment;
		}

		/// <summary>
		/// Sets uniform padding for a given control
		/// </summary>
		/// <returns>The padding.</returns>
		/// <param name="alignment">Aligned object</param>
		/// <param name="padding">Padding amount.</param>
		public static Gtk.Alignment SetPadding(Gtk.Alignment alignment, uint padding)
		{
			alignment.SetPadding (padding, padding, padding, padding);

			return alignment;
		}

		/// <summary>
		/// Sets the padding.
		/// </summary>
		/// <returns>The padding.</returns>
		/// <param name="widget">Widget.</param>
		/// <param name="top">Top.</param>
		/// <param name="bottom">Bottom.</param>
		/// <param name="left">Left.</param>
		/// <param name="right">Right.</param>
		public static Gtk.Alignment SetPadding(Gtk.Widget widget, uint top, uint bottom, uint left, uint right)
		{
			Gtk.Alignment alignment = new Gtk.Alignment (0, 0, 0, 0);
			alignment.Add (widget);

			alignment.SetPadding (top, bottom, left, right);

			return alignment;
		}

		/// <summary>
		/// Sets the padding.
		/// </summary>
		/// <returns>The padding.</returns>
		/// <param name="alignment">Alignment.</param>
		/// <param name="top">Top.</param>
		/// <param name="bottom">Bottom.</param>
		/// <param name="left">Left.</param>
		/// <param name="right">Right.</param>
		public static Gtk.Alignment SetPadding(Gtk.Alignment alignment, uint top, uint bottom, uint left, uint right)
		{
			alignment.SetPadding (top, bottom, left, right);

			return alignment;
		}

		/// <summary>
		/// Stretches the widget horizontally.
		/// </summary>
		/// <returns>Modified alignment</returns>
		/// <param name="alignment">Alignment to modify.</param>
		public static Gtk.Alignment StretchHorizontally(Gtk.Alignment alignment)
		{
			alignment.Xscale = 1;

			return alignment;
		}

		#endregion

		#region Binding

		/// <summary>
		/// Sets up width binding.
		/// </summary>
		/// <param name="parent">Parent.</param>
		/// <param name="child">Child.</param>
		/// <param name="ratio">Ratio between 0 and 1.</param>
		public static void SetUpWidthBinding(Gtk.Widget parent, Gtk.Widget child, double ratio)
		{
			parent.SizeAllocated += (object o, Gtk.SizeAllocatedArgs args) => 
			{
				child.WidthRequest = Convert.ToInt32(args.Allocation.Width * ratio);
			};
		}

		/// <summary>
		/// Sets up width binding.
		/// </summary>
		/// <param name="parent">Parent.</param>
		/// <param name="child">Child.</param>
		/// <param name="offset">Offset.</param>
		public static void SetUpWidthBinding(Gtk.Widget parent, Gtk.Widget child, int offset)
		{
			parent.SizeAllocated += (object o, Gtk.SizeAllocatedArgs args) => 
			{
				child.WidthRequest = Convert.ToInt32(args.Allocation.Width + offset);
			};
		}

		/// <summary>
		/// Sets up height binding.
		/// </summary>
		/// <param name="parent">Parent.</param>
		/// <param name="child">Child.</param>
		/// <param name="ratio">Ratio between 0 and 1.</param>
		public static void SetUpHeighBinding(Gtk.Widget parent, Gtk.Widget child, double ratio)
		{
			parent.SizeAllocated += (object o, Gtk.SizeAllocatedArgs args) => 
			{
				child.HeightRequest = Convert.ToInt32(args.Allocation.Height * ratio);
			};
		}

		#endregion
	}
}

