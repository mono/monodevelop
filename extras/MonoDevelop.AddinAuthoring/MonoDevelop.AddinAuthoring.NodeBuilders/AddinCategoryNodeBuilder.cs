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
using System.Collections.Generic;

namespace MonoDevelop.AddinAuthoring.NodeBuilders
{
	public class AddinCategoryNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(AddinCategoryGroup); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			AddinCategoryGroup ep = (AddinCategoryGroup) dataObject;
			return ep.Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Pixbuf icon, ref Pixbuf closedIcon)
		{
			AddinCategoryGroup ep = (AddinCategoryGroup) dataObject;
			label = ep.Name;
			icon = Context.GetIcon ("md-open-folder");
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			AddinCategoryGroup cat = (AddinCategoryGroup) dataObject;
			foreach (var ad in cat.Registry.CachedRegistry.GetAddinRoots ()) {
				if (ad.Description.Category == cat.Name)
					treeBuilder.AddChild (ad.Description);
			}
			foreach (var ad in cat.Registry.CachedRegistry.GetAddins ()) {
				if (ad.Description.Category == cat.Name)
					treeBuilder.AddChild (ad.Description);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			object o1 = thisNode.DataItem;
			object o2 = otherNode.DataItem;
			if ((o1 is AddinCategoryGroup) && !(o2 is AddinCategoryGroup))
				return -1;
			if ((o2 is AddinCategoryGroup) && !(o1 is AddinCategoryGroup))
				return 1;
			return base.CompareObjects (thisNode, otherNode);
		}

	}
}
