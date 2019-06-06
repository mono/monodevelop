// 
// ProjectNodeBuilder.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin <http://xamarin.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;

namespace MonoDevelop.AssemblyBrowser
{
	class ProjectNodeBuilder : TypeNodeBuilder
	{
		internal AssemblyBrowserWidget Widget {
			get;
			private set;
		}

		public override Type NodeDataType {
			get { return typeof(Project); }
		}

		public ProjectNodeBuilder (AssemblyBrowserWidget widget)
		{
			Widget = widget;
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var project = (Project)dataObject;
			return project.Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			var project = (Project)dataObject;
			
			nodeInfo.Label = Ambience.EscapeText (project.Name);
			nodeInfo.Icon = Context.GetIcon (project.StockIcon);
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			Project project = (Project)dataObject;
			bool publicOnly = Widget.PublicApiOnly;
			var dom = IdeApp.TypeSystemService.GetCompilationAsync (project).Result;
			if (dom == null)
				return;

			var data = new List<NamespaceData> (32);
			CollectNamespaces (data, dom.Assembly.GlobalNamespace.GetNamespaceMembers ());
			builder.AddChildren (data);

			var types = dom.Assembly.GlobalNamespace.GetTypeMembers ();
			if (types.Length > 0) {
				var children = publicOnly
					? types.Where (t => t.IsPublic ())
					: types;

				builder.AddChildren (children);
			}
		}

		static void CollectNamespaces (List<NamespaceData> acc, IEnumerable<Microsoft.CodeAnalysis.INamespaceSymbol> namespaces)
		{
			acc.AddRange (namespaces.Select (x => new NamespaceData (x)));

			foreach (var ns in namespaces) {
				CollectNamespaces (acc, ns.GetNamespaceMembers ());
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			try {
				if (thisNode == null || otherNode == null)
					return -1;
				var e1 = thisNode.DataItem as Project;
				var e2 = otherNode.DataItem as Project;
				
				if (e1 == null && e2 == null)
					return 0;
				if (e1 == null)
					return -1;
				if (e2 == null)
					return 1;
				
				return string.Compare (e1.Name, e2.Name, StringComparison.Ordinal);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in assembly browser sort function.", e);
				return -1;
			}
		}
		
	}
}

