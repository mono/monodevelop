//
// PackageSourceViewModelTests.cs
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
using ICSharpCode.PackageManagement;
using NuGet;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageSourceViewModelTests
	{
		PackageSourceViewModel viewModel;
		PackageSource packageSource;

		void CreatePackageSource ()
		{
			CreatePackageSource ("http://sharpdevelop.codeplex.com", "Test");
		}

		void CreatePackageSource (string source, string name)
		{
			packageSource = new PackageSource (source, name);
		}

		void CreatePackageSourceWithName (string  name)
		{
			CreatePackageSource ("http://sharpdevelop.codeplex.com", name);
		}

		void  CreatePackageSourceWithSourceUrl (string  sourceUrl)
		{
			CreatePackageSource (sourceUrl, "Test");
		}

		void CreateViewModel (PackageSource packageSource)
		{
			viewModel = new PackageSourceViewModel (packageSource);
		}

		void CreateEnabledPackageSource ()
		{
			CreatePackageSource ();
			packageSource.IsEnabled = true;
		}

		void CreateDisabledPackageSource ()
		{
			CreatePackageSource ();
			packageSource.IsEnabled = false;
		}

		[Test]
		public void Name_InstanceCreatedWithRegisteredPackageSource_MatchesRegisteredPackageSourceName ()
		{
			CreatePackageSourceWithName ("Test");
			CreateViewModel (packageSource);

			Assert.AreEqual ("Test", viewModel.Name);
		}

		[Test]
		public void Name_Changed_NamePropertyIsChanged ()
		{
			CreatePackageSourceWithName ("Test");
			CreateViewModel (packageSource);
			viewModel.Name = "changed";

			Assert.AreEqual ("changed", viewModel.Name);
		}

		[Test]
		public void SourceUrl_InstanceCreatedWithRegisteredPackageSource_MatchesRegisteredPackageSourceSourceUrl ()
		{
			CreatePackageSourceWithSourceUrl ("Test-url");
			CreateViewModel (packageSource);

			Assert.AreEqual ("Test-url", viewModel.SourceUrl);
		}

		[Test]
		public void Source_Changed_SourcePropertyIsChanged ()
		{
			CreatePackageSourceWithSourceUrl ("source-url");
			CreateViewModel (packageSource);
			viewModel.SourceUrl = "changed";

			Assert.AreEqual ("changed", viewModel.SourceUrl);
		}

		[Test]
		public void IsEnabled_PackageSourceIsEnabled_ReturnsTrue ()
		{
			CreateEnabledPackageSource ();
			CreateViewModel (packageSource);

			Assert.IsTrue (viewModel.IsEnabled);
		}

		[Test]
		public void IsEnabled_PackageSourceIsNotEnabled_ReturnsFalse ()
		{
			CreateDisabledPackageSource ();
			CreateViewModel (packageSource);

			Assert.IsFalse (viewModel.IsEnabled);
		}

		[Test]
		public void IsEnabled_ChangedFromTrueToFalse_UpdatesPackageSource ()
		{
			CreateEnabledPackageSource ();
			CreateViewModel (packageSource);

			viewModel.IsEnabled = false;

			PackageSource updatedPackageSource = viewModel.GetPackageSource ();
			Assert.IsFalse (updatedPackageSource.IsEnabled);
		}

		[Test]
		public void IsEnabled_ChangedFromFalseToTrue_UpdatesPackageSource ()
		{
			CreateDisabledPackageSource ();
			CreateViewModel (packageSource);

			viewModel.IsEnabled = true;

			PackageSource updatedPackageSource = viewModel.GetPackageSource ();
			Assert.IsTrue (updatedPackageSource.IsEnabled);
		}
	}
}

