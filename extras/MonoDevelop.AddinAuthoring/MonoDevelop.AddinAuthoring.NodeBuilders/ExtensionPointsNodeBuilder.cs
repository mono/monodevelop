// ExtensionPointsNodeBuilder.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Components.Commands;
using Mono.Addins;
using Mono.Addins.Description;
using MonoDevelop.Ide.Gui.Components;
using Gdk;

namespace MonoDevelop.AddinAuthoring
{
	public class ExtensionPointsNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ExtensionPointCollection); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ExtensionPointsCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/MonoDevelop/AddinAuthoring/ContextMenu/ProjectPad/AddinReference"; }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return "extension-points";
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Pixbuf icon, ref Pixbuf closedIcon)
		{
			AddinData data = (AddinData) treeBuilder.GetParentDataItem (typeof(AddinData), false);
			label = AddinManager.CurrentLocalizer.GetString ("Extension Points ({0})", data.CachedAddinManifest.ExtensionPoints.Count);
			icon = Context.GetIcon ("md-extension-point");
		}	
	}
	
	class ExtensionPointsCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			AddinData data = (AddinData) CurrentNode.GetParentDataItem (typeof(AddinData), false);
			Document doc = IdeApp.Workbench.OpenDocument (data.AddinManifestFileName);
			if (doc != null) {
				AddinDescriptionView view = doc.GetContent<AddinDescriptionView> ();
				if (view != null)
					view.ShowExtensionPoints ();
			}
		}

	}
}
