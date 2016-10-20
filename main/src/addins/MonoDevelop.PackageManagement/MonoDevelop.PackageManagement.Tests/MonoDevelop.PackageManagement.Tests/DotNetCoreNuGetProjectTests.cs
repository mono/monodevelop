//
// DotNetCoreNuGetProjectTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.PackageManagement.Tests.Helpers;
using MonoDevelop.Projects;
using NUnit.Framework;
using NuGet.PackageManagement;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class DotNetCoreNuGetProjectTests
	{
		DotNetProject dotNetProject;
		TestableDotNetCoreNuGetProject project;
		FakeNuGetProjectContext context;

		void CreateNuGetProject (string projectName = "MyProject")
		{
			context = new FakeNuGetProjectContext ();
			dotNetProject = new DummyDotNetProject ();
			dotNetProject.Name = projectName;
			project = new TestableDotNetCoreNuGetProject (dotNetProject);
		}

		void AddDotNetProjectPackageReference (string packageId, string version)
		{
			var packageIdentity = new PackageIdentity (packageId, NuGetVersion.Parse (version));
			var packageReference = ProjectPackageReference.Create (packageIdentity);

			dotNetProject.Items.Add (packageReference);
		}

		Task<bool> UninstallPackageAsync (string packageId, string version)
		{
			var packageIdentity = new PackageIdentity (packageId, NuGetVersion.Parse (version));
			return project.UninstallPackageAsync (packageIdentity, context, CancellationToken.None);
		}

		Task<bool> InstallPackageAsync (string packageId, string version)
		{
			var packageIdentity = new PackageIdentity (packageId, NuGetVersion.Parse (version));
			return project.InstallPackageAsync (packageIdentity, null, context, CancellationToken.None);
		}

		[Test]
		public async Task GetInstalledPackagesAsync_OnePackageReference_ReturnsOnePackageReference ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			var packageReferences = await project.GetInstalledPackagesAsync (CancellationToken.None);

			var packageReference = packageReferences.Single ();
			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("2.6.1", packageReference.PackageIdentity.Version.ToNormalizedString ());
		}

		[Test]
		public async Task PreviewInstallPackageAsync_OldPackageInstalled_UninstallActionForOldPackageAdded ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");
			var actions = new List<NuGetProjectAction> ();
			actions.Add (new FakeNuGetProjectAction ("NUnit", "3.5.0", NuGetProjectActionType.Install));

			var updatedActions = (await project.PreviewInstallPackageAsync (actions)).ToList ();

			var lastAction = updatedActions.LastOrDefault ();
			Assert.AreEqual (actions[0], updatedActions[0]);
			Assert.AreEqual (2, updatedActions.Count);
			Assert.AreEqual ("NUnit", lastAction.PackageIdentity.Id);
			Assert.AreEqual ("2.6.1", lastAction.PackageIdentity.Version.ToNormalizedString ());
			Assert.AreEqual (NuGetProjectActionType.Uninstall, lastAction.NuGetProjectActionType);
		}

		[Test]
		public async Task PreviewInstallPackageAsync_NoExistingPackagesInstalled_UninstallActionForOldPackageIsNotAdded ()
		{
			CreateNuGetProject ();
			var actions = new List<NuGetProjectAction> ();
			actions.Add (new FakeNuGetProjectAction ("NUnit", "3.5.0", NuGetProjectActionType.Install));

			var updatedActions = (await project.PreviewInstallPackageAsync (actions)).ToList ();

			Assert.AreEqual (actions, updatedActions);
		}

		[Test]
		public async Task PreviewInstallPackageAsync_OldPackageInstalledAndUninstallActionAlreadyExistsForOldPackage_UninstallActionForOldPackageIsNotAdded ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");
			var actions = new List<NuGetProjectAction> ();
			actions.Add (new FakeNuGetProjectAction ("NUnit", "3.5.0", NuGetProjectActionType.Install));
			actions.Add (new FakeNuGetProjectAction ("NUnit", "2.6.1", NuGetProjectActionType.Uninstall));

			var updatedActions = (await project.PreviewInstallPackageAsync (actions)).ToList ();

			Assert.AreEqual (actions, updatedActions);
		}

		[Test]
		public async Task PreviewInstallPackageAsync_OldPackageInstalledMatchesNewPackageBeingInstalled_UninstallActionForOldPackageIsNotAdded ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");
			var actions = new List<NuGetProjectAction> ();
			actions.Add (new FakeNuGetProjectAction ("NUnit", "2.6.1", NuGetProjectActionType.Uninstall));

			var updatedActions = (await project.PreviewInstallPackageAsync (actions)).ToList ();

			Assert.AreEqual (actions, updatedActions);
		}

		[Test]
		public async Task UninstallPackageAsync_OldPackageInstalled_PackageReferenceRemoved ()
		{
			CreateNuGetProject ();
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			bool result = await UninstallPackageAsync ("NUnit", "2.6.1");

			Assert.IsFalse (dotNetProject.Items.OfType<ProjectPackageReference> ().Any ());
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task UninstallPackageAsync_OldPackageIsNotInstalled_PackageReferenceRemoved ()
		{
			CreateNuGetProject ();
			dotNetProject.Name = "MyProject";

			bool result = await UninstallPackageAsync ("NUnit", "2.6.1");

			Assert.IsFalse (result);
			Assert.IsFalse (project.IsSaved);
			Assert.AreEqual (MessageLevel.Warning, context.LastLogLevel);
			Assert.AreEqual ("Package 'NUnit.2.6.1' does not exist in project 'MyProject'", context.LastMessageLogged);
		}

		[Test]
		public async Task InstallPackageAsync_PackageNotInstalled_PackageReferenceAdded ()
		{
			CreateNuGetProject ();

			bool result = await InstallPackageAsync ("NUnit", "2.6.1");

			var packageReference = dotNetProject.Items.OfType<ProjectPackageReference> ()
				.Single ()
				.CreatePackageReference ();

			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("2.6.1", packageReference.PackageIdentity.Version.ToNormalizedString ());
			Assert.IsTrue (result);
			Assert.IsTrue (project.IsSaved);
		}

		[Test]
		public async Task InstallPackageAsync_PackageAlreadyInstalled_PackageReferenceNotAdded ()
		{
			CreateNuGetProject ("MyProject");
			AddDotNetProjectPackageReference ("NUnit", "2.6.1");

			bool result = await InstallPackageAsync ("NUnit", "2.6.1");

			var packageReference = dotNetProject.Items.OfType<ProjectPackageReference> ()
				.Single ()
				.CreatePackageReference ();

			Assert.AreEqual ("NUnit", packageReference.PackageIdentity.Id);
			Assert.AreEqual ("2.6.1", packageReference.PackageIdentity.Version.ToNormalizedString ());
			Assert.IsFalse (result);
			Assert.IsFalse (project.IsSaved);
			Assert.AreEqual ("Package 'NUnit.2.6.1' already exists in project 'MyProject'", context.LastMessageLogged);
			Assert.AreEqual (MessageLevel.Warning, context.LastLogLevel);
		}
	}
}
