//
// DiscoveredTests.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MonoDevelop.UnitTesting;

namespace MonoDevelop.UnitTesting.VsTest
{
	class DiscoveredTests
	{
		readonly List<TestCase> tests = new List<TestCase> ();

		public IEnumerable<TestCase> Tests {
			get { return tests; }
		}

		public void Add (IEnumerable<TestCase> newTests)
		{
			tests.AddRange (newTests);
		}

		public IEnumerable<UnitTest> BuildTestInfo (VsTestProjectTestSuite projectTestSuite)
		{
			tests.Sort (OrderByName);

			var parentNamespace = new VsTestNamespaceTestGroup (projectTestSuite, null, projectTestSuite.Project, String.Empty);
			parentNamespace.AddTests (tests);
			return parentNamespace.Tests;
		}

		static int OrderByName (TestCase x, TestCase y)
		{
			return StringComparer.Ordinal.Compare (x.FullyQualifiedName, y.FullyQualifiedName);
		}
	}
}
