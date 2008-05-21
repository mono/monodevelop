// SelectNodeSetDialog.cs
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
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Projects;
using Gtk;

namespace MonoDevelop.AddinAuthoring
{
	public partial class SelectNodeSetDialog : Gtk.Dialog
	{
		Hashtable sets = new Hashtable ();
		DotNetProject project;
		AddinRegistry registry;
		AddinDescription desc;
		
		public SelectNodeSetDialog (DotNetProject project, AddinRegistry registry, AddinDescription desc)
		{
			this.Build();
			this.project = project;
			this.registry = registry;
			this.desc = desc;
			
			foreach (AddinDependency adep in desc.MainModule.Dependencies) {
				Addin addin = registry.GetAddin (adep.FullAddinId);
				if (addin != null && addin.Description != null) {
					foreach (ExtensionNodeSet ns in addin.Description.ExtensionNodeSets) {
						combo.AppendText (ns.Id);
						sets [ns.Id] = ns;
					}
				}
			}
			
			foreach (ExtensionNodeSet ns in desc.ExtensionNodeSets) {
				combo.AppendText (ns.Id);
				sets [ns.Id] = ns;
			}
			
			nodeseteditor.AllowEditing = false;
			buttonOk.Sensitive = false;
		}

		protected virtual void OnComboChanged (object sender, System.EventArgs e)
		{
			ExtensionNodeSet ns = (ExtensionNodeSet) sets [combo.Entry.Text];
			nodeseteditor.Fill (project, registry, desc, ns);
			buttonOk.Sensitive = combo.Entry.Text.Length > 0;
		}
		
		public string SelectedNodeSet {
			get { return combo.Entry.Text; }
		}
	}
}
