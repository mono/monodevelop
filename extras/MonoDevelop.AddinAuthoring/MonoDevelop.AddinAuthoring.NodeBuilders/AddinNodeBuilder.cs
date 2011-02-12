// 
// AddinNodeBuilder.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

using System;
using MonoDevelop.Ide.Gui.Components;
using Mono.Addins.Description;
using System.Collections.Generic;
using MonoDevelop.Core;

namespace MonoDevelop.AddinAuthoring.NodeBuilders
{
	public class AddinNodeBuilder: TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(AddinDescription); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			AddinDescription ad = (AddinDescription) dataObject;
			return ad.AddinId;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			AddinDescription ad = (AddinDescription) dataObject;
			label = Util.GetDisplayName (ad);
			icon = Context.GetIcon ("md-addin");
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			AddinDescription ad = (AddinDescription) dataObject;
			HashSet<string> localPoints = new HashSet<string> ();
			foreach (ExtensionPoint ep in ad.ExtensionPoints) {
				treeBuilder.AddChild (ep);
				localPoints.Add (ep.Path);
			}
			foreach (Extension ex in ad.MainModule.Extensions) {
				if (!localPoints.Contains (ex.Path))
					treeBuilder.AddChild (ex);
			}
			treeBuilder.AddChild (new TreeViewItem (GettextCatalog.GetString ("Dependencies"), MonoDevelop.Ide.Gui.Stock.ClosedReferenceFolder), true);
			foreach (Dependency dep in ad.MainModule.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep != null)
					treeBuilder.AddChild (new TreeViewItem (adep.FullAddinId, "md-addin-reference"));
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			AddinDescription ad1 = thisNode.DataItem as AddinDescription;
			AddinDescription ad2 = otherNode.DataItem as AddinDescription;
			if (ad1 != null && ad2 != null)
				return Util.GetDisplayName (ad1).CompareTo (Util.GetDisplayName (ad2));
			return base.CompareObjects (thisNode, otherNode);
		}

	}
}

