//
// ReferenceNodeBuilder.cs
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
using System.Collections.Generic;
using System.IO;

using Mono.Cecil;

using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.AssemblyBrowser
{
	public class ReferenceNodeBuilder : TypeNodeBuilder
	{
		AssemblyBrowserWidget widget;
		
		public ReferenceNodeBuilder (AssemblyBrowserWidget widget)
		{
			this.widget = widget;
		}
				
		public override Type NodeDataType {
			get { return typeof(Reference); }
		}
		
		public override Type CommandHandlerType {
			get { return typeof(ReferenceNodeCommandHandler); }
		}

		public AssemblyBrowserWidget Widget {
			get {
				return widget;
			}
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			Reference reference = (Reference)dataObject;
			return reference.FileName;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			Reference reference = (Reference)dataObject;
			label = Path.GetFileNameWithoutExtension (reference.FileName);
			icon  = Context.GetIcon (Stock.Reference);
		}
	}
	
	public class ReferenceNodeCommandHandler : NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			ReferenceNodeBuilder nodeBuilder = CurrentNode.TypeNodeBuilder as ReferenceNodeBuilder;
			Reference reference = (Reference)CurrentNode.DataItem;
//			AssemblyDefinition definition = 
			nodeBuilder.Widget.AddReference (reference.FileName);
			nodeBuilder.Widget.SelectAssembly (reference.FileName);
		}
	}	
	
}
