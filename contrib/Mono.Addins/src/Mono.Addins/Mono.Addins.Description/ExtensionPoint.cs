//
// ExtensionPoint.cs
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
using System.Collections;
using System.Xml;
using Mono.Addins.Serialization;
using System.Collections.Specialized;

namespace Mono.Addins.Description
{
	public class ExtensionPoint: ObjectDescription
	{
		string path;
		string name;
		string description;
		ExtensionNodeSet nodeSet;
		ConditionTypeDescriptionCollection conditions;
		
		// Information gathered from others addins:
		
		StringCollection addins;	// Add-ins which extend this extension point
		string rootAddin;			// Add-in which defines this extension point
		
		internal ExtensionPoint (XmlElement elem): base (elem)
		{
			path = elem.GetAttribute ("path");
			name = elem.GetAttribute ("name");
			description = ReadXmlDescription ();
		}
		
		public ExtensionPoint ()
		{
		}
		
		internal override void Verify (string location, StringCollection errors)
		{
			VerifyNotEmpty (location + "ExtensionPoint", errors, Path, "path");
			NodeSet.Verify (location + "ExtensionPoint (" + Path + ")/", errors);
			Conditions.Verify (location + "ExtensionPoint (" + Path + ")/", errors);
		}
		
		internal void SetExtensionsAddinId (string addinId)
		{
			NodeSet.SetExtensionsAddinId (addinId);
			foreach (ConditionTypeDescription cond in Conditions)
				cond.AddinId = addinId;
			Addins.Add (addinId);
		}
		
		internal void MergeWith (string thisAddinId, ExtensionPoint ep)
		{
			NodeSet.MergeWith (thisAddinId, ep.NodeSet);
			
			foreach (ConditionTypeDescription cond in ep.Conditions) {
				if (cond.AddinId != thisAddinId && !Conditions.Contains (cond))
					Conditions.Add (cond);
			}
			foreach (string s in ep.Addins) {
				if (!Addins.Contains (s))
					Addins.Add (s);
			}
		}
		
		internal void UnmergeExternalData (string thisAddinId, Hashtable addinsToUnmerge)
		{
			NodeSet.UnmergeExternalData (thisAddinId, addinsToUnmerge);
			
			ArrayList todel = new ArrayList ();
			foreach (ConditionTypeDescription cond in Conditions) {
				if (cond.AddinId != thisAddinId && (addinsToUnmerge == null || addinsToUnmerge.Contains (cond.AddinId)))
					todel.Add (cond);
			}
			foreach (ConditionTypeDescription cond in todel)
				Conditions.Remove (cond);
			
			if (addinsToUnmerge == null)
				Addins.Clear ();
			else {
				foreach (string s in addinsToUnmerge.Keys)
					Addins.Remove (s);
			}
			if (thisAddinId != null && !Addins.Contains (thisAddinId))
				Addins.Add (thisAddinId);
		}
		
		internal void Clear ()
		{
			NodeSet.Clear ();
			Conditions.Clear ();
			Addins.Clear ();
		}
		
		internal override void SaveXml (XmlElement parent)
		{
			CreateElement (parent, "ExtensionPoint"); 
			
			Element.SetAttribute ("path", Path);
			
			if (Name.Length > 0)
				Element.SetAttribute ("name", Name);
			else
				Element.RemoveAttribute ("name");
				
			SaveXmlDescription (Description);
			
			if (nodeSet != null) {
				nodeSet.Element = Element;
				nodeSet.SaveXml (parent);
			}
		}
		
		public string Path {
			get { return path != null ? path : string.Empty; }
			set { path = value; }
		}
		
		public string Name {
			get { return name != null ? name : string.Empty; }
			set { name = value; }
		}
		
		public string Description {
			get { return description != null ? description : string.Empty; }
			set { description = value; }
		}
		
		internal StringCollection Addins {
			get {
				if (addins == null)
					addins = new StringCollection ();
				return addins;
			}
		}
		
		internal string RootAddin {
			get { return rootAddin; }
			set { rootAddin = value; }
		}
		
		public ExtensionNodeSet NodeSet {
			get {
				if (nodeSet == null) {
					if (Element != null)
						nodeSet = new ExtensionNodeSet (Element);
					else
						nodeSet = new ExtensionNodeSet ();
					nodeSet.SetParent (this);
				}
				return nodeSet;
			}
		}
		
		internal void SetNodeSet (ExtensionNodeSet nset)
		{
			// Used only by the addin updater
			nodeSet = nset;
			nodeSet.SetParent (this);
		}
		
		public ConditionTypeDescriptionCollection Conditions {
			get {
				if (conditions == null) {
					conditions = new ConditionTypeDescriptionCollection (this);
					if (Element != null) {
						foreach (XmlElement elem in Element.SelectNodes ("ConditionType"))
							conditions.Add (new ConditionTypeDescription (elem));
					}
				}
				return conditions;
			}
		}
		
		public ExtensionNodeType AddExtensionNode (string name, string typeName)
		{
			ExtensionNodeType ntype = new ExtensionNodeType ();
			ntype.Id = name;
			ntype.TypeName = typeName;
			NodeSet.NodeTypes.Add (ntype);
			return ntype;
		}
		
		internal override void Write (BinaryXmlWriter writer)
		{
			writer.WriteValue ("path", path);
			writer.WriteValue ("name", name);
			writer.WriteValue ("description", Description);
			writer.WriteValue ("rootAddin", rootAddin);
			writer.WriteValue ("addins", Addins);
			writer.WriteValue ("NodeSet", NodeSet);
			writer.WriteValue ("Conditions", Conditions);
		}
		
		internal override void Read (BinaryXmlReader reader)
		{
			path = reader.ReadStringValue ("path");
			name = reader.ReadStringValue ("name");
			if (!reader.IgnoreDescriptionData)
				description = reader.ReadStringValue ("description");
			rootAddin = reader.ReadStringValue ("rootAddin");
			addins = (StringCollection) reader.ReadValue ("addins", new StringCollection ());
			nodeSet = (ExtensionNodeSet) reader.ReadValue ("NodeSet");
			conditions = (ConditionTypeDescriptionCollection) reader.ReadValue ("Conditions", new ConditionTypeDescriptionCollection (this));
			if (nodeSet != null)
				nodeSet.SetParent (this);
		}
	}
}
