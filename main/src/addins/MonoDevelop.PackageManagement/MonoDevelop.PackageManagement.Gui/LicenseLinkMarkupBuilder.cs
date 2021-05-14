//
// LicenseLinkMarkupBuilder.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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
using System.Linq;
using System.Security;
using MonoDevelop.Core;
using NuGet.PackageManagement.UI;

namespace MonoDevelop.PackageManagement
{
	sealed class LicenseLinkMarkupBuilder
	{
		List<WarningText> warnings;

		public string GetMarkup (IReadOnlyList<IText> textLinks)
		{
			var markupBuilder = StringBuilderCache.Allocate ();

			foreach (IText textLink in textLinks) {
				if (textLink is LicenseText licenseText) {
					markupBuilder.Append (GetUriMarkup (licenseText.Link, licenseText.Text));
				} else if (textLink is LicenseFileText licenseFileText) {
					// Should not happen. Building an expression should not contain a license file.
					LoggingService.LogError ("Unexpected LicenseFileText when building markup {0}", licenseFileText.Text);
				} else if (textLink is WarningText warning) {
					warnings ??= new List<WarningText> ();
					warnings.Add (warning);
				} else {
					markupBuilder.Append (textLink.Text);
				}
			}

			return StringBuilderCache.ReturnAndFree (markupBuilder);
		}

		public IEnumerable<WarningText> Warnings {
			get { return warnings ?? Enumerable.Empty<WarningText> (); }
		}

		static string GetUriMarkup (Uri uri, string text)
		{
			return string.Format (
				"<a href=\"{0}\">{1}</a>",
				uri != null ? SecurityElement.Escape (uri.ToString ()) : string.Empty,
				SecurityElement.Escape (text));
		}
	}
}
