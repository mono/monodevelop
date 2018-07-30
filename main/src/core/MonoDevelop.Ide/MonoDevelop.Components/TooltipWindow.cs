// 
// TooltipWindow.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using MonoDevelop.Ide;
using Gtk;
using Gdk;

namespace MonoDevelop.Components
{
	public abstract class TooltipWindow : Gtk.Window
	{
		WindowTransparencyDecorator decorator;
		
		public string LinkColor {
			get {
				var color = HslColor.GenerateHighlightColors (Style.Background (State), Style.Text (State), 3)[2];
				return color.ToPangoString ();
			}
		}
		
		protected TooltipWindow () : base (Gtk.WindowType.Popup)
		{
			this.SkipPagerHint = true;
			this.SkipTaskbarHint = true;
			this.Decorated = false;
			this.BorderWidth = 2;
			this.TypeHint = WindowTypeHint.Tooltip;
			this.Resizable = true;
			this.Title = "tooltip"; // fixes the annoying '** Message: ATK_ROLE_TOOLTIP object found, but doesn't look like a tooltip.** Message: ATK_ROLE_TOOLTIP object found, but doesn't look like a tooltip.'
			
			//fake widget name for stupid theme engines
			this.Name = "gtk-tooltip";
		}
		
		public bool NudgeVertical {
			get; set;
		}
		
		public bool NudgeHorizontal {
			get; set;
		}
		
		public bool EnableTransparencyControl {
			get { return decorator != null; }
			set {
				if (value && decorator == null)
					decorator = WindowTransparencyDecorator.Attach (this);
				else if (!value && decorator != null)
					decorator.Detach ();
			}
		}
		
//		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
//		{
//			int winWidth, winHeight;
//			this.GetSize (out winWidth, out winHeight);
//			Gtk.Style.PaintFlatBox (Style, this.GdkWindow, StateType.Normal, ShadowType.Out, evnt.Area, this, "tooltip", 0, 0, winWidth, winHeight);
//			foreach (var child in this.Children)
//				this.PropagateExpose (child, evnt);
//			return false;
//		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (decorator != null) {
				decorator.Detach ();
				decorator = null;
			}
		}
		
		
		protected override void OnSizeAllocated (Gdk.Rectangle allocation)
		{
			if (NudgeHorizontal || NudgeVertical) {
				int x, y;
				this.GetPosition (out x, out y);
				int oldY = y, oldX = x;
				const int edgeGap = 2;
				
//				int w = allocation.Width;
//				
//				if (fitWidthToScreen && (x + w >= screenW - edgeGap)) {
//					int fittedWidth = screenW - x - edgeGap;
//					if (fittedWidth < minFittedWidth) {
//						x -= (minFittedWidth - fittedWidth);
//						fittedWidth = minFittedWidth;
//					}
//					LimitWidth (fittedWidth);
//				}
				
				Xwt.Rectangle geometry = DesktopService.GetUsableMonitorGeometry (Screen.Number, Screen.GetMonitorAtPoint (x, y));
				int left = (int)geometry.Left;
				int top = (int)geometry.Top;
				int width = (int)geometry.Width;
				int height = (int)geometry.Height;
				if (NudgeHorizontal) {
					if (allocation.Width <= geometry.Width && x + allocation.Width >= geometry.Left + geometry.Width - edgeGap)
						x = left + (width - allocation.Width - edgeGap);
					if (x <= left + edgeGap)
						x = left + edgeGap;
				}
				
				if (NudgeVertical) {
					if (allocation.Height <= geometry.Height && y + allocation.Height >= geometry.Top + geometry.Height - edgeGap)
						y = top + (height - allocation.Height - edgeGap);
					if (y <= top + edgeGap)
						y = top + edgeGap;
				}
				
				if (y != oldY || x != oldX)
					Move (x, y);
			}
			
			base.OnSizeAllocated (allocation);
		}
		
//		void LimitWidth (int width)
//		{
//			if (Child is MonoDevelop.Components.FixedWidthWrapLabel) 
//				((MonoDevelop.Components.FixedWidthWrapLabel)Child).MaxWidth = width - 2 * (int)this.BorderWidth;
//			
//			int childWidth = Child.SizeRequest ().Width;
//			if (childWidth < width)
//				WidthRequest = childWidth;
//			else
//				WidthRequest = width;
//		}
	}
}
