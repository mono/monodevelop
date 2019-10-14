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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using AppKit;
using Foundation;

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;

using Microsoft.VisualStudio.Language.Intellisense.AsyncCompletion;

namespace MonoDevelop.Debugger
{
	/// <summary>
	/// The NSTableViewCell used for the "Name" column.
	/// </summary>
	class MacDebuggerObjectNameView : MacDebuggerObjectCellViewBase
	{
		sealed class CodeCompletionTextFieldDataSource : NSComboBoxDataSource
		{
			List<CompletionItem> completionItems;

			public CodeCompletionTextFieldDataSource ()
			{
				completionItems = new List<CompletionItem> ();
			}

			public void SetCompletionData (string expression, CompletionData completionData)
			{
				completionItems.Clear ();

				if (completionData != null) {
					foreach (var item in completionData.Items) {
						if (item.Name.StartsWith (expression, StringComparison.OrdinalIgnoreCase))
							completionItems.Add (item);
					}
				}
			}

			public override nint ItemCount (NSComboBox comboBox)
			{
				return completionItems.Count;
			}

			public override nint IndexOfItem (NSComboBox comboBox, string value)
			{
				int best = -1;

				for (int i = 0; i < completionItems.Count; i++) {
					if (completionItems[i].Name.Equals (value, StringComparison.Ordinal))
						return i;

					if (best == -1 && completionItems[i].Name.Equals (value, StringComparison.OrdinalIgnoreCase))
						best = i;
				}

				return best;
			}

			public override NSObject ObjectValueForItem (NSComboBox comboBox, nint index)
			{
				if (index >= completionItems.Count)
					return null;

				return new NSString (completionItems[(int) index].Name);
			}

			public override string CompletedString (NSComboBox comboBox, string uncompletedString)
			{
				string caseInsensitiveMatch = null;
				string caseInsensitiveStartsWith = null;
				string startsWith = null;

				for (int i = 0; i < completionItems.Count; i++) {
					var completionText = completionItems[i].Name;

					if (completionText.Equals (uncompletedString, StringComparison.Ordinal))
						return completionText;

					if (caseInsensitiveMatch == null && completionText.Equals (uncompletedString, StringComparison.OrdinalIgnoreCase)) {
						caseInsensitiveMatch = completionText;
						continue;
					}

					if (startsWith == null && completionText.StartsWith (uncompletedString, StringComparison.Ordinal)) {
						startsWith = completionText;
						continue;
					}

					if (caseInsensitiveStartsWith == null && completionText.StartsWith (uncompletedString, StringComparison.OrdinalIgnoreCase)) {
						caseInsensitiveStartsWith = completionText;
						continue;
					}
				}

				if (caseInsensitiveMatch != null)
					return caseInsensitiveMatch;

				if (startsWith != null)
					return startsWith;

				if (caseInsensitiveStartsWith != null)
					return caseInsensitiveStartsWith;

				return uncompletedString;
			}
		}

		sealed class CodeCompletionTextField : NSComboBox
		{
			readonly CodeCompletionTextFieldDataSource dataSource;
			readonly MacDebuggerObjectNameView nameView;
			CancellationTokenSource cts;
			string oldValue, newValue;
			bool editing;

			public CodeCompletionTextField (MacDebuggerObjectNameView nameView)
			{
				dataSource = new CodeCompletionTextFieldDataSource ();
				UsesDataSource = true;
				DataSource = dataSource;
				Completes = true;

				this.nameView = nameView;
			}

			public override bool AcceptsFirstResponder ()
			{
				if (!base.AcceptsFirstResponder ())
					return false;

				// Note: The MacDebuggerObjectNameView sets the PlaceholderAttributedString property
				// so that it can control the font color and the baseline offset. Unfortunately, this
				// breaks once the NSTextField is in "edit" mode because the placeholder text ends up
				// being rendered as black instead of gray. By reverting to using the basic
				// PlaceholderString property once we enter "edit" mode, it fixes the text color.
				var placeholder = PlaceholderAttributedString;

				if (placeholder != null)
					PlaceholderString = placeholder.Value;

				TextColor = NSColor.ControlText;

				return true;
			}

			public override void DidBeginEditing (NSNotification notification)
			{
				base.DidBeginEditing (notification);
				nameView.TreeView.OnStartEditing ();
				oldValue = newValue = StringValue.Trim ();
				editing = true;
			}

