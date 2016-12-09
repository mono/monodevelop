//
// CrossPlatformLibraryTemplateWizard.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.Core.StringParsing;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Templates;
using MonoDevelop.Projects;

namespace MonoDevelop.Packaging.Templating
{
	class CrossPlatformLibraryTemplateWizard : TemplateWizard
	{
		public override string Id {
			get { return "MonoDevelop.Packaging.CrossPlatformLibraryTemplateWizard"; }
		}

		public override WizardPage GetPage (int pageNumber)
		{
			return new CrossPlatformLibraryTemplateWizardPage (this);
		}

		public override void ItemsCreated (IEnumerable<IWorkspaceFileObject> items)
		{
			string projectName = Parameters["ProjectName"];
			var libraryProjects = GetLibraryProjects (items).ToList ();

			foreach (DotNetProject project in libraryProjects) {
				project.SetOutputAssemblyName (projectName);
			}

			if (Parameters.GetBoolValue ("CreatePortableProject")) {
				AddNuGetPackageMetadataToPclProject (libraryProjects);
			}

			SaveAsync (libraryProjects);
		}

		IEnumerable<DotNetProject> GetLibraryProjects (IEnumerable<IWorkspaceFileObject> items)
		{
			Solution solution = items.OfType<Solution> ().FirstOrDefault ();
			if (solution != null) {
				return GetLibraryProjects (solution.GetAllProjects ());
			}

			return items.OfType<DotNetProject> ().Where (p => !(p is PackagingProject));
		}

		void AddNuGetPackageMetadataToPclProject (IEnumerable<DotNetProject> projects)
		{
			var pclProject = projects.FirstOrDefault (p => p.IsPortableLibrary);
			if (pclProject != null) {
				var metadata = new NuGetPackageMetadata ();
				metadata.Id = Parameters["PackageId"];
				metadata.Description = Parameters["PackageDescription"];
				metadata.Version = Parameters["PackageVersion"];
				metadata.Authors = Parameters["PackageAuthors"];

				metadata.UpdateProject (pclProject);
			}
		}

		protected virtual void SaveAsync (IEnumerable<SolutionItem> projects)
		{
			IdeApp.ProjectOperations.SaveAsync (projects);
		}
	}
}
