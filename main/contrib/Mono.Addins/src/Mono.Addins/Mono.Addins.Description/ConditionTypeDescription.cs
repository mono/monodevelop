//
// ConditionTypeDescription.cs
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
	public class ConditionTypeDescription: ObjectDescription
	{
		string id;
		string typeName;
		string addinId;
		string description;
		
		public ConditionTypeDescription ()
		{
		}
		
		internal ConditionTypeDescription (XmlElement elem): base (elem)
		{
			id = elem.GetAttribute ("id");
			typeName = elem.GetAttribute ("type");
			description = ReadXmlDescription ();
		}
		
		internal override void Verify (string location, StringCollection errors)
		{
			VerifyNotEmpty (location + "ConditionType", errors, Id, "id");
			VerifyNotEmpty (location + "ConditionType (" + Id + ")", errors, TypeName, "type");
		}
		
		public string Id {
			get { return id != null ? id : string.Empty; }
			set { id = value; }
		}
		
		public string TypeName {
			get { return typeName != null ? typeName : string.Empty; }
			set { typeName = value; }
		}
		
		public string Description {
			get { return description != null ? description : string.Empty; }
			set { description = value; }
		}
		
		internal string AddinId {
			get { return addinId; }
			set { addinId = value; }
		}
		
		internal override void SaveXml (XmlElement parent)
		{
			CreateElement (parent, "ConditionType");
			Element.SetAttribute ("id", id);
			Element.SetAttribute ("type", typeName);
			SaveXmlDescription (description);
		}
		
		internal override void Write (BinaryXmlWriter writer)
		{
			writer.WriteValue ("Id", Id);
			writer.WriteValue ("TypeName", TypeName);
			writer.WriteValue ("Description", Description);
			writer.WriteValue ("AddinId", AddinId);
		}
		
		internal override void Read (BinaryXmlReader reader)
		{
			Id = reader.ReadStringValue ("Id");
			TypeName = reader.ReadStringValue ("TypeName");
			if (!reader.IgnoreDescriptionData)
				Description = reader.ReadStringValue ("Description");
			AddinId = reader.ReadStringValue ("AddinId");
		}
	}
}
