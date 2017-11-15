//
// DockNotebookTab.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using Xwt.Motion;

using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using System.Collections.Generic;
using Cairo;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Components.DockNotebook
{
	class DockNotebookTab: IAnimatable, IDisposable
	{
		internal static Xwt.Drawing.Image tabActiveBackImage = Xwt.Drawing.Image.FromResource ("tabbar-active.9.png");
		internal static Xwt.Drawing.Image tabBackImage = Xwt.Drawing.Image.FromResource ("tabbar-inactive.9.png");

		readonly static DockNotebookTabRenderer renderer = new DockNotebookTabRenderer ();

		static readonly int VerticalTextSize = 11;

		public const string CloseButtonIdentifier = "DockNotebook.Tab.CloseButton";

		List<DockNotebookTabButton> Buttons = new List<DockNotebookTabButton> ();

		DockNotebook notebook;
		readonly TabStrip strip;

		internal AtkCocoaHelper.AccessibilityElementProxy Accessible { get; private set; }
		internal event EventHandler AccessibilityShowMenu;

		string text;
		string markup;
		string tooltip;
		Xwt.Drawing.Image icon;
		Widget content;

		Gdk.Rectangle allocation;
		internal Gdk.Rectangle Allocation {
			get {
				return allocation;
			}
			set {
				Gdk.Rectangle cocoaFrame;

				cocoaFrame.X = value.X;

				// This will fail if Y != 0
				cocoaFrame.Y = value.Y;
				cocoaFrame.Width = value.Width;
				cocoaFrame.Height = value.Height;

				Accessible.FrameInParent = cocoaFrame;
				Accessible.FrameInGtkParent = value;
				allocation = value;
			}
		}

		public DockNotebook Notebook { get { return notebook; } }

		public int Index { get; internal set; }

		public bool Notify { get; set; }

		public double WidthModifier { get; set; }

		public double Opacity { get; set; }

		public double GlowStrength { get; set; }

		public bool Hidden { get; set; }

		public double DirtyStrength { get; set; }
		
		void IAnimatable.BatchBegin () { }
		void IAnimatable.BatchCommit () { QueueDraw (); }

		bool dirty;
		public bool Dirty {
			get { return dirty; }
			set { 
				if (dirty == value)
					return;
				dirty = value;
				this.Animate ("Dirty", f => DirtyStrength = f,
				              easing: Easing.CubicInOut,
				              start: DirtyStrength, end: value ? 1 : 0);

				string accTitle;

				if (dirty) {
					accTitle = string.Format (Core.GettextCatalog.GetString ("{0}. (dirty)"), Text ?? Markup);
				} else {
					accTitle = Text ?? Markup;
				}
				Accessible.Title = accTitle;
			}
		}

		public string Text {
			get {
				return text;
			}
			set {
				text = value;
				markup = null;

				string accTitle;

				if (dirty) {
					accTitle = string.Format (Core.GettextCatalog.GetString ("{0}. (dirty)"), value);
				} else {
					accTitle = value;
				}

				Accessible.Title = accTitle;
				foreach (var button in Buttons) {
					button.OnTabTextChanged (value);
				}
				strip.Update ();
			}
		}

		public string Markup {
			get {
				return markup;
			}
			set {
				markup = value;
				text = null;

				// FIXME: Strip markup
				string accTitle;
				if (dirty) {
					accTitle = string.Format (Core.GettextCatalog.GetString ("{0}. (dirty)"), value);
				} else {
					accTitle = value;
				}

				Accessible.Title = accTitle;
				foreach (var button in Buttons) {
					button.OnTabMarkupChanged (value);
				}
				strip.Update ();
			}
		}

		public Xwt.Drawing.Image Icon {
			get {
				return icon;
			}
			set {
				icon = value;
				strip.Update ();
			}
		}

		public Widget Content {
			get {
				return content;
			}
			set {
				content = value;
				notebook.ShowContent (this);
			}
		}

		public string Tooltip {
			get {
				return tooltip;
			}
			set {
				tooltip = value;
				Accessible.Help = string.Format (Core.GettextCatalog.GetString ("Switch to {0}"), value);
			}
		}

		internal DockNotebookTab (DockNotebook notebook, TabStrip strip)
		{
			this.notebook = notebook;
			this.strip = strip;

			Accessible = AccessibilityElementProxy.ButtonElementProxy ();
			Accessible.PerformPress += OnPressTab;
			// FIXME Should Role descriptions be translated?
			Accessible.SetRole (AtkCocoa.Roles.AXRadioButton, "tab");
			Accessible.GtkParent = strip;
			Accessible.PerformShowMenu += OnShowMenu;
			Accessible.Identifier = "DockNotebook.Tab";

			AddTabButton (CloseButtonIdentifier, Core.GettextCatalog.GetString ("Close document"));
		}

		internal Gdk.Rectangle SavedAllocation { get; private set; }
		internal double SaveStrength { get; set; }

		internal void SaveAllocation ()
		{
			SavedAllocation = Allocation;
		}

		public void QueueDraw ()
		{
			strip.QueueDraw ();
		}

		public void OnDraw (Cairo.Context ctx, TabStrip tabStrip, Gdk.Rectangle tabBounds, bool active, bool focused) 
		{
			renderer.Draw (ctx, this, tabStrip, tabBounds, active, focused);
		}

		internal event EventHandler AccessibilityPressTab;
		internal event EventHandler AccessibilityPressCloseButton;

		void OnPressTab (object sender, EventArgs args)
		{
			AccessibilityPressTab?.Invoke (this, args);
		}

		void OnShowMenu (object sender, EventArgs args)
		{
			AccessibilityShowMenu?.Invoke (this, args);
		}

		#region Buttons

		void AddTabButton (string identifier, string title)
		{
			var button = new DockNotebookTabButton (this, strip, identifier, title);
			button.ShowMenu += (sender, e) => AccessibilityShowMenu?.Invoke (sender, EventArgs.Empty);
			button.Pressed += (sender, e) => {
				if (e == CloseButtonIdentifier) {
					AccessibilityPressCloseButton?.Invoke (sender, EventArgs.Empty);
				}
			};
			Accessible.AddAccessibleChild (button.AccessibilityElement);
			Buttons.Add (button);
		}

		internal DockNotebookTabButton GetButton (string identifier)
		{
			return Buttons.FirstOrDefault (s => s.Identifier == identifier);
		}

		internal DockNotebookTabButton GetCloseButton ()
		{
			return GetButton (DockNotebookTab.CloseButtonIdentifier);
		}

		internal bool IsOverButton (string identifier, int x, int y)
		{
			var buttonSelected = GetButton (identifier);
			if (buttonSelected != null) {
				return buttonSelected.Allocation.Contains (x, y);
			}
			return false;
		}

		internal bool IsOverCloseButton (int x, int y)
		{
			return IsOverButton (CloseButtonIdentifier, x, y);
		}

		#endregion

		public void Dispose ()
		{
			Accessible.PerformPress -= OnPressTab;
			Accessible.PerformShowMenu -= OnShowMenu;
			foreach (var button in Buttons) {
				button.Dispose ();
			}
		}

		class DockNotebookTabRenderer
		{
			public DockNotebookTabRenderer ()
			{
			}

			public void Draw (Cairo.Context ctx, DockNotebookTab tab, TabStrip tabStrip, Gdk.Rectangle tabBounds, bool active, bool focused)
			{
				var la = CreateTabLayout (tabStrip.PangoContext, tab, active);
				ctx.LineWidth = 1;
				ctx.NewPath ();

				var paddingSpacing = GetPaddingSpacing (tabBounds.Width, active);
				var closeButtonAllocation = new Cairo.Rectangle (tabBounds.Right - paddingSpacing.Right - (DockNotebookTabButton.tabCloseImage.Width / 2) - DockNotebookTabButton.CloseButtonMarginRight,
				                                                 tabBounds.Height - paddingSpacing.Bottom - DockNotebookTabButton.tabCloseImage.Height - DockNotebookTabButton.CloseButtonMarginBottom,
				                                                 DockNotebookTabButton.tabCloseImage.Width, DockNotebookTabButton.tabCloseImage.Height);

				DrawTabBackground (ctx, tabStrip, tabBounds.Width, tabBounds.X, active);

				bool drawButtons = active || focused || tabStrip.IsElementHovered (tab);
				if (drawButtons) {
					tab.Buttons.ForEach (btn => btn.Draw (ctx, tabStrip, closeButtonAllocation));
				}

				DrawTabText (ctx, tab, la, tabBounds, closeButtonAllocation, paddingSpacing, active, drawButtons);
				la.Dispose ();
			}

			void DrawTabBackground (Cairo.Context ctx, TabStrip tabStrip, int contentWidth, int px, bool active = true)
			{
				int lean = Math.Min (TabStrip.LeanWidth, contentWidth / 2);
				int halfLean = lean / 2;

				double x = px + TabStrip.TabSpacing - halfLean;
				double y = 0;
				double height = tabStrip.Allocation.Height;
				double width = contentWidth - (TabStrip.TabSpacing * 2) + lean;

				var image = active ? tabActiveBackImage : tabBackImage;
				image = image.WithSize (width, height);

				ctx.DrawImage (tabStrip, image, x, y);
			}

			void DrawTabText (Cairo.Context ctx, DockNotebookTab tab, Pango.Layout la, Gdk.Rectangle tabBounds, Cairo.Rectangle closeButtonAllocation, Xwt.WidgetSpacing paddingSpacing, bool active, bool drawButtons)
			{
				// Render Text
				double tw = tabBounds.Width - (paddingSpacing.Left + paddingSpacing.Right);
				if (drawButtons || tab.DirtyStrength > 0.5)
					tw -= closeButtonAllocation.Width / 2;

				double tx = tabBounds.X + paddingSpacing.Left;
				var baseline = la.GetLine (0).Layout.GetPixelBaseline ();
				double ty = tabBounds.Height - paddingSpacing.Bottom - baseline;

				ctx.MoveTo (tx, ty);
				if (!MonoDevelop.Core.Platform.IsMac && !MonoDevelop.Core.Platform.IsWindows) {
					// This is a work around for a linux specific problem.
					// A bug in the proprietary ATI driver caused TAB text not to draw.
					// If that bug get's fixed remove this HACK asap.
					la.Ellipsize = Pango.EllipsizeMode.End;
					la.Width = (int)(tw * Pango.Scale.PangoScale);
					ctx.SetSourceColor ((tab.Notify ? Styles.TabBarNotifyTextColor : (active ? Styles.TabBarActiveTextColor : Styles.TabBarInactiveTextColor)).ToCairoColor ());
					Pango.CairoHelper.ShowLayout (ctx, la.GetLine (0).Layout);
				} else {
					// ellipses are for space wasting ..., we cant afford that
					using (var lg = new LinearGradient (tx + tw - 10, 0, tx + tw, 0)) {
						var color = (tab.Notify ? Styles.TabBarNotifyTextColor : (active ? Styles.TabBarActiveTextColor : Styles.TabBarInactiveTextColor)).ToCairoColor ();
						color = color.MultiplyAlpha (tab.Opacity);
						lg.AddColorStop (0, color);
						color.A = 0;
						lg.AddColorStop (1, color);
						ctx.SetSource (lg);
						Pango.CairoHelper.ShowLayout (ctx, la.GetLine (0).Layout);
					}
				}
			}

			static Xwt.WidgetSpacing GetPaddingSpacing (double width, bool active)
			{
				double rightPadding = (active ? TabStrip.TabActivePadding.Right : TabStrip.TabPadding.Right) - (TabStrip.LeanWidth / 2);
				rightPadding = (rightPadding * Math.Min (1.0, Math.Max (0.5, (width - 30) / 70.0)));
				double leftPadding = (active ? TabStrip.TabActivePadding.Left : TabStrip.TabPadding.Left) - (TabStrip.LeanWidth / 2);
				leftPadding = (leftPadding * Math.Min (1.0, Math.Max (0.5, (width - 30) / 70.0)));
				double bottomPadding = active ? TabStrip.TabActivePadding.Bottom : TabStrip.TabPadding.Bottom;
				return new Xwt.WidgetSpacing (leftPadding, 0, rightPadding, bottomPadding);
			}

			static Pango.Layout CreateTabLayout (Pango.Context context, DockNotebookTab tab, bool active = false)
			{
				Pango.Layout la = CreateSizedLayout (context, active);
				if (!string.IsNullOrEmpty (tab.Markup))
					la.SetMarkup (tab.Markup);
				else if (!string.IsNullOrEmpty (tab.Text))
					la.SetText (tab.Text);
				return la;
			}

			static Pango.Layout CreateSizedLayout (Pango.Context context, bool active)
			{
				var la = new Pango.Layout (context);
				la.FontDescription = Ide.Fonts.FontService.SansFont.Copy ();
				if (!Core.Platform.IsWindows)
					la.FontDescription.Weight = Pango.Weight.Bold;
				la.FontDescription.AbsoluteSize = Pango.Units.FromPixels (VerticalTextSize);

				return la;
			}
		}
	}
}
