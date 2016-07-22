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
using Mono.MHex.Rendering;
using Xwt;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using Xwt.Drawing;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Editor.Highlighting;

namespace MonoDevelop.HexEditor
{
	class MonoDevelopHexEditorStyle : HexEditorStyle, IDisposable
	{
		EditorTheme colorStyle;
		Mono.MHex.HexEditor hexEditor;

		public MonoDevelopHexEditorStyle (Mono.MHex.HexEditor hexEditor)
		{
			this.hexEditor = hexEditor;
			SetStyle ();
			IdeApp.Preferences.ColorScheme.Changed += ColorSchemeChanged;
		}

		void ColorSchemeChanged (object sender, EventArgs e)
		{
			SetStyle ();
			this.hexEditor.Options.RaiseChanged ();
			this.hexEditor.PurgeLayoutCaches ();
			this.hexEditor.Repaint ();
		}

		void SetStyle ()
		{
			colorStyle = SyntaxHighlightingService.GetEditorTheme (IdeApp.Preferences.ColorScheme);
		}

		Color ConvertColor (Cairo.Color foreground)
		{
			return new Color (foreground.R, foreground.G, foreground.B, foreground.A);
		}

		public void Dispose ()
		{
			IdeApp.Preferences.ColorScheme.Changed -= ColorSchemeChanged;
		}

		public override Color HexOffset {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.LineNumbers));
			}
		}

		public override Color HexOffsetBg {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.LineNumbersBackground));
			}
		}

		/*		public override Color HexOffsetHighlighted {
					get {
						return ConvertColor (colorStyle.LineNumbers.fo);
					}
				}*/

		public override Color HexDigit {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Foreground));
			}
		}

		public override Color HexDigitBg {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Background));
			}
		}

		public override Color DashedLineFg {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Foreground));
			}
		}

		public override Color DashedLineBg {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Background));
			}
		}

		public override Color IconBarBg {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.IndicatorMarginSeparator));
			}
		}

		public override Color IconBarSeperator {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.IndicatorMarginSeparator));
			}
		}

		public override Color BookmarkColor1 {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.MessageBubbleWarningLine));
			}
		}

		public override Color BookmarkColor2 {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.MessageBubbleWarningLine2));
			}
		}
		
		public override Color Selection {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Foreground));
			}
		}
		
		public override Color SelectionBg {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.Selection));
			}
		}
		
		public override Color HighlightOffset {
			get {
				return ConvertColor (SyntaxHighlightingService.GetColor (colorStyle, EditorThemeColors.FindHighlight));
			}
		}
	}
}
