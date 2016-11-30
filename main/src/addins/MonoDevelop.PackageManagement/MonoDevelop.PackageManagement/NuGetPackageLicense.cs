//
// NuGetPackageLicense.cs
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
using NuGet.Packaging.Core;
using NuGet.Protocol.Core.Types;

namespace MonoDevelop.PackageManagement
{
	internal class NuGetPackageLicense
	{
		public NuGetPackageLicense (IPackageSearchMetadata metadata)
		{
			PackageIdentity = metadata.Identity;
			PackageId = metadata.Identity.Id;
			PackageTitle = metadata.Title;
			PackageAuthor = metadata.Authors;
			LicenseUrl = metadata.LicenseUrl;
			IconUrl = metadata.IconUrl;
		}

		public PackageIdentity PackageIdentity { get; private set; }
		public string PackageId { get; private set; }
		public string PackageTitle { get; private set; }
		public string PackageAuthor { get; private set; }
		public Uri LicenseUrl { get; private set; }
		public Uri IconUrl { get; private set; }
	}
}

