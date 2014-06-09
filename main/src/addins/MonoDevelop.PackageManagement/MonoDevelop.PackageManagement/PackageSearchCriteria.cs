//
// PackageSearchCriteria.cs
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
using NuGet;

namespace MonoDevelop.PackageManagement
{
	public class PackageSearchCriteria
	{
		WildcardVersionSpec wildcardVersionSpec;

		public PackageSearchCriteria (string searchText)
		{
			SearchText = RemoveWhitespace (searchText);
			ParseSearchText (SearchText);
		}

		public string PackageId { get; private set; }
		public string SearchText { get; private set; }

		public bool IsPackageVersionSearch {
			get { return !String.IsNullOrEmpty (PackageId); }
		}

		public bool IsVersionMatch (SemanticVersion version)
		{
			if (wildcardVersionSpec == null)
				return true;

			return wildcardVersionSpec.Satisfies (version);
		}

		string RemoveWhitespace (string searchText)
		{
			if (String.IsNullOrWhiteSpace (searchText))
				return null;

			return searchText;
		}

		void ParseSearchText (string searchText)
		{
			if (searchText == null)
				return;

			string[] parts = searchText.Split (new [] {' '}, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length < 2)
				return;

			if (!IsVersionOption (parts [1]))
				return;

			PackageId = parts [0].Trim ();

			wildcardVersionSpec = new WildcardVersionSpec (GetVersion (parts [1]));
		}

		bool IsVersionOption (string option)
		{
			return option.StartsWith ("version:", StringComparison.OrdinalIgnoreCase);
		}

		string GetVersion (string option)
		{
			int index = option.IndexOf (':');
			return option.Substring (index + 1);
		}
	}
}