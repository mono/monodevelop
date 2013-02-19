//
// AmbientColor.cs
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

	public class AmbientColor
	{
		public string Name { get; private set; }
		public readonly List<Tuple<string, Cairo.Color>> Colors = new List<Tuple<string, Cairo.Color>> ();
		
		public Cairo.Color GetColor (string name)
		{
			foreach (var color in Colors) {
				if (color.Item1 == name)
					return color.Item2;
			}

			return new Cairo.Color (0, 0, 0);
		}

		public static AmbientColor Create (XElement element, Dictionary<string, Cairo.Color> palette)
		{
			var result = new AmbientColor ();
			foreach (var node in element.DescendantNodes ()) {
				if (node.NodeType == System.Xml.XmlNodeType.Element) {
					var el = (XElement)node;
					switch (el.Name.LocalName) {
					case "name":
						result.Name = el.Value;
						break;
					default:
						result.Colors.Add (Tuple.Create (el.Name.LocalName, ColorScheme.ParsePaletteColor (palette, el.Value)));
						break;
					}
				}
			}
			
			return result;
		}

		public override bool Equals (object obj)
		{
			if (obj == null)
				return false;
			if (ReferenceEquals (this, obj))
				return true;
			if (obj.GetType () != typeof(AmbientColor))
				return false;
			AmbientColor other = (AmbientColor)obj;
			return Colors.Equals (other.Colors) && Name == other.Name;
		}

		public override int GetHashCode ()
		{
			unchecked {
				return (Colors != null ? Colors.GetHashCode () : 0) ^ (Name != null ? Name.GetHashCode () : 0);
			}
		}
		

		public static AmbientColor Import (Dictionary<string, ColorScheme.VSSettingColor> colors, string vsSetting)
		{
			var result = new AmbientColor ();
			var attrs = vsSetting.Split (',');
			foreach (var attr in attrs) {
				var info = attr.Split ('=');
				if (info.Length != 2)
					continue;
				var idx = info [1].LastIndexOf ('/');
				var source = info [1].Substring (0, idx);
				var dest   = info [1].Substring (idx + 1);

				ColorScheme.VSSettingColor color;
				if (!colors.TryGetValue (source, out color))
					continue;
				result.Name = color.Name;
				string colorString;
				switch (dest) {
				case "Foreground":
					colorString = color.Foreground;
					break;
				case "Background":
					colorString = color.Background;
					break;
				default:
					throw new InvalidDataException ("Invalid attribute source: " + dest);
				}
				result.Colors.Add (Tuple.Create (info [0], ColorScheme.ImportVsColor (colorString)));
			}
			if (result.Colors.Count == 0)
				return null;
			return result;
		}
	}
	
}
