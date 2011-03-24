// 
// MonoMacPackagingTool.cs
//  
// Author:
//       David Siegel <djsiegel@gmail.com>
// 
// Copyright (c) 2010 David Siegel
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
using System.IO;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.MacDev;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;

using MonoDevelop.MonoMac.Gui;

namespace MonoDevelop.MonoMac
{
	public class MonoMacPackagingTool : IApplication
	{
		const string Name = "mac-bundle";

		public int Run (string[] arguments)
		{
			var options = new MonoMacPackagingToolOptions (arguments);
			
			switch (options.Parse ()) {
			case MonoMacPackagingToolOptions.ParseResult.Failure:
				Console.WriteLine ("{0}: {1}", Name, options.ParseFailureMessage);
				Console.WriteLine ("Try `{0} --help' for more information.", Name);
				return 1;
			case MonoMacPackagingToolOptions.ParseResult.Success:
				break;
			}
			
			if (options.ShowHelp) {
				Console.WriteLine ("Usage: {0} [options]", Name);
				Console.WriteLine ("Builds an application bundle from the MonoMac project under the current directory.");
				Console.WriteLine ();
				options.Show ();
				return 0;
			}
			
			var monitor = new ConsoleProgressMonitor ();
			var project = MaybeFindMonoMacProject (monitor);
			if (project == null) {
				Console.WriteLine ("Error: No MonoMac project found.");
				return 1;
			}
			
			var config = project.Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == options.Configuration);
			if (config == null) {
				Console.WriteLine ("Error: Could not find configuration: {0}", options.Configuration);
				return 1;
			}
			
			MonoMacPackaging.BuildPackage (monitor, project, config.Selector, options.PackagingSettings, project.Name + ".app");			
			return 0;
		}

		MonoMacProject MaybeFindMonoMacProject (IProgressMonitor monitor)
		{
			var projects =
				from solutionFile in Directory.GetFiles (".", "*.sln")
				let solution = Services.ProjectService.ReadWorkspaceItem (monitor, solutionFile) 
				from project in solution.GetAllProjects ().OfType<MonoMacProject> ()
				select project;
			
			return projects.FirstOrDefault ();
		}
	}
}
