//
// FakeSolutionManager.cs
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
using System.Threading.Tasks;
using NuGet.Configuration;
using NuGet.PackageManagement;
using NuGet.ProjectManagement;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakeSolutionManager : IMonoDevelopSolutionManager
	{
		public FakeSolutionManager ()
		{
			SolutionDirectory = @"d:\projects\MyProject".ToNativePath ();
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
			get {
				throw new NotImplementedException ();
			}
		}

		public bool IsSolutionOpen {
			get {
				throw new NotImplementedException ();
			}
		}

		public INuGetProjectContext NuGetProjectContext {
			get {
				throw new NotImplementedException ();
			}

			set {
				throw new NotImplementedException ();
			}
		}

		public FakeNuGetSettings FakeSettings = new FakeNuGetSettings ();

		public ISettings Settings {
			get { return FakeSettings; }
		}

		public string SolutionDirectory { get; set; }

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

		public FakeSourceRepositoryProvider SourceRepositoryProvider = new FakeSourceRepositoryProvider ();

		public ISourceRepositoryProvider CreateSourceRepositoryProvider ()
		{
			return SourceRepositoryProvider;
		}

		public NuGetProject GetNuGetProject (string nuGetProjectSafeName)
		{
			throw new NotImplementedException ();
		}

		public Dictionary<IDotNetProject, FakeNuGetProject> NuGetProjects = new Dictionary<IDotNetProject, FakeNuGetProject> ();

		public NuGetProject GetNuGetProject (IDotNetProject project)
		{
			FakeNuGetProject nugetProject = null;
			if (NuGetProjects.TryGetValue (project, out nugetProject))
				return nugetProject;

			return new FakeNuGetProject (project);
		}

		public IEnumerable<NuGetProject> GetNuGetProjects ()
		{
			return NuGetProjects.Values;
		}

		public string GetNuGetProjectSafeName (NuGetProject nuGetProject)
		{
			throw new NotImplementedException ();
		}

		public void OnActionsExecuted (IEnumerable<ResolvedAction> actions)
		{
			throw new NotImplementedException ();
		}

		public void ReloadSettings ()
		{
			throw new NotImplementedException ();
		}

		public void SaveProject (NuGetProject nugetProject)
		{
		}

		public bool IsSolutionDPLEnabled { get; set; }

		public void EnsureSolutionIsLoaded ()
		{
		}

		public void ClearProjectCache ()
		{
		}

		public Task<NuGetProject> UpdateNuGetProjectToPackageRef (NuGetProject oldProject)
		{
			throw new NotImplementedException ();
		}
	}
}

