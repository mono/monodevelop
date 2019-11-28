//
// Copyright (c) Microsoft Corp. (https://www.microsoft.com)
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
using Foundation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Adornments;
using Microsoft.VisualStudio.Text.Editor;
using Mono.Debugging.Client;
using MonoDevelop.Components;
using MonoDevelop.Components.Mac;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TextEditing;
using GettextCatalog = MonoDevelop.Core.GettextCatalog;

namespace MonoDevelop.Debugger.VSTextView.ExceptionCaught
{
	public class ExceptionCaughtAdornmentManager : IDisposable
	{
		readonly ICocoaViewFactory cocoaViewFactory;
		readonly ICocoaTextView textView;
		private readonly string filePath;
		readonly IXPlatAdornmentLayer _exceptionCaughtLayer;
		FileLineExtension extension;
		NSPanel exceptionCaughtButtonWindow;

		public ExceptionCaughtAdornmentManager (ICocoaViewFactory cocoaViewFactory, ICocoaTextView textView)
		{
			filePath = textView.TextDataModel.DocumentBuffer.GetFilePathOrNull ();
			if (filePath == null)
				return;

			IdeServices.TextEditorService.FileExtensionAdded += FileExtensionAdded;
			IdeServices.TextEditorService.FileExtensionRemoved += FileExtensionRemoved;
			_exceptionCaughtLayer = textView.GetXPlatAdornmentLayer ("ExceptionCaught");

			this.cocoaViewFactory = cocoaViewFactory;

			this.textView = textView;
			this.textView.LayoutChanged += TextView_LayoutChanged;

			foreach (var ext in IdeServices.TextEditorService.GetFileExtensions (filePath))
				FileExtensionAdded (null, new FileExtensionEventArgs  { Extension = ext });
		}

		private void FileExtensionRemoved (object sender, FileExtensionEventArgs e)
		{
			if (e.Extension == extension) {
				extension = null;
				_exceptionCaughtLayer.RemoveAllAdornments ();
				if (exceptionCaughtButtonWindow != null) {
					exceptionCaughtButtonWindow.Close ();
					exceptionCaughtButtonWindow = null;
				}
			}
		}

		private void FileExtensionAdded (object sender, FileExtensionEventArgs e)
		{
			if (e.Extension is FileLineExtension fileLineExtension && fileLineExtension.File == filePath) {
				RenderAdornment (fileLineExtension);
			}
		}
		ITrackingSpan trackingSpan;
		private void RenderAdornment (FileLineExtension fileLineExtension)
		{
			NSView view;
			bool mini;
			if (fileLineExtension is ExceptionCaughtButton button) {
				mini = false;
				view = CreateButton (cocoaViewFactory, button);
			} else if (fileLineExtension is ExceptionCaughtMiniButton miniButton) {
				mini = true;
				view = CreateMiniButton (cocoaViewFactory, miniButton);
			} else
				return;
			if (extension != fileLineExtension) {
				extension = fileLineExtension;
				var newSpan = textView.TextSnapshot.SpanFromMDColumnAndLine (extension.Line, extension.Column, extension.Line, extension.Column);
				trackingSpan = textView.TextSnapshot.CreateTrackingSpan (newSpan, SpanTrackingMode.EdgeInclusive);
			}
			var span = trackingSpan.GetSpan (textView.TextSnapshot);
			if (textView.TextViewLines == null)
				return;
			if (!textView.TextViewLines.FormattedSpan.Contains (span.End))
				return;
			_exceptionCaughtLayer.RemoveAllAdornments ();
			if (exceptionCaughtButtonWindow != null) {
				exceptionCaughtButtonWindow.Close ();
				exceptionCaughtButtonWindow = null;
			}
			var charBound = textView.TextViewLines.GetCharacterBounds (span.End);
			if (mini) {
				try {
					view.SetFrameOrigin (new CGPoint (
					Math.Round (charBound.Left),
					Math.Round (charBound.TextTop - charBound.TextHeight / 2 - view.Frame.Height / 2)));
				} catch (Exception e) {
					view.SetFrameOrigin (default);
					LoggingService.LogInternalError ("https://vsmac.dev/923058", e);
				}
				_exceptionCaughtLayer.AddAdornment (XPlatAdornmentPositioningBehavior.TextRelative, span, null, view, null);
			} else {
				var editorWindow = textView.VisualElement.Window;
				var pointOnScreen = editorWindow.ConvertPointToScreen (textView.VisualElement.ConvertPointToView (new CGPoint (charBound.Left, charBound.TextTop), null));
				exceptionCaughtButtonWindow = new NSPanel (CGRect.Empty, NSWindowStyle.Borderless, NSBackingStore.Buffered, false);
				exceptionCaughtButtonWindow.AccessibilityRole = NSAccessibilityRoles.PopoverRole;
				editorWindow.AddChildWindow (exceptionCaughtButtonWindow, NSWindowOrderingMode.Above);
				exceptionCaughtButtonWindow.IsOpaque = false;
				exceptionCaughtButtonWindow.BackgroundColor = NSColor.Clear;
				exceptionCaughtButtonWindow.HasShadow = true;
				exceptionCaughtButtonWindow.ContentView = view;
				var fittingSize = view.FittingSize;
				var x = Math.Min (editorWindow.Screen.VisibleFrame.Width - fittingSize.Width, pointOnScreen.X);
				var y = Math.Max (0, pointOnScreen.Y - fittingSize.Height / 2);
				exceptionCaughtButtonWindow.SetFrame (new CGRect (x, y, fittingSize.Width, fittingSize.Height), true);
				exceptionCaughtButtonWindow.MakeKeyAndOrderFront (null);
			}
		}

