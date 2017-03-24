//
// FakePackageFilesReader.cs
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


using System;
using System.Collections.Generic;
using NuGet.Packaging;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class FakePackageFilesReader : IPackageFilesReader
	{
		public void Dispose ()
		{
		}

		public List<FrameworkSpecificGroup> BuildItems = new List<FrameworkSpecificGroup> ();

		public IEnumerable<FrameworkSpecificGroup> GetBuildItems ()
		{
			return BuildItems;
		}

		public List<FrameworkSpecificGroup> ContentItems = new List<FrameworkSpecificGroup> ();

		public IEnumerable<FrameworkSpecificGroup> GetContentItems ()
		{
			return ContentItems;
		}

		public List<FrameworkSpecificGroup> FrameworkItems = new List<FrameworkSpecificGroup> ();

		public IEnumerable<FrameworkSpecificGroup> GetFrameworkItems ()
		{
			return FrameworkItems;
		}

		public List<FrameworkSpecificGroup> LibItems = new List<FrameworkSpecificGroup> ();

		public IEnumerable<FrameworkSpecificGroup> GetLibItems ()
		{
			return LibItems;
		}

		public IEnumerable<PackageDependencyGroup> GetPackageDependencies ()
		{
			throw new NotImplementedException ();
		}

		public List<FrameworkSpecificGroup> ReferenceItems = new List<FrameworkSpecificGroup> ();

		public IEnumerable<FrameworkSpecificGroup> GetReferenceItems ()
		{
			return ReferenceItems;
		}

		public List<FrameworkSpecificGroup> ToolItems = new List<FrameworkSpecificGroup> ();

		public IEnumerable<FrameworkSpecificGroup> GetToolItems ()
		{
			return ToolItems;
		}
	}
}
