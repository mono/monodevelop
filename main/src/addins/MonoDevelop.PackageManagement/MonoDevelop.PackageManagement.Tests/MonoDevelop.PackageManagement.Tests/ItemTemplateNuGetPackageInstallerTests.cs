//
// ItemTemplateNuGetPackageInstallerTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MonoDevelop.Core;
using MonoDevelop.Ide.Templates;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ItemTemplateNuGetPackageInstallerTests : TestBase
	{
		ItemTemplateNuGetPackageInstaller installer;
		FakeBackgroundPackageActionRunner runner;
		DummyDotNetProject dotNetProject;
		List<InstallNuGetPackageAction> actionsRun;
		InstallNuGetPackageAction installActionRun;
		ProgressMonitorStatusMessage statusMessage;

		void CreateInstaller ()
		{
			dotNetProject = CreateDotNetProject ();

			actionsRun = new List<InstallNuGetPackageAction> ();
			runner = new FakeBackgroundPackageActionRunner ();
			runner.RunActions = OnBackgroundActionRunActions;

			installer = new ItemTemplateNuGetPackageInstaller (runner);
		}

		void OnBackgroundActionRunActions (ProgressMonitorStatusMessage message, IEnumerable<IPackageAction> actions, bool clearConsole)
		{
			statusMessage = message;
			actionsRun = actions.OfType<InstallNuGetPackageAction> ().ToList ();
			installActionRun = actions.FirstOrDefault () as InstallNuGetPackageAction;
		}

		static DummyDotNetProject CreateDotNetProject (string projectName = "MyProject")
		{
			FilePath directory = Util.CreateTmpDir (projectName);
			var fileName = directory.Combine (projectName + ".csproj");

			var project = new DummyDotNetProject ();
			project.Name = projectName;
			project.FileName = fileName;

			var solution = new Solution ();
			solution.FileName = fileName.ChangeExtension (".sln");
			solution.RootFolder.AddItem (project);

			return project;
		}

		Task InstallOneNuGetPackage (string packageId, string packageVersion)
		{
			var packageReference = new TemplatePackageReference (packageId, packageVersion);

			return installer.Run (dotNetProject, new [] { packageReference });
		}

		Task InstallTwoNuGetPackages (string packageId1, string packageVersion1, string packageId2, string packageVersion2)
		{
			var packages = new [] {
				new TemplatePackageReference (packageId1, packageVersion1),
				new TemplatePackageReference (packageId2, packageVersion2)
			};

			return installer.Run (dotNetProject, packages);
		}

		void AddPackageToProject (string packageId, string packageVersion)
		{
			var packageReference = ProjectPackageReference.Create (packageId, packageVersion);
			dotNetProject.Items.Add (packageReference);
		}

		[TearDown]
		public override void TearDown ()
		{
			base.TearDown ();

			statusMessage = null;
			installActionRun = null;
		}

		[Test]
		public async Task InstallOneNuGetPackage ()
		{
			CreateInstaller ();

			await InstallOneNuGetPackage ("Test", "1.2");

			Assert.AreEqual ("Test", installActionRun.PackageId);
			Assert.AreEqual ("1.2", installActionRun.Version.ToString ());
			Assert.AreEqual ("Adding Test...", statusMessage.Status);
		}

		[Test]
		public async Task InstallTwoNuGetPackages ()
		{
			CreateInstaller ();

			await InstallTwoNuGetPackages ("TestA", "1.2", "TestB", "2.3");

			Assert.AreEqual ("TestA", actionsRun [0].PackageId);
			Assert.AreEqual ("1.2", actionsRun [0].Version.ToString ());
			Assert.AreEqual ("TestB", actionsRun [1].PackageId);
			Assert.AreEqual ("2.3", actionsRun [1].Version.ToString ());
			Assert.AreEqual ("Adding 2 packages...", statusMessage.Status);
		}

		[Test]
		public async Task NoPackages_NoBackgroundActionStarted ()
		{
			CreateInstaller ();

			await installer.Run (dotNetProject, new TemplatePackageReference [0]);

			Assert.IsNull (statusMessage);
		}

		[Test]
		public async Task InstallOneNuGetPackage_PackageAlreadyInstalled_PackageIsNotInstalled ()
		{
			CreateInstaller ();
			AddPackageToProject ("Test", "1.2");

			await InstallOneNuGetPackage ("Test", "1.2");

			Assert.AreEqual (0, actionsRun.Count);
			Assert.IsNull (installActionRun);
		}

		[Test]
		public async Task InstallOneNuGetPackage_PackageAlreadyInstalledWithDifferentCase_PackageIsNotInstalled ()
		{
			CreateInstaller ();
			AddPackageToProject ("TEST", "1.2");

			await InstallOneNuGetPackage ("Test", "1.2");

			Assert.AreEqual (0, actionsRun.Count);
			Assert.IsNull (installActionRun);
		}

		[Test]
		public async Task InstallOneNuGetPackage_OlderPackageInstalled_PackageIsInstalled ()
		{
			CreateInstaller ();
			AddPackageToProject ("Test", "1.1");

			await InstallOneNuGetPackage ("Test", "1.2");

			Assert.AreEqual (1, actionsRun.Count);
			Assert.AreEqual ("Test", installActionRun.PackageId);
			Assert.AreEqual ("1.2", installActionRun.Version.ToString ());
		}

		[Test]
		public async Task InstallOnePrereleaseNuGetPackage_OlderPrereleasePackageInstalled_PackageIsInstalled ()
		{
			CreateInstaller ();
			AddPackageToProject ("Test", "1.1-beta1");

			await InstallOneNuGetPackage ("Test", "1.1-beta2");

			Assert.AreEqual (1, actionsRun.Count);
			Assert.AreEqual ("Test", installActionRun.PackageId);
			Assert.AreEqual ("1.1-beta2", installActionRun.Version.ToString ());
		}

		[Test]
		public async Task InstallOneNuGetPackage_PrereleasePackageInstalled_PackageIsInstalled ()
		{
			CreateInstaller ();
			AddPackageToProject ("Test", "1.1-beta1");

			await InstallOneNuGetPackage ("Test", "1.1");

			Assert.AreEqual (1, actionsRun.Count);
			Assert.AreEqual ("Test", installActionRun.PackageId);
			Assert.AreEqual ("1.1", installActionRun.Version.ToString ());
		}
	}
}
