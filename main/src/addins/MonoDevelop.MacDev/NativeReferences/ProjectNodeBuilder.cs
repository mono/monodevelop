// 
// ProjectNodeBuilder.cs
//  
// Author:
//       Michael Hutchinson <m.j.hutchinson@gmail.com>
// 
// Copyright (c) 2011 Michael Hutchinson
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
using System.Collections.Generic;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.MacDev.NativeReferences
{
	public struct NativeReferenceFileFilter
	{
		public string Description;
		public string FileExtension;

		public NativeReferenceFileFilter (string desc, string ext)
		{
			Description = desc;
			FileExtension = ext;
		}
	}

	public interface INativeReferencingProject
	{
		bool AlwaysShowNativeReferencesFolder { get; }

		IEnumerable<NativeReferenceFileFilter> NativeReferenceFileFilters { get; }

		NativeReferenceKind GetNativeReferenceKind (FilePath path);
	}
	
	class ProjectNodeBuilder: NodeBuilderExtension
	{
		public override bool CanBuildNode (Type dataType)
		{
			return typeof (INativeReferencingProject).IsAssignableFrom (dataType)
				&& typeof (DotNetProject).IsAssignableFrom (dataType);
		}
		
		public override Type CommandHandlerType
		{
			get { return typeof (NativeReferenceFolderCommandHandler); }
		}
		
		public override bool HasChildNodes (ITreeBuilder builder, object dataObject)
		{
			var project = (DotNetProject) dataObject;
			return ((INativeReferencingProject) project).AlwaysShowNativeReferencesFolder || project.Items.GetAll<NativeReference> ().Any ();
		}
		
		public override void BuildChildNodes (ITreeBuilder builder, object dataObject)
		{
			var project = (DotNetProject) dataObject;
			if (HasChildNodes (builder, dataObject))
				builder.AddChild (new NativeReferenceFolder (project));
		}
		
		public override void OnNodeAdded (object dataObject)
		{
			var project = (DotNetProject) dataObject;
			project.ProjectItemAdded += OnItemsChanged;
			project.ProjectItemRemoved += OnItemsChanged;
			base.OnNodeAdded (dataObject);
		}
		
		public override void OnNodeRemoved (object dataObject)
		{
			var project = (DotNetProject) dataObject;
			project.ProjectItemAdded -= OnItemsChanged;
			project.ProjectItemRemoved -= OnItemsChanged;
			base.OnNodeRemoved (dataObject);
		}

		void OnItemsChanged (object sender, ProjectItemEventArgs e)
		{
			var projects = new HashSet<DotNetProject> ();
			foreach (ProjectItemEventInfo evt in e)
				if (evt.Item is NativeReference)
					projects.Add ((DotNetProject)evt.SolutionItem);
			foreach (var project in projects) {
				ITreeBuilder builder = Context.GetTreeBuilder (project);
				if (builder != null)
					builder.UpdateChildren ();
			}
		}
	}
}

