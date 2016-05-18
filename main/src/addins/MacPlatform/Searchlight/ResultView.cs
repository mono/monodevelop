//
// ResultView.cs
//
// Author:
//       iain <iain@xamarin.com>
//
// Copyright (c) 2016 
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
using AppKit;
using CoreGraphics;

using MonoDevelop.Components;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Ide;

namespace MonoDevelop.MacIntegration.OverlaySearch
{
	public class ResultView : NSStackView
	{
		public event EventHandler<EventArgs> ResultActivated;
		NSStackView detailStack;
		NSImageView imageView;
		NSTextField titleField;
		NSTextField detailField;

		public SearchResult Result { get; private set; }

		bool highlighted;
		public bool Highlighted {
			get {
				return highlighted;
			}

			set {
				highlighted = value;

				if (detailField != null) {
					detailField.TextColor = highlighted ? NSColor.White : NSColor.DarkGray;
				}

				NeedsDisplay = true;
			}
		}

		public ResultView (SearchResult result)
		{
			Result = result;
			Spacing = 8f;
			Orientation = NSUserInterfaceLayoutOrientation.Horizontal;
			SetHuggingPriority (900, NSLayoutConstraintOrientation.Vertical);
			SetHuggingPriority (100, NSLayoutConstraintOrientation.Horizontal);
			SetContentCompressionResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);
			SetClippingResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);

			Alignment = NSLayoutAttribute.Top;

			imageView = new NSImageView ();
			imageView.Image = result.Icon.ToNSImage ();
			imageView.SetContentHuggingPriorityForOrientation (1000, NSLayoutConstraintOrientation.Vertical);
			AddView (imageView, NSStackViewGravity.Leading);

			detailStack = new NSStackView ();
			detailStack.Orientation = NSUserInterfaceLayoutOrientation.Vertical;
			detailStack.Alignment = NSLayoutAttribute.Leading;
			detailStack.Spacing = 0f;
			detailStack.SetHuggingPriority (1000, NSLayoutConstraintOrientation.Vertical);
			detailStack.SetHuggingPriority (100, NSLayoutConstraintOrientation.Horizontal);

			detailStack.SetContentCompressionResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);
			detailStack.SetClippingResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);

			AddView (detailStack, NSStackViewGravity.Leading);

			titleField = new NSTextField ();
			titleField.DrawsBackground = false;
			titleField.Selectable = false;
			titleField.Editable = false;
			titleField.Bordered = false;
			titleField.Bezeled = false;
			titleField.TextColor = IdeApp.Preferences.UserInterfaceTheme == Theme.Dark ? NSColor.White : NSColor.Black;

			titleField.StringValue = result.PlainText ?? result.GetMarkupText (false);
			// So that the titleField fills the whole width
			titleField.SetContentHuggingPriorityForOrientation (99, NSLayoutConstraintOrientation.Horizontal);
			titleField.SetContentHuggingPriorityForOrientation (1000, NSLayoutConstraintOrientation.Vertical);
			titleField.SetContentCompressionResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);

			detailStack.AddView (titleField, NSStackViewGravity.Leading);

			if (!string.IsNullOrEmpty (result.Description)) {
				detailField = new NSTextField ();
				detailField.DrawsBackground = false;
				detailField.Selectable = false;
				detailField.Editable = false;
				detailField.Bordered = false;
				detailField.Bezeled = false;

				detailField.Font = NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize);
				detailField.TextColor = NSColor.DarkGray;
				detailField.StringValue = result.Description;
				detailField.SetContentHuggingPriorityForOrientation (99, NSLayoutConstraintOrientation.Horizontal);
				detailField.SetContentHuggingPriorityForOrientation (1000, NSLayoutConstraintOrientation.Vertical);
				detailField.SetContentCompressionResistancePriority (1000, NSLayoutConstraintOrientation.Vertical);

				detailStack.AddView (detailField, NSStackViewGravity.Leading);
			}
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			NSComposite comp;
			if (highlighted) {
				// Xam blue
				NSColor.FromRgba (52f / 255, 152f / 255, 219f / 255, 1f).SetFill ();
				comp = NSComposite.SourceOver;
			} else {
				return;
				//comp = NSComposite.Clear;
			}

			NSGraphicsContext.CurrentContext.CompositingOperation = comp;
			var path = NSBezierPath.FromRect (Bounds);
			path.Fill ();
		}

		public override void MouseUp (NSEvent theEvent)
		{
			ResultActivated?.Invoke (this, EventArgs.Empty);
		}
	}
}

