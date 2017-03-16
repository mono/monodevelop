//
// StyleTextLineMarker.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using Gdk;
using Mono.TextEditor.Highlighting;

namespace Mono.TextEditor
{
	class StyleTextLineMarker: TextLineMarker
	{
		[Flags]
		public enum StyleFlag {
			None = 0,
			Color = 1,
			BackgroundColor = 2,
			Bold = 4,
			Italic = 8
		}
		
		Cairo.Color color;
		Cairo.Color backColor;
		bool bold;
		bool italic;
		
		public bool Italic {
			get {
				return italic;
			}
			set {
				italic = value;
				IncludedStyles |= StyleFlag.Italic;
			}
		}
		
		public virtual StyleFlag IncludedStyles {
			get;
			set;
		}
		
		public virtual Cairo.Color Color {
			get {
				return color;
			}
			set {
				color = value;
				IncludedStyles |= StyleFlag.Color;
			}
		}
		
		public bool Bold {
			get {
				return bold;
			}
			set {
				bold = value;
				IncludedStyles |= StyleFlag.Bold;
			}
		}
		
		public virtual Cairo.Color BackgroundColor {
			get {
				return backColor;
			}
			set {
				backColor = value;
				IncludedStyles |= StyleFlag.BackgroundColor;
			}
		}
		
		protected virtual MonoDevelop.Ide.Editor.Highlighting.ChunkStyle CreateStyle (MonoDevelop.Ide.Editor.Highlighting.ChunkStyle baseStyle, Cairo.Color color, Cairo.Color bgColor)
		{
			var style = new MonoDevelop.Ide.Editor.Highlighting.ChunkStyle (baseStyle);
			if ((IncludedStyles & StyleFlag.Color) != 0)
				style.Foreground = color;
			
			if ((IncludedStyles & StyleFlag.BackgroundColor) != 0) {
				style.Background = bgColor;
			}
			
			if ((IncludedStyles & StyleFlag.Bold) != 0)
				style.FontWeight = Xwt.Drawing.FontWeight.Bold;
			
			if ((IncludedStyles & StyleFlag.Italic) != 0)
				style.FontStyle = Xwt.Drawing.FontStyle.Italic;
			return style;
		}
		
		internal override MonoDevelop.Ide.Editor.Highlighting.ChunkStyle GetStyle (MonoDevelop.Ide.Editor.Highlighting.ChunkStyle baseStyle)
		{
			if (baseStyle == null || IncludedStyles == StyleFlag.None)
				return baseStyle;
			
			return CreateStyle (baseStyle, Color, BackgroundColor);
		}
	}
}
