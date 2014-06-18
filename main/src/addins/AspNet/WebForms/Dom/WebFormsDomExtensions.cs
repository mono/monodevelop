//
// WebFormsDomExtensions.cs
//
// Author:
//   Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Xml.Parser;
using MonoDevelop.Xml.Dom;

namespace MonoDevelop.AspNet.WebForms.Dom
{
	public static class WebFormsDomExtensions
	{
		static XName scriptName = new XName ("script");
		static XName runatName = new XName ("runat");
		static XName idName = new XName ("id");

		public static bool IsRunatServer (this XElement el)
		{
			var val = el.Attributes.GetValue (runatName, true);
			return string.Equals (val, "server", StringComparison.OrdinalIgnoreCase);
		}

		public static string GetId (this IAttributedXObject el)
		{
			return el.Attributes.GetValue (idName, true);
		}

		public static bool IsServerScriptTag (this XElement el)
		{
			return XName.Equals (el.Name, scriptName, true) && IsRunatServer (el);
		}

		public static IEnumerable<T> WithName<T> (this IEnumerable<XNode> nodes, XName name, bool ignoreCase)
			where T : XNode, INamedXObject
		{
			return nodes.OfType<T> ().Where (el => XName.Equals (el.Name, name, ignoreCase));
		}

		public static IEnumerable<string> GetAllPlaceholderIds (this XDocument doc)
		{
			return doc.AllDescendentNodes
				.WithName<XElement> (new XName ("asp", "ContentPlaceHolder"), true)
				.Where (x => x.IsRunatServer ())
				.Select (x => x.GetId ())
				.Where (id => !string.IsNullOrEmpty (id));
		}
	}
}
