//
// MacDebuggerObjectNameView.cs
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

using AppKit;

using MonoDevelop.Core;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// The NSTableViewCell used for the "Name" column.
	/// </summary>
	class MacDebuggerObjectNameView : MacDebuggerObjectCellViewBase
	{
		readonly NSLayoutConstraint textFieldTrailingAnchorConstraint;
		PreviewButtonIcon currentIcon;
		bool previewIconVisible;
		bool textChanged;
		bool disposed;

		public MacDebuggerObjectNameView (MacObjectValueTreeView treeView) : base (treeView, "name")
		{
			ImageView = new NSImageView {
				TranslatesAutoresizingMaskIntoConstraints = false
			};

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

			AddSubview (ImageView);
			AddSubview (TextField);

			PreviewButton = new NSButton {
				TranslatesAutoresizingMaskIntoConstraints = false,
				BezelStyle = NSBezelStyle.Inline,
				Bordered = false
			};
			PreviewButton.Activated += OnPreviewButtonClicked;

			ImageView.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
			ImageView.LeadingAnchor.ConstraintEqualToAnchor (LeadingAnchor, MarginSize).Active = true;
			ImageView.WidthAnchor.ConstraintEqualToConstant (ImageSize).Active = true;
			ImageView.HeightAnchor.ConstraintEqualToConstant (ImageSize).Active = true;

			TextField.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
			TextField.LeadingAnchor.ConstraintEqualToAnchor (ImageView.TrailingAnchor, RowCellSpacing).Active = true;
			textFieldTrailingAnchorConstraint = TextField.TrailingAnchor.ConstraintEqualToAnchor (TrailingAnchor, -MarginSize);
			textFieldTrailingAnchorConstraint.Active = true;
		}

		public MacDebuggerObjectNameView (IntPtr handle) : base (handle)
		{
		}

		public NSButton PreviewButton {
			get; private set;
		}

		protected override void UpdateContents ()
		{
			if (Node == null)
				return;

			var iconName = ObjectValueTreeViewController.GetIcon (Node.Flags);
			ImageView.Image = GetImage (iconName, Gtk.IconSize.Menu);

			var editable = TreeView.AllowWatchExpressions && Node.Parent is RootObjectValueNode;
			var textColor = NSColor.ControlText;
			var placeholder = string.Empty;
			var name = Node.Name;

			if (Node.IsUnknown) {
				if (TreeView.DebuggerService.Frame != null)
					textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
			} else if (Node.IsError || Node.IsNotSupported) {
			} else if (Node.IsImplicitNotSupported) {
			} else if (Node.IsEvaluating) {
				if (Node.GetIsEvaluatingGroup ()) {
					textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
					name = Node.Name;
				}
			} else if (Node.IsEnumerable) {
			} else if (Node is AddNewExpressionObjectValueNode) {
				placeholder = GettextCatalog.GetString ("Add new expression");
				name = string.Empty;
				editable = true;
			} else if (TreeView.Controller.GetNodeHasChangedSinceLastCheckpoint (Node)) {
				textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueModifiedText));
			}

			TextField.AttributedStringValue = GetAttributedString (name);
			TextField.PlaceholderString = placeholder;
			TextField.TextColor = textColor;
			TextField.Editable = editable;

			if (MacObjectValueTreeView.ValidObjectForPreviewIcon (Node)) {
				SetPreviewButtonIcon (PreviewButtonIcon.Hidden);

				if (!previewIconVisible) {
					AddSubview (PreviewButton);
					previewIconVisible = true;

					textFieldTrailingAnchorConstraint.Active = false;
					PreviewButton.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor).Active = true;
					PreviewButton.LeadingAnchor.ConstraintEqualToAnchor (TextField.TrailingAnchor, RowCellSpacing).Active = true;
					PreviewButton.WidthAnchor.ConstraintEqualToConstant (ImageSize).Active = true;
					PreviewButton.HeightAnchor.ConstraintEqualToConstant (ImageSize).Active = true;
				}
			} else {
				textFieldTrailingAnchorConstraint.Active = true;
				PreviewButton.RemoveFromSuperview ();
				previewIconVisible = false;
			}
		}

		public void SetPreviewButtonIcon (PreviewButtonIcon icon)
		{
			if (!previewIconVisible || icon == currentIcon)
				return;

			var name = ObjectValueTreeViewController.GetPreviewButtonIcon (icon);
			PreviewButton.Image = GetImage (name, Gtk.IconSize.Menu);
			currentIcon = icon;

			SetNeedsDisplayInRect (PreviewButton.Frame);
		}

		void OnPreviewButtonClicked (object sender, EventArgs e)
		{
			if (!TreeView.DebuggerService.CanQueryDebugger || PreviewWindowManager.IsVisible)
				return;

			if (!MacObjectValueTreeView.ValidObjectForPreviewIcon (Node))
				return;

			var bounds = ConvertRectFromView (PreviewButton.Bounds, PreviewButton);
			var buttonArea = new Gdk.Rectangle ((int) bounds.X, (int) bounds.Y, (int) bounds.Width, (int) bounds.Height);
			var val = Node.GetDebuggerObjectValue ();

			SetPreviewButtonIcon (PreviewButtonIcon.Active);

			// FIXME: this crashes because Mac native widgets aren't handled...
			//TreeView.DebuggingService.ShowPreviewVisualizer (val, this, buttonArea);
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

			var expression = TextField.StringValue.Trim ();

			if (Node is AddNewExpressionObjectValueNode) {
				if (expression.Length > 0)
					TreeView.OnExpressionAdded (expression);
			} else {
				TreeView.OnExpressionEdited (Node, expression);
			}
		}

		void OnTextChanged (object sender, EventArgs e)
		{
			textChanged = true;
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				PreviewButton.Activated -= OnPreviewButtonClicked;
				TextField.EditingBegan -= OnEditingBegan;
				TextField.EditingEnded -= OnEditingEnded;
				TextField.Changed -= OnTextChanged;
				disposed = true;
			}

			base.Dispose (disposing);
		}
	}
}
