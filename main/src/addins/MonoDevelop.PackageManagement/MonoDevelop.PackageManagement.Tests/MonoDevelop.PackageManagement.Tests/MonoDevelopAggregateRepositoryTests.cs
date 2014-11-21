//
// AggregateRepositoryTests.cs
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
using NUnit.Framework;
using NuGet;
using System.Collections.Generic;
using MonoDevelop.PackageManagement.Tests.Helpers;
using System.Linq;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class MonoDevelopAggregateRepositoryTests
	{
		MonoDevelopAggregateRepository aggregateRepository;
		List<IPackageRepository> repositories;

		[SetUp]
		public void Init ()
		{
			repositories = new List<IPackageRepository> ();
		}

		FakePackageRepository AddRepository ()
		{
			var repository = new FakePackageRepository ();
			repositories.Add (repository);
			return repository;
		}

		FakePackage AddRepositoryWithOnePackage (string packageId)
		{
			FakePackageRepository repository = AddRepository ();
			return repository.AddFakePackage (packageId);
		}

		void CreateAggregateRepository ()
		{
			aggregateRepository = new MonoDevelopAggregateRepository (repositories);
			aggregateRepository.IgnoreFailingRepositories = true;
		}

		ExceptionThrowingPackageRepository AddFailingPackageRepository ()
		{
			return AddFailingPackageRepository (new Exception ("Error"));
		}

		ExceptionThrowingPackageRepository AddFailingPackageRepository (Exception exception)
		{
			var repository = new ExceptionThrowingPackageRepository {
				GetPackagesException = exception
			};
			repositories.Add (repository);
			return repository;
		}

		List<IPackage> Search ()
		{
			return aggregateRepository.Search (null, false).ToList ();
		}

		[Test]
		public void Search_IgnoreFailingRepositoriesAndOnePackageSourceFails_ErrorFromFailingRepositoryIsSupressed ()
		{
			FakePackage package1 = AddRepositoryWithOnePackage ("Package1");
			FakePackage package2 = AddRepositoryWithOnePackage ("Package2");
			AddFailingPackageRepository ();
			CreateAggregateRepository ();
			aggregateRepository.IgnoreFailingRepositories = true;

			List<IPackage> packages = Search ();

			Assert.AreEqual (2, packages.Count);
			Assert.That (packages, Contains.Item (package1));
			Assert.That (packages, Contains.Item (package2));
		}

		[Test]
		public void Search_TwoPackageRepositoriesOneFailingWhenGetPackagesCalled_AnyFailuresReturnsTrueAndAllFailedReturnsFalse ()
		{
			AddRepository ();
			AddFailingPackageRepository ();
			CreateAggregateRepository ();
			Search ();

			bool failures = aggregateRepository.AnyFailures ();
			bool allFailed = aggregateRepository.AllFailed ();

			Assert.IsTrue (failures);
			Assert.IsFalse (allFailed);
		}

		[Test]
		public void Search_TwoPackageRepositoriesBothFailingWhenGetPackagesCalled_AllFailedReturnsTrue ()
		{
			AddFailingPackageRepository ();
			AddFailingPackageRepository ();
			CreateAggregateRepository ();
			Search ();

			bool failures = aggregateRepository.AnyFailures ();
			bool allFailed = aggregateRepository.AllFailed ();

			Assert.IsTrue (failures);
			Assert.IsTrue (allFailed);
		}

		[Test]
		public void Search_TwoPackageRepositoriesNoneFailingWhenGetPackagesCalled_AnyFailuresAndAllFailedReturnFalse ()
		{
			AddRepository ();
			AddRepository ();
			CreateAggregateRepository ();
			Search ();

			bool failures = aggregateRepository.AnyFailures ();
			bool allFailed = aggregateRepository.AllFailed ();

			Assert.IsFalse (failures);
			Assert.IsFalse (allFailed);
		}

		[Test]
		public void Search_TwoPackageRepositoriesOneFailingWhenGetPackagesCalled_GetAggregateExceptionIncludesRepositoryException ()
		{
			AddRepository ();
			var exception = new Exception ("Error");
			AddFailingPackageRepository (exception);
			CreateAggregateRepository ();
			Search ();

			AggregateException aggregateException = aggregateRepository.GetAggregateException ();

			Assert.That (aggregateException.InnerExceptions, Contains.Item (exception));
		}

		[Test]
		public void Search_TwoPackageRepositoriesBothFailingWhenGetPackagesCalled_GetAggregateExceptionIncludesBothRepositoryExceptions ()
		{
			var exception1 = new Exception ("Error1");
			AddFailingPackageRepository (exception1);
			var exception2 = new Exception ("Error2");
			AddFailingPackageRepository (exception2);
			CreateAggregateRepository ();
			Search ();

			AggregateException aggregateException = aggregateRepository.GetAggregateException ();

			Assert.That (aggregateException.InnerExceptions, Contains.Item (exception1));
			Assert.That (aggregateException.InnerExceptions, Contains.Item (exception2));
		}

		[Test]
		public void Search_TwoPackageRepositoriesBothFailingWhenGetPackagesCalledFirstThenSecondTimeNewRepositoryAddedToRepositories_AllFailedReturnsFalseSecondTime ()
		{
			AddFailingPackageRepository ();
			AddFailingPackageRepository ();
			CreateAggregateRepository ();
			Search ();
			AddRepository ();

			bool failures = aggregateRepository.AnyFailures ();
			bool allFailed = aggregateRepository.AllFailed ();

			Assert.IsTrue (failures);
			Assert.IsFalse (allFailed);
		}
	}
}

