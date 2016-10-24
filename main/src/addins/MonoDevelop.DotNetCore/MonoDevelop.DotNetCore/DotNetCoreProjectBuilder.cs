//
// DotNetCoreProjectBuilder.cs
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace MonoDevelop.DotNetCore
{
	class DotNetCoreProjectBuilder : IDisposable
	{
		DotNetProject project;
		ProgressMonitor monitor;

		public DotNetCoreProjectBuilder (DotNetProject project, ProgressMonitor monitor)
		{
			this.project = project;
			this.monitor = monitor;
		}

		public void Dispose ()
		{
		}

		public Task<BuildResult> BuildAsnc (DotNetProjectConfiguration config, string target)
		{
			var path = new DotNetCorePath ();
			var console = new DotNetCoreOperationConsole (monitor);

			ProcessAsyncOperation operation = Runtime.ProcessService.StartConsoleProcess (
				path.FileName,
				string.Format ("{0} --configuration {1}", target, config.Name),
				project.BaseDirectory,
				console,
				null,
				(sender, e) => { }
			);
			return operation.Task.ContinueWith (t => {
				return CreateBuildResult (operation, console);
			});
		}

		DotNetCoreOperationConsole GetConsole ()
		{
			return new DotNetCoreOperationConsole (monitor);
		}

		BuildResult CreateBuildResult (ProcessAsyncOperation operation, DotNetCoreOperationConsole console)
		{
			if (operation.Task.IsFaulted || operation.ExitCode != 0) {
				BuildResult result = console.GetBuildResult (project);
				if (!(result.HasErrors || result.HasWarnings)) {
					result.AddError (GettextCatalog.GetString ("Build failed. Please see the Build Output for more details."));
				}
				return result;
			}

			return console.GetBuildResult (project);
		}
	}
}
