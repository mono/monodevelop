// NewExtensionPointDialog.cs
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
using Mono.Addins.Description;
using MonoDevelop.Projects;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	
	
	public partial class NewExtensionPointDialog : Gtk.Dialog
	{
		ExtensionPoint ep;
		DotNetProject project;
		AddinRegistry registry;
		AddinDescription adesc;
		
		public NewExtensionPointDialog (DotNetProject project, AddinRegistry registry, AddinDescription adesc, ExtensionPoint ep)
		{
			this.Build();
			this.ep = ep;
			this.project = project;
			this.registry = registry;
			this.adesc = adesc;

			notebook.Page = 0;
			
			Fill ();
		}
		
		void Fill ()
		{
			entryPath.Text = ep.Path;
			entryName.Text = ep.Name;
			entryDesc.Text = ep.Description;
			
			if (ep.Parent == null)
				entryNodeName.Text = "Type";
			
			if (ep.Parent == null && ep.Path.Length == 0) {
				if (adesc.Namespace.Length > 0)
					entryPath.Text = "/" + adesc.Namespace + "/";
				else if (adesc.LocalId.Length > 0)
					entryPath.Text = "/" + adesc.LocalId + "/";
			}
			
			if (ep.NodeSet.NodeSets.Count == 0 && ep.NodeSet.NodeTypes.Count == 1) {
				ExtensionNodeType nt = ep.NodeSet.NodeTypes [0];
				if (nt.TypeName == string.Empty || nt.TypeName == "Mono.Addins.TypeExtensionNode") {
					notebook.Page = 0;
					entryNodeName.Text = nt.NodeName;
					baseTypeSelector.Project = project;
					baseTypeSelector.TypeName = nt.ObjectTypeName;
					entryNodeDescription.Text = nt.Description;
				}
			}
			nodeseteditorwidget.Fill (project, registry, adesc, ep.NodeSet);
			UpdateButtons ();
		}

		protected virtual void OnRadioTypeExtensionClicked (object sender, System.EventArgs e)
		{
			notebook.Page = 0;
		}

		protected virtual void OnRadioCustomExtensionClicked (object sender, System.EventArgs e)
		{
			notebook.Page = 1;
		}

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			ep.Path = entryPath.Text;
			ep.Name = entryName.Text;
			ep.Description = entryDesc.Text;
			
			if (notebook.Page == 0) {
				ep.NodeSet.CopyFrom (new ExtensionNodeSet ());
				ExtensionNodeType nt = new ExtensionNodeType ();
				nt.NodeName = entryNodeName.Text;
				nt.TypeName = "Mono.Addins.TypeExtensionNode";
				nt.ObjectTypeName = baseTypeSelector.TypeName;
				nt.Description = entryNodeDescription.Text;
				ep.NodeSet.NodeTypes.Add (nt);
			}
		}
		
		void UpdateButtons ()
		{
			buttonOk.Sensitive = (entryPath.Text.Length > 0);
		}

		protected virtual void OnEntryPathChanged (object sender, System.EventArgs e)
		{
			UpdateButtons ();
		}
	}
}