		private void TextView_LayoutChanged (object sender, TextViewLayoutChangedEventArgs e)
		{
			RenderAdornment (extension);
		}

		static NSImage GetIcon(string iconName, bool tint)
		{
			var image = ImageService.GetIcon (iconName)?.ToNSImage ();
			if (image != null) {
				image.Template = true;
				if (tint)
					image = WithTint (image, NSColor.SystemOrangeColor);
			}
			return image;
		}

		class VSHandButton : NSButton
		{
			public VSHandButton()
			{
				Bordered = false;

				AddTrackingArea (new NSTrackingArea (
					default,
					NSTrackingAreaOptions.ActiveInKeyWindow |
					NSTrackingAreaOptions.InVisibleRect |
					NSTrackingAreaOptions.MouseEnteredAndExited |
					NSTrackingAreaOptions.CursorUpdate,
					this,
					null));
			}

			public override void CursorUpdate (NSEvent theEvent)
				=> NSCursor.PointingHandCursor.Set ();
		}

		class VSLinkLabelButton : VSHandButton
		{
			public VSLinkLabelButton(string title)
			{
				// terminate the string with a zero-width-non-breaking-space to
				// work around a bug in AppKit where the last character does not
				// have the underline style applied to it when rendered.
				var attributedTitle = new NSMutableAttributedString (title + "\uFEFF");
				var range = new NSRange (0, attributedTitle.Length);
				attributedTitle.AddAttribute (NSStringAttributeKey.Link, new NSUrl ("#"), range);
				AttributedTitle = attributedTitle;
			}
		}

		class ExceptionCaughtPopoverViewContentView : NSView
		{
			public ExceptionCaughtPopoverViewContentView (NSView contentView)
			{
				AcceptsTouchEvents = true;

				AddTrackingArea (new NSTrackingArea (
					default,
					NSTrackingAreaOptions.ActiveInKeyWindow |
					NSTrackingAreaOptions.InVisibleRect |
					NSTrackingAreaOptions.MouseEnteredAndExited |
					NSTrackingAreaOptions.MouseMoved |
					NSTrackingAreaOptions.CursorUpdate,
					this,
					null));

				AddSubview (contentView);
				contentView.TranslatesAutoresizingMaskIntoConstraints = false;
				LeadingAnchor.ConstraintEqualToAnchor (contentView.LeadingAnchor).Active = true;
				TrailingAnchor.ConstraintEqualToAnchor (contentView.TrailingAnchor).Active = true;
				TopAnchor.ConstraintEqualToAnchor (contentView.TopAnchor).Active = true;
				BottomAnchor.ConstraintEqualToAnchor (contentView.BottomAnchor).Active = true;
			}

			public override void CursorUpdate (NSEvent theEvent)
				=> NSCursor.ArrowCursor.Set ();

			public override bool AcceptsFirstMouse (NSEvent theEvent)
				=> true;

			public override void MouseEntered (NSEvent theEvent) { }
			public override void MouseExited (NSEvent theEvent) { }
			public override void MouseMoved (NSEvent theEvent) { }
		}

		static NSView CreateMiniButton (ICocoaViewFactory cocoaViewFactory, ExceptionCaughtMiniButton miniButton)
		{
			var image = GetIcon ("md-exception-caught-template", tint: true);

			var nsButton = new VSHandButton {
				Image = image,
				TranslatesAutoresizingMaskIntoConstraints = false
			};

			nsButton.SetContentCompressionResistancePriority (1, NSLayoutConstraintOrientation.Horizontal);
			nsButton.SetContentCompressionResistancePriority (1, NSLayoutConstraintOrientation.Vertical);
			nsButton.WidthAnchor.ConstraintEqualToConstant (image.Size.Width + 8).Active = true;
			nsButton.HeightAnchor.ConstraintEqualToConstant (image.Size.Height + 8).Active = true;

			nsButton.Activated += delegate {
				miniButton.dlg.ShowButton ();
			};

			var materialView = cocoaViewFactory.CreateMaterialView ();
			materialView.ContentView = new ExceptionCaughtPopoverViewContentView (nsButton);
			materialView.CornerRadius = 3;
			materialView.Material =  NSVisualEffectMaterial.WindowBackground;
			return (NSView)materialView;
		}

