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
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class MonoDevelopSolutionManager : IMonoDevelopSolutionManager
	{
		List<NuGetProject> projects;

		public MonoDevelopSolutionManager (ISolution solution)
			: this (solution.Solution)
		{
		}

		public MonoDevelopSolutionManager (Solution solution)
		{
			Solution = solution;
			LoadSettings ();
			solution.SolutionItemAdded += (sender, e) => {
				projects = null;
			};
			solution.SolutionItemRemoved += (sender, e) => {
				projects = null;
			};
		}

		public Solution Solution { get; private set; }
		public ISettings Settings { get; private set; }

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
			get { return Solution.BaseDirectory; }
		}

		#pragma warning disable 67
		public event EventHandler<ActionsExecutedEventArgs> ActionsExecuted;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectAdded;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectRemoved;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectRenamed;
		public event EventHandler<NuGetProjectEventArgs> AfterNuGetProjectRenamed;
		public event EventHandler<NuGetProjectEventArgs> NuGetProjectUpdated;
		public event EventHandler<NuGetEventArgs<string>> AfterNuGetCacheUpdated;
		public event EventHandler SolutionClosed;
		public event EventHandler SolutionClosing;
		public event EventHandler SolutionOpened;
		public event EventHandler SolutionOpening;
		#pragma warning restore 67

		public NuGetProject GetNuGetProject (string nuGetProjectSafeName)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<NuGetProject> GetNuGetProjects ()
		{
			if (projects == null) {
				Runtime.RunInMainThread (() => {
					projects = GetNuGetProjects (Solution, Settings).ToList ();
				}).Wait ();
			}
			return projects;
		}

		static IEnumerable<NuGetProject> GetNuGetProjects (Solution solution, ISettings settings)
		{
			var factory = new MonoDevelopNuGetProjectFactory (settings);
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

		public NuGetProject GetNuGetProject (IDotNetProject project)
		{
			return new MonoDevelopNuGetProjectFactory (Settings)
				.CreateNuGetProject (project);
		}

		public ISourceRepositoryProvider CreateSourceRepositoryProvider ()
		{
			return SourceRepositoryProviderFactory.CreateSourceRepositoryProvider (Settings);
		}

		public void SaveProject (NuGetProject nuGetProject)
		{
			IHasDotNetProject hasProject = null;

			var msbuildProject = nuGetProject as MSBuildNuGetProject;
			if (msbuildProject != null) {
				hasProject = msbuildProject.MSBuildNuGetProjectSystem as IHasDotNetProject;
			}

			if (hasProject == null) {
				hasProject = nuGetProject as IHasDotNetProject;
			}

			if (hasProject != null) {
				hasProject.SaveProject ().Wait ();
				return;
			}

			throw new ApplicationException (string.Format ("Unsupported NuGetProject type: {0}", nuGetProject.GetType ().FullName));
		}

		public void ReloadSettings ()
		{
			try {
				LoadSettings ();
			} catch (Exception ex) {
				LoggingService.LogError ("Failed to reload settings.", ex);
			}
		}

		void LoadSettings ()
		{
			string rootDirectory = Path.Combine (Solution.BaseDirectory, ".nuget");
			Settings = SettingsLoader.LoadDefaultSettings (rootDirectory, reportError: true);
		}

		public void ClearProjectCache ()
		{
			projects = null;
		}

		public bool IsSolutionDPLEnabled { get; private set; }

		public void EnsureSolutionIsLoaded ()
		{
		}

		public Task<NuGetProject> UpdateNuGetProjectToPackageRef (NuGetProject oldProject)
		{
			return Task.FromResult (oldProject);
		}
	}
}

