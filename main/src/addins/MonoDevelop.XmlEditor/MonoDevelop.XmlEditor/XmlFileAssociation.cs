//
// MonoDevelop XML Editor
//
// Copyright (C) 2005-2007 Matthew Ward
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

using MonoDevelop.Core;
using System.Xml;

namespace MonoDevelop.XmlEditor
{
	/// <summary>Represents an file extension that can be handled by the editor.</summary>
	public class XmlFileAssociation : ICustomXmlSerializer
	{
		static readonly string schemaAssociationElementName = "SchemaAssociation";
		static readonly string extensionAttributeName = "extension";
		static readonly string namespaceAttributeName = "namespace";
		static readonly string prefixAttributeName = "prefix";
		
		//deseriialization ctor
		public XmlFileAssociation ()
		{
		}
		
		public XmlFileAssociation (string extension, string namespaceUri, string namespacePrefix)
		{
			this.Extension = extension == null? "" : extension.ToLower ();
			this.NamespaceUri = namespaceUri ?? "";
			this.NamespacePrefix = namespacePrefix ?? "";
		}
		
		public string NamespaceUri { get; private set; }
		public string Extension { get; private set; }
		public string NamespacePrefix { get; private set; }
		
		public ICustomXmlSerializer ReadFrom (XmlReader reader)
		{
			while (reader.Read ()) {
				if (reader.NodeType == XmlNodeType.Element && reader.Name == schemaAssociationElementName) {
					Extension = reader.GetAttribute (extensionAttributeName);
					NamespaceUri = reader.GetAttribute (namespaceAttributeName);
					NamespacePrefix = reader.GetAttribute (prefixAttributeName);
					return this;
				}
			}
			return null;
		}
		
		public void WriteTo (XmlWriter writer)
		{
			writer.WriteStartElement (schemaAssociationElementName);
			writer.WriteAttributeString (extensionAttributeName, Extension);
			writer.WriteAttributeString (namespaceAttributeName, NamespaceUri);
			writer.WriteAttributeString (prefixAttributeName, NamespacePrefix);
			writer.WriteEndElement ();
		}		
	}
}
