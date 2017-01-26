//
// CombineNodeBuilder.cs
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

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class CombineNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(SolutionFolder); }
		}

		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Combine"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((SolutionFolder)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			SolutionFolder folder = dataObject as SolutionFolder;
			nodeInfo.Label = folder.Name;
			nodeInfo.Icon = Context.GetIcon (Stock.SolutionFolderOpen);
			nodeInfo.ClosedIcon = Context.GetIcon (Stock.SolutionFolderClosed);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			if (builder.Options ["ShowProjects"]) {
				builder.AddChildren (combine.Items);
			} else {
				AddClasses (builder, combine);
			}
		}

		void AddClasses (ITreeBuilder builder, SolutionFolderItem entry)
		{
			if (entry is SolutionFolder) {
				foreach (SolutionFolderItem e in ((SolutionFolder)entry).Items)
					AddClasses (builder, e);
			} else if (entry is Project) {
				ProjectNodeBuilder.BuildChildNodes (builder, entry as Project);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((SolutionFolder) dataObject).Items.Count > 0;
		}

		public override int GetSortIndex (ITreeNavigator node)
		{
			return -100;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			combine.NameChanged += OnCombineRenamed;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			combine.NameChanged -= OnCombineRenamed;
		}
		
		void OnCombineRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) tb.Update ();
		}
	}
}
