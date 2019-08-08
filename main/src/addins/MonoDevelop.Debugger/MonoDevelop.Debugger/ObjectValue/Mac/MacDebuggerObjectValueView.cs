//
// MacDebuggerObjectValueView.cs
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
using System.Collections.Generic;

using AppKit;

using Xwt.Drawing;

using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// The NSTableViewCell used for the "Value" column.
	/// </summary>
	class MacDebuggerObjectValueView : MacDebuggerObjectCellViewBase
	{
		NSImageView statusIcon;
		bool statusIconVisible;
		NSView colorPreview;
		bool colorPreviewVisible;
		NSButton valueButton;
		bool valueButtonVisible;
		NSButton viewerButton;
		bool viewerButtonVisible;
		bool textChanged;
		bool disposed;

		public MacDebuggerObjectValueView (MacObjectValueTreeView treeView) : base (treeView, "value")
		{
			statusIcon = new NSImageView {
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			colorPreview = new NSView {
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			valueButton = new NSButton {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Title = GettextCatalog.GetString (""),
				BezelStyle = NSBezelStyle.Inline
			};
			valueButton.Cell.UsesSingleLineMode = true;
			valueButton.Font = NSFont.FromDescription (valueButton.Font.FontDescriptor, valueButton.Font.PointSize - 3);
			valueButton.Activated += OnValueButtonActivated;

			int imageSize = treeView.CompactView ? CompactImageSize : ImageSize;
			viewerButton = new NSButton {
				Image = GetImage (Gtk.Stock.Edit, imageSize, imageSize),
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			viewerButton.BezelStyle = NSBezelStyle.Inline;
			viewerButton.Bordered = false;
			viewerButton.Activated += OnViewerButtonActivated;

			TextField = new NSTextField {
				AutoresizingMask = NSViewResizingMask.WidthSizable,
				TranslatesAutoresizingMaskIntoConstraints = false,
				BackgroundColor = NSColor.Clear,
				Bordered = false,
				Editable = false
			};
			TextField.Cell.UsesSingleLineMode = true;
			TextField.Cell.Wraps = false;
			TextField.EditingBegan += OnEditingBegan;
			TextField.EditingEnded += OnEditingEnded;
			TextField.Changed += OnTextChanged;

			AddSubview (TextField);

			TextField.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
			TextField.TrailingAnchor.ConstraintEqualToAnchor (TrailingAnchor, -MarginSize).Active = true;
		}

		public MacDebuggerObjectValueView (IntPtr handle) : base (handle)
		{
		}

		protected override void UpdateContents ()
		{
			if (Node == null)
				return;

			var editable = TreeView.GetCanEditNode (Node);
			var textColor = NSColor.ControlText;
			string evaluateStatusIcon = null;
			string valueButtonText = null;
			var showViewerButton = false;
			Color? previewColor = null;
			string strval;

			if (Node.IsUnknown) {
				if (TreeView.DebuggerService.Frame != null) {
					strval = GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", Node.Name);
				} else {
					strval = string.Empty;
				}
				evaluateStatusIcon = Ide.Gui.Stock.Warning;
			} else if (Node.IsError || Node.IsNotSupported) {
				evaluateStatusIcon = Ide.Gui.Stock.Warning;
				strval = Node.Value;
				int i = strval.IndexOf ('\n');
				if (i != -1)
					strval = strval.Substring (0, i);
				textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueErrorText));
			} else if (Node.IsImplicitNotSupported) {
				strval = "";//val.Value; with new "Show Value" button we don't want to display message "Implicit evaluation is disabled"
				textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
				if (Node.CanRefresh)
					valueButtonText = GettextCatalog.GetString ("Show Value");
			} else if (Node.IsEvaluating) {
				strval = GettextCatalog.GetString ("Evaluating\u2026");

				evaluateStatusIcon = "md-spinner-16";

				textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
			} else if (Node.IsEnumerable) {
				if (Node is ShowMoreValuesObjectValueNode) {
					valueButtonText = GettextCatalog.GetString ("Show More");
				} else {
					valueButtonText = GettextCatalog.GetString ("Show Values");
				}
				strval = "";
			} else if (Node is AddNewExpressionObjectValueNode) {
				strval = string.Empty;
				editable = false;
			} else {
				strval = TreeView.Controller.GetDisplayValueWithVisualisers (Node, out showViewerButton);

				if (TreeView.Controller.GetNodeHasChangedSinceLastCheckpoint (Node))
					textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueModifiedText));

				var val = Node.GetDebuggerObjectValue ();
				if (val != null && !val.IsNull && DebuggingService.HasGetConverter<Color> (val)) {
					try {
						previewColor = DebuggingService.GetGetConverter<Color> (val).GetValue (val);
					} catch {
						previewColor = null;
					}
				}
			}

			strval = strval.Replace ("\r\n", " ").Replace ("\n", " ");

			var views = new List<NSView> ();

			// First item: Status Icon
			if (evaluateStatusIcon != null) {
				statusIcon.Image = GetImage (evaluateStatusIcon, Gtk.IconSize.Menu);
				views.Add (statusIcon);

				if (!statusIconVisible) {
					AddSubview (statusIcon);
					statusIconVisible = true;

					statusIcon.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
					statusIcon.WidthAnchor.ConstraintEqualToConstant (ImageSize).Active = true;
					statusIcon.HeightAnchor.ConstraintEqualToConstant (ImageSize).Active = true;
				}
			} else if (statusIconVisible) {
				statusIcon.RemoveFromSuperview ();
				statusIconVisible = false;
			}

			// Second Item: Color Preview
			if (previewColor != null) {
				colorPreview.Layer.BackgroundColor = GetCGColor (previewColor.Value);
				views.Add (colorPreview);

				if (!colorPreviewVisible) {
					AddSubview (colorPreview);
					colorPreviewVisible = true;

					colorPreview.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
					colorPreview.WidthAnchor.ConstraintEqualToConstant (ImageSize).Active = true;
					colorPreview.HeightAnchor.ConstraintEqualToConstant (ImageSize).Active = true;
				}
			} else if (colorPreviewVisible) {
				colorPreview.RemoveFromSuperview ();
				colorPreviewVisible = false;
			}

			// Third Item: Value Button
			if (valueButtonText != null && !((MacObjectValueNode) ObjectValue).HideValueButton) {
				valueButton.AttributedTitle = GetAttributedString (valueButtonText);
				views.Add (valueButton);

				if (!valueButtonVisible) {
					AddSubview (valueButton);
					valueButtonVisible = true;

					valueButton.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
				}
			} else if (valueButtonVisible) {
				valueButton.RemoveFromSuperview ();
				valueButtonVisible = false;
			}

			// Fourth Item: Viewer Button
			if (showViewerButton) {
				views.Add (viewerButton);

				if (!viewerButtonVisible) {
					AddSubview (viewerButton);
					viewerButtonVisible = true;

					viewerButton.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
					viewerButton.WidthAnchor.ConstraintEqualToConstant (viewerButton.Image.Size.Width).Active = true;
					viewerButton.HeightAnchor.ConstraintEqualToConstant (viewerButton.Image.Size.Height).Active = true;
				}
			} else if (viewerButtonVisible) {
				viewerButton.RemoveFromSuperview ();
				viewerButtonVisible = false;
			}

			// Fifth Item: Text Value
			TextField.AttributedStringValue = GetAttributedString (strval);
			TextField.TextColor = textColor;
			TextField.Editable = editable;
			views.Add (TextField);

			// lay out our views...
			var leadingAnchor = LeadingAnchor;

			for (int i = 0; i < views.Count; i++) {
				var view = views[i];

				view.LeadingAnchor.ConstraintEqualToAnchor (leadingAnchor, i == 0 ? MarginSize : RowCellSpacing).Active = true;
				leadingAnchor = view.TrailingAnchor;
			}
		}

		void OnEditingBegan (object sender, EventArgs e)
		{
			TreeView.OnStartEditing ();
		}

		void OnEditingEnded (object sender, EventArgs e)
		{
			TreeView.OnEndEditing ();

			if (!textChanged)
				return;

			textChanged = false;

			var newValue = TextField.StringValue;

			if (TreeView.GetEditValue (Node, newValue))
				Refresh ();
		}

		void OnTextChanged (object sender, EventArgs e)
		{
			textChanged = true;
		}

		void OnValueButtonActivated (object sender, EventArgs e)
		{
			if (Node.IsEnumerable) {
				if (Node is ShowMoreValuesObjectValueNode moreNode) {
					TreeView.LoadMoreChildren (moreNode.EnumerableNode);
				} else {
					// use ExpandItem to expand so we see the loading message, expanding the node will trigger a fetch of the children
					TreeView.ExpandItem (ObjectValue, false);
				}
			} else {
				// this is likely to support IsImplicitNotSupported
				TreeView.Refresh (Node);
			}

			((MacObjectValueNode) ObjectValue).HideValueButton = true;
			Refresh ();
		}

		void OnViewerButtonActivated (object sender, EventArgs e)
		{
			if (!TreeView.DebuggerService.CanQueryDebugger)
				return;

			if (TreeView.ShowVisualizer (Node))
				Refresh ();
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				viewerButton.Activated -= OnViewerButtonActivated;
				valueButton.Activated -= OnValueButtonActivated;
				TextField.EditingBegan -= OnEditingBegan;
				TextField.EditingEnded -= OnEditingEnded;
				TextField.Changed -= OnTextChanged;
				disposed = true;
			}

			base.Dispose (disposing);
		}
	}
}
