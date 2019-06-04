//
// MonoDevelopWorkspaceProjectDataMapTests.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft Inc.
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
using System.Collections.Immutable;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class MonoDevelopWorkspaceProjectDataMapTests : IdeTestBase
	{
		[Test]
		public async Task TestSimpleCreation ()
		{
			using (var project = Services.ProjectService.CreateDotNetProject ("C#"))
			using (var workspace = await IdeApp.TypeSystemService.CreateEmptyWorkspace ()) {
				var map = new MonoDevelopWorkspace.ProjectDataMap (workspace);

				var pid = map.GetId (project);
				Assert.IsNull (pid);

				pid = map.GetOrCreateId (project, null);
				Assert.IsNotNull (pid);

				var projectInMap = map.GetMonoProject (pid);
				Assert.AreSame (project, projectInMap);

				var projectRemovedFromMap = map.RemoveProject (pid);
				Assert.AreSame (projectInMap, projectRemovedFromMap);

				Assert.IsNull (map.GetId (project));

				pid = map.GetOrCreateId (project, null);
				map.RemoveProject (project);

				Assert.IsNull (map.GetId (project));
			}
		}

		[Test]
		public async Task TestSimpleCreation_MultiTargetFramework ()
		{
			var projectFile = Util.GetSampleProject ("multi-target", "multi-target.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile))
			using (var workspace = await IdeApp.TypeSystemService.CreateEmptyWorkspace ()) {
				var map = new MonoDevelopWorkspace.ProjectDataMap (workspace);

				var pid = map.GetId (project);
				Assert.IsNull (pid);

				pid = map.GetOrCreateId (project, null, "netcoreapp1.1");
				Assert.IsNotNull (pid);

				var pid2 = map.GetOrCreateId (project, null, "netstandard1.0");
				Assert.IsNotNull (pid2);

				Assert.AreNotEqual (pid, pid2);

				var projectInMap = map.GetMonoProject (pid);
				Assert.AreSame (project, projectInMap);

				projectInMap = map.GetMonoProject (pid2);
				Assert.AreSame (project, projectInMap);

				var foundPid = map.GetId (project, "netcoreapp1.1");
				Assert.AreSame (pid, foundPid);

				foundPid = map.GetId (project, "netstandard1.0");
				Assert.AreSame (pid2, foundPid);

				var projectRemovedFromMap = map.RemoveProject (pid);
				Assert.AreSame (projectInMap, projectRemovedFromMap);

				projectRemovedFromMap = map.RemoveProject (pid2);
				Assert.AreSame (projectInMap, projectRemovedFromMap);

				Assert.IsNull (map.GetId (project, "netcoreapp1.1"));
				Assert.IsNull (map.GetId (project, "netstandard1.0"));

				pid = map.GetOrCreateId (project, null, "netcoreapp1.1");
				map.RemoveProject (project);

				Assert.IsNull (map.GetId (project, "netcoreapp1.1"));

				pid = map.GetOrCreateId (project, null, "netstandard1.0");
				map.RemoveProject (project);

				Assert.IsNull (map.GetId (project, "netstandard1.0"));
			}
		}

		[Test]
		public async Task TestDataHandling ()
		{
			using (var project = Services.ProjectService.CreateDotNetProject ("C#"))
			using (var workspace = await IdeApp.TypeSystemService.CreateEmptyWorkspace ()) {
				var map = new MonoDevelopWorkspace.ProjectDataMap (workspace);

				var pid = map.GetOrCreateId (project, null);
				var data = map.GetData (pid);

				Assert.IsNull (data);
				Assert.IsFalse (map.Contains (pid));

				data = map.CreateData (pid, ImmutableArray<MonoDevelopMetadataReference>.Empty);

				Assert.IsNotNull (data);
				Assert.IsTrue (map.Contains (pid));

				map.RemoveData (pid);

				data = map.GetData (pid);

				Assert.IsNull (data);
				Assert.IsFalse (map.Contains (pid));
			}
		}

		[Test]
		public async Task TestDataHandling_MultiTargetFramework ()
		{
			var projectFile = Util.GetSampleProject ("multi-target", "multi-target.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile))
			using (var workspace = await IdeApp.TypeSystemService.CreateEmptyWorkspace ()) {
				var map = new MonoDevelopWorkspace.ProjectDataMap (workspace);

				var pid = map.GetOrCreateId (project, null, "netcoreapp1.1");
				var data = map.GetData (pid);

				Assert.IsNull (data);
				Assert.IsFalse (map.Contains (pid));

				var pid2 = map.GetOrCreateId (project, null, "netstandard1.0");
				var data2 = map.GetData (pid2);

				Assert.IsNull (data2);
				Assert.IsFalse (map.Contains (pid2));

				data = map.CreateData (pid, ImmutableArray<MonoDevelopMetadataReference>.Empty);

				Assert.IsNotNull (data);
				Assert.IsTrue (map.Contains (pid));

				data2 = map.CreateData (pid2, ImmutableArray<MonoDevelopMetadataReference>.Empty);

				Assert.IsNotNull (data2);
				Assert.IsTrue (map.Contains (pid2));

				map.RemoveData (pid);

				data = map.GetData (pid);

				Assert.IsNull (data);
				Assert.IsFalse (map.Contains (pid));
				Assert.IsTrue (map.Contains (pid2));

				map.RemoveData (pid2);

				data2 = map.GetData (pid2);

				Assert.IsNull (data2);
				Assert.IsFalse (map.Contains (pid2));
			}
		}

		[Test]
		public async Task TestMigration ()
		{
			using (var project = Services.ProjectService.CreateDotNetProject ("C#"))
			using (var project2 = Services.ProjectService.CreateDotNetProject ("C#"))
			using (var workspace = await IdeApp.TypeSystemService.CreateEmptyWorkspace ()) {
				var map = new MonoDevelopWorkspace.ProjectDataMap (workspace);

				var pid = map.GetOrCreateId (project, null);
				Assert.IsNotNull (pid);

				var pid2 = map.GetOrCreateId (project2, project);
				Assert.AreSame (pid, pid2);
			}
		}

		[Test]
		public async Task TestMigration_MultiTargetFramework ()
		{
			var projectFile = Util.GetSampleProject ("multi-target", "multi-target.csproj");
			using (var project = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile))
			using (var project2 = (DotNetProject)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projectFile))
			using (var workspace = await IdeApp.TypeSystemService.CreateEmptyWorkspace ()) {
				var map = new MonoDevelopWorkspace.ProjectDataMap (workspace);

				var pid = map.GetOrCreateId (project, null, "netcoreapp1.1");
				Assert.IsNotNull (pid);

				var pid2 = map.GetOrCreateId (project, null, "netstandard1.0");
				Assert.IsNotNull (pid2);

				var pid3 = map.GetOrCreateId (project2, project, "netcoreapp1.1");
				Assert.AreSame (pid, pid3);

				var pid4 = map.GetOrCreateId (project2, project, "netstandard1.0");
				Assert.AreSame (pid2, pid4);

				Assert.AreNotEqual (pid, pid2);
			}
		}
	}
}
