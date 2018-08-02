//
// RoslynSearchCategoryTests.cs
//
// Author:
//       Marius <maungu@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Threading;
using System.Threading.Tasks;
using MonoDevelop.Components.MainToolbar;
using MonoDevelop.Core;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using NUnit.Framework;
using UnitTests;

namespace MonoDevelop.Ide
{
	[TestFixture]
	public class RoslynSearchCategoryTests : IdeTestBase
	{
		static async Task<List<SearchResult>> Search (string pattern)
		{
			var category = new RoslynSearchCategory ();
			var callback = new GatherResultCallback ();

			await category.GetResults (callback, SearchPopupSearchPattern.ParsePattern (pattern), CancellationToken.None);
			return callback.Results;
		}

		[Test]
		public async Task TestConsoleProjectWorks ()
		{
			string solFile = Util.GetSampleProject ("console-with-libs", "console-with-libs.sln");

			using (Solution sol = (Solution)await Services.ProjectService.ReadWorkspaceItem (Util.GetMonitor (), solFile))
			using (var ws = await TypeSystemServiceTestExtensions.LoadSolution (sol)) {
				var results = await Search ("");
				Assert.AreEqual (0, results.Count);

				results = await Search ("M");
				Assert.AreEqual (6, results.Count);

				results = await Search ("type:M");
				Assert.AreEqual (3, results.Count);

				results = await Search ("My");
				// Should be 4: https://github.com/dotnet/roslyn/issues/29031
				Assert.AreEqual (2, results.Count);

				results = await Search ("MC");
				Assert.AreEqual (5, results.Count);
			}
		}

		class GatherResultCallback : ISearchResultCallback
		{
			public List<SearchResult> Results = new List<SearchResult> ();

			public void ReportResult (SearchResult result)
			{
				lock (Results)
					Results.Add (result);
			}
		}
	}
}
