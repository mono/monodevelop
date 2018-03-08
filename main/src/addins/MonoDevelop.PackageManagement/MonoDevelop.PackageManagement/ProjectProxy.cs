//
// ProjectProxy.cs
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

using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.MSBuild;

namespace MonoDevelop.PackageManagement
{
	internal class ProjectProxy : IProject
	{
		readonly Project project;

		public ProjectProxy (Project project)
		{
			this.project = project;
		}

		public string Name {
			get { return project.Name; }
		}

		public FilePath FileName {
			get { return project.FileName; }
		}

		public FilePath BaseDirectory {
			get { return project.BaseDirectory; }
		}

		public FilePath BaseIntermediateOutputPath {
			get { return project.BaseIntermediateOutputPath; }
		}

		public ISolution ParentSolution {
			get { return new SolutionProxy (project.ParentSolution); }
		}

		public IDictionary ExtendedProperties {
			get { return project.ExtendedProperties; }
		}

		public IEnumerable<string> FlavorGuids {
			get { return project.FlavorGuids; }
		}

		public IMSBuildEvaluatedPropertyCollection EvaluatedProperties {
			get { return project.MSBuildProject?.EvaluatedProperties; }
		}

		public async Task SaveAsync ()
		{
			using (var monitor = new ProgressMonitor ()) {
				await project.SaveAsync (monitor);
			}
		}

		public Task ReevaluateProject (ProgressMonitor monitor)
		{
			return project.ReevaluateProject (monitor);
		}

		public override int GetHashCode ()
		{
			return project.GetHashCode ();
		}

		public override bool Equals (object obj)
		{
			return Equals (obj as ProjectProxy);
		}

		public bool Equals (ProjectProxy other)
		{
			if (other != null) {
				return other.project == project;
			}
			return false;
		}
	}
}

