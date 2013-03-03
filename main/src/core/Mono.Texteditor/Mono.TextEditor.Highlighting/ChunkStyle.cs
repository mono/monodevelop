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
using Xwt.Drawing;

namespace Mono.TextEditor.Highlighting
{

	public class ChunkStyle
	{
		public string Name { get; set; }
		public Cairo.Color Foreground { get; set; }
		public Cairo.Color Background { get; set; }

		public bool TransparentForeground {
			get {
				return Foreground.A == 0.0;

			}
		}
		public bool TransparentBackground {
			get {
				return Background.A == 0.0;
			}
		}

		[Obsolete("Will be removed - use FontWeight")]
		public TextWeight Weight { 
			get { 
				TextWeight weight = TextWeight.None;
				if (FontWeight == FontWeight.Bold)
					weight |= TextWeight.Bold;
				if (FontStyle == FontStyle.Italic)
					weight |= TextWeight.Italic;
				return weight;
			} 
			set {
				if (value.HasFlag (TextWeight.Bold)) {
					FontWeight = FontWeight.Bold;
				} else {
					FontWeight = FontWeight.Normal;
				}

				if (value.HasFlag (TextWeight.Italic)) {
					FontStyle = FontStyle.Italic;
				} else {
					FontStyle = FontStyle.Normal;
				}
			}
		}

		public FontWeight FontWeight { get; set; }

		public FontStyle FontStyle { get; set; }

		[Obsolete("Will be removed - use FontWeight")]
		public bool Bold {
			get {
				return FontWeight == FontWeight.Bold;
			}
		}
		
		[Obsolete("Will be removed - use FontStyle")]
		public bool Italic {
			get {
				return FontStyle == FontStyle.Italic;
			}
		}
		
		public bool Underline {
			get; set;
		}

		public ChunkStyle ()
		{
			Foreground = Background = new Cairo.Color (0, 0, 0, 0);
			FontWeight = FontWeight.Normal;
			FontStyle = FontStyle.Normal;
		}

		public ChunkStyle (ChunkStyle baseStyle)
		{
			this.Name = baseStyle.Name;
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
			return Name == other.Name && Foreground.Equals (other.Foreground) && Background.Equals (other.Background) && FontWeight == other.FontWeight && FontStyle == other.FontStyle;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (Name != null ? Name.GetHashCode () : 0) ^ Foreground.GetHashCode () ^ Background.GetHashCode () ^ FontWeight.GetHashCode () ^ FontStyle.GetHashCode ();
			}
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
						result.Foreground = ColorScheme.ParsePaletteColor (palette, el.Value);
						break;
					case "back":
						result.Background = ColorScheme.ParsePaletteColor (palette, el.Value);
						break;
					case "weight":
						FontWeight weight;
						if (!Enum.TryParse<FontWeight> (el.Value, true, out weight)) 
							throw new InvalidDataException (el.Value + " is no valid text weight values are: " + string.Join (",", Enum.GetNames (typeof(FontWeight))) );
						result.FontWeight = weight;
						break;
					case "style":
						FontStyle style;
						if (!Enum.TryParse<FontStyle> (el.Value, true, out style)) 
							throw new InvalidDataException (el.Value + " is no valid text weight values are: " + string.Join (",", Enum.GetNames (typeof(FontStyle))) );
						result.FontStyle = style;
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
			return new Gdk.GC (drawable) { RgbBgColor = (HslColor)Foreground, RgbFgColor = (HslColor)Background };
		}
		
		public Gdk.GC CreateFgGC (Gdk.Drawable drawable)
		{
			return new Gdk.GC (drawable) { RgbBgColor = (HslColor)Background, RgbFgColor = (HslColor)Foreground };
		}

		static string ColorToString (Cairo.Color cairoColor)
		{
			return "R:" + cairoColor.R + " G:" + cairoColor.G + " B:" + cairoColor.B + " A:" + cairoColor.A;
		}

		public override string ToString ()
		{
			return string.Format ("[ChunkStyle: Name={0}, CairoColor={1}, CairoBackgroundColor={2}, FontWeight={3}, FontStyle={4}]", Name, ColorToString (Foreground), ColorToString (Background), FontWeight, FontStyle);
		}

		public static ChunkStyle Import (string name, ColorScheme.VSSettingColor vsc)
		{
			var textColor = new ChunkStyle ();
			textColor.Name = name;
			if (!string.IsNullOrEmpty (vsc.Foreground) && vsc.Foreground != "0x02000000") {
				textColor.Foreground = ColorScheme.ImportVsColor (vsc.Foreground);
				if (textColor.TransparentForeground && name != "Selected Text" && name != "Selected Text(Inactive)")
					textColor.Foreground = new Cairo.Color (0, 0, 0);
			}
			if (!string.IsNullOrEmpty (vsc.Background) && vsc.Background != "0x02000000")
				textColor.Background = ColorScheme.ImportVsColor (vsc.Background);
			if (vsc.BoldFont)
				textColor.FontWeight = FontWeight.Bold;
			return textColor;
		}

		public ChunkStyle Clone ()
		{
			return (ChunkStyle)this.MemberwiseClone ();
		}
	}
	
}
