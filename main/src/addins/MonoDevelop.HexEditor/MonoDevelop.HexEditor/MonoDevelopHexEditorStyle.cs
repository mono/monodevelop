// 
// MonoDevelopHexEditorStyle.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using Mono.TextEditor.Highlighting;
using Mono.MHex.Rendering;
using MonoDevelop.Ide;
using Xwt.Drawing;

namespace MonoDevelop.HexEditor
{
	class MonoDevelopHexEditorStyle : HexEditorStyle
	{
		ColorScheme colorStyle;
		readonly Mono.MHex.HexEditor hexEditor;
		
		public MonoDevelopHexEditorStyle (Mono.MHex.HexEditor hexEditor)
		{
			this.hexEditor = hexEditor;
			SetStyle ();
			IdeApp.Preferences.ColorSchemeChanged += delegate {
				SetStyle ();
				this.hexEditor.Options.RaiseChanged ();
				this.hexEditor.PurgeLayoutCaches ();
				this.hexEditor.Repaint ();
			};
		}
		
		void SetStyle ()
		{
			colorStyle = SyntaxModeService.GetColorStyle (IdeApp.Preferences.ColorScheme);
		}

		Color ConvertColor (Cairo.Color foreground)
		{
			return new Color (foreground.R, foreground.G, foreground.B, foreground.A);
		}

		public override Color HexOffset {
			get {
				return ConvertColor (colorStyle.LineNumbers.Foreground);
			}
		}
		
		public override Color HexOffsetBg {
			get {
				return ConvertColor (colorStyle.LineNumbers.Background);
			}
		}
		
/*		public override Color HexOffsetHighlighted {
			get {
				return ConvertColor (colorStyle.LineNumbers.fo);
			}
		}*/
		
		public override Color HexDigit {
			get {
				return ConvertColor (colorStyle.PlainText.Foreground);
			}
		}
		
		public override Color HexDigitBg {
			get {
				return ConvertColor (colorStyle.PlainText.Background);
			}
		}
		
		public override Color DashedLineFg {
			get {
				return ConvertColor (colorStyle.PlainText.Foreground);
			}
		}
		
		public override Color DashedLineBg {
			get {
				return ConvertColor (colorStyle.PlainText.Background);
			}
		}
		
		public override Color IconBarBg {
			get {
				return ConvertColor (colorStyle.IndicatorMarginSeparator.Color);
			}
		}
		
		public override Color IconBarSeperator {
			get {
				return ConvertColor (colorStyle.IndicatorMarginSeparator.Color);
			}
		}
		
		public override Color BookmarkColor1 {
			get {
				return ConvertColor (colorStyle.Bookmarks.Color);
			}
		}
		
		public override Color BookmarkColor2 {
			get {
				return ConvertColor (colorStyle.Bookmarks.SecondColor);
			}
		}
		
		public override Color Selection {
			get {
				return ConvertColor (colorStyle.SelectedText.Foreground);
			}
		}
		
		public override Color SelectionBg {
			get {
				return ConvertColor (colorStyle.SelectedText.Background);
			}
		}
		
		public override Color HighlightOffset {
			get {
				return ConvertColor (colorStyle.SearchResult.Color);
			}
		}
	}
}
