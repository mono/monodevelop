//
// ObjectsDocument.cs
//
// Authors:
//   Lluis Sanchez Gual
//   Mike Kestner
//
// Copyright (C) 2006-2008 Novell, Inc (http://www.novell.com)
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
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using MonoDevelop.Ide.TypeSystem;
using Microsoft.CodeAnalysis;
using ICSharpCode.NRefactory6.CSharp;
using MonoDevelop.CSharp.Refactoring;
using MonoDevelop.Refactoring;

namespace MonoDevelop.GtkCore
{


	public class ObjectsDocument : XmlDocument
	{
		string path;

		public ObjectsDocument (string path) : base ()
		{
			this.path = path;
			Load (path);
		}
		
		public StringCollection ObjectNames {
			get {
				StringCollection names = new StringCollection ();
				foreach (XmlNode node in DocumentElement) {
					if (node.Name != "object")
						continue;
					XmlElement elem = node as XmlElement;
					names.Add (elem.GetAttribute ("type"));
				}
				return names;
			}
		}

		enum SyncState 
		{
			Unspecified,
			Off,
			On,
		}

		SyncState AttrSyncState {
			get {
				if (DocumentElement.HasAttribute ("attr-sync")) {
					if (string.Equals (DocumentElement.GetAttribute ("attr-sync"), "off", StringComparison.OrdinalIgnoreCase))
						return SyncState.Off;
					else
						return SyncState.On;
				} else
					return SyncState.Unspecified;
			}
			set {
				switch (value) {
				case SyncState.Unspecified:
					DocumentElement.RemoveAttribute ("attr-sync");
					break;
				case SyncState.Off:
					DocumentElement.SetAttribute ("attr-sync", "off");
					break;
				case SyncState.On:
					DocumentElement.SetAttribute ("attr-sync", "on");
					break;
				default:
					throw new ArgumentOutOfRangeException ("value");
				}
				Save ();
			}
		}

		public void Save ()
		{
			//Always write line endings as \n to be consistent with other stetic files
			//and explicitly write with no BOM or XML declaration in order to be consistent with existing format.
			var settings = new XmlWriterSettings () {
				Encoding = Stetic.EncodingUtility.UTF8NoBom,
				NewLineChars = "\n",
				Indent = true,
				OmitXmlDeclaration = true,
			};
			using (var writer = XmlTextWriter.Create (path, settings)) {
				Save (writer);
			}
		}
		void InsertToolboxItemAttributes (WidgetParser parser)
		{
			var tb_items = parser.GetToolboxItems ();
			foreach (string clsname in ObjectNames) {
				if (tb_items.ContainsKey (clsname))
					continue;

				var cls = parser.GetClass (clsname);
				if (cls == null)
					continue;
				CodeGenerationService.AddAttribute (cls, "System.ComponentModel.ToolboxItem", true);
				XmlElement elem = DocumentElement.SelectSingleNode ("object[@type='" + clsname + "']") as XmlElement;
				if (elem != null && elem.HasAttribute ("palette-category")) {
					CodeGenerationService.AddAttribute (cls, "System.ComponentModel.Category", elem.GetAttribute ("palette-category"));
				}
			}
		}

		public void Update (WidgetParser parser, Stetic.Project stetic)
		{
			if (AttrSyncState == SyncState.Unspecified) {
				InsertToolboxItemAttributes (parser);
				AttrSyncState = SyncState.On;
				return;
			} else if (AttrSyncState == SyncState.Off)
				return;

			StringCollection tb_names = new StringCollection ();
			foreach (var cls in parser.GetToolboxItems().Values) {
				UpdateClass (parser, stetic, cls, null);
				tb_names.Add (cls.GetFullName ());
			}

			List<XmlElement> toDelete = new List<XmlElement> ();

			foreach (XmlElement elem in SelectNodes ("objects/object")) {
				string name = elem.GetAttribute ("type");
				if (!tb_names.Contains (name))
					toDelete.Add (elem);
			}

			foreach (XmlElement elem in toDelete)
				elem.ParentNode.RemoveChild (elem);

			Save ();
		}

		void UpdateClass (WidgetParser parser, Stetic.Project stetic, ITypeSymbol widgetClass, ITypeSymbol wrapperClass)
		{
			string typeName = widgetClass.GetFullName();
			string basetypeName = GetBaseType (parser, widgetClass, stetic);
			XmlElement objectElem = (XmlElement) SelectSingleNode ("objects/object[@type='" + typeName + "']");
			
			if (objectElem == null) {
			
				// The widget class is not yet in the XML file. Create an element for it.
				objectElem = CreateElement ("object");
				objectElem.SetAttribute ("type", typeName);
				string category = widgetClass.GetComponentCategory();
				if (category == String.Empty)
					objectElem.SetAttribute ("palette-category", "General");
				else
					objectElem.SetAttribute ("palette-category", category);
				objectElem.SetAttribute ("allow-children", "false");
				if (wrapperClass != null)
					objectElem.SetAttribute ("wrapper", wrapperClass.GetFullName());
				
				// By default add a reference to Gtk.Widget properties and events
				XmlElement itemGroups = objectElem.OwnerDocument.CreateElement ("itemgroups");
				objectElem.AppendChild (itemGroups);
				
				itemGroups = objectElem.OwnerDocument.CreateElement ("signals");
				objectElem.AppendChild (itemGroups);
				
				objectElem.SetAttribute ("base-type", basetypeName);
				DocumentElement.AppendChild (objectElem);
			}
			
			UpdateObject (parser, basetypeName, objectElem, widgetClass, wrapperClass);
		}
		
