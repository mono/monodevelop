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

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;

namespace MonoDevelop.Gui.Pads.ClassPad
{
	public class CombineNodeBuilder: TypeNodeBuilder
	{
		CombineEntryEventHandler combineEntryAdded;
		CombineEntryEventHandler combineEntryRemoved;
		CombineEntryRenamedEventHandler combineNameChanged;
		
		public CombineNodeBuilder ()
		{
			combineEntryAdded = (CombineEntryEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEntryEventHandler (OnEntryAdded));
			combineEntryRemoved = (CombineEntryEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEntryEventHandler (OnEntryRemoved));
			combineNameChanged = (CombineEntryRenamedEventHandler) Runtime.DispatchService.GuiDispatch (new CombineEntryRenamedEventHandler (OnCombineRenamed));
		}
			
		public override Type NodeDataType {
			get { return typeof(Combine); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ((Combine)dataObject).Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Combine combine = dataObject as Combine;
			label = String.Format (GettextCatalog.GetString ("Solution {0}"), combine.Name);
			icon = Context.GetIcon (Stock.CombineIcon);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Combine combine = (Combine) dataObject;
			if (builder.Options ["ShowProjects"]) {
				foreach (CombineEntry entry in combine.Entries)
					builder.AddChild (entry);
			} else {
				AddClasses (builder, combine);
			}
		}

		void AddClasses (ITreeBuilder builder, CombineEntry entry)
		{
			if (entry is Combine) {
				foreach (CombineEntry e in ((Combine)entry).Entries)
					AddClasses (builder, e);
			} else if (entry is Project) {
				ProjectNodeBuilder.BuildChildNodes (builder, entry as Project);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ((Combine) dataObject).Entries.Count > 0;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is Combine)
				return DefaultSort;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
			Combine combine = (Combine) dataObject;
			combine.EntryAdded += combineEntryAdded;
			combine.EntryRemoved += combineEntryRemoved;
			combine.NameChanged += combineNameChanged;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			Combine combine = (Combine) dataObject;
			combine.EntryAdded -= combineEntryAdded;
			combine.EntryRemoved -= combineEntryRemoved;
			combine.NameChanged -= combineNameChanged;
		}
		
		void OnEntryAdded (object sender, CombineEntryEventArgs e)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (sender);
			if (tb != null) {
				tb.AddChild (e.CombineEntry, true);
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
		}
	}
}
