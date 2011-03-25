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
		
		IProgressMonitor Monitor;
		MonoMacPackagingToolOptions Options;
		
		public int Run (string[] arguments)
		{
			Monitor = new ConsoleProgressMonitor ();
			Options = new MonoMacPackagingToolOptions (arguments);
			
			switch (Options.Parse ()) {
			case MonoMacPackagingToolOptions.ParseResult.Failure:
				Console.WriteLine ("{0}: {1}", Name, Options.ParseFailureMessage);
				Console.WriteLine ("Try `{0} --help' for more information.", Name);
				return 1;
			case MonoMacPackagingToolOptions.ParseResult.Success:
				break;
			}
			
			if (Options.ShowHelp) {
				ShowUsage ();
				return 0;
			}
			
			var project = MaybeFindMonoMacProject ();
			if (project == null) {
				Console.WriteLine ("Error: Could not find MonoMac project.");
				return 1;
			}
			
			var config = project.Configurations.FirstOrDefault<ItemConfiguration> (c => c.Name == Options.Configuration);
			if (config == null) {
				Console.WriteLine ("Error: Could not find configuration: {0}", Options.Configuration);
				return 1;
			}
			
			MonoMacPackaging.BuildPackage (Monitor, project, config.Selector, Options.PackagingSettings, GetTarget (project));			
			return 0;
		}
		
		void ShowUsage ()
		{
			Console.WriteLine ("Usage: {0} [Options] [DEST]", Name);
			Console.WriteLine ("Builds an application bundle from the MonoMac project under the current directory.");
			Console.WriteLine ();
			Options.Show ();
		}
		
		string GetTarget (MonoMacProject project)
		{
			var extension = Options.PackagingSettings.CreatePackage ? ".pkg" : ".app";
			var outputFile = project.Name + extension;
			
			// If tool was passed a path, we make it the target destination.
			if (Options.Files.Any ()) {
				var path = Options.Files.First ();
				if (Directory.Exists (path) && !path.EndsWith (extension)) {
					// We were passed a destination directory (and it's not really a bundle).
					return Path.Combine (path, outputFile);
				} else {
					// We were passed a destination filepath, or something erroneous. Either way,
					// we continue and report any problem with the target during the build phase.
					return path;
				}
			}
				
			return outputFile;
		}

		MonoMacProject MaybeFindMonoMacProject ()
		{
			if (Options.Project != null) {
				return Services.ProjectService.ReadSolutionItem (Monitor, Options.Project) as MonoMacProject;
			}
			
			var projects =
				from solutionFile in Directory.GetFiles (".", "*.sln")
				let solution = Services.ProjectService.ReadWorkspaceItem (Monitor, solutionFile) 
				from project in solution.GetAllProjects ().OfType<MonoMacProject> ()
				select project;
			
			return projects.FirstOrDefault ();
		}
	}
}
