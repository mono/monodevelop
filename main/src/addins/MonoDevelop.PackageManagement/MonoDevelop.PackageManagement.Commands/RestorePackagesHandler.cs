// 
// RestorePackagesHandler.cs
// 
// Author:
//   Matt Ward <ward.matt@gmail.com>
// 
// Copyright (C) 2013 Matthew Ward
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using ICSharpCode.PackageManagement;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.PackageManagement.Commands
{
	public class RestorePackagesHandler : PackagesCommandHandler
	{
		IPackageManagementSolution solution;
		IPackageManagementProgressMonitorFactory progressMonitorFactory;
		
		public RestorePackagesHandler()
			: this(
				PackageManagementServices.Solution,
				PackageManagementServices.ProgressMonitorFactory)
		{
		}
		
		public RestorePackagesHandler(
			IPackageManagementSolution solution,
			IPackageManagementProgressMonitorFactory progressMonitorFactory)
		{
			this.solution = solution;
			this.progressMonitorFactory = progressMonitorFactory;
		}
		
		protected override void Run ()
		{
			IProgressMonitor progressMonitor = CreateProgressMonitor ();
			
			try {
				RestorePackages(progressMonitor);
			} catch (Exception ex) {
				progressMonitor.Log.WriteLine(ex.Message);
				progressMonitor.Dispose();
			}
		}

		IProgressMonitor CreateProgressMonitor ()
		{
			return progressMonitorFactory.CreateProgressMonitor (GettextCatalog.GetString ("Restoring packages..."));
		}
		
		void RestorePackages(IProgressMonitor progressMonitor)
		{
			var commandLine = new NuGetPackageRestoreCommandLine(solution);
			
			progressMonitor.Log.WriteLine(commandLine.ToString());
			
			RestorePackages(progressMonitor, commandLine);
		}
		
		void RestorePackages(IProgressMonitor progressMonitor, NuGetPackageRestoreCommandLine commandLine)
		{
			var aggregatedMonitor = (AggregatedProgressMonitor)progressMonitor;

			Runtime.ProcessService.StartConsoleProcess(
				commandLine.Command,
				commandLine.Arguments,
				commandLine.WorkingDirectory,
				aggregatedMonitor.MasterMonitor as IConsole,
				(e, sender) => progressMonitor.Dispose()
			);
		}
	}
}
