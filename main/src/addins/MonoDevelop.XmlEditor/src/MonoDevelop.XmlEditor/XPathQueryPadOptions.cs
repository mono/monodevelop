//
// MonoDevelop XML Editor
//
// Copyright (C) 2006-2007 Matthew Ward
//

using MonoDevelop.Core;
using System;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	/// <summary>
	/// The XPath Query Pad options.
	/// </summary>
	public class XPathQueryPadOptions
	{
		public const string OptionsProperty = "XPathQueryPad.Options";
		public const string NamespacesProperty = "Namespaces";
		public const string LastXPathQueryProperty = "LastXPathQuery";
		public const string XPathHistoryProperty = "XPathQueryHistory";
		static Properties properties;

		static XPathQueryPadOptions()
 		{
 			properties = PropertyService.Get(OptionsProperty, new Properties());
		}

 		static Properties Properties {
			get {
				return properties;
 			}
		}
		
		/// <summary>
		/// The last xpath query entered in the XPath Query Pad's
		/// combo box.
		/// </summary>
		public static string LastXPathQuery {
			get {
				return Properties.Get(LastXPathQueryProperty, String.Empty);
			}
			set {
				Properties.Set(LastXPathQueryProperty, value);
			}
		}
		
		/// <summary>
		/// Gets or sets the xpath history.
		/// </summary>
		public static XPathHistoryList XPathHistory {
			get {
				return (XPathHistoryList)Properties.Get(XPathHistoryProperty, new XPathHistoryList());
			}
			set {
				Properties.Set(XPathHistoryProperty, value);
			}
		}
		
		/// <summary>
		/// Gets or sets the xpath namespaces used.
		/// </summary>
		public static XPathNamespaceList Namespaces {
			get {
				return (XPathNamespaceList)Properties.Get(NamespacesProperty, new XPathNamespaceList());
			}
			set {
				Properties.Set(NamespacesProperty, value);
			}
		}
	}
}
