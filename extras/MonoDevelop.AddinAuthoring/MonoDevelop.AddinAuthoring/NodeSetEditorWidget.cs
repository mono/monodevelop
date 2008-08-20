// NodeSetEditorWidget.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//
//

using System;
using System.Collections;
using Gtk;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Projects;

namespace MonoDevelop.AddinAuthoring
{
	
	
	[System.ComponentModel.Category("MonoDevelop.AddinAuthoring")]
	[System.ComponentModel.ToolboxItem(true)]
	public partial class NodeSetEditorWidget : Gtk.Bin
	{
		ExtensionNodeSet nodeSet;
		TreeStore store;
		AddinRegistry registry;
		AddinDescription adesc;
		DotNetProject project;
		
		const int ColObject = 0;
		const int ColIcon = 1;
		const int ColName = 2;
		const int ColType = 3;
		const int ColDescription = 4;
		const int ColCanEdit = 5;
		
		public NodeSetEditorWidget()
		{
			this.Build();
			
			store = new TreeStore (typeof(object), typeof(string), typeof(string), typeof(string), typeof(string), typeof(bool));
			tree.Model = store;
			
			TreeViewColumn col = new TreeViewColumn ();
			col.Title = "Name";
			CellRendererPixbuf crp = new CellRendererPixbuf ();
			col.PackStart (crp, false);
			CellRendererText crt = new CellRendererText ();
			col.PackStart (crt, true);
			col.Spacing = 3;
			col.AddAttribute (crp, "stock-id", ColIcon);
			col.AddAttribute (crt, "text", ColName);
			col.AddAttribute (crt, "sensitive", ColCanEdit);
			
			tree.AppendColumn (col);
			tree.AppendColumn ("Type", new CellRendererText (), "text", ColType, "sensitive", ColCanEdit);
			tree.AppendColumn ("Description", new CellRendererText (), "text", ColDescription, "sensitive", ColCanEdit);
			
			tree.Selection.Changed += OnSelectionChanged;
		}
		
		public bool AllowEditing {
			get {
				return buttonBox.Visible;
			}
			set {
				buttonBox.Visible = value;
			}
		}
		
		public void Fill (DotNetProject project, AddinRegistry registry, AddinDescription adesc, ExtensionNodeSet nset)
		{
			this.nodeSet = nset;
			this.registry = registry;
			this.adesc = adesc;
			this.project = project;
			Update ();
		}
		
		public void Update ()
		{
			store.Clear ();
			if (nodeSet == null)
				return;
			Hashtable visited = new Hashtable ();
			Fill (TreeIter.Zero, nodeSet, visited, true);
		}

		void Fill (TreeIter it, ExtensionNodeSet nset, Hashtable visited, bool isReference)
		{
			if (visited.Contains (nset))
				return;
			visited [nset] = nset;
			foreach (ExtensionNodeType nt in nset.NodeTypes) {
				TreeIter cit;
				if (it.Equals (TreeIter.Zero))
					cit = store.AppendValues (nt, "md-extension-node-type", nt.NodeName, nt.TypeName, nt.Description, isReference);
				else
					cit = store.AppendValues (it, nt, "md-extension-node-type", nt.NodeName, nt.TypeName, nt.Description, isReference);
				Fill (cit, nt, visited, isReference);
			}
			foreach (string ns in nset.NodeSets) {
				TreeIter cit;
				ExtensionNodeSet rns = FindNodeSet (ns);
				if (it.Equals (TreeIter.Zero))
					cit = store.AppendValues (rns, "md-extension-node-set", ns, string.Empty, string.Empty, isReference);
				else
					cit = store.AppendValues (it, rns, "md-extension-node-set", ns, string.Empty, string.Empty, isReference);
				if (rns != null)
					Fill (cit, rns, visited, false);
			}
		}
		
		ExtensionNodeSet FindNodeSet (string name)
		{
			foreach (ExtensionNodeSet ns in adesc.ExtensionNodeSets)
				if (ns.Id == name)
					return ns;
			
			foreach (AddinDependency adep in adesc.MainModule.Dependencies) {
				Addin addin = registry.GetAddin (adep.FullAddinId);
				if (addin != null && addin.Description != null) {
					foreach (ExtensionNodeSet ns in addin.Description.ExtensionNodeSets)
						if (ns.Id == name)
							return ns;
				}
			}
			return null;
		}
		
		void UpdateButtons ()
		{
			TreeIter iter;
			if (tree.Selection.GetSelected (out iter)) {
				bool canedit = (bool) store.GetValue (iter, ColCanEdit);
				removeNodeButton.Sensitive = canedit;
				editNodeButton.Sensitive = canedit && (store.GetValue (iter, ColObject) is ExtensionNodeType);
			} else {
				editNodeButton.Sensitive = removeNodeButton.Sensitive = false;
			}
		}
		
		void OnSelectionChanged (object s, EventArgs args)
		{
			UpdateButtons ();
		}
		
		protected virtual void OnAddNodeButtonClicked (object sender, System.EventArgs e)
		{
			ExtensionNodeType nt = new ExtensionNodeType ();
			NodeTypeEditorDialog dlg = new NodeTypeEditorDialog (project, nt);
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				nodeSet.NodeTypes.Add (nt);
				Update ();
			}
			dlg.Destroy ();
		}

		protected virtual void OnAddSetButtonClicked (object sender, System.EventArgs e)
		{
			SelectNodeSetDialog dlg = new SelectNodeSetDialog (project, registry, adesc);
			if (dlg.Run () == (int) ResponseType.Ok) {
				nodeSet.NodeSets.Add (dlg.SelectedNodeSet);
				Update ();
			}
			dlg.Destroy ();
		}

		protected virtual void OnRemoveNodeButtonClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			tree.Selection.GetSelected (out iter);
			ExtensionNodeSet ns = (ExtensionNodeSet) store.GetValue (iter, ColObject);
			if (ns is ExtensionNodeType) {
				((ExtensionNodeSet)ns.Parent).NodeTypes.Remove (ns);
			} else {
				string nid = ns.Id;
				if (store.IterParent (out iter, iter)) {
					ExtensionNodeSet pns = (ExtensionNodeSet) store.GetValue (iter, ColObject);
					pns.NodeSets.Remove (nid);
				} else {
					nodeSet.NodeSets.Remove (nid);
				}
			}
			Update ();
		}

		protected virtual void OnEditNodeButtonClicked (object sender, System.EventArgs e)
		{
			TreeIter iter;
			tree.Selection.GetSelected (out iter);
			ExtensionNodeType ns = (ExtensionNodeType) store.GetValue (iter, ColObject);
			ExtensionNodeType copy = new ExtensionNodeType ();
			copy.CopyFrom (ns);
			
			NodeTypeEditorDialog dlg = new NodeTypeEditorDialog (project, copy);
			if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
				ns.CopyFrom (copy);
				Update ();
			}
			dlg.Destroy ();
		}
	}
}
