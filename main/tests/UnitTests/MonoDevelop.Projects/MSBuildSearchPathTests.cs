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

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class MSBuildSearchPathTests: TestBase
	{
		[Test]
		public async Task InjectTarget ()
		{
			string extPath = Util.GetSampleProjectPath ("msbuild-search-paths", "extensions-path");
			MonoDevelop.Projects.MSBuild.MSBuildProjectService.RegisterProjectImportSearchPath ("MSBuildExtensionsPath", extPath + Path.DirectorySeparatorChar);

			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var project = (Project)sol.Items [0];
			var res = await project.RunTarget (Util.GetMonitor (false), "TestInjected", project.Configurations[0].Selector);
			Assert.AreEqual (1, res.BuildResult.WarningCount);
			Assert.AreEqual ("Works!", res.BuildResult.Errors[0].ErrorText);
		}
	}
}
