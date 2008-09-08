//
// NodeState.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Gui.Components
{
	public class NodeState : ICustomXmlSerializer
	{
		string nodeName;
		
		bool expanded;
		bool selected;
		ExtensibleTreeView.TreeOptions options;
		
		List<NodeState> childrenState;

		public string NodeName {
			get {
				return nodeName;
			}
			set {
				nodeName = value;
			}
		}

		public bool Expanded {
			get {
				return expanded;
			}
			set {
				expanded = value;
			}
		}

		public bool Selected {
			get {
				return selected;
			}
			set {
				selected = value;
			}
		}

		internal ExtensibleTreeView.TreeOptions Options {
			get {
				return options;
			}
			set {
				options = value;
			}
		}

		public System.Collections.Generic.List<NodeState> ChildrenState {
			get {
				return childrenState;
			}
			set {
				childrenState = value;
			}
		}
		
		void ICustomXmlSerializer.WriteTo (XmlWriter writer)
		{
			WriteTo (writer, null);
		}
		
		ICustomXmlSerializer ICustomXmlSerializer.ReadFrom (XmlReader reader)
		{
			return ReadFrom (reader, null);
		}
		
		const string Node              = "Node";
		const string nameAttribute     = "name";
		const string expandedAttribute = "expanded";
		const string selectedAttribute = "selected";

		void WriteTo (XmlWriter writer, ExtensibleTreeView.TreeOptions parentOptions)
		{
			writer.WriteStartElement (Node);
			if (NodeName != null)
				writer.WriteAttributeString (nameAttribute, NodeName);
			if (Expanded)
				writer.WriteAttributeString (expandedAttribute, bool.TrueString);
			if (Selected)
				writer.WriteAttributeString (selectedAttribute, bool.TrueString);

			ExtensibleTreeView.TreeOptions ops = Options;
			if (ops != null) {
				foreach (DictionaryEntry de in ops) {
					object parentVal = parentOptions != null ? parentOptions [de.Key] : null;
					if (parentVal != null && !parentVal.Equals (de.Value) || (parentVal == null && de.Value != null) || parentOptions == null) {
						writer.WriteStartElement ("Option");
						writer.WriteAttributeString ("id", de.Key.ToString());
						writer.WriteAttributeString ("value", de.Value.ToString ());
						writer.WriteEndElement (); // Option
					}
				}
			}
			
			if (ChildrenState != null) { 
				foreach (NodeState ces in ChildrenState) {
					ces.WriteTo (writer, Options != null ? Options : parentOptions);
				}
			}
			
			writer.WriteEndElement (); // NodeState
		}

		ICustomXmlSerializer ReadFrom (XmlReader reader, ExtensibleTreeView.TreeOptions parentOptions)
		{
			NodeState result = new NodeState ();
			result.NodeName = reader.GetAttribute (nameAttribute);
			if (!String.IsNullOrEmpty (reader.GetAttribute (expandedAttribute)))
				result.Expanded = Boolean.Parse (reader.GetAttribute (expandedAttribute));
			if (!String.IsNullOrEmpty (reader.GetAttribute (selectedAttribute)))
				result.Selected = Boolean.Parse (reader.GetAttribute (selectedAttribute));
				
			XmlReadHelper.ReadList (reader, reader.LocalName, delegate () {
				switch (reader.LocalName) {
				case "Option":
					if (result.Options == null) 
						result.Options = parentOptions != null ? parentOptions.CloneOptions (Gtk.TreeIter.Zero) : new ExtensibleTreeView.TreeOptions ();   
					result.Options [reader.GetAttribute ("id")] = bool.Parse (reader.GetAttribute ("value"));
					return true;
				case "Node":
					if (result.ChildrenState == null)
						result.ChildrenState = new List<NodeState> ();
					result.ChildrenState.Add ((NodeState)ReadFrom (reader, result.Options != null ? result.Options : parentOptions));
					return true;
				}
				return false;
			});
			return result;
		}
		
		internal static NodeState SaveState (ExtensibleTreeView pad, ITreeNavigator nav)
		{
			NodeState state = SaveStateRec (pad, nav);
			if (state == null) 
				return new NodeState ();
			else return state;
		}
		
		static NodeState SaveStateRec (ExtensibleTreeView pad, ITreeNavigator nav)
		{
			List<NodeState> childrenState = null;

			if (nav.Filled && nav.MoveToFirstChild ()) {
				do {
					NodeState cs = SaveStateRec (pad, nav);
					if (cs != null) {
						cs.NodeName = nav.NodeName;
						if (childrenState == null) 
							childrenState = new List<NodeState> ();
						childrenState.Add (cs);
					}
				} while (nav.MoveNext ());
				nav.MoveToParent ();
			}
			
			ExtensibleTreeView.TreeOptions ops = pad.GetNodeOptions (nav);
			
			if (ops != null || nav.Expanded || childrenState != null || nav.Selected) {
				NodeState es = new NodeState ();
				es.Expanded = nav.Expanded;
				es.Selected = nav.Selected;
				es.Options = ops;
				es.ChildrenState = childrenState;
				return es;
			} else
				return null;
		}
		
		public override string ToString ()
		{
			return String.Format ("[NodeState: NodeName={0}, Expanded={1}, Selected={2}, Options={3}, #ChildrenState={4}]",
			                      this.nodeName,
			                      this.expanded,
			                      this.selected,
			                      this.options != null ? this.options.ToString () : "null",
			                      this.childrenState != null ? this.childrenState.Count.ToString () : "null");
		}
		 
		internal static void RestoreState (ExtensibleTreeView pad, ITreeNavigator nav, NodeState es)
		{
			if (es == null) 
				return;
			
			if (es.Options != null) 
				pad.SetNodeOptions (nav, es.Options);
			
			pad.ResetState (nav);
			nav.Expanded = es.Expanded;
			
			if (es.ChildrenState != null) {
				foreach (NodeState ces in es.ChildrenState) {
					if (nav.MoveToChild (ces.NodeName, null)) {
						RestoreState (pad, nav, ces);
						nav.MoveToParent ();
					}
				}
			}
			
			if (es.Selected)
				nav.Selected = true;
		}
	}
}
