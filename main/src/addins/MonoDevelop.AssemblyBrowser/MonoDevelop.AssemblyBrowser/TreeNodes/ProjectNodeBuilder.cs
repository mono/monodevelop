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
using System.Linq;
using System.Text;

using Mono.Cecil;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;
using System.Collections.Generic;
using System.IO;
using MonoDevelop.Projects;
using MonoDevelop.Ide.TypeSystem;

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
			//var project = (Project)dataObject;
			// TODO: Roslyn port.
//			var ctx = TypeSystemService.GetProjectContext (project);
//			if (ctx == null)
//				return;
//			
//			var namespaces = new Dictionary<string, Namespace> ();
//
//			foreach (var type in ctx.TopLevelTypeDefinitions) {
//				string namespaceName = string.IsNullOrEmpty (type.Namespace) ? "-" : type.Namespace;
//				if (!namespaces.ContainsKey (namespaceName))
//					namespaces [namespaceName] = new Namespace (namespaceName);
//				
//				var ns = namespaces [namespaceName];
//				ns.Types.Add (type);
//			}
//			
//			foreach (var ns in namespaces.Values) {
//				builder.AddChild (ns);
//			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			//var project = (Project)dataObject;
			// TODO: Roslyn port.
			//var ctx = TypeSystemService.GetProjectContext (project);
			//if (ctx == null)
				return false;
			// return ctx.TopLevelTypeDefinitions.Any ();
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
				
				return e1.Name.CompareTo (e2.Name);
			} catch (Exception e) {
				LoggingService.LogError ("Exception in assembly browser sort function.", e);
				return -1;
			}
		}
		
	}
}

