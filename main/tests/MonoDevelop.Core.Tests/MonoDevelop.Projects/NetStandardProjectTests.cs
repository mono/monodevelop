//
// NetStandardProjectTests.cs
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
	public class NetStandardProjectTests: TestBase
	{
		Solution sol;

		[TearDown]
		public override void TearDown ()
		{
			sol?.Dispose ();
			sol = null;

			base.TearDown ();
		}

		[Test]
		public async Task NetStandardProjectReferenceIncludesFacades ()
		{
			// Test for https://bugzilla.xamarin.com/show_bug.cgi?id=55734

			string solFile = Util.GetSampleProject ("netstandard-project", "NetStandardTest.sln");
			sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);

			var p = (DotNetProject)sol.Items [0];
			var asms = await p.GetReferencedAssemblies (p.Configurations [0].Selector);
			Assert.IsTrue (asms.Any (r => r.FilePath.FileName == "System.Runtime.dll"));
		}
	}
}
