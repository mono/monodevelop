//
// MonoDevelopDotNetCorePackageRestorer.cs
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.PackageManagement
{
	class MonoDevelopDotNetCorePackageRestorer
	{
		List<DotNetProject> projects;

		public MonoDevelopDotNetCorePackageRestorer (DotNetProject project)
		{
			projects = new List<DotNetProject> ();
			projects.Add (project);
		}

		public MonoDevelopDotNetCorePackageRestorer (IEnumerable<DotNetCoreNuGetProject> nugetProjects)
		{
			this.projects = nugetProjects.Select (project => project.DotNetProject).ToList ();
		}

		public bool ReloadProject { get; set; }

		public async Task RestorePackages (CancellationToken cancellationToken)
		{
			string msbuildFileName = GetMSBuildFileName (projects[0]);

			foreach (DotNetProject project in projects) {

				var console = new DotNetCoreOperationConsole ();
				ProcessAsyncOperation operation = StartRestoreProcess (msbuildFileName, project, console);
				using (var registration = cancellationToken.Register (() => operation.Cancel ())) {
					await operation.Task;

					CheckForRestoreFailure (operation);
				}

				if (ReloadProject) {
					project.NeedsReload = true;
					FileService.NotifyFileChanged (project.FileName);
				} else {
					RefreshProjectReferences (project);
				}
			}
		}

		static string GetMSBuildFileName (DotNetProject project)
		{
			TargetRuntime runtime = project.TargetRuntime ?? Runtime.SystemAssemblyService.CurrentRuntime;
			string msbuildPath = runtime.GetMSBuildBinPath ("15.0");

			string msbuildFileName = Path.Combine (msbuildPath, "MSBuild.dll");
			if (File.Exists (msbuildFileName))
				return msbuildFileName;

			msbuildFileName = Path.Combine (msbuildPath, "MSBuild.exe");
			if (File.Exists (msbuildFileName))
				return msbuildFileName;

			throw new UserException (GettextCatalog.GetString ("Unable to find MSBuild 15.0 for runtime {0}", runtime.Id));
		}

		ProcessAsyncOperation StartRestoreProcess (string msbuildFileName, DotNetProject project, OperationConsole console)
		{
			string command = GetCommand (msbuildFileName);
			string arguments = GetArguments (msbuildFileName, project);

			return Runtime.ProcessService.StartConsoleProcess (
				command,
				arguments,
				project.BaseDirectory,
				console,
				null,
				(sender, e) => { }
			);
		}

		/// <summary>
		/// Do not get any Package Console output when using make run on Mono
		/// unless the full path to Mono is used instead of the MSBuild.dll filename.
		/// Also running the MSBuild.dll seems to cause the IDE to run another copy of 
		/// itself and pass the MSBuild path and project filename as parameters which
		/// causes the workspace item to unload triggering the warning dialog about
		/// closing a solution whilst NuGet package actions are being run.
		/// </summary>
		static string GetCommand (string msbuildFileName)
		{
			if (Platform.IsWindows)
				return msbuildFileName;

			string monoPrefix = MonoRuntimeInfo.FromCurrentRuntime ().Prefix;
			return Path.Combine (monoPrefix, "bin", "mono");
		}

		static string GetArguments (string msbuildFileName, DotNetProject project)
		{
			if (Platform.IsWindows)
				return string.Format ("/t:restore \"{0}\"", project.FileName);

			return string.Format ("\"{0}\" /t:restore \"{1}\"", msbuildFileName, project.FileName);
		}

		void CheckForRestoreFailure (ProcessAsyncOperation operation)
		{
			if (operation.Task.IsFaulted || operation.ExitCode != 0) {
				throw new ApplicationException (GettextCatalog.GetString ("Unable to restore packages."));
			}
		}

		void RefreshProjectReferences (DotNetProject project)
		{
			Runtime.RunInMainThread (() => {
				project.ReloadProjectBuilder ();
				project.NotifyModified ("References");
			});
		}
	}
}
