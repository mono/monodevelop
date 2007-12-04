//
// ExtensionNodeDescription.cs
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
	public class ExtensionNodeDescription: ObjectDescription, NodeElement
	{
		ExtensionNodeDescriptionCollection childNodes;
		string[] attributes;
		string nodeName;
		
		public ExtensionNodeDescription (string nodeName)
		{
			this.nodeName = nodeName;
		}
		
		internal ExtensionNodeDescription (XmlElement elem)
		{
			Element = elem;
			nodeName = elem.LocalName;
		}
		
		internal ExtensionNodeDescription ()
		{
		}
		
		public ExtensionNodeType GetNodeType ()
		{
			if (Parent is Extension) {
				Extension ext = (Extension) Parent;
				object ob = ext.GetExtendedObject ();
				if (ob is ExtensionPoint) {
					ExtensionPoint ep = (ExtensionPoint) ob;
					return ep.NodeSet.GetAllowedNodeTypes () [NodeName];
				} else if (ob is ExtensionNodeDescription) {
					ExtensionNodeDescription pn = (ExtensionNodeDescription) ob;
					ExtensionNodeType pt = ((ExtensionNodeDescription) pn).GetNodeType ();
					if (pt != null)
						return pt.GetAllowedNodeTypes () [NodeName];
				}
			}
			else if (Parent is ExtensionNodeDescription) {
				ExtensionNodeType pt = ((ExtensionNodeDescription) Parent).GetNodeType ();
				if (pt != null)
					return pt.GetAllowedNodeTypes () [NodeName];
			}
			return null;
		}
		
		public string GetParentPath ()
		{
			if (Parent is Extension)
				return ((Extension)Parent).Path;
			else if (Parent is ExtensionNodeDescription) {
				ExtensionNodeDescription pn = (ExtensionNodeDescription) Parent;
				return pn.GetParentPath () + "/" + pn.Id;
			}
			else
				return string.Empty;
		}
		
		internal override void Verify (string location, StringCollection errors)
		{
			if (nodeName == null || nodeName.Length == 0)
				errors.Add (location + "Node: NodeName can't be empty.");
			ChildNodes.Verify (location + NodeName + "/", errors);
		}
		
		public string NodeName {
			get { return nodeName; }
		}
		
		public string Id {
			get { return GetAttribute ("id"); }
			set { SetAttribute ("id", value); }
		}
		
		public string InsertAfter {
			get { return GetAttribute ("insertafter"); }
			set { SetAttribute ("insertafter", value); }
		}
		
		public string InsertBefore {
			get { return GetAttribute ("insertbefore"); }
			set { SetAttribute ("insertbefore", value); }
		}
		
		public bool IsCondition {
			get { return nodeName == "Condition" || nodeName == "ComplexCondition"; }
		}
		
		internal override void SaveXml (XmlElement parent)
		{
			if (Element == null) {
				Element = parent.OwnerDocument.CreateElement (nodeName);
				parent.AppendChild (Element);
				if (attributes != null) {
					for (int n=0; n<attributes.Length; n+=2)
						Element.SetAttribute (attributes[n], attributes[n+1]);
				}
				ChildNodes.SaveXml (Element);
			}
		}
		
		public string GetAttribute (string key)
		{
			if (Element != null)
				return Element.GetAttribute (key);

			if (attributes == null)
				return string.Empty;
			for (int n=0; n<attributes.Length; n+=2) {
				if (attributes [n] == key)
					return attributes [n+1];
			}
			return string.Empty;
		}
		
		public void SetAttribute (string key, string value)
		{
			if (Element != null) {
				Element.SetAttribute (key, value);
				return;
			}
			
			if (value == null)
				value = string.Empty;
			
			if (attributes == null) {
				attributes = new string [2];
				attributes [0] = key;
				attributes [1] = value;
				return;
			}
			
			for (int n=0; n<attributes.Length; n+=2) {
				if (attributes [n] == key) {
					attributes [n+1] = value;
					return;
				}
			}
			string[] newList = new string [attributes.Length + 2];
			attributes.CopyTo (newList, 0);
			attributes = newList;
			attributes [attributes.Length - 2] = key;
			attributes [attributes.Length - 1] = value;
		}
		
		public NodeAttribute[] Attributes {
			get {
				if (Element != null)
					SaveXmlAttributes ();
				if (attributes == null)
					return new NodeAttribute [0];
				NodeAttribute[] ats = new NodeAttribute [attributes.Length / 2];
				for (int n=0; n<ats.Length; n++) {
					NodeAttribute at = new NodeAttribute ();
					at.name = attributes [n*2];
					at.value = attributes [n*2 + 1];
					ats [n] = at;
				}
				return ats;
			}
		}
		
		public ExtensionNodeDescriptionCollection ChildNodes {
			get {
				if (childNodes == null) {
					childNodes = new ExtensionNodeDescriptionCollection (this);
					if (Element != null) {
						foreach (XmlNode nod in Element.ChildNodes) {
							if (nod is XmlElement)
								childNodes.Add (new ExtensionNodeDescription ((XmlElement)nod));
						}
					}
				}
				return childNodes;
			}
		}
		
		NodeElementCollection NodeElement.ChildNodes {
			get { return ChildNodes; }
		}
		
		void SaveXmlAttributes ()
		{
			attributes = new string [Element.Attributes.Count * 2];
			for (int n=0; n<attributes.Length; n+=2) {
				XmlAttribute at = Element.Attributes [n/2];
				attributes [n] = at.LocalName;
				attributes [n+1] = at.Value;
			}
		}
		
		internal override void Write (BinaryXmlWriter writer)
		{
			if (Element != null)
				SaveXmlAttributes ();
			
			writer.WriteValue ("nodeName", nodeName);
			writer.WriteValue ("attributes", attributes);
			writer.WriteValue ("ChildNodes", ChildNodes);
		}
		
		internal override void Read (BinaryXmlReader reader)
		{
			nodeName = reader.ReadStringValue ("nodeName");
			attributes = (string[]) reader.ReadValue ("attributes");
			childNodes = (ExtensionNodeDescriptionCollection) reader.ReadValue ("ChildNodes", new ExtensionNodeDescriptionCollection (this));
		}
	}
}
