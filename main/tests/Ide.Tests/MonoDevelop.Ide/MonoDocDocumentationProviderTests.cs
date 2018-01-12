//
// MonoDocDocumentationProviderTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2017 Microsoft Corporation
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
using NUnit.Framework;
using System.Collections.Generic;
using UnitTests;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Core;
using System.Threading.Tasks;
using System.Linq;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide
{
	[TestFixture]
	class MonoDocDocumentationProviderTests
	{
		/// <summary>
		/// Information tooltips are not showing method docs, but object docs
		/// </summary>
		[Test]
		public async Task TestBug530737 ()
		{
			if (Platform.IsWindows)
				return;
			var doc1 = MonoDocDocumentationProvider.GetDocumentation ("T:System.Console");
			var doc2 = MonoDocDocumentationProvider.GetDocumentation ("P:System.Console.Out");
			Assert.NotNull (doc1);
			Assert.NotNull (doc2);
			Assert.AreNotEqual (doc1, doc2);
		}
	}
}

