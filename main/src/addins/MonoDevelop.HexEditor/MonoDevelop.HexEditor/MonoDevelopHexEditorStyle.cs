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

using System;
using Mono.TextEditor.Highlighting;
using Mono.MHex.Rendering;
using Gdk;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Mono.TextEditor;

namespace MonoDevelop.HexEditor
{
	public class MonoDevelopHexEditorStyle : HexEditorStyle
	{
		ColorScheme colorStyle;
		Mono.MHex.HexEditor hexEditor;
		
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
		
		public override Color HexOffset {
			get {
				return (HslColor)colorStyle.LineNumbers.Foreground;
			}
		}
		
		public override Color HexOffsetBg {
			get {
				return (HslColor)colorStyle.LineNumbers.Background;
			}
		}
		
		public override Color HexOffsetHighlighted {
			get {
				return (HslColor)colorStyle.LineMarker.GetColor ("color");
			}
		}
		
		public override Color HexDigit {
			get {
				return (HslColor)colorStyle.PlainText.Foreground;
			}
		}
		
		public override Color HexDigitBg {
			get {
				return (HslColor)colorStyle.PlainText.Background;
			}
		}
		
		public override Color DashedLineFg {
			get {
				return (HslColor)colorStyle.PlainText.Foreground;
			}
		}
		
		public override Color DashedLineBg {
			get {
				return (HslColor)colorStyle.PlainText.Background;
			}
		}
		
		public override Color IconBarBg {
			get {
				return (HslColor) (colorStyle.IndicatorMarginSeparator.GetColor("color"));
			}
		}
		
		public override Color IconBarSeperator {
			get {
				return (HslColor) (colorStyle.IndicatorMarginSeparator.GetColor("color"));
			}
		}
		
		public override Color BookmarkColor1 {
			get {
				return (HslColor) (colorStyle.Bookmarks.GetColor ("color"));
			}
		}
		
		public override Color BookmarkColor2 {
			get {
				return (HslColor) (colorStyle.Bookmarks.GetColor ("secondcolor"));
			}
		}
		
		public override Color Selection {
			get {
				return (HslColor)colorStyle.SelectedText.Foreground;
			}
		}
		
		public override Color SelectionBg {
			get {
				return (HslColor)colorStyle.SelectedText.Background;
			}
		}
		
		public override Color HighlightOffset {
			get {
				return (HslColor) (colorStyle.SearchResult.GetColor ("color"));
			}
		}
	}
}
