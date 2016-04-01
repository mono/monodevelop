//
// MonoDevelopSolutionManager.cs
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

using System;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopSolutionManager : ISolutionManager
	{
		Solution solution;
		List<NuGetProject> projects;

		public MonoDevelopSolutionManager (ISolution solution)
			: this (solution.Solution)
		{
		}

		public MonoDevelopSolutionManager (Solution solution)
		{
			this.solution = solution;
		}

		public NuGetProject DefaultNuGetProject {
			get {
				throw new NotImplementedException ();
			}
		}

		public string DefaultNuGetProjectName {
			get {
				throw new NotImplementedException ();
			}

			set {
				throw new NotImplementedException ();
			}
		}

		public bool IsSolutionAvailable {
			get { return true; }
		}

		public bool IsSolutionOpen {
			get { return true; }
		}

		public INuGetProjectContext NuGetProjectContext { get; set; }

		public string SolutionDirectory {
			get { return solution.BaseDirectory; }
		}

		public event EventHandler<ActionsExecutedEventArgs> ActionsExecuted;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectAdded;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectRemoved;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectRenamed;
		public event EventHandler SolutionClosed;
		public event EventHandler SolutionClosing;
		public event EventHandler SolutionOpened;
		public event EventHandler SolutionOpening;

		public NuGetProject GetNuGetProject (string nuGetProjectSafeName)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<NuGetProject> GetNuGetProjects ()
		{
			if (projects == null) {
				Runtime.RunInMainThread (() => {
					projects = GetNuGetProjects (solution).ToList ();
				}).Wait ();
			}
			return projects;
		}

		static IEnumerable<NuGetProject> GetNuGetProjects (Solution solution)
		{
			var factory = new MonoDevelopNuGetProjectFactory ();
			foreach (DotNetProject project in solution.GetAllDotNetProjects ()) {
				yield return factory.CreateNuGetProject (project);
			}
		}

		public string GetNuGetProjectSafeName (NuGetProject nuGetProject)
		{
			throw new NotImplementedException ();
		}

		public void OnActionsExecuted (IEnumerable<ResolvedAction> actions)
		{
		}
	}
}

