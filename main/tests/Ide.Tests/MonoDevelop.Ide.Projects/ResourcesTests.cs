//
// ResourcesTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.CustomTools;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Projects
{
	public class ResourcesTests: IdeTestBase
	{
		/// <summary>
		/// Tests that files generated from .resx files for .NET Core and .NET Standard
		/// projects use "typeof(Resources).GetTypeInfo().Assembly" instead of
		/// "typeof(Resources).Assembly". Without the GetTypeInfo the project will not
		/// compile with NET Core below version 2.0
		/// </summary>
		[Test]
		[TestCase("DotNetCoreProject")]
		[TestCase("NetStandardProject")]
		[Platform(Exclude = "Win")]
		public async Task BuildDotNetCoreProjectAfterGeneratingResources(string projectName)
		{
			FilePath solFile = Util.GetSampleProject("DotNetCoreResources", "DotNetCoreResources.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (DotNetProject)sol.Items.FirstOrDefault (item => item.Name == projectName);
				p.RequiresMicrosoftBuild = true;

				var resourceFile = p.Files.FirstOrDefault (f => f.FilePath.FileName == "Resources.resx");

				var customToolResult = new SingleFileCustomToolResult ();
				await ResXFileCodeGenerator.GenerateFile (resourceFile, customToolResult, true);
				Assert.IsTrue (customToolResult.Success);

				// Running a restore for a .NET Core project can take a long time if
				// no packages are cached. So instead we just check the generated resource file.
				//var res = await p.RunTarget (Util.GetMonitor (), "Restore", ConfigurationSelector.Default);
				//Assert.AreEqual (0, res.BuildResult.Errors.Count);

				//res = await p.RunTarget (Util.GetMonitor (), "Build", ConfigurationSelector.Default);
				//Assert.AreEqual (0, res.BuildResult.Errors.Count);

				var generatedResourceFile = resourceFile.FilePath.ChangeExtension (".Designer.cs");

				bool foundLine = false;
				foreach (string line in File.ReadAllLines (generatedResourceFile)) {
					if (line.Contains ("typeof")) {
						string lineWithoutSpaces = line.Replace (" ", "");
						foundLine = lineWithoutSpaces.EndsWith ("typeof(Resources).GetTypeInfo().Assembly);", StringComparison.Ordinal);
						break;
					}
				}
				Assert.IsTrue (foundLine);
			}
		}
	}
}
