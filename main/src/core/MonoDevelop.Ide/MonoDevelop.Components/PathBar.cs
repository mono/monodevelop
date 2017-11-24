// 
// PathBar.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Linq;

using Gtk;
using Gdk;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Text;
using MonoDevelop.Ide.Fonts;
using MonoDevelop.Core;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Components
{
	public enum EntryPosition
	{
		Left,
		Right
	}
	
	public class PathEntry : IDisposable
	{
		Xwt.Drawing.Image darkIcon;

		public Xwt.Drawing.Image Icon {
			get;
			private set;
		}
		
		public string Markup {
			get;
			private set;
		}
		
		public object Tag {
			get;
			set;
		}
		
		public bool IsPathEnd {
			get;
			set;
		}
		
		public EntryPosition Position {
			get;
			set;
		}

		AtkCocoaHelper.AccessibilityElementProxy accessible;
		internal AtkCocoaHelper.AccessibilityElementProxy Accessible {
			get {
				if (accessible == null) {
					accessible = AccessibilityElementProxy.ButtonElementProxy ();
					accessible.SetRole (AtkCocoa.Roles.AXPopUpButton);
					accessible.Identifier = "Breadcrumb";
					accessible.PerformPress += OnPerformShowMenu;

					// FIXME: Remove markup from this string?
					accessible.Label = Markup;
				}
				return accessible;
			}
		}

		public PathEntry (Xwt.Drawing.Image icon, string markup) : this (markup)
		{
			this.Icon = icon;
		}
		
		public PathEntry (string markup)
		{
			this.Markup = markup;
		}
		
		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != typeof(PathEntry))
				return false;
			MonoDevelop.Components.PathEntry other = (MonoDevelop.Components.PathEntry)obj;
			return Icon == other.Icon && Markup == other.Markup;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (Icon != null ? Icon.GetHashCode () : 0) ^ (Markup != null ? Markup.GetHashCode () : 0);
			}
		}

		internal Xwt.Drawing.Image DarkIcon {
			get {
				if (darkIcon == null && Icon != null) {
					darkIcon = Icon;
/*					if (Styles.BreadcrumbGreyscaleIcons)
						darkIcon = ImageService.MakeGrayscale (darkIcon);
					if (Styles.BreadcrumbInvertedIcons)
						darkIcon = ImageService.MakeInverted (darkIcon);*/
				}
				return darkIcon;
			}
		}

		internal event EventHandler PerformShowMenu;
		void OnPerformShowMenu (object sender, EventArgs e)
		{
			PerformShowMenu?.Invoke (this, EventArgs.Empty);
		}

		public void Dispose ()
		{
			if (accessible != null) {
				accessible.PerformPress -= OnPerformShowMenu;
				accessible = null;
			}
		}
	}
	
	class PathBar : Gtk.DrawingArea
	{
		PathEntry[] leftPath  = new PathEntry[0];
		PathEntry[] rightPath = new PathEntry[0];
		Pango.Layout layout;
		Pango.AttrList boldAtts = new Pango.AttrList ();
		
		//HACK: a surrogate widget object to pass to style calls instead of "this" when using "button" hint.
		// This avoids GTK-Criticals in themes which try to cast the widget object to a button.
		Gtk.Button styleButton = new Gtk.Button ();

		// The widths array contains the widths of the items at the left and the right
		int[] widths;

		int height;
		int textHeight;

		bool pressed, hovering, menuVisible;
		int hoverIndex = -1;
		int activeIndex = -1;

		const int leftPadding = 6;
		const int rightPadding = 6;
		const int topPadding = 2;
		const int bottomPadding = 4;
		const int iconSpacing = 4;
		const int padding = 3;
		const int buttonPadding = 2;
		const int arrowLeftPadding = 10;
		const int arrowRightPadding = 10;
		const int arrowSize = 6;
		const int spacing = arrowLeftPadding + arrowRightPadding + arrowSize;
		const int minRegionSelectorWidth = 30;
		
		Func<int, Control> createMenuForItem;
		Widget menuWidget;
		bool pressMenuWasVisible;
		int pressHoverIndex;
		int menuIndex;
		uint hideTimeout;

		public PathBar (Func<int, Control> createMenuForItem)
		{
			Accessible.Name = "PathBar";
			Accessible.SetLabel (GettextCatalog.GetString ("Breadcrumb Bar"));
			Accessible.Description = GettextCatalog.GetString ("Jump to definitions in the current file");
			Accessible.SetRole (AtkCocoa.Roles.AXList);
			Accessible.SetOrientation (Orientation.Horizontal);

			CanFocus = true;

			this.Events =  EventMask.ExposureMask | 
				           EventMask.EnterNotifyMask |
				           EventMask.LeaveNotifyMask |
				           EventMask.ButtonPressMask | 
				           EventMask.ButtonReleaseMask | 
				           EventMask.KeyPressMask | 
					       EventMask.PointerMotionMask;
			boldAtts.Insert (new Pango.AttrWeight (Pango.Weight.Bold));
			this.createMenuForItem = createMenuForItem;
			EnsureLayout ();
		}

		internal static string GetFirstLineFromMarkup (string markup)
		{
			var idx = markup.IndexOfAny (new [] { NewLine.CR, NewLine.LF }); 
			if (idx >= 0)
				return markup.Substring (0, idx);
			return markup;
		}
		
		public new PathEntry[] Path { get; private set; }
		public int ActiveIndex { get { return activeIndex; } }

		void UpdatePathAccessibility ()
		{
			var elements = new AtkCocoaHelper.AccessibilityElementProxy [leftPath.Length + rightPath.Length];
			int idx = 0;

			foreach (var entry in leftPath) {
				elements [idx] = entry.Accessible;
				entry.Accessible.GtkParent = this;
				entry.PerformShowMenu += PerformShowMenu;
				idx++;
			}
			foreach (var entry in rightPath) {
				elements [idx] = entry.Accessible;
				entry.Accessible.GtkParent = this;
				entry.PerformShowMenu += PerformShowMenu;
				idx++;
			}

			Accessible.ReplaceAccessibilityElements (elements);
		}

		void DisposeProxies()
		{
			if (Path == null)
				return;

			foreach (var entry in Path) {
				entry.PerformShowMenu -= PerformShowMenu;
			}
		}

		public void SetPath (PathEntry[] path)
		{
			if (ArrSame (this.leftPath, path))
				return;

			DisposeProxies ();
			HideMenu ();

			this.Path = path ?? new PathEntry[0];
			this.leftPath = Path.Where (p => p.Position == EntryPosition.Left).ToArray ();
			this.rightPath = Path.Where (p => p.Position == EntryPosition.Right).ToArray ();
			
			activeIndex = -1;
			widths = null;
			EnsureWidths ();
			QueueResize ();

			UpdatePathAccessibility ();
		}
		
		bool ArrSame (PathEntry[] a, PathEntry[] b)
		{
			if ((a == null || b == null) && a != b)
				return false;
			if (a.Length != b.Length)
				return false;
			for (int i = 0; i < a.Length; i++)
				if (!a[i].Equals(b[i]))
					return false;
			return true;
		}
		
		public void SetActive (int index)
		{
			if (index >= leftPath.Length)
				throw new IndexOutOfRangeException ();
			
			if (activeIndex != index) {
				activeIndex = index;
				widths = null;
				QueueResize ();
			}
		}
		
		protected override void OnSizeRequested (ref Requisition requisition)
		{
			EnsureWidths ();
			requisition.Width = Math.Max (WidthRequest, 0);
			requisition.Height = height + topPadding + bottomPadding;
		}

		int[] GetCurrentWidths (out bool widthReduced)
		{
			int totalWidth = widths.Sum ();
			totalWidth += leftPadding + (arrowSize + arrowRightPadding) * leftPath.Length - 1;
			totalWidth += rightPadding + arrowSize * rightPath.Length - 1;
			int[] currentWidths = widths;
			widthReduced = false;
			int overflow = totalWidth - Allocation.Width;
			if (overflow > 0) {
				currentWidths = ReduceWidths (overflow);
				widthReduced = true;
			}
			return currentWidths;
		}

		void SetAccessibilityFrame (PathEntry entry, int x, int width)
		{
			int y = topPadding - buttonPadding;
			int height = Allocation.Height - topPadding - bottomPadding + buttonPadding * 2;
			Gdk.Rectangle rect = new Gdk.Rectangle (x, y, width, height);

			entry.Accessible.FrameInGtkParent = rect;
			entry.Accessible.FrameInParent = rect;
		}

		protected override bool OnExposeEvent (EventExpose evnt)
		{
			Gdk.Rectangle focusRect = new Gdk.Rectangle (0, 0, 0, 0);

			using (var ctx = Gdk.CairoHelper.Create (GdkWindow)) {
				int index = 0;
				ctx.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				ctx.SetSourceColor (Styles.BreadcrumbBackgroundColor.ToCairoColor ());
				ctx.Fill ();

				if (widths == null)
					return true;

				// Calculate the total required with, and the reduction to be applied in case it doesn't fit the available space

				bool widthReduced;
				var currentWidths = GetCurrentWidths (out widthReduced);

				// Render the paths

				int textTopPadding = topPadding + (height - textHeight) / 2;
				int xpos = leftPadding, ypos = topPadding;

				for (int i = 0; i < leftPath.Length; i++, index++) {
					bool last = i == leftPath.Length - 1;

					// Reduce the item size when required
					int itemWidth = currentWidths [i];
					int x = xpos;
					xpos += itemWidth;

					SetAccessibilityFrame (leftPath [i], x, itemWidth);

					if (hoverIndex >= 0 && hoverIndex < Path.Length && leftPath [i] == Path [hoverIndex] && (menuVisible || pressed || hovering))
						DrawButtonBorder (ctx, x - padding, itemWidth + padding + padding);

					if (index == focusedPathIndex) {
						focusRect = new Gdk.Rectangle (x - padding, 0, itemWidth + (padding * 2) ,0);
					}
					int textOffset = 0;
					if (leftPath [i].DarkIcon != null) {
						int iy = (height - (int)leftPath [i].DarkIcon.Height) / 2 + topPadding;
						ctx.DrawImage (this, leftPath [i].DarkIcon, x, iy);
						textOffset += (int) leftPath [i].DarkIcon.Width + iconSpacing;
					}
					
					layout.Attributes = (i == activeIndex) ? boldAtts : null;
					layout.FontDescription = FontService.SansFont.CopyModified (Styles.FontScale11);
					layout.SetMarkup (GetFirstLineFromMarkup (leftPath [i].Markup));

					ctx.Save ();

					// If size is being reduced, ellipsize it
					bool showText = true;
					if (widthReduced) {
						int w = itemWidth - textOffset;
						if (w > 0) {
							ctx.Rectangle (x + textOffset, textTopPadding, w, height);
							ctx.Clip ();
						} else
							showText = false;
					} else
						layout.Width = -1;

					if (showText) {
						// Text
						ctx.SetSourceColor (Styles.BreadcrumbTextColor.ToCairoColor ());
						ctx.MoveTo (x + textOffset, textTopPadding);
						Pango.CairoHelper.ShowLayout (ctx, layout);
					}
					ctx.Restore ();

					if (!last) {
						xpos += arrowLeftPadding;
						if (leftPath [i].IsPathEnd) {
							Style.PaintVline (Style, GdkWindow, State, evnt.Area, this, "", ypos, ypos + height, xpos - arrowSize / 2);
						} else {
							int arrowH = Math.Min (height, arrowSize);
							int arrowY = ypos + (height - arrowH) / 2;
							DrawPathSeparator (ctx, xpos, arrowY, arrowH);
						}
						xpos += arrowSize + arrowRightPadding;
					}
				}
				
				int xposRight = Allocation.Width - rightPadding;
				for (int i = 0; i < rightPath.Length; i++, index++) {
					//				bool last = i == rightPath.Length - 1;

					// Reduce the item size when required
					int itemWidth = currentWidths [i + leftPath.Length];
					xposRight -= itemWidth;
					xposRight -= arrowSize;
						
					int x = xposRight;

					SetAccessibilityFrame (rightPath [i], x, itemWidth);

					if (hoverIndex >= 0 && hoverIndex < Path.Length && rightPath [i] == Path [hoverIndex] && (menuVisible || pressed || hovering))
						DrawButtonBorder (ctx, x - padding, itemWidth + padding + padding);

					if (index == focusedPathIndex) {
						focusRect = new Gdk.Rectangle (x - padding, 0, itemWidth + (padding * 2), 0);
					}

					int textOffset = 0;
					if (rightPath [i].DarkIcon != null) {
						ctx.DrawImage (this, rightPath [i].DarkIcon, x, ypos);
						textOffset += (int) rightPath [i].DarkIcon.Width + padding;
					}
					
					layout.Attributes = (i == activeIndex) ? boldAtts : null;
					layout.FontDescription = FontService.SansFont.CopyModified (Styles.FontScale11);
					layout.SetMarkup (GetFirstLineFromMarkup (rightPath [i].Markup));

					ctx.Save ();

					// If size is being reduced, ellipsize it
					bool showText = true;
					if (widthReduced) {
						int w = itemWidth - textOffset;
						if (w > 0) {
							ctx.Rectangle (x + textOffset, textTopPadding, w, height);
							ctx.Clip ();
						} else
							showText = false;
					} else
						layout.Width = -1;

					if (showText) {
						// Text
						ctx.SetSourceColor (Styles.BreadcrumbTextColor.ToCairoColor ());
						ctx.MoveTo (x + textOffset, textTopPadding);
						Pango.CairoHelper.ShowLayout (ctx, layout);
					}

					ctx.Restore ();
				}

				ctx.MoveTo (0, Allocation.Height - 0.5);
				ctx.RelLineTo (Allocation.Width, 0);
				ctx.SetSourceColor (Styles.BreadcrumbBottomBorderColor.ToCairoColor ());
				ctx.LineWidth = 1;
				ctx.Stroke ();

				if (HasFocus) {
					int focusY = topPadding - buttonPadding;
					int focusHeight = Allocation.Height - topPadding - bottomPadding + buttonPadding * 2;

					Gtk.Style.PaintFocus (Style, GdkWindow, State, Allocation, this, "label", focusRect.X, focusY, focusRect.Width, focusHeight);
				}
			}
			return true;
		}

		void DrawPathSeparator (Cairo.Context ctx, double x, double y, double size)
		{
			ctx.MoveTo (x, y);
			ctx.LineTo (x + arrowSize, y + size / 2);
			ctx.LineTo (x, y + size);
			ctx.ClosePath ();
			ctx.SetSourceColor (Styles.BaseIconColor.ToCairoColor());
			ctx.Fill ();
		}

		void DrawButtonBorder (Cairo.Context ctx, double x, double width)
		{
			x -= buttonPadding;
			width += buttonPadding;
			double y = topPadding - buttonPadding;
			double height = Allocation.Height - topPadding - bottomPadding + buttonPadding * 2;

			ctx.Rectangle (x, y, width, height);
			ctx.SetSourceColor (Styles.BreadcrumbButtonFillColor.ToCairoColor ());
			ctx.Fill ();
		}

		int[] ReduceWidths (int overflow)
		{
			int minItemWidth = 30;
			int[] currentWidths = new int[widths.Length];
			Array.Copy (widths, currentWidths, widths.Length);
			int itemsToShrink = widths.Count (i => i > minItemWidth);
			while (overflow > 0 && itemsToShrink > 0) {
				int itemSizeReduction = overflow / itemsToShrink;
				if (itemSizeReduction == 0)
					itemSizeReduction = 1;
				int reduced = 0;
				for (int n = 0; n < widths.Length && reduced < overflow; n++) {
					if (currentWidths [n] > minItemWidth) {
						var nw = currentWidths [n] - itemSizeReduction;
						if (nw <= minItemWidth) {
							nw = minItemWidth;
							itemsToShrink--;
						}
						reduced += currentWidths [n] - nw;
						currentWidths [n] = nw;
					}
				}
				overflow -= reduced;
			}
			return currentWidths;
		}

		protected override bool OnButtonPressEvent (EventButton evnt)
		{
			pressMenuWasVisible = menuVisible;
			pressHoverIndex = menuIndex;

			HideMenu ();
			if (hovering) {
				pressed = true;
				QueueDraw ();
			}
			return true;
		}

		void PerformShowMenu (object sender, EventArgs e)
		{
			int idx = 0;

			foreach (var entry in Path) {
				if (entry == sender) {
					hoverIndex = idx;
					pressHoverIndex = idx;
					break;
				}

				idx++;
			}

			if (idx == Path.Length) {
				return;
			}

			ShowMenu ();
		}

		protected override bool OnButtonReleaseEvent (EventButton evnt)
		{
			pressed = false;
			if (hovering) {
				QueueDraw ();
				if (pressMenuWasVisible && pressHoverIndex == hoverIndex)
					return true;
				ShowMenu ();
			}
			return true;
		}
		
		void ShowMenu ()
		{
			if (hoverIndex < 0)
				return;

			HideMenu ();
			menuIndex = hoverIndex;
			menuWidget = createMenuForItem (hoverIndex);
			if (menuWidget == null)
				return;
			menuWidget.Hidden += delegate {
				menuVisible = false;
				QueueDraw ();
				
				//FIXME: for some reason the menu's children don't get activated if we destroy 
				//directly here, so use a timeout to delay it
				hideTimeout = GLib.Timeout.Add (100, delegate {
					hideTimeout = 0;
					HideMenu ();
					return false;
				});
			};
			menuVisible = true;
			if (menuWidget is Menu) {
				((Menu)menuWidget).Popup (null, null, PositionFunc, 0, Gtk.Global.CurrentEventTime);
			} else {
				var window = menuWidget as Gtk.Window;
				PositionWidget (menuWidget);

				if (window != null) {
					window.TransientFor = this.Toplevel as Gtk.Window;
				}

				menuWidget.ShowAll ();
			}
		}

		public void HideMenu ()
		{
			if (hideTimeout != 0) {
				GLib.Source.Remove (hideTimeout); 
			}
			if (menuWidget != null) {
				menuWidget.Destroy ();
				menuWidget = null;
			}

			if (alreadyHaveFocus) {
				var window = Toplevel as Gtk.Window;
				if (window != null) {
					// Present the window because on macOS the main window remains unfocused otherwise.
					window.Present ();
				}
				GrabFocus ();
			}
		}
		
		public int GetHoverXPosition (out int w)
		{
			bool widthReduced;
			int[] currentWidths = GetCurrentWidths (out widthReduced);

			if (Path[hoverIndex].Position == EntryPosition.Left) {
				int idx = leftPath.TakeWhile (p => p != Path[hoverIndex]).Count ();
				
				if (idx >= 0) {
					w = currentWidths[idx];
					return currentWidths.Take (idx).Sum () + idx * spacing;
				}
			} else {
				int idx = rightPath.TakeWhile (p => p != Path[hoverIndex]).Count ();
				if (idx >= 0) {
					w = currentWidths[idx + leftPath.Length];
					return Allocation.Width - padding - currentWidths[idx + leftPath.Length] - spacing;
				}
			}
			w = Allocation.Width;
			return 0;
		}

		void PositionWidget (Gtk.Widget widget)
		{
			if (!(widget is Gtk.Window))
				return;
			int ox, oy;
			ParentWindow.GetOrigin (out ox, out oy);
			int w;
			int itemXPosition = GetHoverXPosition (out w);
			int dx = ox + this.Allocation.X + itemXPosition;
			int dy = oy + this.Allocation.Bottom;
			
			var req = widget.SizeRequest ();

			Xwt.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen.Number, Screen.GetMonitorAtPoint (dx, dy));
			int geomWidth = (int)geometry.Width;
			int geomLeft = (int)geometry.Left;
			int geomRight = (int)geometry.Right;
			int width = System.Math.Max (req.Width, w);
			if (width >= geomWidth - spacing * 2) {
				width = geomWidth - spacing * 2;
				dx = geomLeft + spacing;
			}
			widget.WidthRequest = width;
			if (dy + req.Height > geometry.Bottom)
				dy = oy + this.Allocation.Y - req.Height;
			if (dx + width > geomRight)
				dx = geomRight - width;
			(widget as Gtk.Window).Move (dx, dy);
			(widget as Gtk.Window).Resize (width, req.Height);
			widget.GrabFocus ();
		}
		
		
		
		void PositionFunc (Menu mn, out int x, out int y, out bool push_in)
		{
			this.GdkWindow.GetOrigin (out x, out y);
			int w;
			var rect = this.Allocation;
			y += rect.Height;
			x += GetHoverXPosition (out w);
			//if the menu would be off the bottom of the screen, "drop" it upwards
			if (y + mn.Requisition.Height > this.Screen.Height) {
				y -= mn.Requisition.Height;
				y -= rect.Height;
			}
			
			//let GTK reposition the button if it still doesn't fit on the screen
			push_in = true;
		}
		
		protected override bool OnMotionNotifyEvent (EventMotion evnt)
		{
			SetHover (GetItemAt ((int)evnt.X, (int)evnt.Y));
			return true;
		}
		
		protected override bool OnLeaveNotifyEvent (EventCrossing evnt)
		{
			pressed = false;
			SetHover (-1);
			return true;
		}
		
		protected override bool OnEnterNotifyEvent (EventCrossing evnt)
		{
			SetHover (GetItemAt ((int)evnt.X, (int)evnt.Y));
			return true;
		}
		
		void SetHover (int i)
		{
			bool oldHovering = hovering;
			hovering = i > -1;
			
			if (hoverIndex != i || oldHovering != hovering) {
				if (hovering)
					hoverIndex = i;
				QueueDraw ();
			}
		}
		
		public int IndexOf (PathEntry entry)
		{
			return Path.TakeWhile (p => p != entry).Count ();
		}

		int GetItemAt (int x, int y)
		{
			int xpos = padding, xposRight = Allocation.Width - padding;
			if (widths == null || x < xpos || x > xposRight)
				return -1;

			bool widthReduced;
			int[] currentWidths = GetCurrentWidths (out widthReduced);

			for (int i = 0; i < rightPath.Length; i++) {
				xposRight -= currentWidths[i + leftPath.Length] + spacing;
				if (x > xposRight)
					return IndexOf (rightPath[i]);
			}

			for (int i = 0; i < leftPath.Length; i++) {
				xpos += currentWidths[i] + spacing;
				if (x < xpos)
					return IndexOf (leftPath[i]);
			}
			return -1;
		}
		
		void EnsureLayout ()
		{
			if (layout != null)
				layout.Dispose ();
			layout = new Pango.Layout (PangoContext);
		}
		
		void CreateWidthArray (int[] result, int index, PathEntry[] path)
		{
			// Assume that there will be icons of at least 16 pixels. This avoids
			// annoying path bar height changes when switching between empty and full paths
			int maxIconHeight = 16;

			for (int i = 0; i < path.Length; i++) {
				layout.Attributes = (i == activeIndex)? boldAtts : null;
				layout.SetMarkup (GetFirstLineFromMarkup (path[i].Markup));
				layout.Width = -1;
				int w, h;
				layout.GetPixelSize (out w, out h);
				textHeight = Math.Max (h, textHeight);
				if (path[i].DarkIcon != null) {
					maxIconHeight = Math.Max ((int)path[i].DarkIcon.Height, maxIconHeight);
					w += (int)path[i].DarkIcon.Width + iconSpacing;
				}
				result[i + index] = w;
			}
			height = Math.Max (height, maxIconHeight);
			height = Math.Max (height, textHeight);
		}

		void EnsureWidths ()
		{
			if (widths != null) 
				return;
			
			layout.SetText ("#");
			int w;
			layout.GetPixelSize (out w, out this.height);
			textHeight = height;

			widths = new int [leftPath.Length + rightPath.Length];
			CreateWidthArray (widths, 0, leftPath);
			CreateWidthArray (widths, leftPath.Length, rightPath);
		}
		
		protected override void OnStyleSet (Style previous)
		{
			base.OnStyleSet (previous);
			KillLayout ();
			EnsureLayout ();
		}
		
		void KillLayout ()
		{
			if (layout == null)
				return;
			layout.Dispose ();
			layout = null;
			boldAtts.Dispose ();
			
			widths = null;
		}

		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			styleButton.Destroy ();
			KillLayout ();
			this.boldAtts.Dispose ();
		}

		int focusedPathIndex = -1;
		bool alreadyHaveFocus = false;
		protected override bool OnFocused (DirectionType direction)
		{
			bool ret = true;

			switch (direction) {
			case DirectionType.TabForward:
			case DirectionType.Right:
				if (!alreadyHaveFocus) {
					focusedPathIndex = 0;
				} else {
					focusedPathIndex++;

					if (focusedPathIndex >= leftPath.Length + rightPath.Length) {
						ret = false;
					}
				}
				break;

			case DirectionType.TabBackward:
			case DirectionType.Left:
				if (!alreadyHaveFocus) {
					focusedPathIndex = leftPath.Length + rightPath.Length - 1;
				} else {
					focusedPathIndex--;

					if (focusedPathIndex < 0) {
						ret = false;
					}
				}
				break;
			}

			if (ret) {
				GrabFocus ();
			}
			alreadyHaveFocus = ret;
			QueueDraw ();
			return ret;
		}

		protected override bool OnFocusOutEvent(EventFocus evnt)
		{
			alreadyHaveFocus = false;
			return base.OnFocusOutEvent(evnt);
		}

		protected override void OnActivate ()
		{
			if (focusedPathIndex < 0) {
				return;
			}

			hoverIndex = focusedPathIndex;
			pressHoverIndex = focusedPathIndex;

			ShowMenu ();
		}
	}
}
