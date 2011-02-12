// 
// RegistryNodeBuilder.cs
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
using Mono.Addins;
using System.Collections.Generic;

namespace MonoDevelop.AddinAuthoring.NodeBuilders
{
	public class RegistryNodeBuilder: TypeNodeBuilder
	{
		public RegistryNodeBuilder ()
		{
		}
		
		public override Type NodeDataType {
			get { return typeof(RegistryInfo); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			RegistryInfo reg = (RegistryInfo) dataObject;
			string name = reg.ApplicationName;
			if (string.IsNullOrEmpty (name))
				name = reg.ApplicationPath;
			return name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			RegistryInfo reg = (RegistryInfo) dataObject;
			label = reg.ApplicationName;
			if (string.IsNullOrEmpty (label))
				label = reg.ApplicationPath;
			icon = Context.GetIcon ("md-package");
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			RegistryInfo reg = (RegistryInfo) dataObject;
			if (reg.CachedRegistry == null) {
				reg.CachedRegistry = new AddinRegistry (reg.RegistryPath, reg.ApplicationPath);
				reg.CachedRegistry.Update (null);
			}
			HashSet<string> cats = new HashSet<string> ();
			foreach (var ad in reg.CachedRegistry.GetAddinRoots ()) {
				if (string.IsNullOrEmpty (ad.Description.Category)) {
					treeBuilder.AddChild (ad.Description);
				} else if (cats.Add (ad.Description.Category)) {
					treeBuilder.AddChild (new AddinCategoryGroup (reg, ad.Description.Category));
				}
			}
			foreach (var ad in reg.CachedRegistry.GetAddins ()) {
				if (string.IsNullOrEmpty (ad.Description.Category)) {
					treeBuilder.AddChild (ad.Description);
				} else if (cats.Add (ad.Description.Category)) {
					treeBuilder.AddChild (new AddinCategoryGroup (reg, ad.Description.Category));
				}
			}
		}
	}
}

