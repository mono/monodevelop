//
// PackageSourceCollectionAssert.cs
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
using NuGet.Configuration;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	class PackageSourceCollectionAssert
	{
		public static void AreEqual (IEnumerable<PackageSource> expectedSources, IEnumerable<PackageSourceViewModel> actualViewModels)
		{
			List<string> expectedSourcesAsList = ConvertToStrings (expectedSources);
			List<string> actualSources = ConvertToStrings (actualViewModels);

			CollectionAssert.AreEqual (expectedSourcesAsList, actualSources);
		}

		public static void AreEqual (IEnumerable<PackageSource> expectedSources, IEnumerable<PackageSource> actualSources)
		{
			List<string> expectedSourcesAsList = ConvertToStrings (expectedSources);
			List<string> actualSourcesAsList = ConvertToStrings (actualSources);

			CollectionAssert.AreEqual (expectedSourcesAsList, actualSourcesAsList);
		}

		static List<string> ConvertToStrings (IEnumerable<PackageSource> sources)
		{
			var convertedSources = new List<string> ();
			foreach (PackageSource source in sources) {
				convertedSources.Add (ConvertToString (source));
			}
			return convertedSources;
		}

		static string ConvertToString (PackageSource source)
		{
			if (source != null) {
				return String.Format ("[PackageSource] Name='{0}', Source='{1}'",
					source.Name,
					source.Source);
			}
			return "[PackageSource] == Null";
		}

		static List<string> ConvertToStrings (IEnumerable<PackageSourceViewModel> viewModels)
		{
			List<string> convertedSources = new List<string> ();
			foreach (PackageSourceViewModel viewModel in viewModels) {
				PackageSource source = viewModel.GetPackageSource ();
				convertedSources.Add (ConvertToString (source));
			}
			return convertedSources;
		}
	}
}

