//
// SolutionFolderNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Search;


namespace MonoDevelop.Ide.Gui.Pads.SolutionViewPad
{
	public class SolutionFolderNodeBuilder : TypeNodeBuilder
	{
		public SolutionFolderNodeBuilder ()
		{
		}

		public override Type NodeDataType {
			get { return typeof(SolutionFolder); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			SolutionFolder solutionFolder = dataObject as SolutionFolder;
			if (solutionFolder == null) 
				return "SolutionFolder";
			
			return solutionFolder.Location;
		}
		
		public override void GetNodeAttributes (ITreeNavigator treeNavigator, object dataObject, ref NodeAttributes attributes)
		{
			attributes |= NodeAttributes.AllowRename;
		}
		
/*		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/CombineBrowserNode"; }
		}*/
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			SolutionFolder solutionFolder = dataObject as SolutionFolder;
			label = solutionFolder.Location;
			icon       = Context.GetIcon (Stock.OpenResourceFolder);
			closedIcon = Context.GetIcon (Stock.ClosedResourceFolder);
			
		}

		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			SolutionFolder solutionFolder = dataObject as SolutionFolder;
			if (solutionFolder == null) 
				return;
			foreach (SolutionItem item in solutionFolder.Items)
				ctx.AddChild (item);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			SolutionFolder solutionFolder = dataObject as SolutionFolder;
			return solutionFolder != null && solutionFolder.Items.Count > 0;
		}
		
		public override void OnNodeAdded (object dataObject)
		{
/*			combine.EntryAdded += combineEntryAdded;
			combine.EntryRemoved += combineEntryRemoved;
			combine.NameChanged += combineNameChanged;
			combine.StartupPropertyChanged += startupChanged;*/
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
/*			combine.EntryAdded -= combineEntryAdded;
			combine.EntryRemoved -= combineEntryRemoved;
			combine.NameChanged -= combineNameChanged;
			combine.StartupPropertyChanged -= startupChanged;*/
		}
	}
}
