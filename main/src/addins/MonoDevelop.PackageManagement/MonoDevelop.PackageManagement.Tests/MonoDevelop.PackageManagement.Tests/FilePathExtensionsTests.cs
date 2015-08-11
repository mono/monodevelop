//
// FilePathExtensionsTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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

using MonoDevelop.Core;
using MonoDevelop.PackageManagement.Tests.Helpers;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests
{
	[TestFixture]
	public class FilePathExtensionsTests
	{
		FilePath CreateFilePath (string fileName)
		{
			return new FilePath (fileName.ToNativePath ());
		}

		[Test]
		public void IsPackagesConfigFileName_Null_ReturnsFalse ()
		{
			FilePath nullPath = null;
			bool result = nullPath.IsPackagesConfigFileName ();

			Assert.IsFalse (result);
		}

		[Test]
		public void IsPackagesConfigFileName_TextFile_ReturnsFalse ()
		{
			FilePath filePath = CreateFilePath (@"d:\projects\foo.txt");

			bool result = filePath.IsPackagesConfigFileName ();

			Assert.IsFalse (result);
		}

		[Test]
		public void IsPackagesConfigFileName_PackagesConfigFile_ReturnsTrue ()
		{
			FilePath filePath = CreateFilePath (@"d:\projects\packages.config");

			bool result = filePath.IsPackagesConfigFileName ();

			Assert.IsTrue (result);
		}

		[Test]
		public void IsPackagesConfigFileName_PackagesConfigFileInUpperCase_ReturnsTrue ()
		{
			FilePath filePath = CreateFilePath (@"d:\projects\PACKAGES.CONFIG");

			bool result = filePath.IsPackagesConfigFileName ();

			Assert.IsTrue (result);
		}

		[Test]
		public void IsPackagesConfigFileName_PackagesConfigFileNamedAfterProject_ReturnsTrue ()
		{
			FilePath filePath = CreateFilePath (@"d:\projects\packages.MyProject.config");

			bool result = filePath.IsPackagesConfigFileName ();

			Assert.IsTrue (result);
		}

		[Test]
		public void IsPackagesConfigFileName_PackagesConfigFileNamedAfterProjectInDifferentCase_ReturnsTrue ()
		{
			FilePath filePath = CreateFilePath (@"d:\projects\PACKAGES.MyProject.CONFIG");

			bool result = filePath.IsPackagesConfigFileName ();

			Assert.IsTrue (result);
		}

		[Test]
		public void IsPackagesConfigFileName_PackagesConfigFileNamedAfterProjectButStartsWithProjectName_ReturnsFalse ()
		{
			FilePath filePath = CreateFilePath (@"d:\projects\MyProject.packages.config");

			bool result = filePath.IsPackagesConfigFileName ();

			Assert.IsFalse (result);
		}
	}
}

