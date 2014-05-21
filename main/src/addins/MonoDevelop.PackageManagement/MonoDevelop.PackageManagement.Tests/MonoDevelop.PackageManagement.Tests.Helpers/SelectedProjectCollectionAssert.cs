//
// SelectedProjectCollectionAssert.cs
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
using ICSharpCode.PackageManagement;
using NUnit.Framework;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public static class SelectedProjectCollectionAssert
	{
		public static void AreEqual (
			IEnumerable<IPackageManagementSelectedProject> expected,
			IEnumerable<IPackageManagementSelectedProject> actual)
		{
			List<string> expectedAsStrings = ConvertToStrings (expected);
			List<string> actualAsStrings = ConvertToStrings (actual);
			CollectionAssert.AreEqual (expectedAsStrings, actualAsStrings);
		}

		static List<string> ConvertToStrings (IEnumerable<IPackageManagementSelectedProject> projects)
		{
			var projectsAsString = new List<string> ();
			foreach (IPackageManagementSelectedProject project in projects) {
				string text = String.Format (
					"Name: {0}, IsSelected: {1}, IsEnabled: {2}",
					project.Name,
					project.IsSelected,
					project.IsEnabled);
				projectsAsString.Add (text);
			}
			return projectsAsString;
		}
	}
}