		string GetBaseType (WidgetParser parser, ITypeSymbol widgetClass, Stetic.Project stetic)
		{
			string[] types = stetic.GetWidgetTypes ();
			Hashtable typesHash = new Hashtable ();
			foreach (string t in types)
				typesHash [t] = t;
				
			string ret = parser.GetBaseType (widgetClass, typesHash);
			return ret ?? "Gtk.Widget";
		}
		
		void UpdateObject (WidgetParser parser, string topType, XmlElement objectElem, ITypeSymbol widgetClass, ITypeSymbol wrapperClass)
		{
			if (widgetClass.DeclaredAccessibility == Accessibility.Public)
				objectElem.RemoveAttribute ("internal");
			else
				objectElem.SetAttribute ("internal", "true");

			ListDictionary properties = new ListDictionary ();
			ListDictionary events = new ListDictionary ();
			
			parser.CollectMembers (widgetClass, true, topType, properties, events);
			if (wrapperClass != null)
				parser.CollectMembers (wrapperClass, false, null, properties, events);
			
			foreach (IPropertySymbol prop in properties.Values)
				MergeProperty (parser, objectElem, prop);
			
			foreach (IEventSymbol ev in events.Values)
				MergeEvent (parser, objectElem, ev);
			
			// Remove old properties
			ArrayList toDelete = new ArrayList ();
			foreach (XmlElement xprop in objectElem.SelectNodes ("itemgroups/itemgroup/property")) {
				if (!properties.Contains (xprop.GetAttribute ("name")))
					toDelete.Add (xprop);
			}
			
			// Remove old signals
			foreach (XmlElement xevent in objectElem.SelectNodes ("signals/itemgroup/signal")) {
				if (!events.Contains (xevent.GetAttribute ("name")))
					toDelete.Add (xevent);
			}
			
			foreach (XmlElement el in toDelete) {
				XmlElement pe = (XmlElement) el.ParentNode;
				pe.RemoveChild (el);
				if (pe.ChildNodes.Count == 0)
					pe.ParentNode.RemoveChild (pe);
			}
		}
		
		void MergeProperty (WidgetParser parser, XmlElement objectElem, IPropertySymbol prop)
		{
			XmlElement itemGroups = objectElem ["itemgroups"];
			if (itemGroups == null) {
				itemGroups = objectElem.OwnerDocument.CreateElement ("itemgroups");
				objectElem.AppendChild (itemGroups);
			}
			
			string cat = prop.GetComponentCategory ();
			XmlElement itemGroup = GetItemGroup (prop.ContainingType, itemGroups, cat, "Properties");
			
			XmlElement propElem = (XmlElement) itemGroup.SelectSingleNode ("property[@name='" + prop.Name + "']");
			if (propElem == null) {
				propElem = itemGroup.OwnerDocument.CreateElement ("property");
				propElem.SetAttribute ("name", prop.Name);
				itemGroup.AppendChild (propElem);
			}
		}
		
		void MergeEvent (WidgetParser parser, XmlElement objectElem, IEventSymbol evnt)
		{
			XmlElement itemGroups = objectElem ["signals"];
			if (itemGroups == null) {
				itemGroups = objectElem.OwnerDocument.CreateElement ("signals");
				objectElem.AppendChild (itemGroups);
			}
			
			string cat = evnt.GetComponentCategory ();
			XmlElement itemGroup = GetItemGroup (evnt.ContainingType, itemGroups, cat, "Signals");
			
			XmlElement signalElem = (XmlElement) itemGroup.SelectSingleNode ("signal[@name='" + evnt.Name + "']");
			if (signalElem == null) {
				signalElem = itemGroup.OwnerDocument.CreateElement ("signal");
				signalElem.SetAttribute ("name", evnt.Name);
				itemGroup.AppendChild (signalElem);
			}
		}
		
		XmlElement GetItemGroup (ITypeSymbol cls, XmlElement itemGroups, string cat, string groupName)
		{
			XmlElement itemGroup;
			
			if (cat != "")
				itemGroup = (XmlElement) itemGroups.SelectSingleNode ("itemgroup[@name='" + cat + "']");
			else
				itemGroup = (XmlElement) itemGroups.SelectSingleNode ("itemgroup[(not(@name) or @name='') and not(@ref)]");
			
			if (itemGroup == null) {
				itemGroup = itemGroups.OwnerDocument.CreateElement ("itemgroup");
				if (cat != null && cat != "") {
					itemGroup.SetAttribute ("name", cat);
					itemGroup.SetAttribute ("label", cat);
				} else
					itemGroup.SetAttribute ("label", cls.Name + " " + groupName);
				itemGroups.AppendChild (itemGroup);
			}
			return itemGroup;
		}
	}	
}
