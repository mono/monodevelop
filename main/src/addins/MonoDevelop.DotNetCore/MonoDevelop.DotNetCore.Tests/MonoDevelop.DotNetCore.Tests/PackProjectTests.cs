//
// PackProjectTests.cs
//
// Author:
//       Jason Imison <jaimison@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.DotNetCore.Commands;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.DotNetCore.Tests
{
	[TestFixture]
	public class PackProjectTests
	{
		[Test]
		public async Task Should_pack_multi_target_project ()
		{
			FilePath solFile = Util.GetSampleProject ("DotNetCoreMultiTargetFrameworkProperty", "DotNetCoreMultiTargetFrameworkProperty.sln");

			var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (DotNetProject)sol.Items [0];

			var packTarget = new PackProjectBuildTarget (p);
			var monitor = Util.GetMonitor ();
			var res = await p.RunTarget (monitor, "Restore", ConfigurationSelector.Default);
			var result = await packTarget.Build (monitor, ConfigurationSelector.Default);
			Assert.AreEqual (0, result.ErrorCount);
			var nupkg = sol.FileName.ParentDirectory.Combine ("bin", "Debug", "DotNetCoreMultiTargetFrameworkProperty.1.0.0.nupkg");
			Assert.IsTrue (File.Exists (nupkg));
		}
	}
}
	