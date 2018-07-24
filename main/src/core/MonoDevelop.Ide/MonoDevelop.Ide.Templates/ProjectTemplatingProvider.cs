//
// ProjectTemplateProvider.cs
//
// Author:
//       Todd Berman  <tberman@off.net>
//       Lluis Sanchez Gual <lluis@novell.com>
//       Viktoria Dudka  <viktoriad@remobjects.com>
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2004 Todd Berman
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// Copyright (c) 2009 RemObjects Software
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Ide.Projects;
using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Templates
{
	class ProjectTemplatingProvider : IProjectTemplatingProvider
	{
		public IEnumerable<SolutionTemplate> GetTemplates ()
		{
			return ProjectTemplate.ProjectTemplates
				.Select (template => new DefaultSolutionTemplate (template))
				.ToList ();
		}

		public bool CanProcessTemplate (SolutionTemplate template)
		{
			return template is DefaultSolutionTemplate;
		}

		public Task<ProcessedTemplateResult> ProcessTemplate (SolutionTemplate template, NewProjectConfiguration config, SolutionFolder parentFolder)
		{
			return ProcessTemplate ((DefaultSolutionTemplate)template, config, parentFolder);
		}

		async Task<ProcessedTemplateResult> ProcessTemplate (DefaultSolutionTemplate template, NewProjectConfiguration config, SolutionFolder parentFolder)
		{
			IEnumerable<IWorkspaceFileObject> newItems = null;
			ProjectCreateInformation cinfo = CreateProjectCreateInformation (config, parentFolder);

			if (parentFolder == null) {
				IWorkspaceFileObject newItem = await template.Template.CreateWorkspaceItem (cinfo);
				if (newItem != null) {
					newItems = new [] { newItem };
				}
			} else {
				newItems = template.Template.CreateProjects (parentFolder, cinfo);
			}
			return new DefaultProcessedTemplateResult (template.Template, newItems, cinfo.ProjectBasePath);
		}

		ProjectCreateInformation CreateProjectCreateInformation (NewProjectConfiguration config, SolutionFolder parentFolder)
		{
			ProjectCreateInformation cinfo = new ProjectCreateInformation ();
			cinfo.SolutionPath = new FilePath (config.SolutionLocation).ResolveLinks ();
			cinfo.ProjectBasePath = new FilePath (config.ProjectLocation).ResolveLinks ();
			cinfo.ProjectName = config.ProjectName;
			cinfo.SolutionName = config.SolutionName;
			cinfo.ParentFolder = parentFolder;
			cinfo.ActiveConfiguration = IdeApp.Workspace?.ActiveConfiguration ?? ConfigurationSelector.Default;
			cinfo.Parameters = config.Parameters;
			return cinfo;
		}
	}
}

