//
// AddinFolderNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Core.Gui;
using Mono.Addins;

namespace MonoDevelop.AddinAuthoring
{
	public class AddinFolderNodeBuilder: TypeNodeBuilder
	{
		EventHandler updateDelegate;
		
		public AddinFolderNodeBuilder ()
		{
			updateDelegate = (EventHandler) DispatchService.GuiDispatch (new EventHandler (OnUpdateFiles));
			AddinData.AddinSupportChanged += OnAddinSupportChanged;
		}
		
		public override Type NodeDataType {
			get { return typeof(AddinData); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(AddinFolderCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AddinAuthoring/ContextMenu/ProjectPad/Addin"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "AddinDescription";
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			label = AddinManager.CurrentLocalizer.GetString ("Add-in Description");
			icon = Context.GetIcon (Stock.Addin);
		}

		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			AddinData data = (AddinData) dataObject;
			builder.AddChild (data.CachedAddinManifest.ExtensionPoints);
			builder.AddChild (data.CachedAddinManifest.MainModule.Extensions);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			if (otherNode.DataItem is ProjectReferenceCollection)
				return 1;
			else
				return -1;
		}

		public override void OnNodeAdded (object dataObject)
		{
			AddinData w = (AddinData) dataObject;
			w.Changed += updateDelegate;
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			AddinData w = (AddinData)dataObject;
			w.Changed -= updateDelegate;
		}
		
		void OnUpdateFiles (object s, EventArgs args)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (s);
			if (tb != null)
				tb.UpdateAll ();
		}
		
		void OnAddinSupportChanged (Project p, bool enabled)
		{
			ITreeBuilder tb = Context.GetTreeBuilder (p);
			if (tb != null)
				tb.UpdateAll ();
		}
	}
}
