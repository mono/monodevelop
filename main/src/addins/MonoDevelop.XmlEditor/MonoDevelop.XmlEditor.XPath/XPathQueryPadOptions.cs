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

/*

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

*/
