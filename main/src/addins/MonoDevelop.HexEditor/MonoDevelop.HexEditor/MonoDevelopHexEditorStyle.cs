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

namespace MonoDevelop.HexEditor
{
	public class MonoDevelopHexEditorStyle : HexEditorStyle
	{
		Style colorStyle;
		Mono.MHex.HexEditor hexEditor;
		
		public MonoDevelopHexEditorStyle (Mono.MHex.HexEditor hexEditor)
		{
			this.hexEditor = hexEditor;
			SetStyle ();
			PropertyService.PropertyChanged += delegate(object sender, PropertyChangedEventArgs e) {
				if (e.Key == "ColorScheme") {
					SetStyle ();
					this.hexEditor.Options.RaiseChanged ();
					this.hexEditor.PurgeLayoutCaches ();
					this.hexEditor.Repaint ();
				}
			};
		}
		
		void SetStyle ()
		{
			colorStyle = SyntaxModeService.GetColorStyle (hexEditor.Style, PropertyService.Get ("ColorScheme", "Default"));
		}
		
		public override Color HexOffset {
			get {
				return colorStyle.LineNumber.Color;
			}
		}
		
		public override Color HexOffsetBg {
			get {
				return colorStyle.LineNumber.BackgroundColor;
			}
		}
		
		public override Color HexOffsetHighlighted {
			get {
				return colorStyle.LineNumberFgHighlighted;
			}
		}
		
		public override Color HexDigit {
			get {
				return colorStyle.Default.Color;
			}
		}
		
		public override Color HexDigitBg {
			get {
				return colorStyle.Default.BackgroundColor;
			}
		}
		
		public override Color DashedLineFg {
			get {
				return colorStyle.Default.Color;
			}
		}
		
		public override Color DashedLineBg {
			get {
				return colorStyle.Default.BackgroundColor;
			}
		}
		
		public override Color IconBarBg {
			get {
				return colorStyle.IconBarBg;
			}
		}
		
		public override Color IconBarSeperator {
			get {
				return colorStyle.IconBarSeperator;
			}
		}
		
		public override Color BookmarkColor1 {
			get {
				return colorStyle.BookmarkColor1;
			}
		}
		
		public override Color BookmarkColor2 {
			get {
				return colorStyle.BookmarkColor2;
			}
		}
		
		public override Color Selection {
			get {
				return colorStyle.Selection.Color;
			}
		}
		
		public override Color SelectionBg {
			get {
				return colorStyle.Selection.BackgroundColor;
			}
		}
		
		public override Color HighlightOffset {
			get {
				return colorStyle.SearchTextBg;
			}
		}
	}
}
