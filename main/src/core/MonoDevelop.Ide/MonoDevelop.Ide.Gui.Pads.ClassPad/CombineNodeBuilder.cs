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
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class CombineNodeBuilder: TypeNodeBuilder
	{
//		SolutionItemRenamedEventHandler combineNameChanged;
		
		public CombineNodeBuilder ()
		{
//			combineNameChanged = (SolutionItemRenamedEventHandler) DispatchService.GuiDispatch (new SolutionItemRenamedEventHandler (OnCombineRenamed));
		}
			
		public override Type NodeDataType {
			get { return typeof(SolutionFolder); }
		}

		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/Ide/ContextMenu/ClassPad/Combine"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "";
//			return ((SolutionFolder)dataObject).Name;
		}
		/*
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SolutionFolder folder = dataObject as SolutionFolder;
			label = folder.Name;
			icon = Context.GetIcon (Stock.SolutionFolderOpen);
			closedIcon = Context.GetIcon (Stock.SolutionFolderClosed);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			if (builder.Options ["ShowProjects"]) {
				foreach (SolutionItem entry in combine.Items)
					builder.AddChild (entry);
			} else {
				AddClasses (builder, combine);
			}
		}

		void AddClasses (ITreeBuilder builder, SolutionItem entry)
		{
			if (entry is SolutionFolder) {
				foreach (SolutionItem e in ((SolutionFolder)entry).Items)
					AddClasses (builder, e);
			} else if (entry is Project) {
				ProjectNodeBuilder.BuildChildNodes (builder, entry as Project);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((SolutionFolder) dataObject).Items.Count > 0;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is SolutionFolder)
				return DefaultSort;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			combine.NameChanged += combineNameChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			SolutionFolder combine = (SolutionFolder) dataObject;
			combine.NameChanged -= combineNameChanged;
		}
		
		void OnCombineRenamed (object sender, SolutionItemRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.SolutionItem);
			if (tb != null) tb.Update ();
		}*/
	}
}
