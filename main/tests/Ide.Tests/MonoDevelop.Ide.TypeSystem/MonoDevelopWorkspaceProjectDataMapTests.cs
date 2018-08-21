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
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace MonoDevelop.Ide.TypeSystem
{
	[TestFixture]
	public class MonoDevelopWorkspaceProjectDataMapTests : IdeTestBase
	{
		[Test]
		public void TestSimpleCreation ()
		{
			using (var project = Services.ProjectService.CreateDotNetProject ("C#"))
			using (var workspace = new MonoDevelopWorkspace (null)) {
				var map = new MonoDevelopWorkspace.ProjectDataMap (workspace);

				var pid = map.GetId (project);
				Assert.IsNull (pid);

				pid = map.GetOrCreateId (project, null);
				Assert.IsNotNull (pid);

				var projectInMap = map.GetMonoProject (pid);
				Assert.AreSame (project, projectInMap);

				map.RemoveProject (project, pid);
				Assert.IsNull (map.GetId (project));
			}
		}

		[Test]
		public void TestDataHandling ()
		{
			using (var project = Services.ProjectService.CreateDotNetProject ("C#"))
			using (var workspace = new MonoDevelopWorkspace (null)) {
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
		public void TestMigration ()
		{
			using (var project = Services.ProjectService.CreateDotNetProject ("C#"))
			using (var project2 = Services.ProjectService.CreateDotNetProject ("C#"))
			using (var workspace = new MonoDevelopWorkspace (null)) {
				var map = new MonoDevelopWorkspace.ProjectDataMap (workspace);

				var pid = map.GetOrCreateId (project, null);
				Assert.IsNotNull (pid);

				var pid2 = map.GetOrCreateId (project2, project);
				Assert.AreSame (pid, pid2);
			}
		}
	}
}
