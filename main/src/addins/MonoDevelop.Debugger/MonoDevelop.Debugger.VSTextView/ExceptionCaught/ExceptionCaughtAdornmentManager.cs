#if !WINDOWS

using System;
using AppKit;
using CoreGraphics;
using Foundation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.UI;
using Microsoft.VisualStudio.UI.Controls;
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
		readonly ICocoaTextView textView;
		private readonly string filePath;
		readonly IXPlatAdornmentLayer _exceptionCaughtLayer;
		FileLineExtension extension;

		public ExceptionCaughtAdornmentManager (ICocoaTextView textView)
		{
			filePath = textView.TextBuffer.GetFilePathOrNull ();
			if (filePath == null)
				return;
			TextEditorService.FileExtensionAdded += FileExtensionAdded;
			TextEditorService.FileExtensionRemoved += FileExtensionRemoved;
			_exceptionCaughtLayer = textView.GetXPlatAdornmentLayer ("ExceptionCaught");
			this.textView = textView;
			this.textView.LayoutChanged += TextView_LayoutChanged;

			foreach (var ext in TextEditorService.GetFileExtensions (filePath))
				FileExtensionAdded (null, new FileExtensionEventArgs  { Extension = ext });
		}

		private void FileExtensionRemoved (object sender, FileExtensionEventArgs e)
		{
			if (e.Extension == extension) {
				extension = null;
				_exceptionCaughtLayer.RemoveAllAdornments ();
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
			if (fileLineExtension is ExceptionCaughtButton button)
				view = CreateButton (button);
			else if (fileLineExtension is ExceptionCaughtMiniButton miniButton)
				view = CreateMiniButton (miniButton);
			else
				return;
			if (extension != fileLineExtension) {
				extension = fileLineExtension;
				var newSpan = textView.TextSnapshot.SpanFromMDColumnAndLine (extension.Line, extension.Column, extension.Line, extension.Column);
				trackingSpan = textView.TextSnapshot.CreateTrackingSpan (newSpan, SpanTrackingMode.EdgeInclusive);
			}
			var span = trackingSpan.GetSpan (textView.TextSnapshot);
			if (textView.TextViewLines == null)
				return;
			if (!textView.TextViewLines.FormattedSpan.IntersectsWith (span))
				return;
			var charBound = textView.TextViewLines.GetCharacterBounds (span.End);
			view.SetFrameOrigin (new CGPoint (
				Math.Round (charBound.Left),
				Math.Round (charBound.TextTop + charBound.TextHeight / 2 - view.Frame.Height / 2)));
			_exceptionCaughtLayer.RemoveAllAdornments ();
			_exceptionCaughtLayer.AddAdornment (XPlatAdornmentPositioningBehavior.TextRelative, span, null, view, null);
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
					image = image.WithTint (NSColor.SystemOrangeColor);
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

		class ExceptionCaughtPopoverView : VSPopoverView
		{
			public ExceptionCaughtPopoverView()
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
			}

			public override void CursorUpdate (NSEvent theEvent)
				=> NSCursor.ArrowCursor.Set ();

			public override bool AcceptsFirstMouse (NSEvent theEvent)
				=> true;

			public override void MouseEntered (NSEvent theEvent) { }
			public override void MouseExited (NSEvent theEvent) { }
			public override void MouseMoved (NSEvent theEvent) { }
		}

		static NSView CreateMiniButton (ExceptionCaughtMiniButton miniButton)
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

			return new ExceptionCaughtPopoverView {
				ContentView = nsButton,
				CornerRadius = 3,
				Material = NSVisualEffectMaterial.WindowBackground
			};
		}

		static NSView CreateButton (ExceptionCaughtButton button)
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

			return new ExceptionCaughtPopoverView {
				CornerRadius = 6,
				Padding = new Padding (12),
				ContentView = box
			};
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
			TextEditorService.FileExtensionAdded -= FileExtensionAdded;
			TextEditorService.FileExtensionRemoved -= FileExtensionRemoved;
		}
	}
}

#endif