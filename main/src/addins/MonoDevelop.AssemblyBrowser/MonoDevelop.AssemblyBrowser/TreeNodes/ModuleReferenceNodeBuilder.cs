//
// ModuleReferenceNodeBuilder.cs
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

using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;

namespace MonoDevelop.AssemblyBrowser
{
	class ModuleReferenceNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof(ModuleReference); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var reference = (ModuleReference)dataObject;
			return reference.Name;
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			var reference = (ModuleReference)dataObject;
			label = reference.Name;
			icon = Context.GetIcon (Stock.MiscFiles);
		}
		
		public override int CompareObjects (ITreeNavigator thisNode, ITreeNavigator otherNode)
		{
			try {
				if (thisNode == null || otherNode == null)
					return -1;
				var e1 = thisNode.DataItem as ModuleReference;
				var e2 = otherNode.DataItem as ModuleReference;
				
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
