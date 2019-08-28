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

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using MonoDevelop.Projects;
using UnitTests;
using MonoDevelop.Ide.Projects.FileNesting;

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

				var project = sol.GetAllProjectsWithFlavor<AspNetCoreProjectExtension> ().FirstOrDefault ();

				var dir = project.BaseDirectory;
				project.AddDirectory ("FileNesting");

				foreach (var f in files) {
					// Create files
					string inputFileDestination = Path.Combine (dir, "FileNesting", f.Item1);
					string parentFileDestination = Path.Combine (dir, "FileNesting", f.Item2);

					File.WriteAllText (parentFileDestination, "");
					var parentFile = project.AddFile (parentFileDestination);
					File.WriteAllText (inputFileDestination, "");
					var inputFile = project.AddFile (inputFileDestination);

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
	}
}
