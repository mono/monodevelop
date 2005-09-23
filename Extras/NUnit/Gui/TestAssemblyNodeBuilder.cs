//
// TestAssemblyNodeBuilder.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections;

using MonoDevelop.Internal.Project;
using MonoDevelop.Services;
using MonoDevelop.Gui.Pads;
using MonoDevelop.Commands;
using MonoDevelop.Gui;

namespace MonoDevelop.NUnit
{
	public class TestAssemblyNodeBuilder: TypeNodeBuilder
	{
		public override Type CommandHandlerType {
			get { return typeof(TestAssemblyNodeCommandHandler); }
		}
		
		public override string ContextMenuAddinPath {
			get { return "/SharpDevelop/Views/ProjectBrowser/ContextMenu/TestAssembly"; }
		}

		public override Type NodeDataType {
			get { return typeof(TestAssembly); }
		}
		
		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return Path.GetFileName (((TestAssembly)dataObject).Path);
		}
		
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Gdk.Pixbuf icon, ref Gdk.Pixbuf closedIcon)
		{
			TestAssembly asm = dataObject as TestAssembly;
			label = Path.GetFileName (asm.Path);
			icon = Context.GetIcon (Stock.Reference);
		}
	}
	
	class TestAssemblyNodeCommandHandler: NodeCommandHandler
	{
		[CommandHandler (EditCommands.Delete)]
		protected void OnDeleteAssembly ()
		{
			TestAssembly asm = CurrentNode.DataItem as TestAssembly;
			NUnitAssemblyGroupProjectConfiguration config = (NUnitAssemblyGroupProjectConfiguration) CurrentNode.GetParentDataItem (typeof(NUnitAssemblyGroupProjectConfiguration), false);
			config.Assemblies.Remove (asm);
		}
	}
}
