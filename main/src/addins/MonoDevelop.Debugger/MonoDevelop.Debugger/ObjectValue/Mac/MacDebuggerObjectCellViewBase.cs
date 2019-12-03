//
// MacDebuggerObjectCellViewBase.cs
//
// Author:
//       Jeffrey Stedfast <jestedfa@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corp.
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
using System.Globalization;
using System.Collections.Generic;

using AppKit;
using Foundation;
using CoreGraphics;
using CoreText;

using Xwt.Drawing;

using MonoDevelop.Ide;
using MonoDevelop.Components;

namespace MonoDevelop.Debugger
{
	abstract class MacDebuggerObjectCellViewBase : NSTableCellView
	{
		protected static readonly string[] SelectedStyle = { "sel" };
		protected const int CompactImageSize = 12;
		protected const int RowCellSpacing = 2;
		protected const int ImageSize = 16;
		protected const int MarginSize = 2;

		protected MacDebuggerObjectCellViewBase (MacObjectValueTreeView treeView, string identifier)
		{
			Identifier = identifier;
			TreeView = treeView;
		}

		protected MacDebuggerObjectCellViewBase (IntPtr handle) : base (handle)
		{
		}

		public MacObjectValueTreeView TreeView {
			get; private set;
		}

		public override NSObject ObjectValue {
			get { return base.ObjectValue; }
			set {
				var target = ((MacObjectValueNode) value)?.Target;

				if (Node != target) {
					if (target != null)
						target.ValueChanged += OnValueChanged;

					if (Node != null)
						Node.ValueChanged -= OnValueChanged;

					Node = target;
				}

				base.ObjectValue = value;

				if (Superview != null)
					UpdateContents ();
			}
		}

		public nfloat OptimalWidth {
			get; protected set;
		}

		public ObjectValueNode Node {
			get; private set;
		}

		public nint Row {
			get; set;
		}

		public bool IsShowMoreValues {
			get { return Node is ShowMoreValuesObjectValueNode; }
		}

		public bool IsLoading {
			get { return Node is LoadingObjectValueNode; }
		}

		protected static NSImage GetImage (string name, Gtk.IconSize size, params string[] styles)
		{
			var icon = ImageService.GetIcon (name, size);

			if (styles != null && styles.Length > 0)
				icon = icon.WithStyles (styles);

			try {
				return icon.ToNSImage ();
			} catch (Exception ex) {
				Core.LoggingService.LogError ($"Failed to load '{name}' as an NSImage", ex);
				return icon.ToBitmap (NSScreen.MainScreen.BackingScaleFactor).ToNSImage ();
			}
		}

		protected static NSImage GetImage (string name, Gtk.IconSize size, bool selected = false)
		{
			return GetImage (name, size, selected ? SelectedStyle : null);
		}

		protected static NSImage GetImage (string name, int width, int height, bool selected = false)
		{
			var icon = ImageService.GetIcon (name).WithSize (width, height);

			if (selected)
				icon = icon.WithStyles ("sel");

			try {
				return icon.ToNSImage ();
			} catch (Exception ex) {
				Core.LoggingService.LogError ($"Failed to load '{name}' as an NSImage", ex);
				return icon.ToBitmap (NSScreen.MainScreen.BackingScaleFactor).ToNSImage ();
			}
		}

		protected static CGColor GetCGColor (Color color)
		{
			return new CGColor ((nfloat) color.Red, (nfloat) color.Green, (nfloat) color.Blue);
		}

		protected static NSAttributedString GetAttributedPlaceholderString (string text)
		{
			return new NSAttributedString (text ?? string.Empty, strokeColor: NSColor.PlaceholderTextColor);
		}

		protected static nfloat GetWidthForString (NSFont font, string text, int sizeDelta = 0)
		{
			NSFont modified = null;
			nfloat width;

			if (sizeDelta != 0)
				modified = NSFont.FromDescription (font.FontDescriptor, font.PointSize + sizeDelta);

			using (var str = new NSAttributedString (text, font: modified ?? font))
				width = str.Size.Width;

			modified?.Dispose ();

			width = NMath.Ceiling (width);

			// Note: All code-paths that use sizeDelta == 0 are for NSTextField labels and the only code-path
			// that uses sizeDelta != 0 is for the Show More label in an NSButton.
			if (sizeDelta == 0) {
				// Note: In order to match NSTextField.Frame.Width after calling TextField.SizeToFit(), add 4px.
				width += 4;

				// Note: NSTextField also seems to need an extra 8px to actually fit the entire text w/o clipping.
				width += 8;
			} else {
				// Oddly enough, NSButton padding around the label is also +12px (matched after button.SizeToFit()
				// and checking the resulting button.Frame.Width).
				width += 12;
			}

			return width;
		}

		protected void UpdateFont (NSControl control, int sizeDelta = 0)
		{
			var font = TreeView.CustomFont;

			if (sizeDelta != 0) {
				control.Font = NSFont.FromDescription (font.FontDescriptor, font.PointSize + sizeDelta);
			} else {
				control.Font = font;
			}
		}

		public override void ViewDidMoveToSuperview ()
		{
			base.ViewDidMoveToSuperview ();

			if (Superview != null)
				UpdateContents ();
		}

		public override NSBackgroundStyle BackgroundStyle {
			get { return base.BackgroundStyle; }
			set {
				base.BackgroundStyle = value;

				if (Superview != null)
					UpdateContents ();
			}
		}

		protected abstract void UpdateContents ();

		public void Refresh ()
		{
			UpdateContents ();
			SetNeedsDisplayInRect (Frame);
		}

		void OnValueChanged (object sender, EventArgs e)
		{
			Refresh ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && Node != null) {
				Node.ValueChanged -= OnValueChanged;
				Node = null;
			}

			base.Dispose (disposing);
		}
	}
}
