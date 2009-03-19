// PackageNodeBuilder.cs
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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;

using PyBinding;
using PyBinding.Parser;
using PyBinding.Parser.Dom;

namespace PyBinding.Gui.Navigation
{
	public class PackageNode
	{
		public string Name {
			get;
			set;
		}

		public ProjectFile ProjectFile {
			get;
			set;
		}
	}
	
	public class PackageNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get {
				return typeof (PackageNode);
			}
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			PackageNode node = dataObject as PackageNode;
			return node.Name;
		}

		public override void BuildNode (ITreeBuilder treeBuilder,
		                                object dataObject,
		                                ref string label,
		                                ref Gdk.Pixbuf icon,
		                                ref Gdk.Pixbuf closedIcon)
		{
			PackageNode packageNode = dataObject as PackageNode;

			label = packageNode.Name;
			icon = Context.GetIcon (Stock.NameSpace);
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder,
		                                      object       dataObject)
		{
			PackageNode packageNode = dataObject as PackageNode;
			PythonParsedDocument parsed = ProjectDomService.ParseFile (null, packageNode.ProjectFile.Name) as PythonParsedDocument;

			if (parsed != null && parsed.Module != null) {
				foreach (PythonClass pyClass in parsed.Module.Classes)
					treeBuilder.AddChild (pyClass);
				foreach (PythonAttribute pyAttr in parsed.Module.Attributes)
					treeBuilder.AddChild (pyAttr);
				foreach (PythonFunction pyFunc in parsed.Module.Functions)
					treeBuilder.AddChild (pyFunc);
			}
		}
	}
}
