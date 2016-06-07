//
// TestPackageReferenceFactory.cs
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

using System.Linq;
using Newtonsoft.Json.Linq;
using NuGet.Packaging;
using NuGet.Packaging.Core;
using NuGet.ProjectManagement;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	static class TestPackageReferenceFactory
	{
		public static PackageReference CreatePackageReferenceWithProjectJsonWildcardVersion (
			string package,
			string version)
		{
			string json = "{ \"dependencies\": { \"" + package + "\": \"" + version + "\" } }";
			var jsonObject = JObject.Parse (json);

			var dependency = JsonConfigUtility.GetDependencies (jsonObject).First ();
			var identity = new PackageIdentity (dependency.Id, dependency.VersionRange.MinVersion);

			return new PackageReference (identity, null, true, false, false, dependency.VersionRange);
		}
	}
}

