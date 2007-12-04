//
// NodeTypeAttribute.cs
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
using System.Collections.Specialized;
using Mono.Addins.Serialization;

namespace Mono.Addins.Description
{
	public class NodeTypeAttribute: ObjectDescription
	{
		string name;
		string type;
		bool required;
		bool localizable;
		string description;
	
		public NodeTypeAttribute()
		{
		}
		
		public string Name {
			get { return name != null ? name : string.Empty; }
			set { name = value; }
		}
		
		public bool Required {
			get { return required; }
			set { required = value; }
		}
		
		public bool Localizable {
			get { return localizable; }
			set { localizable = value; }
		}
		
		public string Type {
			get { return type != null ? type : string.Empty; }
			set { type = value; }
		}
		
		public string Description {
			get { return description != null ? description : string.Empty; }
			set { description = value; }
		}
		
		internal override void Verify (string location, StringCollection errors)
		{
			VerifyNotEmpty (location + "Attribute", errors, Name, "name");
		}
		
		internal NodeTypeAttribute (XmlElement elem): base (elem)
		{
			name = elem.GetAttribute ("name");
			type = elem.GetAttribute ("type");
			required = elem.GetAttribute ("required").ToLower () == "true";
			localizable = elem.GetAttribute ("localizable").ToLower () == "true";
			description = ReadXmlDescription ();
		}
		
		internal override void SaveXml (XmlElement parent)
		{
			CreateElement (parent, "Attribute");
			Element.SetAttribute ("name", name);

			if (Type.Length > 0)
				Element.SetAttribute ("type", Type);
			else
				Element.RemoveAttribute ("type");
			
			if (required)
				Element.SetAttribute ("required", "True");
			else
				Element.RemoveAttribute ("required");
			
			if (localizable)
				Element.SetAttribute ("localizable", "True");
			else
				Element.RemoveAttribute ("localizable");
			
			SaveXmlDescription (description);
		}
		
		internal override void Write (BinaryXmlWriter writer)
		{
			writer.WriteValue ("name", name);
			writer.WriteValue ("type", type);
			writer.WriteValue ("required", required);
			writer.WriteValue ("description", description);
			writer.WriteValue ("localizable", localizable);
		}
		
		internal override void Read (BinaryXmlReader reader)
		{
			name = reader.ReadStringValue ("name");
			type = reader.ReadStringValue ("type");
			required = reader.ReadBooleanValue ("required");
			if (!reader.IgnoreDescriptionData)
				description = reader.ReadStringValue ("description");
			localizable = reader.ReadBooleanValue ("localizable");
		}
	}
}
