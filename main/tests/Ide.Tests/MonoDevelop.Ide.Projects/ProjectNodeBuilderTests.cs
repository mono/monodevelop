//
// ProjectNodeBuilderTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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

using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads.ProjectPad;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.Projects
{
	[TestFixture]
	[RequireService (typeof (RootWorkspace))]
	class ProjectNodeBuilderTests : IdeTestBase
	{
		[Test]
		public async Task ConditionalFiles ()
		{
			FilePath solutionFile = Util.GetSampleProject ("ConditionalFiles", "ConditionalFiles.sln");

			using (var solution = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solutionFile)) {
				var p = solution.GetAllProjects ().OfType<DotNetProject> ().Single ();

				// Debug configuration.
				IdeApp.Workspace.ActiveConfigurationId = "Debug";

				var treeBuilder = new TestTreeBuilder ();
				treeBuilder.ParentDataItem [typeof (Project)] = p;

				var nodeBuilder = new ProjectNodeBuilder ();
				nodeBuilder.BuildChildNodes (treeBuilder, p);

				var debugFiles = treeBuilder.ChildNodes.OfType<ProjectFile> ().ToList ();
				var debugFileNames = debugFiles.Select (f => f.FilePath.FileName).ToList ();
				Assert.That (debugFileNames, Has.Member ("MyClass.cs"));
				Assert.That (debugFileNames, Has.Member ("MyClass-Debug.cs"));
				Assert.That (debugFileNames, Has.No.Member ("MyClass-Release.cs"));
				Assert.AreEqual (2, debugFiles.Count);

				// Release configuration.
				IdeApp.Workspace.ActiveConfigurationId = "Release";

				treeBuilder.ChildNodes.Clear ();
				nodeBuilder.BuildChildNodes (treeBuilder, p);
				var releaseFiles = treeBuilder.ChildNodes.OfType<ProjectFile> ().ToList ();
				var releaseFileNames = releaseFiles.Select (f => f.FilePath.FileName).ToList ();
				Assert.That (releaseFileNames, Has.Member ("MyClass.cs"));
				Assert.That (releaseFileNames, Has.Member ("MyClass-Release.cs"));
				Assert.That (releaseFileNames, Has.No.Member ("MyClass-Debug.cs"));
				Assert.AreEqual (2, releaseFiles.Count);
			}
		}
	}

}
