//
// OneRegisteredPackageSourceHelper.cs
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

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class OneRegisteredPackageSourceHelper
	{
		public TestablePackageManagementOptions Options;
		public FakeSettings FakeSettings;
		public PackageSource PackageSource = new PackageSource ("http://sharpdevelop.com", "Test Package Source");

		public RegisteredPackageSources RegisteredPackageSources {
			get { return Options.PackageSources; }
		}

		public OneRegisteredPackageSourceHelper ()
		{
			CreateOneRegisteredPackageSource ();
		}

		void CreateOneRegisteredPackageSource ()
		{
			Options = new TestablePackageManagementOptions ();
			FakeSettings = Options.FakeSettings;
			AddOnePackageSource ();
		}

		public void AddOnePackageSource ()
		{
			RegisteredPackageSources.Clear ();
			RegisteredPackageSources.Add (PackageSource);
		}

		public void AddOnePackageSource (string source)
		{
			RegisteredPackageSources.Clear ();
			AddPackageSource (source);
		}

		public void AddTwoPackageSources ()
		{
			AddOnePackageSource ();
			var packageSource = new PackageSource ("http://second.codeplex.com", "second");
			RegisteredPackageSources.Add (packageSource);
		}

		public void AddTwoPackageSources (string source1, string source2)
		{
			RegisteredPackageSources.Clear ();
			AddPackageSource (source1);
			AddPackageSource (source2);
		}

		void AddPackageSource (string source)
		{
			var packageSource = new PackageSource (source);
			RegisteredPackageSources.Add (packageSource);
		}
	}
}

