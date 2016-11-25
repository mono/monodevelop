//
// ChunkStyle.cs
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
using System.IO;
using System.Collections.Generic;
using System.Xml.Linq;
using MonoDevelop.Components;
using System.Collections.Immutable;

namespace MonoDevelop.Ide.Editor.Highlighting
{
	public sealed class ChunkStyle
	{
		public ScopeStack ScopeStack { get; set; }
		public HslColor Foreground { get; set; }
		public HslColor Background { get; set; }

		public bool TransparentForeground {
			get {
				return Foreground.Alpha == 0.0;

			}
		}

		public bool TransparentBackground {
			get {
				return Background.Alpha == 0.0;
			}
		}

		public Xwt.Drawing.FontWeight FontWeight { get; set; }

		public Xwt.Drawing.FontStyle FontStyle { get; set; }

		public bool Underline {
			get; set;
		}

		public ChunkStyle ()
		{
			Foreground = Background = new HslColor (0, 0, 0, 0);
			FontWeight = Xwt.Drawing.FontWeight.Normal;
			FontStyle = Xwt.Drawing.FontStyle.Normal;
		}

		public ChunkStyle (ChunkStyle baseStyle)
		{
			this.ScopeStack = baseStyle.ScopeStack;
			this.Foreground = baseStyle.Foreground;
			this.Background = baseStyle.Background;
			this.FontWeight = baseStyle.FontWeight;
			this.FontStyle = baseStyle.FontStyle;
			this.Underline = baseStyle.Underline;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != typeof(ChunkStyle))
				return false;
			ChunkStyle other = (ChunkStyle)obj;
			return ScopeStack == other.ScopeStack && Foreground.Equals (other.Foreground) && Background.Equals (other.Background) && FontWeight == other.FontWeight && FontStyle == other.FontStyle;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (ScopeStack != null ? ScopeStack.GetHashCode () : 0) ^ Foreground.GetHashCode () ^ Background.GetHashCode () ^ FontWeight.GetHashCode () ^ FontStyle.GetHashCode ();
			}
		}

		internal Gdk.GC CreateBgGC (Gdk.Drawable drawable)
		{
			return new Gdk.GC (drawable) { RgbBgColor = (HslColor)Foreground, RgbFgColor = (HslColor)Background };
		}
		
		internal Gdk.GC CreateFgGC (Gdk.Drawable drawable)
		{
			return new Gdk.GC (drawable) { RgbBgColor = (HslColor)Background, RgbFgColor = (HslColor)Foreground };
		}

		public override string ToString ()
		{
			return string.Format ("[ChunkStyle: ScopeStack={0}, CairoColor={1}, CairoBackgroundColor={2}, FontWeight={3}, FontStyle={4}]", ScopeStack, Foreground, Background, FontWeight, FontStyle);
		}


		public ChunkStyle Clone ()
		{
			return (ChunkStyle)this.MemberwiseClone ();
		}
	}
	
}
