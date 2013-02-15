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
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Reflection;

namespace Mono.TextEditor.Highlighting
{

	public class ChunkStyle
	{
		public string Name { get; private set; }
		public Cairo.Color CairoColor { get; set; }
		public Cairo.Color CairoBackgroundColor { get; set; }

		public bool GotForegroundColorAssigned {
			get {
				return CairoColor.A != 1.0;

			}
		}
		public bool TransparentBackround {
			get {
				return CairoBackgroundColor.A == 1.0;
			}
		}
		public TextWeight Weight { get; set; }

		public bool Bold {
			get {
				return Weight.HasFlag (TextWeight.Bold);
			}
		}
		
		public bool Italic {
			get {
				return Weight.HasFlag (TextWeight.Italic);
			}
		}
		
		public bool Underline {
			get {
				return Weight.HasFlag (TextWeight.Underline);
			}
		}

		public ChunkStyle ()
		{
			CairoColor = CairoBackgroundColor = new Cairo.Color (0, 0, 0, 1.0);
		}

		public ChunkStyle (ChunkStyle baseStyle)
		{
			this.Name = baseStyle.Name;
			this.CairoColor = baseStyle.CairoColor;
			this.CairoBackgroundColor = baseStyle.CairoBackgroundColor;
			this.Weight = baseStyle.Weight;
		}

		public static ChunkStyle Create (XElement element, Dictionary<string, Cairo.Color> palette)
		{
			var result = new ChunkStyle ();

			foreach (var node in element.DescendantNodes ()) {
				if (node.NodeType == System.Xml.XmlNodeType.Element) {
					var el = (XElement)node;
					switch (el.Name.LocalName) {
					case "name":
						result.Name = el.Value;
						break;
					case "fore":
						result.CairoColor = ColorScheme.ParsePaletteColor (palette, el.Value);
						break;
					case "back":
						result.CairoBackgroundColor = ColorScheme.ParsePaletteColor (palette, el.Value);
						break;
					case "weight":
						TextWeight val;
						if (!Enum.TryParse<TextWeight> (el.Value, true, out val)) 
							throw new InvalidDataException (el.Value + " is no valid text weight values are: " + string.Join (",", Enum.GetNames (typeof(TextWeight))) );
						break;
					default:
						throw new InvalidDataException ("Invalid element in text color:" + el.Name);
					}
				}
			}

			return result;
		}

		public Gdk.GC CreateBgGC (Gdk.Drawable drawable)
		{
			return new Gdk.GC (drawable) { RgbBgColor = (HslColor)CairoColor, RgbFgColor = (HslColor)CairoBackgroundColor };
		}
		
		public Gdk.GC CreateFgGC (Gdk.Drawable drawable)
		{
			return new Gdk.GC (drawable) { RgbBgColor = (HslColor)CairoBackgroundColor, RgbFgColor = (HslColor)CairoColor };
		}
	}
	
}
