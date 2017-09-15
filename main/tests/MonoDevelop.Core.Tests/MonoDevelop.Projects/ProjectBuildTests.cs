//
// ProjectBuildTests.cs
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
using System.Xml;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Linq;
using MonoDevelop.Projects.MSBuild;
using System.Threading.Tasks;
using MonoDevelop.Core.Serialization;
using MonoDevelop.Projects.Extensions;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ProjectBuildTests : TestBase
	{
		[Test]
		public async Task UseCorrentProjectDependencyWhenBuildingReferences ()
		{
			// This solution maps the Debug solution configuration to Debug in the 'app'
			// project and to Extra in the 'lib' project. This test checks that when the
			// app project is built, the Extra dependency from 'lib' is taken.

			FilePath solFile = Util.GetSampleProject ("sln-config-mapping", "sln-config-mapping.sln");
			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var app = sol.Items.First (i => i.Name == "app");
			var lib = sol.Items.First (i => i.Name == "lib");

			await app.Build (Util.GetMonitor (false), sol.Configurations["Debug|x86"].Selector, true);

			// lib has been built using libspecial2.dll
			Assert.IsTrue (File.Exists (lib.ItemDirectory.Combine ("bin", "Extra", "libspecial2.dll")));

			// app has been built using libextra.dll
			Assert.IsTrue (File.Exists (app.ItemDirectory.Combine ("bin","Debug","libextra.dll")));

			await app.Build (Util.GetMonitor (), sol.Configurations ["Release|x86"].Selector, true);
			Assert.IsTrue (File.Exists (app.ItemDirectory.Combine ("bin", "Release", "lib.dll")));
			Assert.IsTrue (File.Exists (lib.ItemDirectory.Combine ("bin", "Release", "lib2.dll")));
		}

		[Test]
		public async Task UseCorrentProjectDependencyWhenNotBuildingReferences ()
		{
			// Same as above, but now project dependencies are not included in the build.
			// The project should still pick the right dependency

			FilePath solFile = Util.GetSampleProject ("sln-config-mapping", "sln-config-mapping.sln");
			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var lib = sol.Items.First (i => i.Name == "lib");
			var lib2 = sol.Items.First (i => i.Name == "lib2");
			var app = sol.Items.First (i => i.Name == "app");

			// Build the library in Debug and Release modes, to make sure all assemblies are generated
			// Also, the last build is Release, so that the project in memory is configured for reelase

			await lib.Build (Util.GetMonitor (false), sol.Configurations ["Debug|x86"].Selector, true);
			await lib.Build (Util.GetMonitor (false), sol.Configurations ["Release|x86"].Selector, true);

			// Build the app in Debug mode. It should still pick the Extra dependency
			await app.Build (Util.GetMonitor (false), sol.Configurations ["Debug|x86"].Selector, false);
			Assert.IsTrue (File.Exists (app.ItemDirectory.Combine ("bin", "Debug", "libextra.dll")));
		}

		[Test]
		public async Task UseCorrentProjectDependencyWhenNotBuildingReferencesOnCleanBuilder ()
		{
			// Same as above, but now project dependencies are not included in the build.
			// The project should still pick the right dependency

			FilePath solFile = Util.GetSampleProject ("sln-config-mapping", "sln-config-mapping.sln");
			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var lib = sol.Items.First (i => i.Name == "lib");
			var lib2 = sol.Items.First (i => i.Name == "lib2");
			var app = sol.Items.First (i => i.Name == "app");

			// Build the library in Debug and Release modes, to make sure all assemblies are generated
			// Also, the last build is Release, so that the project in memory is configured for reelase

			await lib.Build (Util.GetMonitor (false), sol.Configurations ["Debug|x86"].Selector, true);
			await lib.Build (Util.GetMonitor (false), sol.Configurations ["Release|x86"].Selector, true);

			await RemoteBuildEngineManager.RecycleAllBuilders ();
			Assert.AreEqual (0, RemoteBuildEngineManager.ActiveEnginesCount);

			// Build the app in Debug mode. It should still pick the Extra dependency
			await app.Build (Util.GetMonitor (false), sol.Configurations ["Debug|x86"].Selector, false);
			Assert.IsTrue (File.Exists (app.ItemDirectory.Combine ("bin", "Debug", "libextra.dll")));
		}
	}
}
