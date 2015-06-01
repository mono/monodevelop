//
// MSBuildProjectExtensions.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using System.Xml;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class MSBuildProjectExtensions: MSBuildObject
	{
		XmlElement elem;
		XmlDocument doc;

		public MSBuildProjectExtensions ()
		{
		}

		internal override void Read (MSBuildXmlReader reader)
		{
			base.Read (reader);
			doc = new XmlDocument ();
			elem = (XmlElement) doc.ReadNode (reader.XmlReader);
		}

		internal override string GetElementName ()
		{
			return "ProjectExtensions";
		}

		internal bool IsEmpty {
			get {
				return elem == null || elem.ChildNodes.OfType<XmlNode> ().All (n => n is XmlWhitespace);
			}
		}

		public XmlElement GetProjectExtension (string section)
		{
			if (elem == null)
				return null;
			return elem.SelectSingleNode ("tns:" + section, MSBuildProject.XmlNamespaceManager) as XmlElement;
		}

		public void SetProjectExtension (XmlElement value)
		{
			if (doc == null) {
				doc = new XmlDocument ();
				elem = doc.CreateElement (null, "ProjectExtensions", MSBuildProject.Schema);
				doc.DocumentElement.AppendChild (elem);
			}
			
			if (value.OwnerDocument != doc)
				value = (XmlElement)doc.ImportNode (value, true);

			XmlElement sec = elem [value.LocalName];
			if (sec == null)
				elem.AppendChild (value);
			else {
				elem.InsertAfter (value, sec);
				XmlUtil.RemoveElementAndIndenting (sec);
			}
			XmlUtil.Indent (Project.TextFormat, value, true);
			NotifyChanged ();
		}

		public void RemoveProjectExtension (string section)
		{
			if (doc == null)
				return;
			
			XmlElement es = elem.SelectSingleNode ("tns:" + section, MSBuildProject.XmlNamespaceManager) as XmlElement;
			if (es != null) {
				XmlUtil.RemoveElementAndIndenting (es);
				NotifyChanged ();
			}
		}
	}
}

