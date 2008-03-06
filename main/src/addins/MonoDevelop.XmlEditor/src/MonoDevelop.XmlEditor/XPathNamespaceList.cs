//
// MonoDevelop XML Editor
//
// Copyright (C) 2006 Matthew Ward
//

using MonoDevelop.Core.Properties;
using System;
using System.Collections.Generic;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	public class XPathNamespaceList : IXmlConvertable
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
		/// Creates an xml element from this XPathNamespaceList.
		/// </summary>
		public XmlElement ToXmlElement(XmlDocument doc)
		{	
			XmlElement element = doc.CreateElement(xpathNamespaceListElementName);
			foreach (XmlNamespace ns in namespaces) {
				XmlElement namespaceElement = doc.CreateElement(xpathNamespaceElementName); 
				namespaceElement.InnerText = ns.ToString();
				element.AppendChild(namespaceElement);
			}
			return element;	
		}
		
		/// <summary>
		/// Creates an XPathNamespaceList from the saved xml.
		/// </summary>
		public object FromXmlElement(XmlElement element)
		{
			XPathNamespaceList list = null;
			if (element != null) {
				if (element.Name == xpathNamespaceListElementName) {
					list = new XPathNamespaceList();
					foreach (XmlNode node in element.ChildNodes) {
						XmlElement namespaceElement = node as XmlElement;
						if (namespaceElement != null) {
							XmlNamespace ns = XmlNamespace.FromString(namespaceElement.InnerText);
							if (ns != null) {
								list.Add(ns.Prefix, ns.Uri);
							}
						}
					}
				} else {
					throw new UnknownPropertyNodeException(element.Name);
				}					
			}
			return list;
		}
	}
	
}
