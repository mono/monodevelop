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
		public string NodeName { get; set; }
		public bool Expanded { get; set; }
		public bool Selected { get; set; }
		public bool IsRoot { get { return NodeName == "__root__"; } }
		public List<NodeState> ChildrenState { get; set; }

		internal static NodeState CreateRoot ()
		{
			return new NodeState { NodeName = "__root__" };
		}

		internal Dictionary<string,bool> Options { get; set; }
		
		void ICustomXmlSerializer.WriteTo (XmlWriter writer)
		{
			WriteTo (writer);
		}
		
		ICustomXmlSerializer ICustomXmlSerializer.ReadFrom (XmlReader reader)
		{
			return ReadFrom (reader);
		}
		
		const string Node              = "Node";
		const string nameAttribute     = "name";
		const string expandedAttribute = "expanded";
		const string selectedAttribute = "selected";

		void WriteTo (XmlWriter writer)
		{
			writer.WriteStartElement (Node);
			if (NodeName != null)
				writer.WriteAttributeString (nameAttribute, NodeName);
			if (Expanded)
				writer.WriteAttributeString (expandedAttribute, bool.TrueString);
			if (Selected)
				writer.WriteAttributeString (selectedAttribute, bool.TrueString);

			if (Options != null) {
				foreach (var opt in Options) {
					writer.WriteStartElement ("Option");
					writer.WriteAttributeString ("id", opt.Key.ToString());
					writer.WriteAttributeString ("value", opt.Value.ToString ());
					writer.WriteEndElement (); // Option
				}
			}
			
			if (ChildrenState != null) { 
				foreach (NodeState ces in ChildrenState) {
					ces.WriteTo (writer);
				}
			}
			
			writer.WriteEndElement (); // NodeState
		}

		ICustomXmlSerializer ReadFrom (XmlReader reader)
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
						result.Options = new Dictionary<string, bool> ();
					result.Options [reader.GetAttribute ("id")] = bool.Parse (reader.GetAttribute ("value"));
					return true;
				case "Node":
					if (result.ChildrenState == null)
						result.ChildrenState = new List<NodeState> ();
					result.ChildrenState.Add ((NodeState)ReadFrom (reader));
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

			if (nav.Expanded || childrenState != null || nav.Selected) {
				NodeState es = new NodeState ();
				es.NodeName = nav.NodeName;
				es.Expanded = nav.Expanded;
				es.Selected = nav.Selected;
				es.ChildrenState = childrenState;
				return es;
			} else
				return null;
		}
		
		public override string ToString ()
		{
			return String.Format ("[NodeState: NodeName={0}, Expanded={1}, Selected={2}, Options={3}, #ChildrenState={4}]",
			                      this.NodeName,
			                      this.Expanded,
			                      this.Selected,
			                      this.Options != null ? this.Options.ToString () : "null",
			                      this.ChildrenState != null ? this.ChildrenState.Count.ToString () : "null");
		}
		 
		internal static void RestoreState (ExtensibleTreeView pad, ITreeNavigator nav, NodeState es)
		{
			if (es == null) 
				return;

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