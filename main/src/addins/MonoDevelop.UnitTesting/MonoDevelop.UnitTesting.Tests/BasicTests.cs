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
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.DotNetCore;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.UnitTesting.Tests
{
	[TestFixture()]
	public class BasicTests : TestBase
	{
		Solution sol;

		[TestFixtureSetUp]
		public void Start()
		{
			DesktopService.Initialize();
			IdeApp.Initialize(new ProgressMonitor());
			IdeApp.Workspace.ActiveConfigurationId = "Debug";
		}

		[TearDown]
		public override void TearDown ()
		{
			sol?.Dispose ();
			sol = null;

			base.TearDown ();
		}

		/// <summary>
		/// Clear all other package sources and just use the main NuGet package source when
		/// restoring the packages for the project temlate tests.
		/// </summary>
		void CreateNuGetConfigFile (FilePath directory)
		{
			var fileName = directory.Combine ("NuGet.Config");

			string xml =
				"<configuration>\r\n" +
				"  <packageSources>\r\n" +
				"    <clear />\r\n" +
				"    <add key=\"NuGet v3 Official\" value=\"https://api.nuget.org/v3/index.json\" />\r\n" +
				"  </packageSources>\r\n" +
				"</configuration>";

			File.WriteAllText (fileName, xml);
		}

		[Test()]
		public async Task TestsXUnitDotNetFull()
		{
			await CommonTestDiscovery("unit-testing-xunit-dotnetfull");
		}

		[Test()]
		public async Task TestsXUnitDotNetCore()
		{
			if (!DotNetCoreRuntime.IsInstalled) {
				Assert.Ignore (".NET Core needs to be installed.");
			}

			await CommonTestDiscovery("unit-testing-xunit-dotnetcore");
		}

		async Task CommonTestDiscovery(string projectName)
		{
			FilePath solFile = Util.GetSampleProject("unit-testing-addin", "unit-testing-addin.sln");

			CreateNuGetConfigFile (solFile.ParentDirectory);

			var process = Process.Start("nuget", $"restore -DisableParallelProcessing {solFile}");
			Assert.IsTrue(process.WaitForExit(60000), "Timeout restoring nuget packages.");
			Assert.AreEqual(0, process.ExitCode);

			sol = await Services.ProjectService.ReadWorkspaceItem(Util.GetMonitor(), solFile) as Solution;
			Assert.AreEqual(0, (await sol.Build(Util.GetMonitor(), "Debug")).ErrorCount);
			var project1 = sol.GetAllProjects().Single(p => p.Name == projectName);
			var rootUnitTest1 = UnitTestService.BuildTest(project1) as UnitTestGroup;
			var token = new CancellationTokenSource(TimeSpan.FromSeconds(5)).Token;
			rootUnitTest1.Refresh(token).Ignore();
			var readyTaskSource = new TaskCompletionSource<bool>();
			rootUnitTest1.TestStatusChanged += delegate
			{
				if (rootUnitTest1.Status == TestStatus.Ready)
					readyTaskSource.TrySetResult(true);
			};
			token.Register(() => readyTaskSource.TrySetCanceled());
			if (rootUnitTest1.Status != TestStatus.Ready)
				await readyTaskSource.Task;
			Assert.AreEqual(TestStatus.Ready, rootUnitTest1.Status);
			Assert.AreEqual(2, rootUnitTest1.CountTestCases());
		}
	}
}
