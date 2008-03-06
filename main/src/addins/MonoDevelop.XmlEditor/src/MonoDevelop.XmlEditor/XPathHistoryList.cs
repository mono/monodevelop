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
	public class XPathHistoryList : IXmlConvertable
	{
		const string xpathHistoryListElementName = "XPathHistoryList";
		const string xpathElementName = "XPath";

		List<string> xpaths = new List<string>();
		
		public XPathHistoryList()
		{
		}
		
		/// <summary>
		/// Adds a single xpath to the list.
		/// </summary>
		public void Add(string xpath)
		{
			xpaths.Add(xpath);
		}
		
		/// <summary>
		/// Gets the xpath strings.
		/// </summary>
		public string[] GetXPaths()
		{
			return xpaths.ToArray();
		}
		
		/// <summary>
		/// Creates an xml element from this XPathHistoryList.
		/// </summary>
		public XmlElement ToXmlElement(XmlDocument doc)
		{	
			XmlElement element = doc.CreateElement(xpathHistoryListElementName);
			foreach (string xpath in xpaths) {
				XmlElement xpathElement = doc.CreateElement(xpathElementName); 
				xpathElement.InnerText = xpath;
				element.AppendChild(xpathElement);
			}
			return element;	
		}
		
		/// <summary>
		/// Creates an XPathHistoryList from the saved xml.
		/// </summary>
		public object FromXmlElement(XmlElement element)
		{
			XPathHistoryList list = null;
			if (element != null) {
				if (element.Name == xpathHistoryListElementName) {
					list = new XPathHistoryList();
					foreach (XmlNode node in element.ChildNodes) {
						XmlElement xpathElement = node as XmlElement;
						if (xpathElement != null) {
							list.Add(xpathElement.InnerText);
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
