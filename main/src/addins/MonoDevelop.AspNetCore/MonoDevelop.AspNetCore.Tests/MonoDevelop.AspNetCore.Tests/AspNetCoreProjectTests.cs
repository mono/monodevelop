//
// AspNetCoreProjectTests.cs
//
// Author:
//       Rodrigo Moya <rodrigo.moya@xamarin.com>
//
// Copyright (c) 2019 
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

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using UnitTests;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Projects.FileNesting;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.AspNetCore.Tests
{
	[TestFixture]
	class AspNetCoreProjectTests : TestBase
	{
		Solution solution;

		[TearDown]
		public override void TearDown ()
		{
			solution?.Dispose ();
			solution = null;

			base.TearDown ();
		}

		[Test]
		public async Task RazorClassLib_Load_LoadsProject ()
		{
			// Just test, for now, that we can load a Razor Class Lib project
			string projectFileName = Util.GetSampleProject ("aspnetcore-razor-class-lib", "aspnetcore-razor-class-lib.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName)) {
				Assert.NotNull (project.Items.Single (item => item.Include == "Areas\\MyFeature\\Pages\\Page1.cshtml.cs"));
				Assert.NotNull (project.Items.Single (item => item.Include == "Areas\\MyFeature\\Pages\\Page1.cshtml"));
			}
		}

		static ProjectFile AddFile (Project project, string file)
		{
			File.WriteAllText (file, "");
			return project.AddFile (file);
		}

		[Test]
		public async Task AspNetCore_FileNesting ()
		{
			string projectFileName = Util.GetSampleProject ("aspnetcore-empty-30", "aspnetcore-empty-30.sln");
			using (var sol = (Solution) await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), projectFileName)) {
				var files = new List<(string, string)> ();
				files.Add (("Index.cshtml.cs", "Index.cshtml"));
				files.Add (("file.html.css", "file.html"));
				files.Add (("bootstrap.css.map", "bootstrap.css"));
				files.Add (("jquery.js", "jquery.ts"));
				files.Add (("site-vsdoc.js", "site.js"));
				files.Add (("jquery.min.js", "jquery.js"));
				files.Add (("template.cs", "template.tt"));
				files.Add (("template.doc", "template.tt"));
				files.Add ((".bowerrc", "bower.json"));
				files.Add (("package-lock.json", "package.json"));

				var project = sol.GetAllProjectsWithFlavor<AspNetCoreProjectExtension> ().FirstOrDefault ();

				var dir = project.BaseDirectory;
				project.AddDirectory ("FileNesting");

				foreach (var f in files) {
					// Create files
					string inputFileDestination = Path.Combine (dir, "FileNesting", f.Item1);
					string parentFileDestination = Path.Combine (dir, "FileNesting", f.Item2);

					var parentFile = AddFile (project, parentFileDestination);
					var inputFile = AddFile (project, inputFileDestination);

					var actualParentFile = FileNestingService.GetParentFile (inputFile);
					Assert.That (parentFile, Is.EqualTo (actualParentFile), $"Was expecting parent file {parentFileDestination} for {inputFileDestination} but got {actualParentFile}");

					// Disable file nesting on the solution
					sol.UserProperties.SetValue ("MonoDevelop.Ide.FileNesting.Enabled", false);
					Assert.False (FileNestingService.IsEnabledForProject (project));
					Assert.Null (FileNestingService.GetParentFile (inputFile));

					// Re-enable file nesting on the solution
					sol.UserProperties.SetValue ("MonoDevelop.Ide.FileNesting.Enabled", true);
					Assert.True (FileNestingService.IsEnabledForProject (project));

					// Test removing files
					project.Files.Remove (inputFile);
					Assert.That (FileNestingService.GetChildren (parentFile).Count, Is.EqualTo (0));
					project.Files.Remove (parentFile);
				}
			}
		}

		[Test]
		public async Task AspNetCore_MultiFileNesting ()
		{
			string projectFileName = Util.GetSampleProject ("aspnetcore-empty-30", "aspnetcore-empty-30.sln");
			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), projectFileName)) {
				var project = sol.GetAllProjectsWithFlavor<AspNetCoreProjectExtension> ().FirstOrDefault ();

				var dir = project.BaseDirectory;
				project.AddDirectory ("FileNesting");

				var rootFile = AddFile (project, Path.Combine (dir, "FileNesting", "bootstrap.css"));
				Assert.That (FileNestingService.GetChildren (rootFile), Is.Null);
				Assert.That (FileNestingService.GetDependentOrNestedChildren (rootFile)?.Count () ?? 0, Is.EqualTo (0));
				Assert.That (FileNestingService.GetDependentOrNestedTree (rootFile)?.Count () ?? 0, Is.EqualTo (0));

				// This should be nested under bootstrap.css
				var mapFile = AddFile (project, Path.Combine (dir, "FileNesting", "bootstrap.css.map"));
				Assert.That (FileNestingService.GetChildren (rootFile), Contains.Item (mapFile));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (rootFile), Contains.Item (mapFile));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (rootFile)?.Count () ?? 0, Is.EqualTo (1));
				Assert.That (FileNestingService.GetDependentOrNestedTree (rootFile), Contains.Item (mapFile));
				Assert.That (FileNestingService.GetDependentOrNestedTree (rootFile)?.Count () ?? 0, Is.EqualTo (1));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (mapFile)?.Count () ?? 0, Is.EqualTo (0));
				Assert.That (FileNestingService.GetDependentOrNestedTree (mapFile)?.Count () ?? 0, Is.EqualTo (0));

				// This should be nested under bootstrap.css
				var minFile = AddFile (project, Path.Combine (dir, "FileNesting", "bootstrap.min.css"));
				Assert.That (FileNestingService.GetChildren (rootFile), Contains.Item (minFile));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (rootFile), Contains.Item (minFile));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (rootFile)?.Count () ?? 0, Is.EqualTo (2));
				Assert.That (FileNestingService.GetDependentOrNestedTree (rootFile), Contains.Item (minFile));
				Assert.That (FileNestingService.GetDependentOrNestedTree (rootFile)?.Count () ?? 0, Is.EqualTo (2));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (minFile)?.Count () ?? 0, Is.EqualTo (0));
				Assert.That (FileNestingService.GetDependentOrNestedTree (minFile)?.Count () ?? 0, Is.EqualTo (0));

				// This should be nested under bootstrap.min.css
				var minMapFile = AddFile (project, Path.Combine (dir, "FileNesting", "bootstrap.min.css.map"));
				Assert.That (FileNestingService.GetChildren (rootFile), !Contains.Item (minMapFile));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (rootFile), !Contains.Item (minMapFile));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (rootFile)?.Count () ?? 0, Is.EqualTo (2));
				Assert.That (FileNestingService.GetDependentOrNestedTree (rootFile), Contains.Item (minMapFile));
				Assert.That (FileNestingService.GetDependentOrNestedTree (rootFile)?.Count () ?? 0, Is.EqualTo (3));
				Assert.That (FileNestingService.GetChildren (minFile), Contains.Item (minMapFile));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (minFile), Contains.Item (minMapFile));
				Assert.That (FileNestingService.GetDependentOrNestedChildren (minFile)?.Count () ?? 0, Is.EqualTo (1));
				Assert.That (FileNestingService.GetDependentOrNestedTree (minFile), Contains.Item (minMapFile));
				Assert.That (FileNestingService.GetDependentOrNestedTree (minFile)?.Count () ?? 0, Is.EqualTo (1));

				// Check if all files are taken into account when renaming the top one
				var files = MonoDevelop.Ide.ProjectOperations.GetDependentFilesToRename (rootFile, "reboot.css");
				Assert.That (files.Count, Is.EqualTo (3));
				Assert.True (files.Any (x => x.File == mapFile && x.NewName == "reboot.css.map"));
				Assert.True (files.Any (x => x.File == minFile && x.NewName == "reboot.min.css"));
				Assert.True (files.Any (x => x.File == minMapFile && x.NewName == "reboot.min.css.map"));
			}
		}

		[Test]
		public async Task RazorClassLib_Supports_FileNesting ()
		{
			string projectFileName = Util.GetSampleProject ("aspnetcore-razor-class-lib", "aspnetcore-razor-class-lib.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFileName)) {
				Assert.True (FileNestingService.AppliesToProject (project));
			}
		}

		[Test]
		public async Task MultiTargetFrameworks_ExecutionTargets ()
		{
			string solutionFileName = Util.GetSampleProject ("aspnetcore-multi-target-execution", "aspnetcore-multi-target.sln");
			RestoreNuGetPackages (solutionFileName);

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFileName)) {
				var project = (DotNetProject)sol.Items[0];

				var groups = project.GetExecutionTargets (ConfigurationSelector.Default)
					.Cast<ExecutionTargetGroup> ()
					.ToList ();
				var targets = groups.SelectMany (x => x.ToArray ());

				if (Directory.Exists ("/Applications/Safari.app")) {
					var matchNetCoreApp21 = targets.FirstOrDefault (x => x.Name.Contains ("Safari • netcoreapp2.1"));
					var matchNetCoreApp30 = targets.FirstOrDefault (x => x.Name.Contains ("Safari • netcoreapp3.0"));
					Assert.IsNotNull (matchNetCoreApp21, $"Execution target did not contain Safari netcoreapp2.1");
					Assert.IsNotNull (matchNetCoreApp30, $"Execution target did not contain Safari netcoreapp3.0");
					Assert.AreEqual ("/Applications/Safari.app-netcoreapp2.1", matchNetCoreApp21.Id);
					Assert.AreEqual ("/Applications/Safari.app-netcoreapp3.0", matchNetCoreApp30.Id);
					Assert.AreEqual (Stock.Browser.ToString (), matchNetCoreApp21.Image);
					Assert.AreEqual (Stock.Browser.ToString (), matchNetCoreApp21.Image);
				} else if (Directory.Exists ("/Applications/Google Chrome.app")) {
					var matchNetCoreApp21 = targets.FirstOrDefault (x => x.Name.Contains ("Google Chrome • netcoreapp2.1"));
					var matchNetCoreApp30 = targets.FirstOrDefault (x => x.Name.Contains ("Google Chrome • netcoreapp3.0"));
					Assert.IsNotNull (matchNetCoreApp21, $"Execution target did not contain Chrome netcoreapp2.1");
					Assert.IsNotNull (matchNetCoreApp30, $"Execution target did not contain Chrome netcoreapp3.0");
					Assert.AreEqual ("/Applications/Google Chrome.app-netcoreapp2.1", matchNetCoreApp21.Id);
					Assert.AreEqual ("/Applications/Google Chrome.app-netcoreapp3.0", matchNetCoreApp30.Id);
					Assert.AreEqual (Stock.Browser.ToString (), matchNetCoreApp21.Image);
					Assert.AreEqual (Stock.Browser.ToString (), matchNetCoreApp21.Image);
				} else {
					Assert.Ignore ("No browsers found to run test");
				}
			}
		}

		static void RestoreNuGetPackages (FilePath solutionFileName)
		{
			CreateNuGetConfigFile (solutionFileName.ParentDirectory);

			var process = Process.Start ("msbuild", $"/t:Restore /p:RestoreDisableParallel=true \"{solutionFileName}\"");
			Assert.IsTrue (process.WaitForExit (120000), "Timeout restoring NuGet packages.");
			Assert.AreEqual (0, process.ExitCode);
		}

		static void CreateNuGetConfigFile (FilePath directory)
		{
			var fileName = directory.Combine ("NuGet.Config");

			string xml =
				"<configuration>\r\n" +
				"  <packageSources>\r\n" +
				"    <clear />\r\n" +
				"    <add key=\"NuGet v3 Official\" value=\"https://api.nuget.org/v3/index.json\" />\r\n" +
				"  </packageSources>\r\n" +
				"</configuration>";

			File.WriteAllText (fileName, xml);
		}
	}
}
