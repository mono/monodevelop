//
// SolutionProxy.cs
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

namespace MonoDevelop.PackageManagement
{
	internal class SolutionProxy : ISolution
	{
		Solution solution;
		EventHandler<DotNetProjectEventArgs> projectAdded;
		EventHandler<DotNetProjectEventArgs> projectRemoved;

		public SolutionProxy (Solution solution)
		{
			this.solution = solution;
		}

		public Solution Solution {
			get { return solution; }
		}

		public FilePath BaseDirectory {
			get { return solution.BaseDirectory; }
		}

		public FilePath FileName {
			get { return solution.FileName; }
		}

		public IEnumerable<IDotNetProject> GetAllProjects ()
		{
			return solution
				.GetAllProjects ()
				.OfType<DotNetProject> ()
				.Select (project => new DotNetProjectProxy (project));
		}

		public event EventHandler<DotNetProjectEventArgs> ProjectAdded {
			add {
				if (projectAdded == null) {
					solution.SolutionItemAdded += SolutionItemAdded;
				}
				projectAdded += value;
			}
			remove {
				projectAdded -= value;
				if (projectAdded == null) {
					solution.SolutionItemAdded -= SolutionItemAdded;
				}
			}
		}

		void SolutionItemAdded (object sender, SolutionItemChangeEventArgs e)
		{
			var project = e.SolutionItem as DotNetProject;
			if (project != null) {
				projectAdded (this, new DotNetProjectEventArgs (project));
			}
		}

		public event EventHandler<DotNetProjectEventArgs> ProjectRemoved {
			add {
				if (projectRemoved == null) {
					solution.SolutionItemRemoved += SolutionItemRemoved;
				}
				projectRemoved += value;
			}
			remove {
				projectRemoved -= value;
				if (projectRemoved == null) {
					solution.SolutionItemRemoved -= SolutionItemRemoved;
				}
			}
		}

		void SolutionItemRemoved (object sender, SolutionItemChangeEventArgs e)
		{
			var project = e.SolutionItem as DotNetProject;
			if (project != null) {
				projectRemoved (this, new DotNetProjectEventArgs (project));
			}
		}

		public bool Equals (ISolution solution)
		{
			if (solution == null)
				return false;

			return Solution == solution.Solution;
		}

		public IDotNetProject ResolveProject (ProjectReference projectReference)
		{
			var project = projectReference.ResolveProject (solution) as DotNetProject;
			if (project != null)
				return new DotNetProjectProxy (project);

			return null;
		}
	}
}

