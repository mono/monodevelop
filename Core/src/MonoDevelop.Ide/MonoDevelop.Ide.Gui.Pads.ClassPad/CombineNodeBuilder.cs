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

using MonoDevelop.Ide.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Ide.Gui.Pads.ClassPad
{
	public class CombineNodeBuilder: TypeNodeBuilder
	{
		
		public CombineNodeBuilder ()
		{
		}
			
		public override Type NodeDataType {
			get { return typeof(Solution); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Solution)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Solution combine = dataObject as Solution;
			label = GettextCatalog.GetString ("Solution {0}", combine.Name);
			icon = Context.GetIcon (Stock.CombineIcon);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Solution combine = (Solution) dataObject;
			if (builder.Options ["ShowProjects"]) {
				foreach (IProject entry in combine.AllProjects)
					builder.AddChild (entry);
			} else {
				AddClasses (builder, combine);
			}
		}

		void AddClasses (ITreeBuilder builder, Solution entry)
		{
			foreach (IProject project in entry.AllProjects) {
				AddClasses (builder, project);
			} 
		}
		
		void AddClasses (ITreeBuilder builder, IProject entry)
		{
			ProjectNodeBuilder.BuildChildNodes (builder, entry);
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((Solution) dataObject).Items.Count > 0;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is Solution)
				return DefaultSort;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
/* TODO: Project Conversion
			Combine combine = (Combine) dataObject;
			combine.EntryAdded += combineEntryAdded;
			combine.EntryRemoved += combineEntryRemoved;
			combine.NameChanged += combineNameChanged;*/
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
/* TODO: Project Conversion
			Combine combine = (Combine) dataObject;
			combine.EntryAdded -= combineEntryAdded;
			combine.EntryRemoved -= combineEntryRemoved;
			combine.NameChanged -= combineNameChanged;*/
		}
		
/* TODO: Project Conversion
		void OnEntryAdded (object sender, CombineEntryEventArgs e)
		{
			IdeApp.Services.DispatchService.GuiDispatch (OnAddEntry, e.CombineEntry);
		}
		
		void OnAddEntry (object newEntry)
		{
			CombineEntry e = (CombineEntry) newEntry;
			ITreeBuilder tb = Context.GetTreeBuilder (e.ParentCombine);
			if (tb != null) {
				tb.AddChild (e, true);
				tb.Expanded = true;
			}
		}

		void OnEntryRemoved (object sender, CombineEntryEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.CombineEntry);
			if (tb != null) tb.Remove ();
		}
		
		void OnCombineRenamed (object sender, CombineEntryRenamedEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (e.CombineEntry);
			if (tb != null) tb.Update ();
		}*/
	}
}
