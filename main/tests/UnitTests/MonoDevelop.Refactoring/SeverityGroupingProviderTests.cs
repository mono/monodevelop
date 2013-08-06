//
// SeverityGroupingProviderTests.cs
//
// Author:
//       Simon Lindgren <simon.n.lindgren@gmail.com>
//
// Copyright (c) 2013 Simon Lindgren
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
using MonoDevelop.CodeIssues;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.Refactoring
{
	public class SeverityGroupingProviderTests : GroupingProviderTestBase<SeverityGroupingProvider>
	{
		#region implemented abstract members of GroupingProviderTestBase

		protected override SeverityGroupingProvider CreateProviderInstance ()
		{
			return new SeverityGroupingProvider ();
		}

		protected override IssueSummary[] GetDistinctSummaries ()
		{
			return new [] {
				new IssueSummary { Severity = Severity.None },
				new IssueSummary { Severity = Severity.Hint },
				new IssueSummary { Severity = Severity.Suggestion },
				new IssueSummary { Severity = Severity.Warning },
				new IssueSummary { Severity = Severity.Error }
			};
		}

		#endregion
	}
}

