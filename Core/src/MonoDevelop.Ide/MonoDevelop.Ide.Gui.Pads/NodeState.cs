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

namespace MonoDevelop.Ide.Gui.Pads
{
	public class NodeState
	{
		string nodeName;
		
		bool expanded;
		bool selected;
		TreeViewPad.TreeOptions options;
		
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

		public MonoDevelop.Ide.Gui.Pads.TreeViewPad.TreeOptions Options {
			get {
				return options;
			}
			set {
				options = value;
			}
		}

		public System.Collections.Generic.List<MonoDevelop.Ide.Gui.Pads.NodeState> ChildrenState {
			get {
				return childrenState;
			}
			set {
				childrenState = value;
			}
		}
		
		public Properties ToXml ()
		{
			return ToXml (null);
		}
		
		public static NodeState FromXml (Properties state)
		{
			return FromXml (state, null);
		}
		
		Properties  ToXml (TreeViewPad.TreeOptions parentOptions)
		{
			Properties result = new Properties();
			
			if (NodeName != null)
				result.Set ("name", NodeName);
			if (Expanded)
				result.Set ("expanded", bool.TrueString);
			if (Selected)
				result.Set ("selected", bool.TrueString);

			Properties optionProperties = new Properties();
			result.Set ("Options", optionProperties);
			
			TreeViewPad.TreeOptions ops = Options;
			if (ops != null) {
				foreach (DictionaryEntry de in ops) {
					object parentVal = parentOptions != null ? parentOptions [de.Key] : null;
					if (parentVal != null && !parentVal.Equals (de.Value) || (parentVal == null && de.Value != null) || parentOptions == null) {
						optionProperties.Set (de.Key.ToString(), de.Value.ToString ()); 
					}
				}
			}
			
			if (ChildrenState == null) 
				return result;
			
			Properties nodeProperties = new Properties();
			result.Set ("Nodes", nodeProperties);
			foreach (NodeState ces in ChildrenState) {
				nodeProperties.Set (ces.NodeName, ces.ToXml (ops != null ? ops : parentOptions));
			}
			
			return result;
		}

		static NodeState FromXml (Properties state, TreeViewPad.TreeOptions parentOptions)
		{
			NodeState result = new NodeState ();
			result.NodeName = state.Get ("name", "");
			result.Expanded = state.Get ("expanded", false);
			result.Selected = state.Get ("selected", false);
			
			Properties optionProperties = state.Get ("Options", new Properties ());
			foreach (string key in optionProperties.Keys) {
				if (result.Options == null) 
					result.Options = parentOptions != null ? parentOptions.CloneOptions (Gtk.TreeIter.Zero) : new TreeViewPad.TreeOptions ();   
				result.Options [key] = bool.Parse (optionProperties.Get<string> (key));
			}
			
			Properties nodeProperties = state.Get ("Nodes", new Properties ());
			result.ChildrenState = new List<MonoDevelop.Ide.Gui.Pads.NodeState> ();
			foreach (string key in nodeProperties.Keys) {
				result.ChildrenState.Add (FromXml (nodeProperties.Get (key, new Properties ()), result.Options != null ? result.Options : parentOptions));
			}
			return result;
		}
		
		internal static NodeState SaveState (TreeViewPad pad, ITreeNavigator nav)
		{
			NodeState state = SaveStateRec (pad, nav);
			if (state == null) 
				return new NodeState ();
			else return state;
		}
		
		static NodeState SaveStateRec (TreeViewPad pad, ITreeNavigator nav)
		{
			Gtk.TreeIter it = nav.CurrentPosition._iter;
			
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
