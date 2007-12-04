//
// ExtensionNodeType.cs
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
using System.Xml;
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using Mono.Addins.Serialization;

namespace Mono.Addins.Description
{
	public class ExtensionNodeType: ExtensionNodeSet
	{
		string typeName;
		string objectTypeName;
		string description;
		string addinId;
		NodeTypeAttributeCollection attributes;
		
		// Cached clr type
		[NonSerialized]
		internal Type Type;
		
		// Cached serializable fields
		[NonSerialized]
		internal Hashtable Fields;
		
		internal class FieldData {
			public FieldInfo Field;
			public bool Required;
			public bool Localizable;
		}
		
		// Addin where this extension type is implemented
		internal string AddinId {
			get { return addinId; }
			set { addinId = value; }
		}
		
		// Type of the extension node
		public string TypeName {
			get { return typeName != null ? typeName : string.Empty; }
			set { typeName = value; }
		}
		
		public string NodeName {
			get { return Id; }
			set { Id = value; }
		}
		
		// Type of the object that the extension creates (only valid for TypeNodeExtension).
		public string ObjectTypeName {
			get { return objectTypeName != null ? objectTypeName : string.Empty; }
			set { objectTypeName = value; }
		}
		
		// The description
		public string Description {
			get { return description != null ? description : string.Empty; }
			set { description = value; }
		}
		
		public NodeTypeAttributeCollection Attributes {
			get {
				if (attributes == null) {
					attributes = new NodeTypeAttributeCollection (this);
					if (Element != null) {
						XmlElement atts = Element ["Attributes"];
						if (atts != null) {
							foreach (XmlNode node in atts.ChildNodes) {
								XmlElement e = node as XmlElement;
								if (e != null)
									attributes.Add (new NodeTypeAttribute (e));
							}
						}
					}
				}
				return attributes;
			}
		}

		internal ExtensionNodeType (XmlElement element): base (element)
		{
			XmlAttribute at = element.Attributes ["type"];
			if (at != null)
				typeName = at.Value;
			at = element.Attributes ["objectType"];
			if (at != null)
				objectTypeName = at.Value;
			XmlElement de = element ["Description"];
			if (de != null)
				description = de.InnerText;
		}
		
		internal ExtensionNodeType ()
		{
		}
			
		internal override string IdAttribute {
			get { return "name"; }
		}
		
		internal override void Verify (string location, StringCollection errors)
		{
			base.Verify (location, errors);
		}
		
		internal override void SaveXml (XmlElement parent, string nodeName)
		{
			base.SaveXml (parent, "ExtensionNode");
			
			XmlElement atts = Element ["Attributes"];
			if (Attributes.Count > 0) {
				if (atts == null) {
					atts = parent.OwnerDocument.CreateElement ("Attributes");
					Element.AppendChild (atts);
				}
				Attributes.SaveXml (atts);
			} else {
				if (atts != null)
					Element.RemoveChild (atts);
			}
			
			if (TypeName.Length > 0)
				Element.SetAttribute ("type", TypeName);
			else
				Element.RemoveAttribute ("type");
			
			if (ObjectTypeName.Length > 0)
				Element.SetAttribute ("objectType", ObjectTypeName);
			else
				Element.RemoveAttribute ("objectType");

			SaveXmlDescription (Description);
		}
		
		internal override void Write (BinaryXmlWriter writer)
		{
			base.Write (writer);
			if (Id.Length == 0)
				Id = "Type";
			if (TypeName.Length == 0)
				typeName = "Mono.Addins.TypeExtensionNode";
			writer.WriteValue ("typeName", typeName);
			writer.WriteValue ("objectTypeName", objectTypeName);
			writer.WriteValue ("description", description);
			writer.WriteValue ("addinId", addinId);
			writer.WriteValue ("Attributes", attributes);
		}
		
		internal override void Read (BinaryXmlReader reader)
		{
			base.Read (reader);
			typeName = reader.ReadStringValue ("typeName");
			objectTypeName = reader.ReadStringValue ("objectTypeName");
			if (!reader.IgnoreDescriptionData)
				description = reader.ReadStringValue ("description");
			addinId = reader.ReadStringValue ("addinId");
			if (!reader.IgnoreDescriptionData)
				attributes = (NodeTypeAttributeCollection) reader.ReadValue ("Attributes", new NodeTypeAttributeCollection (this));
		}
	}
}
