//
// MSBuildObject.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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

using System.Xml;


namespace MonoDevelop.Projects.Formats.MSBuild
{
	
	public class MSBuildObject
	{
		XmlElement elem;

		public MSBuildObject (XmlElement elem)
		{
			this.elem = elem;
		}
		
		public XmlElement Element {
			get { return elem; }
			internal set { elem = value; }
		}

		protected XmlElement AddChildElement (string name)
		{
			XmlElement e = elem.OwnerDocument.CreateElement (null, name, MSBuildProject.Schema);
			elem.AppendChild (e);
			return e;
		}

		public string Label {
			get { return Element.GetAttribute ("Label"); }
			set { Element.SetAttribute ("Label", value); }
		}

		public string Condition {
			get {
				return Element != null ? Element.GetAttribute ("Condition") : null;
			}
			set {
				if (string.IsNullOrEmpty (value))
					Element.RemoveAttribute ("Condition");
				else
					Element.SetAttribute ("Condition", value);
			}
		}
	}

}
