//
// Extension.cs
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
	public class Extension: ObjectDescription, IComparable
	{
		string path;
		ExtensionNodeDescriptionCollection nodes;
		
		public Extension ()
		{
		}
		
		public Extension (string path)
		{
			this.path = path;
		}
		
		// Returns the object extended by this Extension. It can be an ExtensionPoint or
		// an ExtensionNodeDescription.
		public ObjectDescription GetExtendedObject ()
		{
			AddinDescription desc = ParentAddinDescription;
			if (desc == null)
				return null;
			ExtensionPoint ep = FindExtensionPoint (desc, path);
			if (ep == null && desc.OwnerDatabase != null) {
				foreach (Dependency dep in desc.MainModule.Dependencies) {
					AddinDependency adep = dep as AddinDependency;
					if (adep == null) continue;
					Addin ad = desc.OwnerDatabase.GetInstalledAddin (ParentAddinDescription.Domain, adep.FullAddinId);
					if (ad != null && ad.Description != null) {
						ep = FindExtensionPoint (ad.Description, path);
						if (ep != null)
							break;
					}
				}
			}
			if (ep != null) {
				string subp = path.Substring (ep.Path.Length).Trim ('/');
				if (subp.Length == 0)
					return ep; // The extension is directly extending the extension point
				
				// The extension is extending a node of the extension point

				return desc.FindExtensionNode (path, true);
			}
			return null;
		}
		
		public ExtensionNodeTypeCollection GetAllowedNodeTypes ()
		{
			ObjectDescription ob = GetExtendedObject ();
			ExtensionPoint ep = ob as ExtensionPoint;
			if (ep != null)
				return ep.NodeSet.GetAllowedNodeTypes ();
			
			ExtensionNodeDescription node = ob as ExtensionNodeDescription;
			if (node != null) {
				ExtensionNodeType nt = node.GetNodeType ();
				if (nt != null)
					return nt.GetAllowedNodeTypes ();
			}
			return new ExtensionNodeTypeCollection ();
		}
		
		ExtensionPoint FindExtensionPoint (AddinDescription desc, string path)
		{
			foreach (ExtensionPoint ep in desc.ExtensionPoints) {
				if (ep.Path == path || path.StartsWith (ep.Path + "/"))
					return ep;
			}
			return null;
		}
		
		internal override void Verify (string location, StringCollection errors)
		{
			VerifyNotEmpty (location + "Extension", errors, path, "path");
			ExtensionNodes.Verify (location + "Extension (" + path + ")/", errors);
			
			foreach (ExtensionNodeDescription cnode in ExtensionNodes)
				VerifyNode (location, cnode, errors);
		}
		
		void VerifyNode (string location, ExtensionNodeDescription node, StringCollection errors)
		{
			string id = node.GetAttribute ("id");
			if (id.Length > 0)
				id = "(" + id + ")";
			if (node.NodeName == "Condition" && node.GetAttribute ("id").Length == 0) {
				errors.Add (location + node.NodeName + id + ": Missing 'id' attribute in Condition element.");
			}
			if (node.NodeName == "ComplexCondition") {
				if (node.ChildNodes.Count > 0) {
					VerifyConditionNode (location, node.ChildNodes[0], errors);
					for (int n=1; n<node.ChildNodes.Count; n++)
						VerifyNode (location + node.NodeName + id + "/", node.ChildNodes[n], errors);
				}
				else
					errors.Add (location + "ComplexCondition: Missing child condition in ComplexCondition element.");
			}
			foreach (ExtensionNodeDescription cnode in node.ChildNodes)
				VerifyNode (location + node.NodeName + id + "/", cnode, errors);
		}
		
		void VerifyConditionNode (string location, ExtensionNodeDescription node, StringCollection errors)
		{
			string nodeName = node.NodeName;
			if (nodeName != "Or" && nodeName != "And" && nodeName != "Condition") {
				errors.Add (location + "ComplexCondition: Invalid condition element: " + nodeName);
				return;
			}
			foreach (ExtensionNodeDescription cnode in node.ChildNodes)
				VerifyConditionNode (location, cnode, errors);
		}
		
		public Extension (XmlElement element)
		{
			Element = element;
			path = element.GetAttribute ("path");
		}
		
		public string Path {
			get { return path; }
			set { path = value; }
		}
		
		internal override void SaveXml (XmlElement parent)
		{
			if (Element == null) {
				Element = parent.OwnerDocument.CreateElement ("Extension");
				parent.AppendChild (Element);
			}
			Element.SetAttribute ("path", path);
			if (nodes != null)
				nodes.SaveXml (Element);
		}
		
		public ExtensionNodeDescriptionCollection ExtensionNodes {
			get {
				if (nodes == null) {
					nodes = new ExtensionNodeDescriptionCollection (this);
					if (Element != null) {
						foreach (XmlNode node in Element.ChildNodes) {
							XmlElement e = node as XmlElement;
							if (e != null)
								nodes.Add (new ExtensionNodeDescription (e));
						}
					}
				}
				return nodes;
			}
		}
		
		int IComparable.CompareTo (object obj)
		{
			Extension other = (Extension) obj;
			return Path.CompareTo (other.Path);
		}
		
		internal override void Write (BinaryXmlWriter writer)
		{
			writer.WriteValue ("path", path);
			writer.WriteValue ("Nodes", ExtensionNodes);
		}
		
		internal override void Read (BinaryXmlReader reader)
		{
			path = reader.ReadStringValue ("path");
			nodes = (ExtensionNodeDescriptionCollection) reader.ReadValue ("Nodes", new ExtensionNodeDescriptionCollection (this));
		}
	}
}
