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
		Underline = 4
	}
	
	public class ChunkStyle
	{
		Gdk.Color color;
		Gdk.Color backColor;
		
		public virtual Gdk.Color Color {
			get {
				return color;
			}
			set {
				color = value;
			}
		}

		public virtual Gdk.Color BackgroundColor {
			get {
				return backColor;
			}
			set {
				backColor = value;
			}
		}
		
		public bool TransparentBackround {
			get {
				return BackgroundColor.Equal (Gdk.Color.Zero); 
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
			Color                = style.Color;
			BackgroundColor      = style.BackgroundColor;
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
		
		public ChunkStyle () : this (Gdk.Color.Zero, Gdk.Color.Zero, ChunkProperties.None)
		{
		}
		
		public ChunkStyle (Gdk.Color color) : this (color, Gdk.Color.Zero, ChunkProperties.None)
		{
		}
		
		public ChunkStyle (Gdk.Color color, ChunkProperties chunkProperties) : this (color, Gdk.Color.Zero, chunkProperties)
		{
		}
		
		public ChunkStyle (Gdk.Color color, Gdk.Color bgColor) : this (color, bgColor, ChunkProperties.None)
		{
		}
		
		public ChunkStyle (Gdk.Color color, Gdk.Color bgColor, ChunkProperties chunkProperties)
		{
			this.Color           = color;
			this.BackgroundColor = bgColor;
			this.ChunkProperties = chunkProperties;
		}
		
		public override string ToString ()
		{
			return string.Format("[ChunkStyle: Color={0}, BackgroundColor={1}, TransparentBackround={2}, ChunkProperties={3}, Link={4}]", Color, BackgroundColor, TransparentBackround, ChunkProperties, Link);
		}
		
		public override int GetHashCode ()
		{
			return color.GetHashCode () ^ Bold.GetHashCode ();
		}

		public override bool Equals (object o)
		{
			ChunkStyle c = o as ChunkStyle;
			return c != null && Bold == c.Bold && Italic == c.Italic && color.GetHashCode () == c.color.GetHashCode ();
		}
	}
}
