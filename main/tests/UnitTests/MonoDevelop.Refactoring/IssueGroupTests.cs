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

namespace MonoDevelop.Refactoring
{
	[TestFixture]
	public class IssueGroupTests
	{
		IssueGroup group;
		IIssueTreeNode node;
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
			var issueGroup = new IssueGroup (null, "secondary group");
			issueGroup.EnableProcessing ();
			return issueGroup;
		}

		[SetUp]
		public void SetUp ()
		{
			nextProvider = new MockGroupingProvider ();
			sourceProvider = new MockGroupingProvider ();
			group = new IssueGroup (nextProvider, "sut");
			node = group;
			group.EnableProcessing ();
		}

		[Test]
		public void CallsChildAddedEventHandlers ()
		{
			nextProvider.Group = CreateSecondaryGroup ();

			bool eventHandlerCalled = false;
			bool groupEventHandlerCalled = false;
			((IIssueTreeNode)nextProvider.Group).ChildAdded += delegate {
				groupEventHandlerCalled = true;
			};
			node.ChildAdded += delegate {
				eventHandlerCalled = true;
			};

			group.AddIssue (new IssueSummary ());
			Assert.IsTrue (eventHandlerCalled, "The event handler for the root group was not called.");
			Assert.IsTrue (groupEventHandlerCalled, "The event handler for the nested group was not called.");
		}

		[Test]
		public void CallsTextChangedEventHandlersWhenIssueIsAdded ()
		{
			bool textChangedHandlerCalled = false;
			node.TextChanged += delegate {
				textChangedHandlerCalled = true;
			};

			group.AddIssue (new IssueSummary ());
			Assert.IsTrue (textChangedHandlerCalled, "The event handler was not called.");
		}

		[Test]
		public void CallsTextChangedEventHandlersWhenIssueIsAddedToNonProcessingGroup ()
		{
			var inactiveGroup = new IssueGroup (nextProvider, "sut");
			bool textChangedHandlerCalled = false;
			((IIssueTreeNode)inactiveGroup).TextChanged += delegate {
				textChangedHandlerCalled = true;
			};

			inactiveGroup.AddIssue (new IssueSummary ());
			Assert.IsTrue (textChangedHandlerCalled, "The event handler was not called.");
		}

		[Test]
		public void DoesNotCallChildAddedEventHandlersIfNotEnabled ()
		{
			var disabledGroup = new IssueGroup (null, "disabledNode");
			IIssueTreeNode disabledNode = disabledGroup;
			nextProvider.Group = CreateSecondaryGroup ();
			
			disabledNode.ChildAdded += Forbidden<IssueTreeNodeEventArgs> ("node.ChildAdded");
			disabledGroup.AddIssue (new IssueSummary ());
		}

		[Test]
		public void PassesIssueSummaryToExistingGroup ()
		{
			// "prime" the tree of groups
			nextProvider.Group = CreateSecondaryGroup ();
			group.AddIssue (new IssueSummary ());
			
			var probe = new IssueSummary ();
			group.AddIssue (probe);
			Assert.IsTrue (((IIssueTreeNode)nextProvider.Group).Children.Contains (probe), "The issue was not added to the existing group.");
		}

		[Test]
		public void PassesIssueSummaryToNewGroup ()
		{
			nextProvider.Group = CreateSecondaryGroup ();
			
			var probe = new IssueSummary ();
			group.AddIssue (probe);
			Assert.IsTrue (((IIssueTreeNode)nextProvider.Group).Children.Contains (probe), "The issue was not added to the new group.");
		}

		[Test]
		public void PassesIssueSummaryToExistingGroupDuringEnableProcessing ()
		{
			var disabledGroup = new IssueGroup (nextProvider, "sut");
			// "prime" the tree of groups
			nextProvider.Group = CreateSecondaryGroup ();
			disabledGroup.AddIssue (new IssueSummary ());
			
			var probe = new IssueSummary ();
			disabledGroup.AddIssue (probe);
			disabledGroup.EnableProcessing ();
			var issues = ((IIssueTreeNode)nextProvider.Group).Children;
			Assert.IsTrue (issues.Contains (probe), "The issue was not added to the existing group.");
		}

		[Test]
		public void PassesIssueSummaryToNewGroupDuringEnableProcessing ()
		{
			var disabledGroup = new IssueGroup (nextProvider, "sut");
			// "prime" the tree of groups
			nextProvider.Group = CreateSecondaryGroup ();
			
			var probe = new IssueSummary ();
			disabledGroup.AddIssue (probe);
			disabledGroup.EnableProcessing ();
			var issues = ((IIssueTreeNode)nextProvider.Group).Children;
			Assert.IsTrue (issues.Contains (probe), "The issue was not added to the new group.");
		}

		[Test]
		public void ClearStatisticsTest ()
		{
			group.AddIssue (new IssueSummary ());
			
			Assert.AreEqual (1, group.IssueCount, "Incorrect issue count.");
			
			group.ClearStatistics ();
			
			Assert.AreEqual (0, group.IssueCount, "Incorrect issue count after reset.");
			Assert.IsTrue (nextProvider.ResetCalled, "Reset was not called on the provider");
		}

		[Test]
		public void DoesNotInteractIfGroupingDisabled ()
		{
			var disabledGroup = new IssueGroup (nextProvider, "disabled group");
			
			disabledGroup.AddIssue (new IssueSummary ());
			Assert.IsFalse (nextProvider.GetIssueGroupCalled, "The provider should not be called by a disabled group.");
		}

		[Test]
		public void ChildrenInvalidatedCalledWhenNextProviderChanges ()
		{
			bool eventCalled = false;
			node.ChildrenInvalidated += (sender, eventArgs) => {
				eventCalled = true;
			};
			group.GroupingProvider = new MockGroupingProvider ();
			Assert.IsTrue (eventCalled, "The event was not called.");
		}
		
		[Test]
		public void NoEventsCalledForIssuesAddedBeforeProcessingEnabled ()
		{
			var localGroup = new IssueGroup (null, "sut");
			IIssueTreeNode localNode = localGroup;
			localNode.ChildAdded += Forbidden<IssueTreeNodeEventArgs> ("node.ChildAdded");
			var issue = new IssueSummary ();
			localGroup.AddIssue (issue);

			var children = localNode.Children;
			Assert.That (children.Contains (issue));
			Assert.AreEqual (1, children.Count, "The number of children was incorrect.");
		}

		[Test]
		public void LeavesFilteredCorrectly ()
		{
			var leafGroup = new IssueGroup (NullGroupingProvider.Instance, "sut");
			IIssueTreeNode leafNode = leafGroup;
			var issue = new IssueSummary ();
			leafGroup.AddIssue (issue);
			
			var children = leafNode.Children;
			Assert.That (children.Contains (issue));
			Assert.AreEqual (1, children.Count, "The group contained too many issues.");
		}
	}
}

