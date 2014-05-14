//
// ReducedPackageOperationsTests.cs
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
using System.Linq;
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;
using MonoDevelop.PackageManagement.Tests.Helpers;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class ReducedPackageOperationsTests
	{
		ReducedPackageOperations reducedPackageOperations;
		FakePackageOperationResolver fakePackageOperationResolver;
		List<IPackage> packages;

		void CreateReducedPackageOperations ()
		{
			packages = new List<IPackage> ();
			fakePackageOperationResolver = new FakePackageOperationResolver ();
			reducedPackageOperations = new ReducedPackageOperations (fakePackageOperationResolver, packages);
		}

		IPackage AddPackage (string id, string version)
		{
			IPackage package = CreatePackage (id, version);
			packages.Add (package);

			return package;
		}

		IPackage CreatePackage (string id, string version)
		{
			return new TestPackageHelper (id, version).Package;
		}

		PackageOperation AddInstallOperationForPackage (IPackage package)
		{
			var operation = new PackageOperation (package, PackageAction.Install);
			AddInstallOperationsForPackage (package, operation);
			return operation;
		}

		void AddInstallOperationsForPackage (IPackage package, params PackageOperation[] operations)
		{
			fakePackageOperationResolver.AddOperations (package, operations);
		}

		PackageOperation CreatePackageOperation (string id, string version, PackageAction action)
		{
			IPackage package = CreatePackage (id, version);
			return new PackageOperation (package, action);
		}

		void AssertReducedOperationsContains (PackageOperation operation)
		{
			Assert.IsTrue (reducedPackageOperations.Operations.ToList ().Contains (operation));
		}

		[Test]
		public void Reduce_OnePackage_ReturnsPackageOperationsFromResolverForPackage ()
		{
			CreateReducedPackageOperations ();
			IPackage package = AddPackage ("Test", "1.0");
			PackageOperation operation = AddInstallOperationForPackage (package);

			reducedPackageOperations.Reduce ();

			Assert.AreEqual (1, reducedPackageOperations.Operations.Count ());
			Assert.AreEqual (operation, reducedPackageOperations.Operations.First ());
		}

		[Test]
		public void Reduce_TwoPackages_ReturnsPackageOperationsForBothPackages ()
		{
			CreateReducedPackageOperations ();
			IPackage package1 = AddPackage ("Test", "1.0");
			IPackage package2 = AddPackage ("Test2", "1.0");
			PackageOperation operation1 = AddInstallOperationForPackage (package1);
			PackageOperation operation2 = AddInstallOperationForPackage (package2);

			reducedPackageOperations.Reduce ();

			Assert.AreEqual (2, reducedPackageOperations.Operations.Count ());
			AssertReducedOperationsContains (operation1);
			AssertReducedOperationsContains (operation2);
		}

		[Test]
		public void Reduce_OncePackageOperationInstallsPackageWhilstOneUninstallsSamePackage_PackageOperationNotIncludedInReducedSet ()
		{
			CreateReducedPackageOperations ();
			IPackage package = AddPackage ("Test", "1.0");
			PackageOperation installOperation = CreatePackageOperation ("Foo", "1.0", PackageAction.Install);
			PackageOperation uninstallOperation = CreatePackageOperation ("Foo", "1.0", PackageAction.Uninstall);
			AddInstallOperationsForPackage (package, installOperation, uninstallOperation);

			reducedPackageOperations.Reduce ();

			Assert.AreEqual (0, reducedPackageOperations.Operations.Count ());
		}

		[Test]
		public void Reduce_OnePackageOperationMatchesPackageBeingInstalled_ReturnsOnlyOnePackageInstallOperationForThisPackage ()
		{
			CreateReducedPackageOperations ();
			IPackage package1 = AddPackage ("Test", "1.0");
			IPackage package2 = AddPackage ("Test2", "1.0");
			PackageOperation operation1a = CreatePackageOperation ("Test", "1.0", PackageAction.Install);
			PackageOperation operation1b = CreatePackageOperation ("Test2", "1.0", PackageAction.Install);
			PackageOperation operation2 = CreatePackageOperation ("Test2", "1.0", PackageAction.Install);
			AddInstallOperationsForPackage (package1, operation1a, operation1b);
			AddInstallOperationsForPackage (package2, operation2);

			reducedPackageOperations.Reduce ();

			reducedPackageOperations
				.Operations
				.SingleOrDefault (o => o.Package.Id == "Test2");
			Assert.AreEqual (2, reducedPackageOperations.Operations.Count ());
		}

		[Test]
		public void Reduce_OnePackageOperationMatchesPackageBeingInstalledOnlyById_MatchingPackageOperationByIdIncludedInSet ()
		{
			CreateReducedPackageOperations ();
			IPackage package1 = AddPackage ("Test", "1.0");
			IPackage package2 = AddPackage ("Test2", "1.0");
			PackageOperation operation1a = CreatePackageOperation ("Test", "1.0", PackageAction.Install);
			PackageOperation operation1b = CreatePackageOperation ("Test2", "1.1", PackageAction.Install);
			PackageOperation operation2 = CreatePackageOperation ("Test2", "1.0", PackageAction.Install);
			AddInstallOperationsForPackage (package1, operation1a, operation1b);
			AddInstallOperationsForPackage (package2, operation2);

			reducedPackageOperations.Reduce ();

			Assert.AreEqual (3, reducedPackageOperations.Operations.Count ());
		}

		[Test]
		public void Reduce_OnePackageOperationMatchesPackageBeingInstalledByIdAndVersionButOneIsInstallAndOneIsUninstall_BothOperationsNotIncludedInSet ()
		{
			CreateReducedPackageOperations ();
			IPackage package1 = AddPackage ("Test", "1.0");
			IPackage package2 = AddPackage ("Test2", "1.0");
			PackageOperation operation1a = CreatePackageOperation ("Test", "1.0", PackageAction.Install);
			PackageOperation operation1b = CreatePackageOperation ("Test2", "1.0", PackageAction.Uninstall);
			PackageOperation operation2 = CreatePackageOperation ("Test2", "1.0", PackageAction.Install);
			AddInstallOperationsForPackage (package1, operation1a, operation1b);
			AddInstallOperationsForPackage (package2, operation2);

			reducedPackageOperations.Reduce ();

			reducedPackageOperations
				.Operations
				.SingleOrDefault (o => o.Package.Id == "Test");
			Assert.AreEqual (1, reducedPackageOperations.Operations.Count ());
		}
	}
}

