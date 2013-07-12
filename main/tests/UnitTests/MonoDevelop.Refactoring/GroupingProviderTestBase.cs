//
// GroupingProviderTestBase.cs
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
using MonoDevelop.CodeIssues;
using NUnit.Framework;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory;

namespace MonoDevelop.Refactoring
{

	/// <summary>
	/// Base class containing common tests and helpers for testing
	/// <see cref="IGroupingProvider">IGroupingProviders</see>.
	/// </summary>
	public abstract class GroupingProviderTestBase<T> where T: IGroupingProvider
	{
		IssueSummary summary;
	
		protected T Provider { get; private set; }
		
		/// <summary>
		/// Creates a provider instance for use during tests.
		/// </summary>
		/// <returns>The provider instance.</returns>
		protected abstract T CreateProviderInstance ();
		
		/// <summary>
		/// Returns a set of <see cref="IssueSummary">IssueSummaries</see> that
		/// should be placed in different groups.
		/// </summary>
		/// <returns>The summaries.</returns>
		protected abstract IssueSummary[] GetDistinctSummaries ();
		
		[SetUp]
		public void SetUp() {
			Provider = CreateProviderInstance ();
			summary = new IssueSummary {
				IssueDescription = "IssueDescription",
				IssueMarker = IssueMarker.None,
				ProviderCategory = "ProviderCategory",
				ProviderDescription = "ProviderDescription",
				ProviderTitle = "ProviderTitle",
				Region = new DomRegion("fileName", new TextLocation(2, 3), new TextLocation(2, 10)),
				Severity = Severity.None
			};
		}
		
		[Test]
		public void ReturnsSameGroupForSameIssueSummary()
		{
			var first = Provider.GetIssueGroup (summary);
			var second = Provider.GetIssueGroup (summary);
			
			Assert.AreSame (first, second, "Two invocations with the same issue summary returned different groups.");
		}
		
		[Test]
		public void TestDistinctGroups ()
		{
			var summaries = GetDistinctSummaries ();
			for (int i = 0; i < summaries.Length; i++) {
				for (int j = 0; j < summaries.Length; j++) {
					if (i == j) {
						continue;
					}
					
					var first = Provider.GetIssueGroup (summaries[i]);
					var second = Provider.GetIssueGroup (summaries[j]);
					Assert.AreNotSame (first, second, "The groups should not be the same.");
				}
			}
		}
		
		[Test]
		public void FiresNextUpdatedEvent ()
		{
			bool fired = false;
			Provider.NextChanged += (sender, obj) => {
				fired = true;
			};
			Provider.Next = new MockGroupingProvider ();
			Assert.IsTrue (fired, "The NextChanged event was not fired.");
		}
		
		[Test]
		public void NextIsNotNull ()
		{
			Assert.NotNull (Provider.Next);
		}
		
		[Test]
		public void ResetRemovesGroups ()
		{
			var firstGroup = Provider.GetIssueGroup (summary);
			Provider.Reset ();
			var secondGroup = Provider.GetIssueGroup (summary);
			Assert.AreNotEqual (firstGroup, secondGroup, "Reset did not remove the old group.");
		}
		
		[Test]
		public void ResetCascadesToNext ()
		{
			var mock = new MockGroupingProvider ();
			Provider.Next = mock;
			Provider.Reset ();
			Assert.IsTrue (mock.ResetCalled, "The provider did not reset its Next provider.");
		}
		
		[Test]
		public void SetsNextProviderOfGeneratedGroups ()
		{
			var group = Provider.GetIssueGroup (summary);
			var mock = new MockGroupingProvider ();
			Provider.Next = mock;
			Assert.AreSame (mock, group.GroupingProvider, "The grouping provider change was not propagated to the existing group.");
		}
	}
}

