//
// PackageManagementSelectedProjectTests.cs
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
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class PackageManagementSelectedProjectTests
	{
		PackageManagementSelectedProject selectedProject;
		FakePackageManagementProject fakeProject;

		void CreateFakePackageManagementProject ()
		{
			fakeProject = new FakePackageManagementProject ();
		}

		void CreateSelectedProject (FakePackageManagementProject fakeProject)
		{
			selectedProject = new PackageManagementSelectedProject (fakeProject);
		}

		[Test]
		public void Name_PackageManagementProjectNameIsTest_ReturnsTest ()
		{
			CreateFakePackageManagementProject ();
			CreateSelectedProject (fakeProject);
			fakeProject.Name = "Test";

			string name = selectedProject.Name;

			Assert.AreEqual ("Test", name);
		}

		[Test]
		public void IsSelected_SelectedNotSpecifiedInConstructor_ReturnsFalse ()
		{
			CreateFakePackageManagementProject ();
			CreateSelectedProject (fakeProject);

			bool selected = selectedProject.IsSelected;

			Assert.IsFalse (selected);
		}

		[Test]
		public void IsEnabled_EnabledNotSpecifiedInConstructor_ReturnsTrue ()
		{
			CreateFakePackageManagementProject ();
			CreateSelectedProject (fakeProject);

			bool enabled = selectedProject.IsEnabled;

			Assert.IsTrue (enabled);
		}
	}
}

