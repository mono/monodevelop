//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//

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
