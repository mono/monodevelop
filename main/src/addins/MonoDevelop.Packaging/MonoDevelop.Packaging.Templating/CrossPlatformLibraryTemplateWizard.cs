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

		public override int TotalPages {
			get { return 0; }
		}

		public override WizardPage GetPage (int pageNumber)
		{
			return null;
		}

		public override void ConfigureWizard ()
		{
			Parameters["PackageId"] = Parameters["UserDefinedProjectName"];
			Parameters["PackageDescription"] = Parameters["UserDefinedProjectName"];
			Parameters["Authors"] = AuthorInformation.Default.Name;
		}

		public override void ItemsCreated (IEnumerable<IWorkspaceFileObject> items)
		{
			var libraryProjects = GetLibraryProjects (items).ToList ();

			foreach (DotNetProject project in libraryProjects) {
				project.AddCommonPackagingImports ();
			}

			IdeApp.ProjectOperations.SaveAsync (libraryProjects);
		}

		IEnumerable<DotNetProject> GetLibraryProjects (IEnumerable<IWorkspaceFileObject> items)
		{
			Solution solution = items.OfType<Solution> ().FirstOrDefault ();
			if (solution != null) {
				return GetLibraryProjects (solution.GetAllProjects ());
			}

			return items.OfType<DotNetProject> ().Where (p => !(p is PackagingProject));
		}
	}
}
