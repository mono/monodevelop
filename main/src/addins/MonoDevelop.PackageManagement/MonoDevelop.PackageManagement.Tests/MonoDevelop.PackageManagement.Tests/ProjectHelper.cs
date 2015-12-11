//
// ProjectHelper.cs
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
using MonoDevelop.Core;
using MonoDevelop.PackageManagement;
using MonoDevelop.Projects;
using System.Linq;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public static class ProjectHelper
	{
		public static ISolution CreateSolution ()
		{
			return new FakeSolution ();
		}

		public static FakeDotNetProject CreateTestProject ()
		{
			return CreateTestProject ("TestProject");
		}

		public static FakeDotNetProject CreateTestProject (string name)
		{
			ISolution solution = CreateSolution ();

			return CreateTestProject (solution, name);
		}

		public static FakeDotNetProject CreateTestProject (
			ISolution parentSolution,
			string name,
			string fileName = null)
		{
			return new FakeDotNetProject {
				ParentSolution = parentSolution,
				FileName = new FilePath (fileName),
				Name = name
			};
		}

		public static FakeDotNetProject CreateTestWebApplicationProject ()
		{
			FakeDotNetProject project = CreateTestProject ();
			AddWebApplicationProjectType (project);
			return project;
		}

		public static FakeDotNetProject CreateTestWebSiteProject ()
		{
			FakeDotNetProject project = CreateTestProject ();
			AddWebSiteProjectType (project);
			return project;
		}

		public static void AddWebApplicationProjectType (FakeDotNetProject project)
		{
			AddProjectType (project, DotNetProjectExtensions.WebApplication);
		}

		public static void AddWebSiteProjectType (FakeDotNetProject project)
		{
			AddProjectType (project, DotNetProjectExtensions.WebSite);
		}

		public static void AddProjectType (FakeDotNetProject project, Guid guid)
		{
			project.AddProjectType (guid);
		}

		public static void AddReference (FakeDotNetProject project, string referenceName, string hintPath = null)
		{
			var reference = ProjectReference.CreateCustomReference (ReferenceType.Assembly, referenceName, hintPath);
			project.References.Add (reference);
		}

		public static void AddGacReference (FakeDotNetProject project, string referenceName)
		{
			var reference = ProjectReference.CreateAssemblyReference (referenceName);
			project.References.Add (reference);
		}

		public static void AddFile (FakeDotNetProject project, string fileName)
		{
			project.Files.Add (new ProjectFile (fileName.ToNativePath ()));
		}

		public static ProjectReference GetReference (FakeDotNetProject project, string referenceName)
		{
			foreach (ProjectReference referenceProjectItem in project.References) {
				if (referenceProjectItem.Reference == referenceName) {
					return referenceProjectItem;
				}
			}
			return null;
		}

		public static ProjectFile GetFile (FakeDotNetProject project, string fileName)
		{
			return project.FilesAdded.FirstOrDefault (file => file.FilePath == new FilePath (fileName));
		}
	}
}