// NodeTypeEditorDialog.cs
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

namespace MonoDevelop.AddinAuthoring
{
	
	
	public partial class NodeTypeEditorDialog : Gtk.Dialog
	{
		ExtensionNodeType ntype;
		bool nodeNameSet, loading;
		
		public NodeTypeEditorDialog (DotNetProject project, ExtensionNodeType nt)
		{
			this.Build();
			this.ntype = nt;
			nodeType.Project = project;
			baseType.Project = project;
			
			Fill ();
			
			if (nt.Parent == null) {
				loading = true;
				nodeType.TypeName = "Mono.Addins.TypeExtensionNode";
				entryName.Text = "Type";
				loading = false;
			}
		}
		
		void Fill ()
		{
			loading = true;
			entryName.Text = ntype.NodeName;
			nodeType.TypeName = ntype.TypeName;
			baseType.TypeName = ntype.ObjectTypeName;
			entryDesc.Text = ntype.Description;
			loading = false;
		}

		protected virtual void OnButtonOkClicked (object sender, System.EventArgs e)
		{
			ntype.NodeName = entryName.Text;
			ntype.TypeName = nodeType.TypeName;
			ntype.ObjectTypeName = baseType.TypeName;
			ntype.Description = entryDesc.Text;
		}

		protected virtual void OnNodeTypeChanged (object sender, System.EventArgs e)
		{
			if (nodeType.TypeName == "Mono.Addins.TypeExtensionNode" && !nodeNameSet) {
				entryName.Text = "Type";
			}
		}

		protected virtual void OnEntryNameChanged (object sender, System.EventArgs e)
		{
			if (!loading)
				nodeNameSet = true;
		}
	}
}
