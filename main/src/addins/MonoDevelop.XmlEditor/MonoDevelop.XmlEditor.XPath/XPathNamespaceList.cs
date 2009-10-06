//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

using MonoDevelop.Core;
using System;
using System.Collections.Generic;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	public class XPathNamespaceList : ICustomXmlSerializer
	{
		const string xpathNamespaceListElementName = "XPathNamespaceList";
		const string xpathNamespaceElementName = "Namespace";
	
		List<XmlNamespace> namespaces = new List<XmlNamespace>();

		public XPathNamespaceList()
		{
		}
		
		/// <summary>
		/// Adds a single namespace to the list.
		/// </summary>
		public void Add(string prefix, string uri)
		{
			namespaces.Add(new XmlNamespace(prefix, uri));
		}
		
		/// <summary>
		/// Gets the namespaces.
		/// </summary>
		public XmlNamespace[] GetNamespaces()
		{
			return namespaces.ToArray();
		}
						
		/// <summary>
		/// Creates an XPathNamespaceList from the saved xml.
		/// </summary>
		public ICustomXmlSerializer ReadFrom(XmlReader reader)
		{
			XPathNamespaceList list = new XPathNamespaceList();
			bool finished = false;
			while (reader.Read() && !finished) {
				switch (reader.NodeType) {
					case XmlNodeType.Element:
						if (reader.Name == xpathNamespaceElementName) {
							XmlNamespace ns = XmlNamespace.FromString(reader.ReadElementString());
							list.Add(ns.Prefix, ns.Uri);
						}
						break;
					case XmlNodeType.EndElement:
						if (reader.Name != xpathNamespaceElementName) {
							finished = true;
						}
						break;
				}
			}
			return list;
		}

		/// <summary>
		/// Creates an xml element from an XPathNamespaceList.
		/// </summary>
		public void WriteTo(XmlWriter writer)
		{
			writer.WriteStartElement(xpathNamespaceListElementName);
			foreach (XmlNamespace ns in namespaces) {
				writer.WriteElementString(xpathNamespaceElementName, ns.ToString());
			}
			writer.WriteEndElement();
		}	
	}	
}
