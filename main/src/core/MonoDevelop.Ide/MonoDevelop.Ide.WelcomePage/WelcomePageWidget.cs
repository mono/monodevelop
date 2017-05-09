// 
// WelcomePageFallbackWidget.cs
// 
// Author:
//   Scott Ellington
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
// Copyright (c) 2005 Scott Ellington
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

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using System.Linq;
using Gdk;
using Gtk;
using Mono.Addins;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Desktop;
using System.Reflection;
using System.Xml.Linq;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageWidget : Gtk.EventBox
	{
		public Xwt.Drawing.Image LogoImage { get; set; }
		public int LogoHeight { get; set; }
		public Xwt.Drawing.Image TopBorderImage { get; set; }
		public Xwt.Drawing.Image BackgroundImage { get; set; }
		public string BackgroundColor { get; set; }

		protected double OverdrawOpacity {
			get { return Background.OverdrawOpacity; }
			set { Background.OverdrawOpacity = value; }
		}

		protected int OverdrawOffset {
			get { return Background.OverdrawOffset; }
			set { Background.OverdrawOffset = value; }
		}

		WelcomePageWidgetBackground Background { get; set; }

		public bool ShowScrollbars { get; set; }

		public WelcomePageWidget ()
		{
			ShowScrollbars = true;
			VisibleWindow = false;

			UpdateTeme (null, null);
			LogoHeight = 90;

			var background = new WelcomePageWidgetBackground ();
			background.Accessible.SetShouldIgnore (true);
			Background = background;
			background.Owner = this;
			var mainAlignment = new Gtk.Alignment (0f, 0f, 1f, 1f);
			mainAlignment.Accessible.SetShouldIgnore (true);
			background.Add (mainAlignment);

			BuildContent (mainAlignment);

			if (ShowScrollbars) {
				var scroller = new ScrolledWindow ();
				scroller.Accessible.SetShouldIgnore (true);
				scroller.AddWithViewport (background);
				((Gtk.Viewport)scroller.Child).ShadowType = ShadowType.None;
				scroller.Child.Accessible.SetShouldIgnore (true);

				scroller.ShadowType = ShadowType.None;
				scroller.FocusChain = new Widget[] { background };
				scroller.Show ();
				Add (scroller);
			} else
				this.Add (background);

			if (LogoImage != null) {
				var logoHeight = LogoHeight;
				mainAlignment.SetPadding ((uint)(logoHeight + Styles.WelcomeScreen.Spacing), 0, (uint)Styles.WelcomeScreen.Spacing, 0);
			}

			ShowAll ();

			IdeApp.Workbench.GuiLocked += OnLock;
			IdeApp.Workbench.GuiUnlocked += OnUnlock;
			MonoDevelop.Ide.Gui.Styles.Changed += UpdateTeme;
		}

		void UpdateTeme (object sender, EventArgs e)
		{
			BackgroundColor = Styles.WelcomeScreen.BackgroundColor;
		}

		void OnLock (object s, EventArgs a)
		{
			Sensitive = false;
		}
		
		void OnUnlock (object s, EventArgs a)
		{
			Sensitive = true;
		}

		protected virtual void BuildContent (Container parent)
		{
		}

		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			IdeApp.Workbench.GuiLocked -= OnLock;
			IdeApp.Workbench.GuiUnlocked -= OnUnlock;
			MonoDevelop.Ide.Gui.Styles.Changed -= UpdateTeme;
		}

		public class WelcomePageWidgetBackground : Gtk.EventBox
		{
			public WelcomePageWidget Owner { get; set; }

			public double OverdrawOpacity { get; set; }
			public int OverdrawOffset { get; set; }

			Gdk.Color backgroundColor = Gdk.Color.Zero;

			public WelcomePageWidgetBackground ()
			{
				MonoDevelop.Ide.Gui.Styles.Changed += UpdateTeme;
			}

			void UpdateTeme (object sender, EventArgs e)
			{
				if (!Gdk.Color.Parse (Owner.BackgroundColor, ref backgroundColor) || !Gdk.Color.Parse (Styles.WelcomeScreen.BackgroundColor, ref backgroundColor))
					backgroundColor = Style.White;
				ModifyBg (StateType.Normal, backgroundColor);
			}

			protected override void OnRealized ()
			{
				UpdateTeme (null, null);
				base.OnRealized ();
			}

			void DrawOverdraw (Cairo.Context context, double opacity)
			{
				if (Owner.BackgroundImage == null)
					return;

				context.RenderTiled (this, Owner.BackgroundImage, Allocation, new Gdk.Rectangle (Allocation.X, Allocation.Y + OverdrawOffset, Allocation.Width, Allocation.Height - OverdrawOffset), opacity);
			}

			void DrawBackground (Cairo.Context context, Gdk.Rectangle area)
			{
				if (Owner.BackgroundImage == null)
					return;

				context.RenderTiled (this, Owner.BackgroundImage, Allocation, Allocation, 1);
			}

			protected override bool OnExposeEvent (EventExpose evnt)
			{
				using (var context = CairoHelper.Create (evnt.Window)) {
					context.SetSourceRGB (backgroundColor.Red, backgroundColor.Green, backgroundColor.Blue);
					context.Operator = Cairo.Operator.Source;
					context.Paint ();
					context.Operator = Cairo.Operator.Over;
					DrawBackground (context, evnt.Area);

					if (Owner.LogoImage != null) {
						var lRect = new Rectangle (Allocation.X, Allocation.Y, (int)Owner.LogoImage.Width, (int)Owner.LogoImage.Height);
						if (evnt.Region.RectIn (lRect) != OverlapType.Out)
							context.DrawImage (this, Owner.LogoImage, Allocation.X, Allocation.Y);
					
						var bgRect = new Rectangle (Allocation.X + (int)Owner.LogoImage.Width, Allocation.Y, Allocation.Width - (int)Owner.LogoImage.Width, (int)Owner.TopBorderImage.Height);
						if (evnt.Region.RectIn (bgRect) != OverlapType.Out)
							for (int x = bgRect.X; x < bgRect.Right; x += (int)Owner.TopBorderImage.Width)
								context.DrawImage (this, Owner.TopBorderImage.WithSize (Owner.TopBorderImage.Width, bgRect.Height), x, Allocation.Y);
					}
				}
				
				foreach (Widget widget in Children)
					PropagateExpose (widget, evnt);

				if (OverdrawOpacity > 0) {
					using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
						DrawOverdraw (context, OverdrawOpacity);
					}
				}
				
				return true;
			}

			protected override void OnDestroyed ()
			{
				MonoDevelop.Ide.Gui.Styles.Changed -= UpdateTeme;
				base.OnDestroyed ();
			}
		}
	}
}
