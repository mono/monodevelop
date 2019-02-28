using System;
using AppKit;
using CoreGraphics;
using Foundation;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Mono.Debugging.Client;
using MonoDevelop.Components;
using MonoDevelop.Components.Mac;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.TextEditing;

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
			view.SetFrameOrigin (new CGPoint (charBound.Left, charBound.TextTop + charBound.TextHeight / 2 - view.Frame.Height / 2));
			_exceptionCaughtLayer.RemoveAllAdornments ();
			_exceptionCaughtLayer.AddAdornment (XPlatAdornmentPositioningBehavior.TextRelative, span, null, view, null);
		}

		private void TextView_LayoutChanged (object sender, TextViewLayoutChangedEventArgs e)
		{
			RenderAdornment (extension);
		}

		private NSView CreateMiniButton (ExceptionCaughtMiniButton miniButton)
		{
			var icon = Xwt.Drawing.Image.FromResource ("lightning-16.png");
			var nsButton = new NSButton ();
			nsButton.Image = icon.ToNSImage ();
			nsButton.SetFrameSize (nsButton.FittingSize);
			nsButton.Bordered = false;
			nsButton.Activated += delegate {
				miniButton.dlg.ShowButton ();
			};
			nsButton.Cell.BackgroundColor = MonoDevelop.Ide.Gui.Styles.PopoverWindow.DefaultBackgroundColor.ToNSColor();
			return nsButton;
		}

		private NSView CreateButton (ExceptionCaughtButton button)
		{
			var box = new NSGridView ();
			var ligthingImage = new NSImageView { Image = Xwt.Drawing.Image.FromResource ("lightning-16.png").ToNSImage () };
			ligthingImage.TranslatesAutoresizingMaskIntoConstraints = false;
			box.AddColumn (new [] { ligthingImage });

			var typeLabel = CreateTextField ();
			var messageLabel = CreateTextField ();

			var detailsBtn = new NSButton {
				Bordered = false
			};
			detailsBtn.AttributedTitle = new NSAttributedString (GettextCatalog.GetString ("Show Details"), new NSStringAttributes {
				UnderlineStyle = (int)NSUnderlineStyle.Single,
				UnderlineColor = NSColor.Blue,
				ForegroundColor = NSColor.Blue
			});
			detailsBtn.Activated += (o, e) => button.dlg.ShowDialog ();
			box.AddColumn (new NSView[] { typeLabel, messageLabel, detailsBtn });

			var closeButton = new NSButton { Image = ImageService.GetIcon ("md-popup-close").ToNSImage () };
			closeButton.Bordered = false;
			closeButton.Activated += delegate {
				button.dlg.ShowMiniButton ();
			};
			box.AddColumn (new NSView [] { closeButton });

			button.exception.Changed += delegate {
				Runtime.RunInMainThread(()=>
				 LoadData(button.exception, messageLabel, typeLabel)).Ignore();
			};
			LoadData (button.exception, messageLabel, typeLabel);
			box.SetFrameSize (box.FittingSize);
			box.WantsLayer = true;
			box.Layer.BackgroundColor = MonoDevelop.Ide.Gui.Styles.PopoverWindow.DefaultBackgroundColor.ToCGColor ();
			return box;
		}

		static NSTextField CreateTextField ( )
		{
			var textField = new NSTextField ();
			textField.Alignment = NSTextAlignment.Left;
			textField.Selectable = true;
			textField.Editable = false;
			textField.DrawsBackground = false;
			textField.Bezeled = false;
			textField.PreferredMaxLayoutWidth = 400;
			textField.Cell.Wraps = true;
			textField.Cell.LineBreakMode = NSLineBreakMode.ByWordWrapping;
			textField.Cell.UsesSingleLineMode = false;
			return textField;
		}

		private void LoadData (ExceptionInfo exception, NSTextField messageLabel, NSTextField typeLabel)
		{
			if (!string.IsNullOrEmpty (exception.Message)) {
				messageLabel.Hidden = false;
				messageLabel.StringValue = exception.Message;
			} else {
				messageLabel.Hidden = true;
			}
			if (!string.IsNullOrEmpty (exception.Type)) {
				var str = GettextCatalog.GetString ("<b>{0}</b> has been thrown", exception.Type);
				var indexOfStartBold = str.IndexOf ("<b>", StringComparison.Ordinal);
				var indexOfEndBold = str.IndexOf ("</b>", StringComparison.Ordinal);
				str = str.Remove (indexOfStartBold, indexOfEndBold + 4 - indexOfStartBold);//Remove <b>TypeName</b>
				var mutableString = new NSMutableAttributedString (str);
				mutableString.Insert (new NSAttributedString (exception.Type, new NSStringAttributes {
					Font = NSFont.BoldSystemFontOfSize (NSFont.SystemFontSize)
				}), indexOfStartBold);
				typeLabel.AttributedStringValue = mutableString;
				typeLabel.Hidden = false;
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
