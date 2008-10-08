// PackagesNodeBuilder.cs
//
// Copyright (c) 2008 Christian Hergert <chris@dronelabs.com>
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
using System.IO;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;

using PyBinding;
using PyBinding.Parser;
using PyBinding.Parser.Dom;

namespace PyBinding.Gui.Navigation
{
	public class PackagesNode
	{
		public PythonProject Project {
			get;
			set;
		}
	}

	public class PackagesNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get {
				return typeof (PackagesNode);
			}
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return GettextCatalog.GetString ("Packages");
		}

		public override void BuildNode (ITreeBuilder treeBuilder,
		                                object dataObject,
		                                ref string label,
		                                ref Gdk.Pixbuf icon,
		                                ref Gdk.Pixbuf closedIcon)
		{
			label = GettextCatalog.GetString ("Packages");
			icon = Context.GetIcon (Stock.NameSpace);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder,
		                                      object       dataObject)
		{
			PackagesNode packages = dataObject as PackagesNode;
			
			foreach (ProjectFile projectFile in packages.Project.Files)
			{
				if (projectFile.BuildAction == BuildAction.Compile)
				{
					if (Path.GetExtension (projectFile.Name) == ".py")
					{
						treeBuilder.AddChild (new PackageNode () {
							Name = PythonHelper.ModuleFromFilename (projectFile.Name),
							ProjectFile = projectFile
						});
					}
				}
			}
		}
	}
}
