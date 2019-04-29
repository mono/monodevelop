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
using MonoDevelop.Ide;
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
			UpdateConfiguration ();
			LoadSettings ();
		}

		public Solution Solution { get; private set; }
		public ISettings Settings { get; private set; }
		public ConfigurationSelector Configuration { get; private set; }

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

		public Task<NuGetProject> GetNuGetProjectAsync (string nuGetProjectSafeName)
		{
			throw new NotImplementedException ();
		}

		public Task<IEnumerable<NuGetProject>> GetNuGetProjectsAsync ()
		{
			if (projects == null) {
				projects = GetNuGetProjects (Solution, Settings, Configuration).ToList ();
			}
			return Task.FromResult (projects.AsEnumerable ());
		}

		static IEnumerable<NuGetProject> GetNuGetProjects (Solution solution, ISettings settings, ConfigurationSelector configuration)
		{
			var factory = new MonoDevelopNuGetProjectFactory (settings, configuration);
			foreach (DotNetProject project in GetAllDotNetProjectsUsingReverseTopologicalSort (solution, configuration)) {
				yield return factory.CreateNuGetProject (project);
			}
		}

		/// <summary>
		/// Returning the projects in a reverse topological sort means that better caching of the
		/// PackageSpecs for each project can occur if PackageReference projects depend on other
		/// PackageReference projects since getting the PackageSpec for the root project will result in
		/// all dependencies being retrieved at the same time and add to the cache.
		/// </summary>
		static IEnumerable<DotNetProject> GetAllDotNetProjectsUsingReverseTopologicalSort (Solution solution, ConfigurationSelector config)
		{
			return solution.GetAllProjectsWithTopologicalSort (config)
				.OfType<DotNetProject> ()
				.Reverse ();
		}

		public Task<string> GetNuGetProjectSafeNameAsync (NuGetProject nuGetProject)
		{
			throw new NotImplementedException ();
		}

		public void OnActionsExecuted (IEnumerable<ResolvedAction> actions)
		{
		}

		public NuGetProject GetNuGetProject (IDotNetProject project)
		{
			return new MonoDevelopNuGetProjectFactory (Settings, Configuration)
				.CreateNuGetProject (project);
		}

		public ISourceRepositoryProvider CreateSourceRepositoryProvider ()
		{
			return SourceRepositoryProviderFactory.CreateSourceRepositoryProvider (Settings);
		}

		public void SaveProject (NuGetProject nuGetProject)
		{
			var hasProject = nuGetProject as IHasDotNetProject;

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
			UpdateConfiguration ();
		}

		void UpdateConfiguration ()
		{
			Configuration = IdeApp.IsInitialized ? IdeApp.Workspace?.ActiveConfiguration ?? ConfigurationSelector.Default : ConfigurationSelector.Default;
		}

		public void EnsureSolutionIsLoaded ()
		{
		}

		public Task<bool> DoesNuGetSupportsAnyProjectAsync ()
		{
			throw new NotImplementedException ();
		}

		public Task<bool> IsSolutionAvailableAsync ()
		{
			return Task.FromResult (true);
		}
	}
}

