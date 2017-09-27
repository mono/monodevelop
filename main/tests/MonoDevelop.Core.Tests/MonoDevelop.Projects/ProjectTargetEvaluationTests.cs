//
// ProjectTargetEvaluationTests.cs
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

using NUnit.Framework;
using UnitTests;
using MonoDevelop.Core;
using System.Linq;
using System.Threading.Tasks;

namespace MonoDevelop.Projects
{
	[TestFixture]
	public class ProjectTargetEvaluationTests: TestBase
	{
		[Test]
		public async Task EvaluateUnknownPropertyDuringBuild ()
		{
			string solFile = Util.GetSampleProject("console-project", "ConsoleProject.sln");

			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem(Util.GetMonitor(), solFile);

			var project = ((Project)sol.Items[0]);

			var context = new TargetEvaluationContext();
			context.PropertiesToEvaluate.Add("TestUnknownPropertyToEvaluate");

			var res = await project.RunTarget(Util.GetMonitor(), "Build", project.Configurations[0].Selector, context);
			Assert.IsNotNull(res);
			Assert.IsNotNull(res.BuildResult);
			Assert.AreEqual(0, res.BuildResult.ErrorCount);
			Assert.AreEqual(0, res.BuildResult.WarningCount);
			Assert.IsNull(res.Properties.GetValue("TestUnknownPropertyToEvaluate"));

			sol.Dispose();
		}


		[Test]
		public async Task RunTarget ()
		{
			string projFile = Util.GetSampleProject ("msbuild-tests", "project-with-custom-target.csproj");
			var p = (Project)await Services.ProjectService.ReadSolutionItem (Util.GetMonitor (), projFile);

			var ctx = new TargetEvaluationContext ();
			ctx.GlobalProperties.SetValue ("TestProp", "has");
			ctx.PropertiesToEvaluate.Add ("GenProp");
			ctx.PropertiesToEvaluate.Add ("AssemblyName");
			ctx.ItemsToEvaluate.Add ("GenItem");
			var res = await p.RunTarget (Util.GetMonitor (), "Test", p.Configurations [0].Selector, ctx);

			Assert.AreEqual (1, res.BuildResult.Errors.Count);
			Assert.AreEqual ("Something failed: has foo bar", res.BuildResult.Errors [0].ErrorText);

			// Verify that properties are returned

			Assert.AreEqual ("ConsoleProject", res.Properties.GetValue ("AssemblyName"));
			Assert.AreEqual ("foo", res.Properties.GetValue ("GenProp"));

			// Verify that items are returned

			var items = res.Items.ToArray ();
			Assert.AreEqual (1, items.Length);
			Assert.AreEqual ("bar", items [0].Include);
			Assert.AreEqual ("Hello", items [0].Metadata.GetValue ("MyMetadata"));
			if (Runtime.Preferences.BuildWithMSBuild.Value) {
				// Standard metadata inclusion is only supported via 4.0 API builder
				Assert.AreEqual ("bar", items [0].Metadata.GetValue ("Filename"));
				Assert.AreEqual (p.ItemDirectory.Combine ("bar").ToString (), items [0].Metadata.GetValue ("FullPath"));
			}

			p.Dispose ();
		}

		[Test]
		public async Task TargetEvaluationResultTryGetPathValueForNullPropertyValue ()
		{
			string solFile = Util.GetSampleProject ("console-project", "ConsoleProject.sln");
			Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile);
			var p = (Project)sol.Items [0];

			var ctx = new TargetEvaluationContext ();
			ctx.PropertiesToEvaluate.Add ("MissingProperty");
			var res = await p.RunTarget (Util.GetMonitor (), "Build", p.Configurations [0].Selector, ctx);

			Assert.IsNull (res.Properties.GetValue ("MissingProperty"));

			FilePath path = null;
			bool foundProperty = res.Properties.TryGetPathValue ("MissingProperty", out path);
			Assert.IsFalse (foundProperty);

			p.Dispose ();
		}
	}

	[TestFixture]
	public class ProjectTargetEvaluationTests_XBuild : ProjectTargetEvaluationTests
	{
		[TestFixtureSetUp]
		public void SetUp ()
		{
			Runtime.Preferences.BuildWithMSBuild.Set (false);
		}

		[TestFixtureTearDown]
		public void Teardown ()
		{
			Runtime.Preferences.BuildWithMSBuild.Set (true);
		}
	}
}
