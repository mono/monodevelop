/* 
 * NativeViewHelper.cs - helper with static methods to create view related things
 * 
 * Author:
 *   Jose Medrano <josmed@microsoft.com>
 *
 * Copyright (C) 2018 Microsoft, Corp
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to permit
 * persons to whom the Software is furnished to do so, subject to the
 * following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS
 * OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
 * NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM,
 * DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
 * USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
#if MAC
using AppKit;
using Foundation;

namespace MonoDevelop.DesignerSupport.Toolbox
{
	static class NativeViewHelper
	{
		public static NSStackView CreateHorizontalStackView (int spacing = 10) => new NSStackView () {
			Orientation = NSUserInterfaceLayoutOrientation.Horizontal,
			Alignment = NSLayoutAttribute.CenterY,
			Spacing = spacing,
			Distribution = NSStackViewDistribution.Fill,
			TranslatesAutoresizingMaskIntoConstraints = false
		};

		public static NSAttributedString GetAttributedStringFromFormattedText (string formattedText)
		{
			formattedText = formattedText.Replace ("&amp;", "&");
			var formated = Xwt.FormattedText.FromMarkup (formattedText);
			return Xwt.Mac.Util.ToAttributedString (formated);
		}

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