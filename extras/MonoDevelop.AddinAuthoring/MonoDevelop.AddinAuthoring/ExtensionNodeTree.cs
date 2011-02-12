// 
// ExtensionNodeBuilder.cs
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
using MonoDevelop.Core;
using MonoDevelop.Projects;
using Mono.Addins;
using Mono.Addins.Description;
using System.Collections.Generic;

namespace MonoDevelop.AddinAuthoring
{
	public class ExtensionNodeTree
	{
		List<ExtensionNodeInfo> nodes = new List<ExtensionNodeInfo> ();

		public List<ExtensionNodeInfo> Nodes {
			get { return this.nodes; }
		}
		
		public void Fill (AddinRegistry reg, ExtensionPoint ep)
		{
			List<AddinDescription> deps = new List<AddinDescription> ();
			foreach (var addinId in ep.ExtenderAddins) {
				Addin ad = reg.GetAddin (addinId);
				if (ad != null && ad.LocalId != ep.ParentAddinDescription.LocalId)
					deps.Add (ad.Description);
			}
			Fill (ep.ParentAddinDescription, ep.ParentAddinDescription, ep.Path, deps);
		}
		
		public void Fill (AddinRegistry reg, Extension ex)
		{
			Fill (reg, ex.ParentAddinDescription, ex.ParentAddinDescription, ex.Path);
		}
		
		public void Fill (AddinData data, AddinDescription pdesc, string path)
		{
			Fill (data.AddinRegistry, data.CachedAddinManifest, pdesc, path);
		}
		
		public void Fill (AddinRegistry reg, AddinDescription localDesc, AddinDescription pdesc, string path)
		{
			List<AddinDescription> deps = new List<AddinDescription> ();
			
			foreach (Dependency dep in pdesc.MainModule.Dependencies) {
				AddinDependency adep = dep as AddinDependency;
				if (adep == null) continue;
				Addin addin = reg.GetAddin (adep.FullAddinId);
				if (addin != null)
					deps.Add (addin.Description);
			}
			Fill (localDesc, pdesc, path, deps);
		}
		
		public void Fill (AddinDescription localDesc, AddinDescription pdesc, string path, List<AddinDescription> contributorAddins)
		{
			var extensions = new List<Extension> ();
			var extNodes = new List<ExtensionNodeDescription> ();
			
			SortReferences (contributorAddins);
			
			foreach (AddinDescription desc in contributorAddins)
				CollectExtensions (desc, path, extensions, extNodes);
			
			CollectExtensions (pdesc, path, extensions, extNodes);
			
			foreach (var node in extNodes)
				nodes.Add (new ExtensionNodeInfo (node, node.ParentAddinDescription == localDesc));
			
			foreach (Extension ext in extensions) {
				bool canModify = ext.ParentAddinDescription == localDesc;
				string subp = ext.Path.Substring (path.Length);
				List<ExtensionNodeInfo> list = nodes;
				foreach (string p in subp.Split ('/')) {
					if (p.Length == 0) continue;
					int i = FindNode (list, p);
					if (i == -1) {
						list = null;
						break;
					}
					list = list [i].Expand ();
				}
				if (list != null) {
					int insertPos = list.Count;
					foreach (ExtensionNodeDescription n in ext.ExtensionNodes) {
						if (!string.IsNullOrEmpty (n.InsertAfter)) {
							int i = FindNode (list, n.InsertAfter);
							if (i != -1)
								insertPos = i + 1;
						}
						else if (!string.IsNullOrEmpty (n.InsertBefore)) {
							int i = FindNode (list, n.InsertBefore);
							if (i != -1)
								insertPos = i;
						}
						list.Insert (insertPos++, new ExtensionNodeInfo (n, canModify));
					}
				}
			}
			StoreOrder (nodes);
		}
		
		int FindNode (List<ExtensionNodeInfo> list, string name)
		{
			if (list == null)
				return -1;
			for (int n = 0; n < list.Count; n++) {
				if (list [n].Node.Id == name)
					return n;
			}
			return -1;
		}
		
		void StoreOrder (List<ExtensionNodeInfo> list)
		{
			for (int n = 0; n < list.Count; n++) {
				ExtensionNodeInfo node = list [n];
				node.Order = n;
				if (node.Children != null)
					StoreOrder (node.Children);
			}
		}
		
		void SortReferences (List<AddinDescription> deps)
		{
			List<AddinDescription> sorted = new List<AddinDescription> ();
			foreach (var desc in deps) {
				int n;
				for (n = 0; n < sorted.Count; n++) {
					if (DependsOn (sorted[n], desc))
					    break;
				}
				sorted.Insert (n, desc);
			}
		}
		
		bool DependsOn (AddinDescription a1, AddinDescription a2)
		{
			foreach (ModuleDescription module in a1.AllModules) {
				foreach (Dependency dep in module.Dependencies) {
					AddinDependency adep = dep as AddinDependency;
					if (adep != null && adep.FullAddinId == a2.AddinId)
						return true;
					else
						Console.WriteLine ("pp???:" + dep);
				}
			}
			return false;
		}
		
		void CollectExtensions (AddinDescription desc, string path, List<Extension> extensions, List<ExtensionNodeDescription> nodes)
		{
			foreach (Extension ext in desc.MainModule.Extensions) {
				if (ext.Path == path || ext.Path.StartsWith (path + "/"))
					extensions.Add (ext);
				else if (path.StartsWith (ext.Path + "/")) {
					string subp = path.Substring (ext.Path.Length);
					ExtensionNodeDescription foundNode = null;
					ExtensionNodeDescriptionCollection list = ext.ExtensionNodes;
					foreach (string p in subp.Split ('/')) {
						if (p.Length == 0) continue;
						foundNode = list [p];
						if (foundNode == null)
							break;
					}
					if (foundNode != null) {
						foreach (ExtensionNodeDescription n in foundNode.ChildNodes)
							nodes.Add (n);
					}
				}
			}
		}
	}
}
