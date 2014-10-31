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
using System.Threading.Tasks;
using System.Collections.Generic;
using MonoDevelop.Projects.Formats.MSBuild;
using Mono.Addins;
using MonoDevelop.Core;

namespace MonoDevelop.Projects.Extensions
{
	[ExtensionNode (ExtensionAttributeType=typeof(ExportProjectTypeAttribute))]
	public class ProjectTypeNode: SolutionItemTypeNode
	{
		public override async Task<SolutionItem> CreateSolutionItem (ProgressMonitor monitor, string fileName, string typeGuid)
		{
			MSBuildProject p = null;

			if (!string.IsNullOrEmpty (fileName)) {
				p = await MSBuildProject.LoadAsync (fileName);
//				if (MSBuildProjectService.CanMigrateFlavor (p.ProjectTypeGuids))
//					await MSBuildProjectService.MigrateFlavor (monitor, fileName, typeGuid, this, p);
			}

			if (p != null)
				Project.CreationContext.LockContext (p, typeGuid);
			try {
				var project = await base.CreateSolutionItem (monitor, fileName, typeGuid) as Project;
				if (project == null)
					throw new InvalidOperationException ("Project node type is not a subclass of MonoDevelop.Projects.Project");
				return project;
			} finally {
				if (p != null)
					Project.CreationContext.UnlockContext ();
			}
		}	
	}
}

