//
// IssueGroupTests.cs
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
using NUnit.Framework;
using MonoDevelop.CodeIssues;
using System.Linq;

namespace MonoDevelop.Refactoring
{
	[TestFixture]
	public class IssueGroupTests
	{
		IssueGroup group;
		MockGroupingProvider nextProvider;
		MockGroupingProvider sourceProvider;

		static EventHandler<T> Forbidden<T> (string eventName) where T: EventArgs
		{
			return (sender, eventArgs) => {
				Assert.Fail ("The event '{0}' was not supposed to be invoked.", eventName);
			};
		}

		static IssueGroup CreateSecondaryGroup ()
		{
			var issueGroup = new IssueGroup (null, null, "secondary group");
			issueGroup.EnableProcessing ();
			return issueGroup;
		}
		
		[SetUp]
		public void SetUp()
		{
			nextProvider = new MockGroupingProvider ();
			sourceProvider = new MockGroupingProvider ();
			group = new IssueGroup (sourceProvider, nextProvider, "sut");
			group.EnableProcessing ();
		}
		
		[Test]
		public void PassesIssueSummaryToExistingGroup()
		{
			// "prime" the tree of groups
			nextProvider.Group = CreateSecondaryGroup ();
			group.Push (new IssueSummary ());
			
			var probe = new IssueSummary ();
			group.Push (probe);
			Assert.IsTrue (nextProvider.Group.Issues.Contains(probe), "The issue was not added to the existing group.");
		}
		
		[Test]
		public void PassesIssueSummaryToNewGroup()
		{
			nextProvider.Group = CreateSecondaryGroup ();
			
			var probe = new IssueSummary ();
			group.Push (probe);
			Assert.IsTrue (nextProvider.Group.Issues.Contains(probe), "The issue was not added to the new group.");
		}
		
		[Test]
		public void PassesIssueSummaryToExistingGroupDuringEnableProcessing ()
		{
			var disabledGroup = new IssueGroup (sourceProvider, nextProvider, "sut");
			// "prime" the tree of groups
			nextProvider.Group = CreateSecondaryGroup ();
			disabledGroup.Push (new IssueSummary ());
			
			var probe = new IssueSummary ();
			disabledGroup.Push (probe);
			disabledGroup.EnableProcessing ();
			var issues = nextProvider.Group.Issues;
			Assert.IsTrue (issues.Contains(probe), "The issue was not added to the existing group.");
		}
		
		[Test]
		public void PassesIssueSummaryToNewGroupDuringEnableProcessing ()
		{
			var disabledGroup = new IssueGroup (sourceProvider, nextProvider, "sut");
			// "prime" the tree of groups
			nextProvider.Group = CreateSecondaryGroup ();
			
			var probe = new IssueSummary ();
			disabledGroup.Push (probe);
			disabledGroup.EnableProcessing ();
			var issues = nextProvider.Group.Issues;
			Assert.IsTrue (issues.Contains(probe), "The issue was not added to the new group.");
		}
		
		[Test]
		public void ClearStatisticsTest ()
		{
			group.Push (new IssueSummary ());
			
			Assert.AreEqual (1, group.IssueCount, "Incorrect issue count.");
			
			group.ClearStatistics ();
			
			Assert.AreEqual (0, group.IssueCount, "Incorrect issue count after reset.");
			Assert.IsTrue (nextProvider.ResetCalled, "Reset was not called on the provider");
		}
		
		[Test]
		public void DoesNotInteractIfGroupingDisabled ()
		{
			var disabledGroup = new IssueGroup (null, nextProvider, "disabled group");
			
			disabledGroup.Push (new IssueSummary ());
			Assert.IsFalse (nextProvider.GetIssueGroupCalled, "The provider should not be called by a disabled group.");
		}
		
		[Test]
		public void ChildrenInvalidatedCalledWhenCreatingProviderNextChanges ()
		{
			bool eventCalled = false;
			group.ChildrenInvalidated += (sender, eventArgs) => {
				eventCalled = true;
			};
			sourceProvider.Next = new MockGroupingProvider();
			Assert.IsTrue (eventCalled, "The event was not called.");
		}
	}
}

