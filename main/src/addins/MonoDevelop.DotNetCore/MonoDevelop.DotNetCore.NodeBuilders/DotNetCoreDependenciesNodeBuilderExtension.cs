//
// DotNetCoreDependenciesNodeBuilderExtension.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.PackageManagement.NodeBuilders;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore.NodeBuilders
{
	class DotNetCoreDependenciesNodeBuilderExtension : NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (DependenciesNode).IsAssignableFrom (dataType);
		}

		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			return ProjectHasReferences ((DependenciesNode)dataObject);
		}

		public override void BuildChildNodes (ITreeBuilder treeBuilder, object dataObject)
		{
			var dependenciesNode = (DependenciesNode)dataObject;
			if (!ProjectHasReferences (dependenciesNode))
				return;

			var assembliesNode = new DotNetCoreAssemblyDependenciesNode (dependenciesNode.Project);
			if (assembliesNode.HasChildNodes ())
				treeBuilder.AddChild (assembliesNode);

			var projectsNode = new DotNetCoreProjectDependenciesNode (dependenciesNode.Project);
			if (projectsNode.HasChildNodes ())
				treeBuilder.AddChild (projectsNode);
		}

		bool ProjectHasReferences (DependenciesNode dependenciesNode)
		{
			return dependenciesNode.Project.References.Any ();
		}

		protected override void Initialize ()
		{
			IdeApp.Workspace.ReferenceAddedToProject += OnReferencesChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject += OnReferencesChanged;
		}

		public override void Dispose ()
		{
			IdeApp.Workspace.ReferenceAddedToProject -= OnReferencesChanged;
			IdeApp.Workspace.ReferenceRemovedFromProject -= OnReferencesChanged;
		}

		void OnReferencesChanged (object sender, ProjectReferenceEventArgs e)
		{
			RefreshAllChildNodes (e.Project as DotNetProject);
		}

		void RefreshAllChildNodes (DotNetProject project)
		{
			if (project == null)
				return;

			Runtime.RunInMainThread (() => {
				RefreshChildNodes (project);
			});
		}

		void RefreshChildNodes (DotNetProject project)
		{
			ITreeBuilder builder = Context.GetTreeBuilder (project);
			if (builder != null) {
				if (builder.MoveToChild (DependenciesNode.NodeName, typeof (DependenciesNode))) {
					builder.UpdateAll ();
				}
			}
		}
	}
}