			async Task UpdateCodeCompletionItemsAsync (string expression)
			{
				try {
					cts?.Cancel ();
					cts?.Dispose ();
					cts = new CancellationTokenSource ();
					var completionData = await nameView.TreeView.DebuggerService.GetCompletionDataAsync (expression, cts.Token);
					dataSource.SetCompletionData (expression, completionData);
					NoteNumberOfItemsChanged ();
				} catch (OperationCanceledException) {
				}
			}

			public override void DidChange (NSNotification notification)
			{
				newValue = StringValue.Trim ();
				base.DidChange (notification);
				UpdateCodeCompletionItemsAsync (newValue).Ignore ();
			}

			public override void DidEndEditing (NSNotification notification)
			{
				base.DidEndEditing (notification);

				if (!editing)
					return;

				cts?.Cancel ();
				cts?.Dispose ();
				cts = null;

				editing = false;

				nameView.TreeView.OnEndEditing ();

				if (nameView.Node is AddNewExpressionObjectValueNode) {
					if (newValue.Length > 0)
						nameView.TreeView.OnExpressionAdded (newValue);
				} else if (newValue != oldValue) {
					nameView.TreeView.OnExpressionEdited (nameView.Node, newValue);
				}

				oldValue = newValue = null;
			}

			protected override void Dispose (bool disposing)
			{
				if (disposing) {
					cts?.Cancel ();
					cts?.Dispose ();
					nameView.Dispose ();
				}

				base.Dispose (disposing);
			}
		}

		readonly List<NSLayoutConstraint> constraints = new List<NSLayoutConstraint> ();
		readonly CodeCompletionTextField codeCompletionTextField;
		bool codeCompletionTextFieldVisible;
		PreviewButtonIcon currentIcon;
		bool previewIconVisible;
		bool textFieldVisible;
		bool disposed;

		public MacDebuggerObjectNameView (MacObjectValueTreeView treeView) : base (treeView, "name")
		{
			ImageView = new NSImageView {
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			AddSubview (ImageView);

			TextField = new NSTextField {
				AutoresizingMask = NSViewResizingMask.WidthSizable,
				TranslatesAutoresizingMaskIntoConstraints = false,
				DrawsBackground = false,
				Bordered = false,
				Editable = false
			};
			TextField.Cell.UsesSingleLineMode = true;
			TextField.Cell.Wraps = false;
			textFieldVisible = true;
			AddSubview (TextField);

			codeCompletionTextField = new CodeCompletionTextField (this) {
				AutoresizingMask = NSViewResizingMask.WidthSizable,
				TranslatesAutoresizingMaskIntoConstraints = false,
				DrawsBackground = false,
				Bordered = false,
				Editable = true
			};
			codeCompletionTextField.Cell.UsesSingleLineMode = true;
			codeCompletionTextField.Cell.Wraps = false;

			PreviewButton = new NSButton {
				TranslatesAutoresizingMaskIntoConstraints = false,
				Image = GetImage ("md-empty", Gtk.IconSize.Menu),
				BezelStyle = NSBezelStyle.Inline,
				Bordered = false
			};
			PreviewButton.Activated += OnPreviewButtonClicked;
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

			foreach (var constraint in constraints) {
				constraint.Active = false;
				constraint.Dispose ();
			}
			constraints.Clear ();

			OptimalWidth = MarginSize;

			bool selected = Superview is NSTableRowView rowView && rowView.Selected;
			var iconName = ObjectValueTreeViewController.GetIcon (Node.Flags);
			ImageView.Image = GetImage (iconName, Gtk.IconSize.Menu, selected);
			constraints.Add (ImageView.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
			constraints.Add (ImageView.LeadingAnchor.ConstraintEqualToAnchor (LeadingAnchor, MarginSize));
			constraints.Add (ImageView.WidthAnchor.ConstraintEqualToConstant (ImageSize));
			constraints.Add (ImageView.HeightAnchor.ConstraintEqualToConstant (ImageSize));

			OptimalWidth += ImageView.Image.Size.Width;
			OptimalWidth += RowCellSpacing;

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
				if (Node.GetIsEvaluatingGroup ())
					textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueDisabledText));
			} else if (Node.IsEnumerable) {
			} else if (Node is AddNewExpressionObjectValueNode) {
				placeholder = GettextCatalog.GetString ("Add new expression");
				name = string.Empty;
				editable = true;
			} else if (TreeView.Controller.GetNodeHasChangedSinceLastCheckpoint (Node)) {
				textColor = NSColor.FromCGColor (GetCGColor (Styles.ObjectValueTreeValueModifiedText));
			}

