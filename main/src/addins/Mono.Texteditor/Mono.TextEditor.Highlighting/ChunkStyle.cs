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
	public class ChunkStyle
	{
		Gdk.Color color;
		Gdk.Color backColor;
		bool      transparentBackround = true;
		
		public Gdk.Color Color {
			get {
				return color;
			}
			set {
				color = value;
			}
		}

		public Gdk.Color BackgroundColor {
			get {
				return backColor;
			}
			set {
				backColor = value; transparentBackround = false;
			}
		}
		
		public bool TransparentBackround {
			get { return transparentBackround; }
			set { transparentBackround = value; }
		}

		public bool Bold {
			get;
			set;
		}

		public bool Italic {
			get;
			set;
		}

		public bool Underline {
			get;
			set;
		}
		
		public ChunkStyle (ChunkStyle style)
		{
			color = style.color;
			backColor = style.backColor;
			Bold = style.Bold;
			Italic = style.Italic;
			transparentBackround = style.transparentBackround;
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
		
		public ChunkStyle () : this (new Gdk.Color (0, 0, 0), false)
		{
		}
		
		public ChunkStyle (Gdk.Color color) : this (color, false)
		{
		}
		
		public ChunkStyle (Gdk.Color color, bool bold)
		{
			this.color = color;
			this.Bold  = bold;
		}
		
		public ChunkStyle (Gdk.Color color, bool bold, bool italic)
		{
			this.color = color;
			this.Bold  = bold;
			this.Italic  = italic;
		}

		public override string ToString ()
		{
			return String.Format ("[ChunkStyle: Color={0}, bold={1}]", color, Bold);
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
