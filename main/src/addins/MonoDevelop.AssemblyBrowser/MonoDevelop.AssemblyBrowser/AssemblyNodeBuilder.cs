//
// AssemblyNodeBuilder.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using Mono.Cecil;

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.AssemblyBrowser
{
	public class AssemblyNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(AssemblyDefinition); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)dataObject;
			return assemblyDefinition.Name.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)dataObject;
			label = assemblyDefinition.Name.Name;
			icon = Context.GetIcon (Stock.Reference);
		}
		
		public override void BuildChildNodes (ITreeBuilder ctx, object dataObject)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)dataObject;
			
			foreach (ModuleDefinition moduleDefinition in assemblyDefinition.Modules) {
				ctx.AddChild (moduleDefinition);
			}
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			AssemblyDefinition assemblyDefinition = (AssemblyDefinition)dataObject;
			return assemblyDefinition.Modules.Count > 0;
		}
		
	}
}
