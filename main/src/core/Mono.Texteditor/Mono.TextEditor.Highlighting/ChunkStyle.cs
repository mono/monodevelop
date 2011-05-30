// ChunkStyle.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//
using System;

namespace Mono.TextEditor
{
	[Flags]
	public enum ChunkProperties {
		None = 0,
		Bold = 1,
		Italic = 2,
		Underline = 4,
		TransparentBackground = 8
	}
	
	public class ChunkStyle
	{
		public virtual Cairo.Color CairoColor {
			get;
			set;
		}
		
		public Gdk.Color Color {
			get {
				return (HslColor)CairoColor;
			}
		}
		
		bool backColorIsZero = true;
		Cairo.Color cairoBackgroundColor = new Cairo.Color (0, 0, 0, 0);
		public virtual Cairo.Color CairoBackgroundColor {
			get { return cairoBackgroundColor; }
			set { cairoBackgroundColor = value; backColorIsZero = false; }
		}
		
		public bool GotBackgroundColorAssigned {
			get {
				return !backColorIsZero;
			}
		}
		
		public Gdk.Color BackgroundColor {
			get {
				return (HslColor)CairoBackgroundColor;
			}
		}
		
		public bool TransparentBackround {
			get {
				return (ChunkProperties & ChunkProperties.TransparentBackground) == ChunkProperties.TransparentBackground || backColorIsZero; 
			}
		}
		
		public virtual ChunkProperties ChunkProperties {
			get;
			set;
		}

		public bool Bold {
			get {
				return (ChunkProperties & ChunkProperties.Bold) == ChunkProperties.Bold;
			}
		}

		public bool Italic {
			get {
				return (ChunkProperties & ChunkProperties.Italic) == ChunkProperties.Italic;
			}
		}

		public bool Underline {
			get {
				return (ChunkProperties & ChunkProperties.Underline) == ChunkProperties.Underline;
			}
		}

		public virtual string Link {
			get;
			set;
		}
		
		public ChunkStyle (ChunkStyle style)
		{
			CairoColor           = style.CairoColor;
			if (!style.backColorIsZero)
				CairoBackgroundColor = style.CairoBackgroundColor;
			ChunkProperties      = style.ChunkProperties;
		}

		public Pango.Style GetStyle (Pango.Style defaultStyle)
		{
			return Italic ? Pango.Style.Italic : Pango.Style.Normal;
		}
		
		public Pango.Weight GetWeight (Pango.Weight defaultWeight)
		{
			if (defaultWeight == Pango.Weight.Bold)
				return Bold ? Pango.Weight.Heavy : Pango.Weight.Bold;
			return Bold ? Pango.Weight.Bold : Pango.Weight.Normal;
		}
		
		public ChunkStyle ()
		{
		}
		
		public ChunkStyle (Gdk.Color color)
		{
			this.CairoColor =(HslColor) color;
		}
		
		public ChunkStyle (Gdk.Color color, ChunkProperties chunkProperties)
		{
			this.CairoColor      = (HslColor)color;
			this.ChunkProperties = chunkProperties;
		}
		
		public ChunkStyle (Gdk.Color color, Gdk.Color bgColor) : this (color, bgColor, ChunkProperties.None)
		{
		}
		
		public ChunkStyle (Gdk.Color color, Gdk.Color bgColor, ChunkProperties chunkProperties)
		{
			this.CairoColor           = (HslColor)color;
			this.CairoBackgroundColor = (HslColor)bgColor;
			this.ChunkProperties = chunkProperties;
		}
		
		public ChunkStyle (Cairo.Color color, Cairo.Color bgColor, ChunkProperties chunkProperties)
		{
			this.CairoColor           = color;
			this.CairoBackgroundColor = bgColor;
			this.ChunkProperties = chunkProperties;
		}
		
		public ChunkStyle (Cairo.Color color, ChunkProperties chunkProperties)
		{
			this.CairoColor           = color;
			this.ChunkProperties = chunkProperties;
		}

		public override string ToString ()
		{
			return string.Format ("[ChunkStyle: Color={0}, BackgroundColor={1}, TransparentBackround={2}, ChunkProperties={3}, Link={4}]", CairoColor, CairoBackgroundColor, TransparentBackround, ChunkProperties, Link);
		}
		
		public override int GetHashCode ()
		{
			return CairoColor.GetHashCode () ^ Bold.GetHashCode ();
		}

		public override bool Equals (object o)
		{
			ChunkStyle c = o as ChunkStyle;
			return c != null && Bold == c.Bold && Italic == c.Italic && CairoColor.GetHashCode () == c.CairoColor.GetHashCode ();
		}
		
		public Gdk.GC CreateBgGC (Gdk.Drawable drawable)
		{
			return new Gdk.GC (drawable) { RgbBgColor = Color, RgbFgColor = BackgroundColor };
		}
		
		public Gdk.GC CreateFgGC (Gdk.Drawable drawable)
		{
			return new Gdk.GC (drawable) { RgbBgColor = BackgroundColor, RgbFgColor = Color };
		}
	}
}
