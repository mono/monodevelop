//
// WelcomePageFirstRun.cs
//
// Author:
//       Jason Smith <jason.smith@xamarin.com>
//
// Copyright (c) 2012 Xamarin Inc.
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
using Gtk;
using Cairo;
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using System.Collections.Generic;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageFirstRun : EventBox, Animatable
	{
		static readonly int Padding = 35;
		static readonly Gdk.Size WidgetSize = new Gdk.Size (880 + 2 * Padding, 460 + 2 * Padding);
		static readonly Gdk.Point TitlePosition = new Gdk.Point (Padding + 52, Padding + 60);
		static readonly Gdk.Point TextPosition = new Gdk.Point (Padding + 52, Padding + 120);
		static readonly Gdk.Size ButtonSize = new Gdk.Size (200, 30);
		static readonly Gdk.Point ButtonPosistion = new Gdk.Point ((Padding + 250) - ButtonSize.Width / 2, (WidgetSize.Height - 60 - Padding) - ButtonSize.Height / 2);
		static readonly Gdk.Point IconPosition = new Gdk.Point (WidgetSize.Width - 220 - Padding, WidgetSize.Height / 2);
		static readonly double PreviewSize = 350;

		Gdk.Pixbuf starburst;
		Gdk.Pixbuf brandedIcon;

		MouseTracker tracker;

		bool buttonHovered;
		bool ButtonHovered {
			get {
				return buttonHovered;
			}
			set {
				if (buttonHovered == value)
					return;
				buttonHovered = value;
				QueueDraw ();
			}
		}

		double TitleOpacity { get; set; }
		double TextOpacity { get; set; }
		double IconOpacity { get; set; }
		double ButtonOpacity { get; set; }
		double BackgroundOpacity { get; set; }
		double IconScale { get; set; }

		Gdk.Point TitleOffset;
		Gdk.Point TextOffset;
		Gdk.Point IconOffset;

		public event EventHandler Dismissed;

		public WelcomePageFirstRun ()
		{
			VisibleWindow = false;
			SetSizeRequest (WidgetSize.Width, WidgetSize.Height);
			starburst = Gdk.Pixbuf.LoadFromResource ("starburst.png");

			string iconFile = BrandingService.GetString ("ApplicationIcon");
			if (iconFile != null) {
				iconFile = BrandingService.GetFile (iconFile);
				brandedIcon = new Gdk.Pixbuf (iconFile);
			}

			TitleOffset = TextOffset = IconOffset = new Gdk.Point ();

			tracker = new MouseTracker (this);
			tracker.MouseMoved += (sender, e) => {
				ButtonHovered = new Gdk.Rectangle (ButtonPosistion, ButtonSize).Contains (tracker.MousePosition);
			};

			tracker.HoveredChanged += (sender, e) => {
				if (!tracker.Hovered) 
					ButtonHovered = false;
			};
		}

		protected override void OnMapped ()
		{
			base.OnMapped ();

			TitleOffset = TextOffset = new Gdk.Point (0, 40);
			ButtonOpacity = TextOpacity = TitleOpacity = IconOpacity = 0;
			BackgroundOpacity = 0;
			IconScale = 0.5;

			GLib.Timeout.Add (750, () => {
				new Animation ()
					.Insert (0, 0.5f,    new Animation ((f) => TitleOffset.Y = (int) (40 * f), start: 1, end: 0, easing: Easing.CubicInOut))
					.Insert (0.1f, 0.6f, new Animation ((f) => TextOffset.Y  = (int) (40 * f), start: 1, end: 0, easing: Easing.CubicInOut))
					.Insert (0, 0.5f,    new Animation ((f) => TitleOpacity = f,      easing: Easing.CubicInOut))
					.Insert (0.1f, 0.6f, new Animation ((f) => TextOpacity = f,       easing: Easing.CubicInOut))
					.Insert (0.3f, 0.9f, new Animation ((f) => ButtonOpacity = f,     easing: Easing.CubicInOut))
					.Insert (0, 0.2f,    new Animation ((f) => BackgroundOpacity = f, easing: Easing.CubicInOut))
					.Insert (0.2f, 0.7f, new Animation ((f) => IconOpacity = f,       easing: Easing.CubicInOut))
					.Insert (0.2f, 0.7f, new Animation ((f) => IconScale = f, start: 0.5f, end: 1, easing: Easing.SpringOut))
					.Commit (this, "Intro", length: 1200);
				return false;
			});
		}

		void RenderBackground (Cairo.Context context, Gdk.Rectangle region)
		{
			region.Inflate (-Padding, -Padding);
			context.RenderOuterShadow (new Gdk.Rectangle (region.X + 10, region.Y + 15, region.Width - 20, region.Height - 15), Padding, 3, .25);

			context.RoundedRectangle (region.X + 0.5, region.Y + 0.5, region.Width - 1, region.Height - 1, 5);
			using (var lg = new LinearGradient (0, region.Y, 0, region.Bottom)) {
				lg.AddColorStop (0, new Cairo.Color (.36, .53, .73));
				lg.AddColorStop (1, new Cairo.Color (.21, .37, .54));

				context.Pattern = lg;
				context.FillPreserve ();
			}

			context.Save ();
			context.Translate (IconPosition.X, IconPosition.Y);
			context.Scale (0.75, 0.75);
			Gdk.CairoHelper.SetSourcePixbuf (context, starburst, -starburst.Width / 2, -starburst.Height / 2);
			context.FillPreserve ();
			context.Restore ();

			context.LineWidth = 1;
			context.Color = new Cairo.Color (.29, .47, .67);
			context.Stroke ();

		}

		void RenderPreview (Cairo.Context context, Gdk.Point position, double opacity)
		{
			if (brandedIcon != null) {
				if (previewSurface == null) {
					previewSurface = new SurfaceWrapper (context, brandedIcon);
				}
				double scale = PreviewSize / previewSurface.Width;

				context.Save ();
				context.Translate (position.X, position.Y);
				context.Scale (scale * IconScale, scale * IconScale);
				context.SetSourceSurface (previewSurface.Surface, -previewSurface.Width / 2, -previewSurface.Height / 2);
				context.PaintWithAlpha (opacity);
				context.Restore ();
			}
		}

		void RenderShadowedText (Cairo.Context context, Gdk.Point position, double opacity, Pango.Layout layout)
		{
			context.MoveTo (position.X, position.Y + 2);
			context.Color = new Cairo.Color (0, 0, 0, 0.3 * opacity);
			Pango.CairoHelper.ShowLayout (context, layout);
			
			context.MoveTo (position.X, position.Y);
			context.Color = new Cairo.Color (1, 1, 1, opacity);
			Pango.CairoHelper.ShowLayout (context, layout);
		}

		void RenderTitle (Cairo.Context context, Gdk.Point position, double opacity)
		{
			using (var layout = TitleLayout (PangoContext)) {
				RenderShadowedText (context, position, opacity, layout);
			}
		}

		void RenderText (Cairo.Context context, Gdk.Point position, double opacity)
		{
			using (var layout = TextLayout (PangoContext)) {
				RenderShadowedText (context, position, opacity, layout);
			}
		}

		void RenderButton (Cairo.Context context, Gdk.Point corner, double opacity, bool hovered)
		{
			Gdk.Rectangle region = new Gdk.Rectangle (corner.X,
			                                          corner.Y,
			                                          ButtonSize.Width, ButtonSize.Height);



			context.RoundedRectangle (region.X + 0.5, region.Y + 0.5, region.Width - 1, region.Height - 1, 3);
			using (var lg = new LinearGradient (0, region.Top, 0, region.Bottom)) {
				if (hovered) {
					lg.AddColorStop (0, new Cairo.Color (.15, .76, .09, opacity));
					lg.AddColorStop (1, new Cairo.Color (.41, .91, .46, opacity));
				} else {
					lg.AddColorStop (0, new Cairo.Color (.41, .91, .46, opacity));
					lg.AddColorStop (1, new Cairo.Color (.15, .76, .09, opacity));
				}

				context.Pattern = lg;
				context.FillPreserve ();
			}

			context.Color = new Cairo.Color (.29, .79, .28, opacity);
			context.LineWidth = 1;
			context.Stroke ();

			region.Inflate (-1, -1);
			context.RoundedRectangle (region.X + 0.5, region.Y + 0.5, region.Width - 1, region.Height - 1, 2);

			using (var lg = new LinearGradient (0, region.Top, 0, region.Bottom)) {
				lg.AddColorStop (0, new Cairo.Color (1, 1, 1, .74 * opacity));
				lg.AddColorStop (0.1, new Cairo.Color (1, 1, 1, 0));
				lg.AddColorStop (0.9, new Cairo.Color (0, 0, 0, 0));
				lg.AddColorStop (1, new Cairo.Color (0, 0, 0, .34 * opacity));

				context.Pattern = lg;
				context.Stroke ();
			}

			using (var layout = ButtonLayout (PangoContext)) {
				int w, h;
				layout.GetPixelSize (out w, out h);

				RenderShadowedText (context, new Gdk.Point (corner.X + ButtonSize.Width / 2 - w / 2, corner.Y + ButtonSize.Height / 2 - h / 2 - 1), opacity, layout);
			}
		}

		Gdk.Point RenderTitlePosition {
			get { return new Gdk.Point (TitlePosition.X + TitleOffset.X, TitlePosition.Y + TitleOffset.Y); }
		}

		Gdk.Point RenderTextPosition {
			get { return new Gdk.Point (TextPosition.X + TextOffset.X, TextPosition.Y + TextOffset.Y); }
		}

		Gdk.Point RenderIconPosition {
			get { return new Gdk.Point (IconPosition.X + IconOffset.X, IconPosition.Y + IconOffset.Y); }
		}

		SurfaceWrapper backgroundSurface;
		SurfaceWrapper titleSurface;
		SurfaceWrapper textSurface;
		SurfaceWrapper buttonSurface;
		SurfaceWrapper previewSurface;

		protected override void OnDestroyed ()
		{
			if (backgroundSurface != null)
				backgroundSurface.Dispose ();
			if (titleSurface != null)
				titleSurface.Dispose ();
			if (textSurface != null)
				textSurface.Dispose ();
			if (buttonSurface != null)
				buttonSurface.Dispose ();
			if (previewSurface != null)
				previewSurface.Dispose ();
			base.OnDestroyed ();
		}

		Gdk.Size? titleSize;
		Gdk.Size TitleSize {
			get {
				if (titleSize == null) {
					var layout = TitleLayout (PangoContext);
					int w, h;
					layout.GetPixelSize (out w, out h);
					titleSize = new Gdk.Size (w, h);
				}
				return titleSize.Value;
			}
		}

		Gdk.Size? textSize;
		Gdk.Size TextSize {
			get {
				if (textSize == null) {
					var layout = TextLayout (PangoContext);
					int w, h;
					layout.GetPixelSize (out w, out h);
					textSize = new Gdk.Size (w, h);
				}
				return textSize.Value;
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Translate (Allocation.X, Allocation.Y);
				Gdk.Rectangle main = new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				context.CachedDraw (ref backgroundSurface, main, 
				                    opacity: (float)BackgroundOpacity,
				                    draw: (ctx, opacity) => RenderBackground (ctx, main));

				context.CachedDraw (ref titleSurface, RenderTitlePosition, TitleSize, new { Surface = backgroundSurface }, (float) TitleOpacity, (ctx, alpha) => {
					ctx.SetSourceSurface (backgroundSurface.Surface, -TitlePosition.X, -TitlePosition.Y);
					ctx.Rectangle (0, 0, TitleSize.Width, TitleSize.Height);
					ctx.Fill ();
					RenderTitle (ctx, new Gdk.Point (), alpha);
				});
				context.CachedDraw (ref textSurface, RenderTextPosition, TextSize, new { Surface = backgroundSurface } , (float) TextOpacity, (ctx, alpha) => {
					ctx.SetSourceSurface (backgroundSurface.Surface, -TextPosition.X, -TextPosition.Y);
					ctx.Rectangle (0, 0, TextSize.Width, TextSize.Height);
					ctx.Fill ();
					RenderText (ctx, new Gdk.Point (), alpha);
				});
				context.CachedDraw (ref buttonSurface, ButtonPosistion, ButtonSize, new { Hovered = ButtonHovered }, (float)ButtonOpacity, (ctx, alpha) => RenderButton (ctx, new Gdk.Point (), alpha, ButtonHovered));
				RenderPreview (context, RenderIconPosition, IconOpacity);
			}
			return base.OnExposeEvent (evnt);
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1 && ButtonHovered) {
				this.Animate ("FadeOut", 
				              (f) => TitleOpacity = TextOpacity = IconOpacity = ButtonOpacity = BackgroundOpacity = f, 
				              start: 1, end: 0,
				              finished: (f, aborted) => OnDismissed ());
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		void OnDismissed ()
		{
			if (Dismissed != null)
				Dismissed (this, EventArgs.Empty);
		}

		Pango.Layout ButtonLayout (Pango.Context context)
		{
			var layout = new Pango.Layout (context);
			layout.FontDescription = Pango.FontDescription.FromString (Platform.IsMac ? Styles.WelcomeScreen.FontFamilyMac : Styles.WelcomeScreen.FontFamilyWindows);
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (16);
			layout.SetText ("Start Building Apps");

			return layout;
		}

		Pango.Layout TitleLayout (Pango.Context context)
		{
			var layout = new Pango.Layout (context);
			layout.FontDescription = Pango.FontDescription.FromString (Platform.IsMac ? Styles.WelcomeScreen.FontFamilyMac : Styles.WelcomeScreen.FontFamilyWindows);
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (26);

			layout.SetText ("Welcome To " + BrandingService.SuiteName + "!");
			return layout;
		}

		Pango.Layout TextLayout (Pango.Context context)
		{
			var layout = new Pango.Layout (context);
			layout.FontDescription = Pango.FontDescription.FromString (Platform.IsMac ? Styles.WelcomeScreen.FontFamilyMac : Styles.WelcomeScreen.FontFamilyWindows);
			layout.FontDescription.AbsoluteSize = Pango.Units.FromPixels (15);
			layout.Width = Pango.Units.FromPixels (420);
			layout.Wrap = Pango.WrapMode.Word;

			Pango.AttrRise rise = new Pango.AttrRise (Pango.Units.FromPixels (7));
			layout.Attributes = new Pango.AttrList ();
			layout.Attributes.Insert (rise);

			string description = BrandingService.GetString ("FirstRunDescription");
			if (description != null)
				description = description.Replace ("\\n",  "\n");
			layout.SetText (description ?? "No Text Set");
			return layout;
		}
	}
}

