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
using System.Xml;

namespace MonoDevelop.Gui.Pads
{
	public class NodeState
	{
		string NodeName;
		bool Expanded;
		bool Selected;
		TreeViewPad.TreeOptions Options;
		ArrayList ChildrenState;
		
		public XmlElement ToXml (XmlDocument doc)
		{
			return ToXml (doc, null);
		}
		
		public static NodeState FromXml (XmlElement elem)
		{
			return FromXml (elem, null);
		}
		
		XmlElement ToXml (XmlDocument doc, TreeViewPad.TreeOptions parentOptions)
		{
			XmlElement el = doc.CreateElement ("Node");
			if (NodeName != null)
				el.SetAttribute ("name", NodeName);
			el.SetAttribute ("expanded", Expanded.ToString ());
			if (Selected)
				el.SetAttribute ("selected", "True");
			
			TreeViewPad.TreeOptions ops = Options;
			if (ops != null) {
				foreach (DictionaryEntry de in ops) {
					object parentVal = parentOptions != null ? parentOptions [de.Key] : null;
					if (parentVal != null && !parentVal.Equals (de.Value) || (parentVal == null && de.Value != null) || parentOptions == null) {
						XmlElement eop = doc.CreateElement ("Option");
						eop.SetAttribute ("id", de.Key.ToString());
						eop.SetAttribute ("value", de.Value.ToString ());
						el.AppendChild (eop);
					}
				}
			}
			
			if (ChildrenState == null) return el;
			foreach (NodeState ces in ChildrenState) {
				XmlElement child = ces.ToXml (doc, ops != null ? ops : parentOptions);
				el.AppendChild (child);
			}
			
			return el;
		}

		static NodeState FromXml (XmlElement elem, TreeViewPad.TreeOptions parentOptions)
		{
			NodeState ns = new NodeState ();
			ns.NodeName = elem.GetAttribute ("name");
			string expanded = elem.GetAttribute ("expanded");
			ns.Expanded = (expanded == "" || bool.Parse (expanded));
			ns.Selected = elem.GetAttribute ("selected") == "True";
			
			XmlNodeList nodelist = elem.ChildNodes;
			foreach (XmlNode nod in nodelist) {
				XmlElement el = nod as XmlElement;
				if (el == null) continue;
				if (el.LocalName == "Option") {
					if (ns.Options == null) {
						if (parentOptions != null) ns.Options = parentOptions.CloneOptions (Gtk.TreeIter.Zero);
						else ns.Options = new TreeViewPad.TreeOptions ();
					}
					ns.Options [el.GetAttribute ("id")] = bool.Parse (el.GetAttribute ("value"));
				}
				else if (el.LocalName == "Node") {
					if (ns.ChildrenState == null) ns.ChildrenState = new ArrayList ();
					ns.ChildrenState.Add (FromXml (el, ns.Options != null ? ns.Options : parentOptions));
				}
			}
			return ns;
		}
		
		internal static NodeState SaveState (TreeViewPad pad, ITreeNavigator nav)
		{
			NodeState state = SaveStateRec (pad, nav);
			if (state == null) return new NodeState ();
			else return state;
		}
		
		static NodeState SaveStateRec (TreeViewPad pad, ITreeNavigator nav)
		{
			Gtk.TreeIter it = nav.CurrentPosition._iter;
			
			ArrayList childrenState = null;

			if (nav.Filled && nav.MoveToFirstChild ()) {
				do {
					NodeState cs = SaveStateRec (pad, nav);
					if (cs != null) {
						cs.NodeName = nav.NodeName;
						if (childrenState == null) childrenState = new ArrayList ();
						childrenState.Add (cs);
					}
				} while (nav.MoveNext ());
				nav.MoveToParent ();
			}
			
			TreeViewPad.TreeOptions ops = pad.GetIterOptions (it);
			
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
		
		internal static void RestoreState (TreeViewPad pad, ITreeNavigator nav, NodeState es)
		{
			if (es == null) return;

			Gtk.TreeIter it = nav.CurrentPosition._iter;
			if (es.Options != null) {
				pad.SetIterOptions (it, es.Options);
			}
			pad.ResetState (it);
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
