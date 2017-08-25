//
// MSBuildSearchPathTests.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2017 Xamarin, Inc (http://www.xamarin.com)
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
using NUnit.Framework;
using UnitTests;
using System.IO;
using System.Linq;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildSearchPathTests: TestBase
	{
		public void RegisterSearchPath ()
		{
			string extPath = Util.GetSampleProjectPath ("msbuild-search-paths", "extensions-path");
			MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildExtensionsPath", extPath);
		}

		public void UnregisterSearchPath ()
		{
			string extPath = Util.GetSampleProjectPath ("msbuild-search-paths", "extensions-path");
			MonoDevelop.Projects.MSBuild.MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildExtensionsPath", extPath);
		}

		[Test]
		public async Task CustomTarget ()
		{
			try {
				RegisterSearchPath ();
				string projectFile = Util.GetSampleProject ("msbuild-search-paths", "ConsoleProject.csproj");
				DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
				Assert.AreEqual ("Works!", p.MSBuildProject.EvaluatedProperties.GetValue ("TestTarget"));
				p.Dispose ();
			} finally {
				UnregisterSearchPath ();
			}
		}

		[Test]
		public async Task InjectTarget ()
		{
			try {
				RegisterSearchPath ();
				string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

				Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
				var project = (Project)sol.Items [0];
				var res = await project.RunTarget (Util.GetMonitor (false), "TestInjected", project.Configurations [0].Selector);
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);
				sol.Dispose ();
			} finally {
				UnregisterSearchPath ();
			}
		}

		[Test]
		public async Task InjectTargetAfterLoadingProject ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var project = (Project)sol.Items [0];
			var res = await project.RunTarget (Util.GetMonitor (false), "TestInjected", project.Configurations [0].Selector);
			Assert.AreEqual (0, res.BuildResult.WarningCount);
			Assert.AreEqual (1, res.BuildResult.ErrorCount);

			try {
				RegisterSearchPath ();
				res = await project.RunTarget (Util.GetMonitor (false), "TestInjected", project.Configurations [0].Selector);
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);
			} finally {
				UnregisterSearchPath ();
			}

			res = await project.RunTarget (Util.GetMonitor (false), "TestInjected", project.Configurations [0].Selector);
			Assert.AreEqual (0, res.BuildResult.WarningCount);
			Assert.AreEqual (1, res.BuildResult.ErrorCount);
			sol.Dispose ();
		}

		[Test]
		public async Task ProjectUsingSdk ()
		{
			string sdkPath = Util.GetSampleProjectPath ("msbuild-search-paths", "sdk-path");
			try {
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath);
				
				string projectFile = Util.GetSampleProject ("msbuild-search-paths", "ProjectUsingSdk.csproj");
				DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
				Assert.AreEqual ("Works!", p.MSBuildProject.EvaluatedProperties.GetValue ("SdkProp"));

				var res = await p.RunTarget (Util.GetMonitor (false), "SdkTarget", p.Configurations [0].Selector);
				Assert.AreEqual (0, res.BuildResult.ErrorCount, res.BuildResult.Errors.FirstOrDefault ()?.ToString ());
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);
				p.Dispose ();
			} finally {
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath);
			}
		}

		[Test]
		public async Task MultipleProjectsUsingSdk ()
		{
			string sdkPath1 = Util.GetSampleProjectPath ("msbuild-search-paths", "sdk-path");
			string sdkPath2 = Util.GetSampleProjectPath ("msbuild-search-paths", "sdk-path-2");
			try {
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath1);
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath2);

				// Load and run the first project

				string projectFile = Util.GetSampleProject ("msbuild-search-paths", "ProjectUsingSdk.csproj");
				DotNetProject p1 = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
				Assert.AreEqual ("Works!", p1.MSBuildProject.EvaluatedProperties.GetValue ("SdkProp"));

				var res = await p1.RunTarget (Util.GetMonitor (false), "SdkTarget", p1.Configurations [0].Selector);
				Assert.AreEqual (0, res.BuildResult.ErrorCount, res.BuildResult.Errors.FirstOrDefault ()?.ToString ());
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);

				// Load and run the second project

				projectFile = Util.GetSampleProject ("msbuild-search-paths", "ProjectUsingSdk2.csproj");
				DotNetProject p2 = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
				Assert.AreEqual ("Works!", p2.MSBuildProject.EvaluatedProperties.GetValue ("BarProp"));

				res = await p2.RunTarget (Util.GetMonitor (false), "BarTarget", p2.Configurations [0].Selector);
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);

				// Try building again the first project

				res = await p1.RunTarget (Util.GetMonitor (false), "SdkTarget", p1.Configurations [0].Selector);
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);

				p1.Dispose ();
				p2.Dispose ();

			} finally {
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath1);
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath2);
			}
		}

		[Test]
		public async Task ProjectUsingMultipleSdk ()
		{
			// A project that references two SDKs must be assigned an SDKs folder that contains both SDKs

			string sdkPath1 = Util.GetSampleProjectPath ("msbuild-search-paths", "sdk-path");
			string sdkPath2 = Util.GetSampleProjectPath ("msbuild-search-paths", "sdk-path-2");
			string sdkPath3 = Util.GetSampleProjectPath ("msbuild-search-paths", "sdk-path-all");

			try {
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath1);
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath2);
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath3);

				string projectFile = Util.GetSampleProject ("msbuild-search-paths", "ProjectUsingMultiSdk.csproj");
				DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;

				var res = await p.RunTarget (Util.GetMonitor (false), "SdkTarget", p.Configurations [0].Selector);
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);
				Assert.AreEqual ("Works!", p.MSBuildProject.EvaluatedProperties.GetValue ("SdkProp"));

				res = await p.RunTarget (Util.GetMonitor (false), "BarTarget", p.Configurations [0].Selector);
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);
				Assert.AreEqual ("Works!", p.MSBuildProject.EvaluatedProperties.GetValue ("BarProp"));
				p.Dispose ();

			} finally {
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath1);
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath2);
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath3);
			}
		}

		[Test]
		public async Task ProjectUsingSdkImport ()
		{
			string sdkPath = Util.GetSampleProjectPath ("msbuild-search-paths", "sdk-path-3");
			try {
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath);

				string projectFile = Util.GetSampleProject ("msbuild-search-paths", "ProjectUsingSdkImport.csproj");
				DotNetProject p = await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile) as DotNetProject;
				Assert.AreEqual ("value1", p.MSBuildProject.EvaluatedProperties.GetValue ("SdkProp1"));
				Assert.AreEqual ("value2", p.MSBuildProject.EvaluatedProperties.GetValue ("SdkProp2"));

				var res = await p.RunTarget (Util.GetMonitor (false), "SdkTarget", p.Configurations [0].Selector);
				Assert.AreEqual (1, res.BuildResult.WarningCount);
				Assert.AreEqual ("Works!", res.BuildResult.Errors [0].ErrorText);
				p.Dispose ();
			} finally {
				MonoDevelop.Projects.MSBuild.MSBuildProjectService.UnregisterProjectImportSearchPath ("MSBuildSDKsPath", sdkPath);
			}
		}
	}
}