		static NSView CreateButton (ICocoaViewFactory cocoaViewFactory, ExceptionCaughtButton button)
		{
			var box = new NSGridView {
				ColumnSpacing = 8,
				RowSpacing = 8
			};

			var lightningImage = new NSImageView {
				Image = GetIcon ("md-exception-caught-template", tint: true)
			};
			box.AddColumn (new [] { lightningImage });

			var typeLabel = CreateTextField ();
			var messageLabel = CreateTextField ();
			var detailsButton = new VSLinkLabelButton (
				GettextCatalog.GetString ("Show Details"));
			detailsButton.Activated += (o, e) => button.dlg.ShowDialog ();
			box.AddColumn (new NSView[] { typeLabel, messageLabel, detailsButton });

			var closeButton = new NSButton {
				Image = GetIcon ("md-popup-close", tint: false),
				BezelStyle = NSBezelStyle.SmallSquare,
				Bordered = false
			};
			closeButton.SetButtonType (NSButtonType.MomentaryLightButton);
			closeButton.Activated += delegate {
				button.dlg.ShowMiniButton ();
			};
			box.AddColumn (new NSView [] { closeButton });

			button.exception.Changed += delegate {
				Runtime
					.RunInMainThread(()=> LoadData(button.exception, messageLabel, typeLabel))
					.Ignore();
			};

			LoadData (button.exception, messageLabel, typeLabel);

			box.GetCell (lightningImage).CustomPlacementConstraints = new [] {
				NSLayoutConstraint.Create (
					lightningImage, NSLayoutAttribute.CenterY,
					NSLayoutRelation.Equal,
					typeLabel, NSLayoutAttribute.CenterY,
					1, 0)
			};

			var materialView = cocoaViewFactory.CreateMaterialView ();
			materialView.ContentView = new ExceptionCaughtPopoverViewContentView (box);
			materialView.CornerRadius = 6;
			materialView.EdgeInsets = new NSEdgeInsets (6, 6, 6, 6);
			return (NSView)materialView;
		}

		static NSTextField CreateTextField ()
			=> new NSTextField {
				Alignment = NSTextAlignment.Natural,
				Selectable = true,
				Editable = false,
				DrawsBackground = false,
				Bezeled = false,
				PreferredMaxLayoutWidth = 400,
				Cell = {
					Wraps = true,
					LineBreakMode = NSLineBreakMode.ByWordWrapping,
					UsesSingleLineMode = false
				}
			};

		static void LoadData (ExceptionInfo exception, NSTextField messageLabel, NSTextField typeLabel)
		{
			if (!string.IsNullOrEmpty (exception.Message)) {
				messageLabel.Hidden = false;
				messageLabel.StringValue = exception.Message;
			} else {
				messageLabel.Hidden = true;
			}
			if (!string.IsNullOrEmpty (exception.Type)) {
				typeLabel.Hidden = false;
				typeLabel.AllowsEditingTextAttributes = true;
				typeLabel.AttributedStringValue = Xwt
					.FormattedText
					.FromMarkup (GettextCatalog.GetString ("<b>{0}</b> has been thrown", exception.Type))
					.ToAttributedString ((str, range) => {
						str.AddAttribute (NSStringAttributeKey.Font, NSFont.LabelFontOfSize (NSFont.LabelFontSize * 1.5f), range);
						str.AddAttribute (NSStringAttributeKey.ForegroundColor, NSColor.LabelColor, range);
					});
			} else {
				typeLabel.Hidden = true;
			}
		}

		public void Dispose ()
		{
			IdeServices.TextEditorService.FileExtensionAdded -= FileExtensionAdded;
			IdeServices.TextEditorService.FileExtensionRemoved -= FileExtensionRemoved;
		}

		// FIXME: move to an extensions class in MD.IDE or something
		static NSImage WithTint (NSImage sourceImage, NSColor tintColor)
		{
			if (tintColor == null)
				throw new ArgumentNullException (nameof (tintColor));

			if (sourceImage == null)
				return null;

			return NSImage.ImageWithSize (sourceImage.Size, false, rect =>
			{
				sourceImage.DrawInRect (rect, CGRect.Empty, NSCompositingOperation.SourceOver, 1f);
				tintColor.Set ();
				NSGraphics.RectFill (rect, NSCompositingOperation.SourceAtop);
				return true;
			});
		}
	}
}