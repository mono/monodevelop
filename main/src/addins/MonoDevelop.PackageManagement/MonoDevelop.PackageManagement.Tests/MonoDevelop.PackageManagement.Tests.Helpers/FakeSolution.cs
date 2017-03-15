//
// FakeSolution.cs
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
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakeSolution : ISolution
	{
		public FilePath BaseDirectory { get; set; }
		public FilePath FileName { get; set; }

		public FakeSolution ()
		{
		}

		public FakeSolution (string fileName)
		{
			FileName = new FilePath (fileName.ToNativePath ());
			BaseDirectory = FileName.ParentDirectory;
		}

		public Solution Solution {
			get { return null; }
		}

		public List<FakeDotNetProject> Projects = new List<FakeDotNetProject> ();

		public IEnumerable<IDotNetProject> GetAllProjects ()
		{
			return Projects;
		}

		public event EventHandler<DotNetProjectEventArgs> ProjectAdded;

		public void RaiseProjectAddedEvent (IDotNetProject project)
		{
			if (ProjectAdded != null) {
				ProjectAdded (this, new DotNetProjectEventArgs (project));
			}
		}

		public event EventHandler<DotNetProjectEventArgs> ProjectRemoved;

		public void RaiseProjectRemovedEvent (IDotNetProject project)
		{
			if (ProjectRemoved != null) {
				ProjectRemoved (this, new DotNetProjectEventArgs (project));
			}
		}

		public bool Equals (ISolution solution)
		{
			return this == solution;
		}

		public IDotNetProject ResolveProject (ProjectReference projectReference)
		{
			if (OnResolveProject != null)
				return OnResolveProject (projectReference);
			return Projects.FirstOrDefault (project => project.Name == projectReference.Include);
		}

		public Func<ProjectReference, IDotNetProject> OnResolveProject;
	}
}

