//
// ObjectDescription.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
//


using System;
using System.Collections.Specialized;
using System.Xml;
using Mono.Addins.Serialization;

namespace Mono.Addins.Description
{
	public class ObjectDescription: IBinaryXmlElement
	{
		internal XmlElement Element;
		object parent;
		
		internal ObjectDescription (XmlElement elem)
		{
			Element = elem;
		}
		
		internal ObjectDescription ()
		{
		}
		
		public object Parent {
			get { return parent; }
		}
		
		public AddinDescription ParentAddinDescription {
			get {
				if (parent is AddinDescription)
					return (AddinDescription) parent;
				else if (parent is ObjectDescription)
					return ((ObjectDescription)parent).ParentAddinDescription;
				else
					return null;
			}
		}
		
		internal void SetParent (object ob)
		{
			parent = ob;
		}
		
		void IBinaryXmlElement.Write (BinaryXmlWriter writer)
		{
			Write (writer);
		}
		
		void IBinaryXmlElement.Read (BinaryXmlReader reader)
		{
			Read (reader);
		}
		
		internal virtual void Write (BinaryXmlWriter writer)
		{
		}
		
		internal virtual void Read (BinaryXmlReader reader)
		{
		}
		
		internal virtual void SaveXml (XmlElement parent)
		{
		}
		
		internal void CreateElement (XmlElement parent, string nodeName)
		{
			if (Element == null) {
				Element = parent.OwnerDocument.CreateElement (nodeName); 
				parent.AppendChild (Element);
			}
		}
		
		internal string ReadXmlDescription ()
		{
			XmlElement de = Element ["Description"];
			if (de != null)
				return de.InnerText;
			else
				return null;
		}
		
		internal void SaveXmlDescription (string desc)
		{
			XmlElement de = Element ["Description"];
			if (desc != null && desc.Length > 0) {
				if (de == null) {
					de = Element.OwnerDocument.CreateElement ("Description");
					Element.AppendChild (de);
				}
				de.InnerText = desc;
			} else {
				if (de != null)
					Element.RemoveChild (de);
			}
		}
		
		internal virtual void Verify (string location, StringCollection errors)
		{
		}
		
		internal void VerifyNotEmpty (string location, StringCollection errors, string attr, string val)
		{
			if (val == null || val.Length == 0)
				errors.Add (location + ": attribute '" + attr + "' can't be empty.");
		}
	}
}