			NSTextField textField;

			if (editable) {
				codeCompletionTextField.PlaceholderAttributedString = GetAttributedPlaceholderString (placeholder);
				textField = codeCompletionTextField;

				if (!codeCompletionTextFieldVisible) {
					TextField.RemoveFromSuperview ();
					textFieldVisible = false;
					codeCompletionTextFieldVisible = true;
					AddSubview (codeCompletionTextField);
				}
			} else {
				textField = TextField;

				if (!textFieldVisible) {
					codeCompletionTextField.RemoveFromSuperview ();
					codeCompletionTextFieldVisible = false;
					textFieldVisible = true;
					AddSubview (TextField);
				}
			}

			textField.AttributedStringValue = GetAttributedString (name);
			textField.TextColor = textColor;
			UpdateFont (textField);
			textField.SizeToFit ();

			OptimalWidth += textField.Frame.Width;

			constraints.Add (textField.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
			constraints.Add (textField.LeadingAnchor.ConstraintEqualToAnchor (ImageView.TrailingAnchor, RowCellSpacing));

			if (MacObjectValueTreeView.ValidObjectForPreviewIcon (Node)) {
				SetPreviewButtonIcon (PreviewButtonIcon.Hidden);

				if (!previewIconVisible) {
					AddSubview (PreviewButton);
					previewIconVisible = true;
				}

				constraints.Add (textField.WidthAnchor.ConstraintGreaterThanOrEqualToConstant (textField.Frame.Width));
				constraints.Add (PreviewButton.CenterYAnchor.ConstraintEqualToAnchor (CenterYAnchor));
				constraints.Add (PreviewButton.LeadingAnchor.ConstraintEqualToAnchor (textField.TrailingAnchor, RowCellSpacing));
				constraints.Add (PreviewButton.WidthAnchor.ConstraintEqualToConstant (ImageSize));
				constraints.Add (PreviewButton.HeightAnchor.ConstraintEqualToConstant (ImageSize));

				OptimalWidth += RowCellSpacing;
				OptimalWidth += PreviewButton.Frame.Width;
			} else {
				if (previewIconVisible) {
					PreviewButton.RemoveFromSuperview ();
					previewIconVisible = false;
				}

				constraints.Add (textField.TrailingAnchor.ConstraintEqualToAnchor (TrailingAnchor, -MarginSize));
			}

			foreach (var constraint in constraints)
				constraint.Active = true;

			OptimalWidth += MarginSize;
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

			// convert the buttons frame to window coords
			var buttonLocation = PreviewButton.ConvertPointToView (CoreGraphics.CGPoint.Empty, null);

			// now convert the frame to absolute screen coordinates
			buttonLocation = PreviewButton.Window.ConvertPointToScreen (buttonLocation);

			var nativeRoot = MacInterop.GtkQuartz.GetWindow (IdeApp.Workbench.RootWindow);

			// convert to root window coordinates
			buttonLocation = nativeRoot.ConvertPointFromScreen (buttonLocation);
			// the Cocoa Y axis is flipped, convert to Gtk
			buttonLocation.Y = nativeRoot.Frame.Height - buttonLocation.Y;
			// Gtk coords don't include the toolbar and decorations ofsset, so substract it
			buttonLocation.Y -= nativeRoot.Frame.Height - nativeRoot.ContentView.Frame.Height;

			int width = (int) PreviewButton.Frame.Width;
			int height = (int) PreviewButton.Frame.Height;

			var buttonArea = new Gdk.Rectangle ((int) buttonLocation.X, (int) buttonLocation.Y, width, height);
			var val = Node.GetDebuggerObjectValue ();

			SetPreviewButtonIcon (PreviewButtonIcon.Active);

			DebuggingService.ShowPreviewVisualizer (val, IdeApp.Workbench.RootWindow, buttonArea);
		}

		protected override void Dispose (bool disposing)
		{
			if (disposing && !disposed) {
				PreviewButton.Activated -= OnPreviewButtonClicked;
				foreach (var constraint in constraints)
					constraint.Dispose ();
				constraints.Clear ();

				codeCompletionTextField.Dispose ();
				PreviewButton.Dispose ();
				TextField.Dispose ();
				ImageView.Dispose ();
				disposed = true;
			}

			base.Dispose (disposing);
		}
	}
}
