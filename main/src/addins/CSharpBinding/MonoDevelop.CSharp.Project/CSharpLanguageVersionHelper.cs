//
// CSharpLanguageVersionHelper.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2018 
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
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Project
{
	class CSharpLanguageVersionHelper
	{
		public static IEnumerable<(string localized, LanguageVersion version)> GetKnownLanguageVersions ()
		{
			yield return (GettextCatalog.GetString ("Default"), LanguageVersion.Default);
			yield return ("ISO-1", LanguageVersion.CSharp1);
			yield return ("ISO-2", LanguageVersion.CSharp2);
			yield return (GettextCatalog.GetString ("Version 3"), LanguageVersion.CSharp3);
			yield return (GettextCatalog.GetString ("Version 4"), LanguageVersion.CSharp4);
			yield return (GettextCatalog.GetString ("Version 5"), LanguageVersion.CSharp5);
			yield return (GettextCatalog.GetString ("Version 6"), LanguageVersion.CSharp6);
			yield return (GettextCatalog.GetString ("Version 7"), LanguageVersion.CSharp7);
			yield return (GettextCatalog.GetString ("Version 7.1"), LanguageVersion.CSharp7_1);
			yield return (GettextCatalog.GetString ("Version 7.2"), LanguageVersion.CSharp7_2);
			yield return (GettextCatalog.GetString ("Latest"), LanguageVersion.Latest);
		}
	}
}
