//
// DockNotebookTabButton.cs
//
// Author:
//       Jose Medrano <josmed@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corp. (http://microsoft.com)
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
using Cairo;
using Gtk;
using Xwt.Motion;
using Gdk;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;

namespace MonoDevelop.Components.DockNotebook
{
	class DockNotebookTabButton : CanvasElement, IDisposable
	{
		internal static Xwt.Drawing.Image tabDirtyImage = Xwt.Drawing.Image.FromResource ("tab-dirty-9.png");
		internal static Xwt.Drawing.Image tabCloseImage = Xwt.Drawing.Image.FromResource ("tab-close-9.png");

		readonly static CairoDockNotebookTabButtonRenderer renderer = new CairoDockNotebookTabButtonRenderer ();

		internal const double CloseButtonMarginRight = 0;
		internal const double CloseButtonMarginBottom = -1.0;

		internal AtkCocoaHelper.AccessibilityElementProxy AccessibilityElement { get; private set; }
		readonly DockNotebookTab tab;

		internal event EventHandler Pressed;
		internal event EventHandler Released;
		internal event EventHandler ShowMenu;

		internal string Identifier => AccessibilityElement.Identifier;

		Gdk.Rectangle allocation;
		public override Gdk.Rectangle Allocation {
			get {
				return allocation;
			}
			set {
				Gdk.Rectangle cocoaFrame;

				// value is in the TabStrip's coordinate space, whereas we need to set the button in the tab space.
				cocoaFrame.X = (int)value.X - tab.Allocation.X;
				int halfParentWidth = tab.Allocation.Height / 2;
				double dy = value.Y - halfParentWidth;
				cocoaFrame.Y = (int)(halfParentWidth + dy) - tab.Allocation.Y;
				cocoaFrame.Width = (int)value.Width;
				cocoaFrame.Height = (int)value.Height;

				AccessibilityElement.FrameInParent = cocoaFrame;

				Gdk.Rectangle realFrame = new Gdk.Rectangle ((int)value.X, (int)value.Y, (int)value.Width, (int)value.Height);
				AccessibilityElement.FrameInGtkParent = realFrame;
				allocation = value;
			}
		}

		internal DockNotebookTabButton (DockNotebookTab tab, Widget parent, string identifier, string title)
		{
			this.tab = tab;
			AccessibilityElement = AccessibilityElementProxy.ButtonElementProxy ();
			AccessibilityElement.PerformPress += OnButtonPressEvent;
			AccessibilityElement.SetRole (AtkCocoa.Roles.AXButton);
			AccessibilityElement.GtkParent = parent;
			AccessibilityElement.PerformShowMenu += OnCloseButtonShowMenu;
			AccessibilityElement.Title = title;
			AccessibilityElement.Identifier = identifier;
		}

		protected override void OnButtonPressEvent (object sender, EventArgs args)
		{
			Pressed?.Invoke (this, args);
		}

		protected override void OnButtonReleaseEvent (object sender, EventArgs args)
		{
			Released?.Invoke (this, args);
		}

		internal void OnTabTextChanged (string text)
		{
			AccessibilityElement.Title = string.Format (Core.GettextCatalog.GetString ("Close {0}"), text);
		}

		internal void OnTabMarkupChanged (string text)
		{
			AccessibilityElement.Title = string.Format (Core.GettextCatalog.GetString ("Close {0}"), text);
		}

		void OnCloseButtonShowMenu (object sender, EventArgs args)
		{
			ShowMenu?.Invoke (this, args);
		}

		public void Dispose ()
		{
			AccessibilityElement.PerformShowMenu -= OnCloseButtonShowMenu;
			AccessibilityElement.PerformPress -= OnButtonPressEvent;
		}

		internal class CairoDockNotebookTabButtonRenderer
		{
			public void Draw (Cairo.Context ctx, DockNotebookTabButton button, DockNotebookTab tab, TabStrip tabStrip, Gdk.Rectangle buttonAllocation) 
			{
				//TODO: This is not correct set from a render
				button.Allocation = buttonAllocation.InflateRect (2, 2);

				bool isButtonHovered = button.IsHovered;
				if (!isButtonHovered && tab.DirtyStrength > 0.5) {
					ctx.DrawImage (tabStrip, tabDirtyImage, buttonAllocation.X, buttonAllocation.Y);
					return;
				}

				ctx.DrawImage (tabStrip, tabCloseImage.WithAlpha ((isButtonHovered ? 1.0 : 0.5) * tab.Opacity), buttonAllocation.X, buttonAllocation.Y);
			}
		}
	}
}
