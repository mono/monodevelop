//
// MSBuildProjectNode.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Threading.Tasks;
using System.Collections.Generic;
using MonoDevelop.Projects.MSBuild;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Extensions
{
	[ExtensionNode (ExtensionAttributeType=typeof(ExportProjectTypeAttribute))]
	public class ProjectTypeNode: SolutionItemTypeNode
	{
		[NodeAttribute ("msbuildSupport")]
		public MSBuildSupport MSBuildSupport { get; set; }

		public ProjectTypeNode ()
		{
			MSBuildSupport = MSBuildSupport.Supported;
		}

		public override async Task<SolutionItem> CreateSolutionItem (ProgressMonitor monitor, SolutionLoadContext ctx, string fileName)
		{
			MSBuildProject p = null;
			Project project = null;

			if (!string.IsNullOrEmpty (fileName)) {
				p = await MSBuildProject.LoadAsync (fileName);
				if (ctx != null && ctx.Solution != null) {
					p.EngineManager = ctx.Solution.MSBuildEngineManager;
					p.SolutionDirectory = ctx.Solution.ItemDirectory;
				}
				
				var migrators = MSBuildProjectService.GetMigrableFlavors (p.ProjectTypeGuids);
				if (migrators.Count > 0)
					await MSBuildProjectService.MigrateFlavors (monitor, fileName, Guid, p, migrators);

				var unsupporedFlavor = p.ProjectTypeGuids.FirstOrDefault (fid => !MSBuildProjectService.IsKnownFlavorGuid (fid) && !MSBuildProjectService.IsKnownTypeGuid (fid));
				if (unsupporedFlavor != null) {
					// The project has a flavor that's not supported. Return a fake project (if possible).
					return MSBuildProjectService.CreateUnknownSolutionItem (monitor, fileName, Guid, unsupporedFlavor, null);
				}

				if (MSBuildSupport == MSBuildSupport.NotSupported || MSBuildProjectService.GetMSBuildSupportForFlavors (p.ProjectTypeGuids) == MSBuildSupport.NotSupported)
					p.UseMSBuildEngine = false;

				// Evaluate the project now. If evaluation fails an exception will be thrown, and when that
				// happens the solution will create a placeholder project.
				p.Evaluate ();
			}

			if (project == null)
				project = await base.CreateSolutionItem (monitor, ctx, fileName) as Project;
			
			if (project == null)
				throw new InvalidOperationException ("Project node type is not a subclass of MonoDevelop.Projects.Project");
			
			if (p != null)
				project.SetCreationContext (Project.CreationContext.Create (p, Guid));
			return project;
		}	

		public virtual Project CreateProject (params string[] flavorGuids)
		{
			Project p;
			using (var monitor = new ProgressMonitor ()) {
				p = (Project)CreateSolutionItem (monitor, null, null).Result;
			}
			p.SetCreationContext (Project.CreationContext.Create (Guid, flavorGuids));
			return p;
		}
	}
}

