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
	public class XPathHistoryList : ICustomXmlSerializer
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
		/// Creates an XPathHistoryList from the saved xml.
		/// </summary>
		public ICustomXmlSerializer ReadFrom(XmlReader reader)
		{
			XPathHistoryList list = new XPathHistoryList();
			bool finished = false;
			while (reader.Read() && !finished) {
				switch (reader.NodeType) {
					case XmlNodeType.Element:
						if (reader.Name == xpathElementName) {
							list.Add(reader.ReadElementString());
						}
						break;
					case XmlNodeType.EndElement:
						if (reader.Name != xpathElementName) {
							finished = true;
						}
						break;
				}
			}
			return list;
		}

		/// <summary>
		/// Creates an xml element from this XPathHistoryList.
		/// </summary>
		public void WriteTo(XmlWriter writer)
		{
			writer.WriteStartElement(xpathHistoryListElementName);
			foreach (string xpath in xpaths) {
				writer.WriteElementString(xpathElementName, xpath);
			}
			writer.WriteEndElement();
		}		
	}
}
