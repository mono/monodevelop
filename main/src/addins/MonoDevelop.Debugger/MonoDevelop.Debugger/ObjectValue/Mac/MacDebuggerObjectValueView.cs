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
using CoreGraphics;

using Xwt.Drawing;

using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// The NSTableViewCell used for the "Value" column.
	/// </summary>
	class MacDebuggerObjectValueView : MacDebuggerObjectCellViewBase
	{
		readonly List<NSLayoutConstraint> constraints = new List<NSLayoutConstraint> ();
		NSProgressIndicator spinner;
		bool spinnerVisible;
		NSImageView statusIcon;
		bool statusIconVisible;
		NSView colorPreview;
		bool colorPreviewVisible;
		NSButton valueButton;
		bool valueButtonVisible;
		NSButton viewerButton;
		bool viewerButtonVisible;
		bool disposed;

		public MacDebuggerObjectValueView (MacObjectValueTreeView treeView) : base (treeView, "value")
		{
			spinner = new NSProgressIndicator (new CGRect (0, 0, ImageSize, ImageSize)) {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Style = NSProgressIndicatorStyle.Spinning,
				Indeterminate = true
			};

			statusIcon = new NSImageView {
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			colorPreview = new NSView (new CGRect (0, 0, ImageSize, ImageSize)) {
				TranslatesAutoresizingMaskIntoConstraints = false,
				WantsLayer = true
			};

			valueButton = new NSButton {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Title = GettextCatalog.GetString ("Show More"),
				BezelStyle = NSBezelStyle.Inline
			};
			valueButton.Cell.UsesSingleLineMode = true;
			UpdateFont (valueButton, -3);
			valueButton.Activated += OnValueButtonActivated;

			int imageSize = treeView.CompactView ? CompactImageSize : ImageSize;
			viewerButton = new NSButton {
				AccessibilityTitle = GettextCatalog.GetString ("Open Value Visualizer"),
				Image = GetImage (Gtk.Stock.Edit, imageSize, imageSize),
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			viewerButton.BezelStyle = NSBezelStyle.Inline;
			viewerButton.Bordered = false;
			viewerButton.Activated += OnViewerButtonActivated;

			TextField = new MacDebuggerTextField (this) {
				TranslatesAutoresizingMaskIntoConstraints = false,
				LineBreakMode = NSLineBreakMode.Clipping,
				MaximumNumberOfLines = 1,
				DrawsBackground = false,
				Bordered = false,
				Editable = false
			};

			AddSubview (TextField);
		}

		public MacDebuggerObjectValueView (IntPtr handle) : base (handle)
		{
		}

		protected override void UpdateContents ()
		{
			if (Node == null)
				return;

			foreach (var constraint in constraints) {
				constraint.Active = false;
				constraint.Dispose ();
			}
			constraints.Clear ();

			bool selected = Superview is NSTableRowView rowView && rowView.Selected;
			var wrapper = (MacObjectValueNode) ObjectValue;
			var editable = TreeView.GetCanEditNode (Node);
			var textColor = NSColor.ControlText;
			string evaluateStatusIcon = null;
			string valueButtonText = null;
			var showViewerButton = false;
			Color? previewColor = null;
			bool showSpinner = false;
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
				strval = Node.Value ?? string.Empty;
				int i = strval.IndexOf ('\n');
				if (i != -1)
					strval = strval.Substring (0, i);
				if (!selected)
					textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueErrorText));
			} else if (Node.IsImplicitNotSupported) {
				strval = string.Empty;
				if (!selected)
					textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
				if (Node.CanRefresh)
					valueButtonText = GettextCatalog.GetString ("Show Value");
			} else if (Node.IsEvaluating) {
				strval = GettextCatalog.GetString ("Evaluating\u2026");
				showSpinner = true;
				if (!selected)
					textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
			} else if (Node.IsEnumerable) {
				if (Node is ShowMoreValuesObjectValueNode) {
					valueButtonText = GettextCatalog.GetString ("Show More");
				} else {
					valueButtonText = GettextCatalog.GetString ("Show Values");
				}
				strval = string.Empty;
			} else if (Node is AddNewExpressionObjectValueNode) {
				strval = string.Empty;
				editable = false;
			} else {
				strval = TreeView.Controller.GetDisplayValueWithVisualisers (Node, out showViewerButton);

				if (!selected && TreeView.Controller.GetNodeHasChangedSinceLastCheckpoint (Node))
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

			OptimalWidth = MarginSize;

			// First item: Status Icon -or- Spinner
			if (evaluateStatusIcon != null) {
				statusIcon.Image = GetImage (evaluateStatusIcon, Gtk.IconSize.Menu, selected);
				statusIcon.AccessibilityTitle = ObjectValueTreeViewController.GetAccessibilityTitleForIcon (
					evaluateStatusIcon,
					GettextCatalog.GetString ("Object Value"));
				if (!statusIconVisible) {
					AddSubview (statusIcon);
					statusIconVisible = true;
				}

				constraints.Add (statusIcon.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				constraints.Add (statusIcon.WidthAnchor.ConstraintEqualToConstant (ImageSize));
				constraints.Add (statusIcon.HeightAnchor.ConstraintEqualToConstant (ImageSize));
				views.Add (statusIcon);

				OptimalWidth += ImageSize;
				OptimalWidth += RowCellSpacing;
			} else if (statusIconVisible) {
				statusIcon.RemoveFromSuperview ();
				statusIconVisible = false;
			}

			if (showSpinner) {
				if (!spinnerVisible) {
					AddSubview (spinner);
					spinner.StartAnimation (this);
					spinnerVisible = true;
				}

				constraints.Add (spinner.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				constraints.Add (spinner.WidthAnchor.ConstraintEqualToConstant (ImageSize));
				constraints.Add (spinner.HeightAnchor.ConstraintEqualToConstant (ImageSize));
				views.Add (spinner);

				OptimalWidth += ImageSize;
				OptimalWidth += RowCellSpacing;
			} else if (spinnerVisible) {
				spinner.RemoveFromSuperview ();
				spinner.StopAnimation (this);
				spinnerVisible = false;
			}

			// Second Item: Color Preview
			if (previewColor.HasValue) {
				colorPreview.Layer.BackgroundColor = GetCGColor (previewColor.Value);

				if (!colorPreviewVisible) {
					AddSubview (colorPreview);
					colorPreviewVisible = true;
				}

				constraints.Add (colorPreview.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				constraints.Add (colorPreview.WidthAnchor.ConstraintEqualToConstant (ImageSize));
				constraints.Add (colorPreview.HeightAnchor.ConstraintEqualToConstant (ImageSize));
				views.Add (colorPreview);

				OptimalWidth += ImageSize;
				OptimalWidth += RowCellSpacing;
			} else if (colorPreviewVisible) {
				colorPreview.RemoveFromSuperview ();
				colorPreviewVisible = false;
			}

			// Third Item: Value Button
			if (valueButtonText != null && !((MacObjectValueNode) ObjectValue).HideValueButton) {
				valueButton.Title = valueButtonText;
				UpdateFont (valueButton, -3);
				valueButton.SizeToFit ();

				if (!valueButtonVisible) {
					AddSubview (valueButton);
					valueButtonVisible = true;
				}

				constraints.Add (valueButton.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				views.Add (valueButton);

				OptimalWidth += valueButton.Frame.Width;
				OptimalWidth += RowCellSpacing;
			} else if (valueButtonVisible) {
				valueButton.RemoveFromSuperview ();
				valueButtonVisible = false;
			}

			// Fourth Item: Viewer Button
			if (showViewerButton) {
				if (!viewerButtonVisible) {
					AddSubview (viewerButton);
					viewerButtonVisible = true;
				}

				constraints.Add (viewerButton.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				constraints.Add (viewerButton.WidthAnchor.ConstraintEqualToConstant (viewerButton.Image.Size.Width));
				constraints.Add (viewerButton.HeightAnchor.ConstraintEqualToConstant (viewerButton.Image.Size.Height));
				views.Add (viewerButton);

				OptimalWidth += viewerButton.Frame.Width;
				OptimalWidth += RowCellSpacing;
			} else if (viewerButtonVisible) {
				viewerButton.RemoveFromSuperview ();
				viewerButtonVisible = false;
			}

			// Fifth Item: Text Value
			TextField.StringValue = strval;
			TextField.TextColor = textColor;
			TextField.Editable = editable;
			UpdateFont (TextField);
			TextField.SizeToFit ();

			OptimalWidth += GetWidthForString (TextField.Font, strval);

			constraints.Add (TextField.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
			views.Add (TextField);

			// lay out our views...
			var leadingAnchor = LeadingAnchor;

			for (int i = 0; i < views.Count; i++) {
				var view = views[i];

				constraints.Add (view.LeadingAnchor.ConstraintEqualToAnchor (leadingAnchor, i == 0 ? MarginSize : RowCellSpacing));
				leadingAnchor = view.TrailingAnchor;
			}

			constraints.Add (TextField.TrailingAnchor.ConstraintEqualToAnchor (TrailingAnchor, -MarginSize));

			foreach (var constraint in constraints)
				constraint.Active = true;

			OptimalWidth += MarginSize;

			wrapper.OptimalValueFont = TreeView.CustomFont;
			wrapper.OptimalValueWidth = OptimalWidth;
		}

		public static nfloat GetOptimalWidth (MacObjectValueTreeView treeView, ObjectValueNode node, bool hideValueButton)
		{
			nfloat optimalWidth = MarginSize;
			string evaluateStatusIcon = null;
			string valueButtonText = null;
			var showViewerButton = false;
			Color? previewColor = null;
			bool showSpinner = false;
			string strval;

			if (node.IsUnknown) {
				if (treeView.DebuggerService.Frame != null) {
					strval = GettextCatalog.GetString ("The name '{0}' does not exist in the current context.", node.Name);
				} else {
					strval = string.Empty;
				}
				evaluateStatusIcon = Ide.Gui.Stock.Warning;
			} else if (node.IsError || node.IsNotSupported) {
				evaluateStatusIcon = Ide.Gui.Stock.Warning;
				strval = node.Value ?? string.Empty;
				int i = strval.IndexOf ('\n');
				if (i != -1)
					strval = strval.Substring (0, i);
			} else if (node.IsImplicitNotSupported) {
				strval = string.Empty;//val.Value; with new "Show Value" button we don't want to display message "Implicit evaluation is disabled"
				if (node.CanRefresh)
					valueButtonText = GettextCatalog.GetString ("Show Value");
			} else if (node.IsEvaluating) {
				strval = GettextCatalog.GetString ("Evaluating\u2026");
				showSpinner = true;
			} else if (node.IsEnumerable) {
				if (node is ShowMoreValuesObjectValueNode) {
					valueButtonText = GettextCatalog.GetString ("Show More");
				} else {
					valueButtonText = GettextCatalog.GetString ("Show Values");
				}
				strval = string.Empty;
			} else if (node is AddNewExpressionObjectValueNode) {
				strval = string.Empty;
			} else {
				strval = treeView.Controller.GetDisplayValueWithVisualisers (node, out showViewerButton);

				var val = node.GetDebuggerObjectValue ();
				if (val != null && !val.IsNull && DebuggingService.HasGetConverter<Color> (val)) {
					try {
						previewColor = DebuggingService.GetGetConverter<Color> (val).GetValue (val);
					} catch {
						previewColor = null;
					}
				}
			}

			strval = strval.Replace ("\r\n", " ").Replace ("\n", " ");

			// First item: Status Icon -or- Spinner
			if (evaluateStatusIcon != null) {
				optimalWidth += ImageSize;
				optimalWidth += RowCellSpacing;
			}

			if (showSpinner) {
				optimalWidth += ImageSize;
				optimalWidth += RowCellSpacing;
			}

			// Second Item: Color Preview
			if (previewColor.HasValue) {
				optimalWidth += ImageSize;
				optimalWidth += RowCellSpacing;
			}

			// Third Item: Value Button
			if (valueButtonText != null && !hideValueButton) {
				optimalWidth += GetWidthForString (treeView.CustomFont, valueButtonText, -3);
				optimalWidth += RowCellSpacing;
			}

			// Fourth Item: Viewer Button
			if (showViewerButton) {
				optimalWidth += treeView.CompactView ? CompactImageSize : ImageSize;
				optimalWidth += RowCellSpacing;
			}

			// Fifth Item: Text Value
			optimalWidth += GetWidthForString (treeView.CustomFont, strval);
			optimalWidth += MarginSize;

			return optimalWidth;
		}

		void OnValueButtonActivated (object sender, EventArgs e)
		{
			if (Node == null)
				return;

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
				foreach (var constraint in constraints)
					constraint.Dispose ();
				constraints.Clear ();
				disposed = true;
			}

			base.Dispose (disposing);
		}
	}
}
