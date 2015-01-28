//
// DotNetProjectProxy.cs
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
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	public class DotNetProjectProxy : ProjectProxy, IDotNetProject
	{
		DotNetProject project;
		EventHandler<ProjectModifiedEventArgs> projectModifiedHandler;

		public DotNetProjectProxy (DotNetProject project)
			: base (project)
		{
			this.project = project;
		}

		public DotNetProject DotNetProject {
			get { return project; }
		}

		public TargetFrameworkMoniker TargetFrameworkMoniker {
			get { return project.TargetFramework.Id; }
		}

		public string DefaultNamespace {
			get { return project.DefaultNamespace; }
		}

		public ProjectReferenceCollection References {
			get { return project.References; }
		}

		public ProjectFileCollection Files {
			get { return project.Files; }
		}

		public void AddFile (ProjectFile projectFile)
		{
			project.AddFile (projectFile);
		}

		public string GetDefaultBuildAction (string fileName)
		{
			return project.GetDefaultBuildAction (fileName);
		}

		public bool IsFileInProject (string fileName)
		{
			return project.IsFileInProject (fileName);
		}

		public void AddImportIfMissing (string name, string condition)
		{
			project.AddImportIfMissing (name, condition);
		}

		public void RemoveImport (string name)
		{
			project.RemoveImport (name);
		}

		public event EventHandler<ProjectModifiedEventArgs> Modified {
			add {
				if (projectModifiedHandler == null) {
					project.Modified += ProjectModified;
				}
				projectModifiedHandler += value;
			}
			remove {
				projectModifiedHandler -= value;
				if (projectModifiedHandler == null) {
					project.Modified -= ProjectModified;
				}
			}
		}

		void ProjectModified (object sender, SolutionItemModifiedEventArgs e)
		{
			foreach (ProjectModifiedEventArgs eventArgs in ProjectModifiedEventArgs.Create (e)) {
				projectModifiedHandler (this, eventArgs);
			}
		}

		public bool Equals (IDotNetProject project)
		{
			return DotNetProject == project.DotNetProject;
		}

		public void RefreshProjectBuilder ()
		{
			DotNetProject.RefreshProjectBuilder ();
		}

		public void DisposeProjectBuilder ()
		{
			DotNetProject.DisposeProjectBuilder ();
		}
	}
}

