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
			return Colors.First (t => t.Item1 == name).Item2;
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
	}
	
}
