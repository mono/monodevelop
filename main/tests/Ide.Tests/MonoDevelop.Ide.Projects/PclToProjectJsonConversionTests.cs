//
// PclToProjectJsonConversionTests.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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

using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;
using MonoDevelop.Ide.Projects.OptionPanels;

namespace MonoDevelop.Ide.Projects
{
	[TestFixture]
	public class PclToProjectJsonConversionTests : TestBase
	{
		/// <summary>
		/// Tests that the project.json file is added to the project and the Xamarin.Forms
		/// MSBuild import is removed.
		/// </summary>
		[Test]
		public async Task MigrateXamarinFormsPclProjectToProjectJson ()
		{
			FilePath solFile = Util.GetSampleProject ("XamarinFormsPcl", "XamarinFormsPcl.sln");

			using (var sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile)) {
				var p = (DotNetProject)sol.Items [0];
				string expectedProjectXml = File.ReadAllText (p.FileName.ChangeName ("XamarinFormsPcl-migrated"));

				var projectJsonFile = PortableRuntimeOptionsPanelWidget.MigrateToProjectJson (p);
				await p.SaveAsync (Util.GetMonitor ());

				string projectXml = File.ReadAllText (p.FileName);
				Assert.AreEqual (BuildAction.None, projectJsonFile.BuildAction);
				Assert.AreEqual (p.BaseDirectory.Combine ("project.json"), projectJsonFile.FilePath);
				Assert.AreEqual (expectedProjectXml, projectXml);
			}
		}
	}
}
