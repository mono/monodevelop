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
using System;
using AppKit;
using Foundation;

namespace MonoDevelop.Components.Mac
{
	static class NativeViewHelper
	{
		public static NSStackView CreateVerticalStackView (int spacing = 10, bool translatesAutoresizingMaskIntoConstraints = false) => new NSStackView () {
			Orientation = NSUserInterfaceLayoutOrientation.Vertical,
			Alignment = NSLayoutAttribute.Leading,
			Spacing = spacing,
			Distribution = NSStackViewDistribution.Fill,
			TranslatesAutoresizingMaskIntoConstraints = translatesAutoresizingMaskIntoConstraints
		};

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

		static readonly NSAttributedString NewLine = new NSAttributedString ("\n");

		public static NSAttributedString GetMultiLineAttributedString (string title, string description, nfloat fontSize, NSColor titleColor, NSColor descriptionColor, NSParagraphStyle pstyle = null)
		{
			var result = new NSMutableAttributedString ();
			if (!String.IsNullOrEmpty (title)) {
				result.Append (new NSAttributedString (title, new NSStringAttributes {
					Font = NSFont.SystemFontOfSize (fontSize),
					ForegroundColor = titleColor
				}));
			}

			if (!String.IsNullOrEmpty (description)) {
				result.Append (NewLine);
				result.Append (new NSAttributedString (description, new NSStringAttributes {
					Font = NSFont.SystemFontOfSize (fontSize - 2),
					ForegroundColor = descriptionColor,
					ParagraphStyle = pstyle ?? NSParagraphStyle.DefaultParagraphStyle
				}));
			}

			return result;
		}

		public static NSAttributedString GetMultiLineAttributedStringWithImage (NSImage image, string title, string description, nfloat fontSize, NSColor titleColor, NSColor descriptionColor)
		{
			var attrString = new NSMutableAttributedString ("");

			if (image != null) {
				var cell = new NSTextAttachmentCell (image);
				image.AlignmentRect = new CoreGraphics.CGRect (0, 5, image.Size.Width, image.Size.Height);
				cell.Alignment = NSTextAlignment.Natural;
				attrString.Append (NSAttributedString.FromAttachment (new NSTextAttachment { AttachmentCell = cell }));
				attrString.Append (new NSAttributedString ("  "));
			}

			var pstyle = new NSMutableParagraphStyle ();
			pstyle.HeadIndent = pstyle.FirstLineHeadIndent = 24;
			attrString.Append (GetMultiLineAttributedString (title, description, fontSize, titleColor, descriptionColor, pstyle));

			return attrString;
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