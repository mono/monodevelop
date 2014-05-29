//
// FakePackageManagementProjectFactory.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
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

using System;
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using MonoDevelop.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakePackageManagementProjectFactory : IPackageManagementProjectFactory
	{
		public FakePackageManagementProjectFactory ()
		{
			CreatePackageManagementProject = (sourceRepository, project) => {
				RepositoriesPassedToCreateProject.Add (sourceRepository);
				ProjectsPassedToCreateProject.Add (project);

				var fakeProject = new FakePackageManagementProject ();
				FakeProjectsCreated.Add (fakeProject);
				return fakeProject;
			};
		}

		public List<FakePackageManagementProject> FakeProjectsCreated = 
			new List<FakePackageManagementProject> ();

		public FakePackageManagementProject FirstFakeProjectCreated {
			get { return FakeProjectsCreated [0]; }
		}

		public IPackageRepository FirstRepositoryPassedToCreateProject {
			get { return RepositoriesPassedToCreateProject [0]; }
		}

		public List<IPackageRepository> RepositoriesPassedToCreateProject = 
			new List<IPackageRepository> ();

		public IDotNetProject FirstProjectPassedToCreateProject {
			get { return ProjectsPassedToCreateProject [0]; }
		}

		public Func<IPackageRepository, IDotNetProject, FakePackageManagementProject>
			CreatePackageManagementProject = (sourceRepository, project) => {
			return null;
		};

		public List<IDotNetProject> ProjectsPassedToCreateProject =
			new List<IDotNetProject> ();

		public IPackageManagementProject CreateProject (IPackageRepository sourceRepository, IDotNetProject project)
		{
			return CreatePackageManagementProject (sourceRepository, project);
		}
	}
}

