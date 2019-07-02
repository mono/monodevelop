//
// OutputOptionsPanelTests.cs
//
// Author:
//       josemiguel <jostor@microsoft.com>
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Projects.OptionPanels
{
	public class OutputOptionsPanelTests
	{
		[Test]
		public async Task When_OutputDir_Is_Modified_Then_It_Should_Hand_AppendTargetFrameworkToOutputPath_Accordingly ()
		{
			FilePath projFile = Util.GetSampleProject ("dotnetcore-console", "dotnetcore-console", "dotnetcore-sdk-console.csproj");

			using (var p = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile)) {
				var debug = (DotNetProjectConfiguration)p.Configurations [0]; //Debug
				var release = (DotNetProjectConfiguration)p.Configurations [1]; //Release

				//gets the dir template for both configs
				var configs = new ItemConfiguration [] { debug, release };
				var outDirTemplate = configs.CompareTemplates ();

				Assert.That (outDirTemplate, Is.Not.Empty);

				//modify it
				outDirTemplate = outDirTemplate.Replace ("bin", "build");

				// parses configs according to AppendTargetFrameworkToOutputPath
				debug.OutputDirectory = debug.ParseOutDirectoryTemplate (outDirTemplate);
				Assert.That (debug.OutputDirectory.FullPath.ToString (), Is.StringEnding (debug.TargetFrameworkShortName));

				outDirTemplate = outDirTemplate.Replace ("$(TargetFramework)", string.Empty);
				release.OutputDirectory = release.ResolveOutDirectoryTemplate (outDirTemplate);
				Assert.That (release.OutputDirectory.FullPath.ToString ().TrimEnd (System.IO.Path.DirectorySeparatorChar), Is.StringEnding (release.Name));
				Assert.False (release.AppendTargetFrameworkToOutputPath);
			}
		}

		[TestCase ("/output/$(Configuration)/$(TargetFramework)", "foo", "/output/foo")]
		[TestCase ("/output/$(Configuration)", "foo", "/output/foo")]
		public void ResolveOutDirectoryTemplateTest (string template, string id, string expected)
		{
			var dotnetConfig = new DotNetProjectConfiguration (id) {
				AppendTargetFrameworkToOutputPath = true,
				TargetFrameworkShortName = "netcore22"
			};

			var result = dotnetConfig.ResolveOutDirectoryTemplate (template);

			Assert.That (result, Is.EqualTo (expected));
		}

		[Test]
		public void ParseOutDirectoryTemplateTest ()
		{
			string expectedOutput = System.IO.Path.Combine ("Users", "ProjectFoo", "Foo", "netcore22");
			var conf = new DotNetProjectConfiguration ("Foo") {
				TargetFrameworkShortName = "netcore22"
			};

			string outputTemplate = System.IO.Path.Combine ("Users", "ProjectFoo", "$(Configuration)", "$(TargetFramework)");
			var parsed = conf.ParseOutDirectoryTemplate (outputTemplate);

			Assert.That (parsed, Is.EqualTo (expectedOutput));
		}

		[Test]
		public void GetTemplateTest ()
		{
			string expectedTemplate = System.IO.Path.Combine ("Users", "ProjectFoo", "$(Configuration)", "$(TargetFramework)");
			var conf = new DotNetProjectConfiguration ("Foo") {
				TargetFrameworkShortName = "netcore22",
				OutputDirectory = System.IO.Path.Combine ("Users", "ProjectFoo", "Foo", "netcore22"),
				AppendTargetFrameworkToOutputPath = true
			};

			var template = conf.GetTemplate ();

			Assert.That (template, Is.EqualTo (expectedTemplate));
		}

		[Test]
		public void GetAssemblyNameTest ()
		{
			var debug = new DotNetProjectConfiguration ("Debug");
			var release = new DotNetProjectConfiguration ("Release");

			debug.OutputAssembly = "assembly_debug";
			release.OutputAssembly = "assembly_release";
			Assert.That (new ItemConfiguration [] { debug, release }.GetAssemblyName (), Is.Empty);

			debug.OutputAssembly = release.OutputAssembly = "assembly";
			Assert.That (new ItemConfiguration [] { debug, release }.GetAssemblyName (), Is.EqualTo ("assembly"));
		}

		[Test]
		public void CompareTemplatesTest ()
		{
			var debug = new DotNetProjectConfiguration ("Debug");
			var release = new DotNetProjectConfiguration ("Release");

			debug.OutputDirectory = new FilePath ("/Users/project/bin/Debug");
			release.OutputDirectory = new FilePath ("/Users/project/bin/Release");
			Assert.That (new ItemConfiguration [] { debug, release }.CompareTemplates (), Is.EqualTo ("/Users/project/bin/$(Configuration)"));

			debug.OutputDirectory = new FilePath ("/Users/project/build/Debug");
			release.OutputDirectory = new FilePath ("/Users/project/bin/Release");
			Assert.That (new ItemConfiguration [] { debug, release }.CompareTemplates (), Is.Empty);
		}
	}
}
