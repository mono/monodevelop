//
// PackageReferenceExtensions.cs
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

using NuGet.Packaging;
using NuGet.Versioning;

namespace MonoDevelop.PackageManagement
{
	internal static class PackageReferenceExtensions
	{
		public static bool IsFloating (this PackageReference packageReference)
		{
			return packageReference.HasAllowedVersions && packageReference.AllowedVersions.IsFloating;
		}

		public static bool IsAtLeastVersion (this PackageReference packageReference, NuGetVersion requestedVersion)
		{
			var comparer = VersionComparer.VersionRelease;
			if (packageReference.HasAllowedVersions) {
				var versionRange = packageReference.AllowedVersions;
				if (versionRange.HasLowerBound) {
					var result = comparer.Compare (versionRange.MinVersion, requestedVersion);
					return versionRange.IsMinInclusive ? result <= 0 : result < 0;
				}
			} else if (packageReference.PackageIdentity.HasVersion) {
				var packageVersion = packageReference.PackageIdentity.Version;
				return comparer.Compare (requestedVersion, packageVersion) <= 0;
			}
			return false;
		}
	}
}

