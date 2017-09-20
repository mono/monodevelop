//
// GenericProjectTests.cs
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

using System.Xml;
using NUnit.Framework;
using UnitTests;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class GenericProjectTests: TestBase
	{
		[Test]
		public void CreateGenericProject ()
		{
			var info = new ProjectCreateInformation ();
			info.ProjectName = "Some.Test";
			info.ProjectBasePath = "/tmp/test";
			var doc = new XmlDocument ();
			var projectOptions = doc.CreateElement ("Options");
			var p = (GenericProject)Services.ProjectService.CreateProject ("GenericProject", info, projectOptions);
			Assert.AreEqual ("Default", p.Configurations [0].Name);
			Assert.AreEqual (MSBuildSupport.NotSupported, p.MSBuildEngineSupport);
			p.Dispose ();
		}

		[Test]
		public async Task LoadGenericProject ()
		{
			string solFile = Util.GetSampleProject ("generic-project", "generic-project.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.FindProjectByName ("GenericProject");

			Assert.IsInstanceOf<GenericProject> (p);

			var pl = (GenericProject)p;
			Assert.AreEqual ("Default", pl.Configurations [0].Name);
			sol.Dispose ();
		}

		[Test]
		public async Task LoadGenericProjectWithImportBeforePropertyGroup ()
		{
			string solFile = Util.GetSampleProject ("generic-project-with-import", "generic-project.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = sol.Items [0];

			Assert.IsInstanceOf<GenericProject> (p);

			var pl = (GenericProject)p;
			Assert.AreEqual ("Default", pl.Configurations [0].Name);
		}
	}
}
