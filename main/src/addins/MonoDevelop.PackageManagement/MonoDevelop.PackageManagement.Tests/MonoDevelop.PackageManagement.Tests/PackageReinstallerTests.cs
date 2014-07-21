//
// PackageReinstallerTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;
using NuGet;
using MonoDevelop.PackageManagement.NodeBuilders;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageReinstallerTests
	{
		PackageReinstaller reinstaller;
		FakePackageManagementSolution solution;
		FakeBackgroundPackageActionRunner backgroundRunner;
		FakePackageManagementProject project;

		void CreateReinstaller ()
		{
			solution = new FakePackageManagementSolution ();
			project = solution.FakeActiveProject;
			backgroundRunner = new FakeBackgroundPackageActionRunner ();

			reinstaller = new PackageReinstaller (solution, backgroundRunner);
		}

		void Run (string packageId, string packageVersion)
		{
			var packageReference = new PackageReference (
				packageId,
				new SemanticVersion (packageVersion),
				null,
				null,
				false);

			var node = new PackageReferenceNode (packageReference, true);

			reinstaller.Run (node);
		}

		[Test]
		public void Run_ReinstallSucceeds_ReinstallActionCreatedFromActiveProjectAndPassedToBackgroundActionRunner ()
		{
			CreateReinstaller ();

			Run ("MyPackage", "1.2.3.4");

			Assert.AreEqual (1, project.ReinstallPackageActionsCreated.Count);
			Assert.AreEqual (project.ReinstallPackageActionsCreated [0], backgroundRunner.ActionRun);
		}

		[Test]
		public void Run_ReinstallSucceeds_ReinstallActionHasPackageIdAndVersionSet ()
		{
			CreateReinstaller ();

			Run ("MyPackage", "1.2.3.4");

			var reinstallAction = backgroundRunner.ActionRun as ReinstallPackageAction;
			Assert.AreEqual ("MyPackage", reinstallAction.PackageId);
			Assert.AreEqual (new SemanticVersion ("1.2.3.4"), reinstallAction.PackageVersion);
		}

		[Test]
		public void Run_ReinstallThrowsException_ExceptionReported ()
		{
			CreateReinstaller ();
			var exception = new Exception ("error");
			backgroundRunner.RunAction = (progressMessage, action) => {
				throw exception;
			};

			Run ("MyPackage", "1.2.3.4");

			Assert.AreEqual (exception, backgroundRunner.ShowErrorException);
		}
	}
}

