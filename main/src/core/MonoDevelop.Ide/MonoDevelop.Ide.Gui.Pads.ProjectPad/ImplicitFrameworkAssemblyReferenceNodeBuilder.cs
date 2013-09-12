//
// ImplicitFrameworkAssemblyReferenceNodeBuilder.cs
//
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
//
// Copyright (c) 2013 Xamarin Inc.
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
using MonoDevelop.Core.Assemblies;
using Gdk;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	public class ImplicitFrameworkAssemblyReference
	{
		public SystemAssembly Assembly { get; private set; }

		public ImplicitFrameworkAssemblyReference (SystemAssembly assembly)
		{
			this.Assembly = assembly;
		}
	}

	class ImplicitFrameworkAssemblyReferenceNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get {
				return typeof(ImplicitFrameworkAssemblyReference);
			}
		}

		public override Type CommandHandlerType {
			get {
				return typeof(ImplicitFrameworkAssemblyReferenceNodeCommandHandler);
			}
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			var aref = (ImplicitFrameworkAssemblyReference) dataObject;
			return aref.Assembly.Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, ref string label, ref Pixbuf icon, ref Pixbuf closedIcon)
		{
			var aref = (ImplicitFrameworkAssemblyReference) dataObject;
			icon = Context.GetIcon ("md-reference-package");
			label = aref.Assembly.Name;
		}
	}

	class ImplicitFrameworkAssemblyReferenceNodeCommandHandler: NodeCommandHandler
	{
		public override void ActivateItem ()
		{
			var aref = (ImplicitFrameworkAssemblyReference) CurrentNode.DataItem;
			IdeApp.Workbench.OpenDocument (aref.Assembly.Location);
		}
	}
}
