//
// TestableProjectManager.cs
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
using System.Collections.Generic;
using ICSharpCode.PackageManagement;
using NuGet;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class TestableProjectManager : SharpDevelopProjectManager
	{
		public IPackage PackagePassedToAddPackageReference;
		public bool IgnoreDependenciesPassedToAddPackageReference;
		public bool AllowPrereleaseVersionsPassedToAddPackageReference;

		public IPackage PackagePassedToRemovePackageReference;
		public bool ForcePassedToRemovePackageReference;
		public bool RemoveDependenciesPassedToRemovePackageReference;

		public IPackage PackagePassedToUpdatePackageReference;
		public bool UpdateDependenciesPassedToUpdatePackageReference;
		public bool AllowPrereleaseVersionsPassedToUpdatePackageReference;
		public List<IPackage> PackagesPassedToUpdatePackageReference = new List<IPackage> ();

		public FakePackageRepository FakeLocalRepository {
			get { return LocalRepository as FakePackageRepository; }
		}

		public TestableProjectManager ()
			: base (
				new FakePackageRepository (),
				new FakePackagePathResolver (),
				new FakeProjectSystem (),
				new FakePackageRepository ())
		{
		}

		public TestableProjectManager (PackageReferenceRepository repository)
			: base (
				new FakePackageRepository (),
				new FakePackagePathResolver (),
				new FakeProjectSystem (),
				repository)
		{
		}

		public override void AddPackageReference (string packageId, SemanticVersion version, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			var package = new FakePackage ();
			package.Id = packageId;
			package.Version = version;
			PackagePassedToAddPackageReference = package;
			IgnoreDependenciesPassedToAddPackageReference = ignoreDependencies;
			AllowPrereleaseVersionsPassedToAddPackageReference = allowPrereleaseVersions;
		}

		public override void AddPackageReference (IPackage package, bool ignoreDependencies, bool allowPrereleaseVersions)
		{
			PackagePassedToAddPackageReference = package;
			IgnoreDependenciesPassedToAddPackageReference = ignoreDependencies;
			AllowPrereleaseVersionsPassedToAddPackageReference = allowPrereleaseVersions;
		}

		public override void RemovePackageReference (IPackage package, bool force, bool removeDependencies)
		{
			PackagePassedToRemovePackageReference = package;
			ForcePassedToRemovePackageReference = force;
			RemoveDependenciesPassedToRemovePackageReference = removeDependencies;
		}

		public override void UpdatePackageReference (string packageId, SemanticVersion version, bool updateDependencies, bool allowPrereleaseVersions)
		{
			var package = new FakePackage ();
			package.Id = packageId;
			package.Version = version;

			PackagePassedToUpdatePackageReference = package;
			UpdateDependenciesPassedToUpdatePackageReference = updateDependencies;
			AllowPrereleaseVersionsPassedToUpdatePackageReference = allowPrereleaseVersions;

			PackagesPassedToUpdatePackageReference.Add (package);
		}

		public FakePackage AddFakePackageToProjectLocalRepository (string packageId, string version)
		{
			return FakeLocalRepository.AddFakePackageWithVersion (packageId, version);
		}
	}
}

