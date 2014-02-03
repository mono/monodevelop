//
// ProjectReferencePclNodeBuilder.cs
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
using MonoDevelop.Projects;
using MonoDevelop.Core;
using Gdk;

namespace MonoDevelop.Ide.Gui.Pads.ProjectPad
{
	class PortableFrameworkSubset
	{
		public PortableDotNetProject Project { get; private set; }

		public PortableFrameworkSubset (PortableDotNetProject project)
		{
			this.Project = project;
		}
	}

	class PortableFrameworkSubsetNodeBuilder : TypeNodeBuilder
	{
		public override Type NodeDataType {
			get { return typeof (PortableFrameworkSubset); }
		}

		public override string GetNodeName (ITreeNavigator thisNode, object dataObject)
		{
			return ".NET Portable Subset";
		}

		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo node)
		{
			base.BuildNode (treeBuilder, dataObject, node);

			var project = ((PortableFrameworkSubset) dataObject).Project;
			node.Label = GLib.Markup.EscapeText (GettextCatalog.GetString (".NET Portable Subset"));
			if (!project.TargetRuntime.IsInstalled (project.TargetFramework))
				node.Label = "<span color='red'>" + node.Label + "</span>";

			node.Icon = Context.GetIcon ("md-reference-package");
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return true;
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var project = ((PortableFrameworkSubset) dataObject).Project;

			if (!project.TargetRuntime.IsInstalled (project.TargetFramework)) {
				string msg = GettextCatalog.GetString ("Framework not installed: {0}", project.TargetFramework.Id);
				treeBuilder.AddChild (new TreeViewItem (msg, Stock.Warning));
			}

			foreach (var asm in project.TargetRuntime.AssemblyContext.GetAssemblies (project.TargetFramework))
				if (asm.Package.IsFrameworkPackage && asm.Name != "mscorlib")
					treeBuilder.AddChild (new ImplicitFrameworkAssemblyReference (asm));
		}
	}
}
