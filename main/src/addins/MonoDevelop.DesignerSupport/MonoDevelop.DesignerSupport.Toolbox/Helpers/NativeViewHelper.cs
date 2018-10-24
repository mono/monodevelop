#if MAC
using AppKit;
using CoreGraphics;
using Foundation;
using System.Linq;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	public static class NativeViewHelper
	{
		public static NSStackView CreateHorizontalStackView (int spacing = 10) => new NSStackView () {
			Orientation = NSUserInterfaceLayoutOrientation.Horizontal,
			Alignment = NSLayoutAttribute.CenterY,
			Spacing = spacing,
			Distribution = NSStackViewDistribution.Fill,
			TranslatesAutoresizingMaskIntoConstraints = false
		};

		public static NSAttributedString GetAttributedString (string text, NSColor foregroundColor, NSFont font)
		{
			//There is no need create NSStringAttributes element
			var attributed = new NSAttributedString (text, new NSStringAttributes {
				ForegroundColor = foregroundColor, Font = font
			});
			return attributed;
		}

		public static NSTextField CreateLabel (string text, NSTextAlignment alignment = NSTextAlignment.Left, NSFont font = null)
		{
			return new NSTextField () {
				StringValue = text ?? "",
				Font = font ?? GetSystemFont (false),
				Editable = false,
				Bordered = false,
				Bezeled = false,
				DrawsBackground = false,
				Selectable = false,
				Alignment = alignment,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
		}

		public static ProgressIndicator GetProgressIndicator (this NSView view)
		{
			return view.Subviews.FirstOrDefault (v => v is ProgressIndicator) as ProgressIndicator;
		}

		public static void ShowProgressIndicator (this NSView view, string title = null)
		{
			if (!(view.GetProgressIndicator () is ProgressIndicator indicator)) {
				view.PostsFrameChangedNotifications = true;
				indicator = new ProgressIndicator () {
					Indeterminate = true,
					Style = NSProgressIndicatorStyle.Spinning,
					UsesThreadedAnimation = false
				};
				view.AddSubview (indicator);
			} else {
				indicator.Frame = view.Frame;
			}

			indicator.Hidden = false;
			indicator.Title = title ?? string.Empty;
			indicator.StartAnimation (view);
		}

		public static void HideProgressIndicator (this NSView tableView)
		{
			var indicator = tableView.GetProgressIndicator ();
			if (indicator != null) {
				indicator.StopAnimation (tableView);
				indicator.Hidden = true;
			}
		}
		public static NSTextField CreateLabel (string text, NSFont font = null, NSTextAlignment alignment = NSTextAlignment.Left)
		{
			var label = new NSTextField () {
				StringValue = text ?? "",
				Font = font ?? GetSystemFont (false),
				Editable = false,
				Bordered = false,
				Bezeled = false,
				DrawsBackground = false,
				Selectable = false,
				Alignment = alignment,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
			return label;
		}

		public static NSStackView CreateVerticalStackView (int spacing = 10) => new NSStackView () {
			Orientation = NSUserInterfaceLayoutOrientation.Vertical,
			Alignment = NSLayoutAttribute.Leading,
			Spacing = spacing,
			Distribution = NSStackViewDistribution.Fill,
			TranslatesAutoresizingMaskIntoConstraints = false
		};

		public static NSFont GetSmallSystemFont (bool bold)
		{
			if (bold)
				return NSFont.BoldSystemFontOfSize (NSFont.SmallSystemFontSize);
			return NSFont.SystemFontOfSize (NSFont.SmallSystemFontSize);
		}

		public static NSButton CreateButton (NSBezelStyle bezelStyle, NSFont font, string text, NSControlSize controlSize = NSControlSize.Regular, NSImage image = null, bool bordered = true)
		{
			var button = new NSButton {
				BezelStyle = bezelStyle,
				Bordered = bordered,
				ControlSize = controlSize,
				Font = font ?? GetSystemFont (false),
				Title = text, TranslatesAutoresizingMaskIntoConstraints = false
			};
			if (image != null) {
				button.Image = image;
			}
			return button;
		}

		public static NSTextField CreateTextField (string text, NSFont font = null, NSTextAlignment alignment = NSTextAlignment.Left)
		{
			return new NSTextField () {
				StringValue = text ?? "",
				Font = font ?? GetSystemFont (false),
				Alignment = alignment,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
		}

		public static NSTextField CreateLabel (CGRect rect, NSFont font, string text, NSTextAlignment alignment = NSTextAlignment.Left)
		{
			return new NSTextField (rect) {
				StringValue = text ?? "",
				Font = font ?? GetSystemFont (false),
				Editable = false,
				Bordered = false,
				Bezeled = false,
				DrawsBackground = false,
				Selectable = false,
				Alignment = alignment,
				TranslatesAutoresizingMaskIntoConstraints = false
			};
		}

		public static NSFont GetSystemFont (bool bold, float size = 0.0f)
		{
			if (size <= 0) {
				size = (float)NSFont.SystemFontSize;
			}
			if (bold)
				return NSFont.BoldSystemFontOfSize (size);
			return NSFont.SystemFontOfSize (size);
		}
	}
}
#endif