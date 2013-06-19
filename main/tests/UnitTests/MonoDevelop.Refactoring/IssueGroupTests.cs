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
		MockGroupingProvider provider;

		Action<T> Forbidden<T> (string eventName)
		{
			return arg => {
				Assert.Fail ("The event '{0}' was not supposed to be invoked.", eventName);
			};
		}
		
		[SetUp]
		public void SetUp()
		{
			provider = new MockGroupingProvider ();
			group = new IssueGroup (provider, "sut");
		}
		
		[Test]
		public void CallsGroupAddedEventHandler()
		{
			provider.Group = new IssueGroup(null, "group to be added");
			provider.Group.ChildGroupAdded += Forbidden<IssueGroup> ("provider.Group.ChildGroupAdded");
			
			bool eventHandlerCalled = false;
			group.ChildGroupAdded += delegate {
				eventHandlerCalled = true;
			};
			group.IssueSummaryAdded += Forbidden<IssueSummary> ("group.IssueSummaryAdded");
			
			group.Push (new IssueSummary ());
			Assert.IsTrue (eventHandlerCalled, "The event handler was not called.");
		}
		
		[Test]
		public void PassesIssueSummaryToExistingGroup()
		{
			// "prime" the tree of groups
			provider.Group = new IssueGroup(null, "group to be added");
			group.Push (new IssueSummary ());
		
			provider.Group.ChildGroupAdded += Forbidden<IssueGroup> ("provider.Group.ChildGroupAdded");
			bool eventHandlerCalled = false;
			provider.Group.IssueSummaryAdded += delegate {
				eventHandlerCalled = true;
			};
			
			group.IssueSummaryAdded += Forbidden<IssueSummary> ("group.IssueSummaryAdded");
			group.ChildGroupAdded += Forbidden<IssueGroup> ("group.ChildGroupAdded");
			
			group.Push (new IssueSummary ());
			Assert.IsTrue (eventHandlerCalled, "The issue was not added to the new group.");
		}
		
		[Test]
		public void PassesIssueSummaryToNewGroup()
		{
			provider.Group = new IssueGroup(null, "group to be added");
			provider.Group.ChildGroupAdded += Forbidden<IssueGroup> ("provider.Group.ChildGroupAdded");
			bool eventHandlerCalled = false;
			provider.Group.IssueSummaryAdded += delegate {
				eventHandlerCalled = true;
			};
			
			group.IssueSummaryAdded += Forbidden<IssueSummary> ("group.IssueSummaryAdded");
			
			group.Push (new IssueSummary ());
			Assert.IsTrue (eventHandlerCalled, "The issue summary was not added to the new group.");
		}
		
		[Test]
		public void CallsIssueSummaryAddedEventHandler()
		{
			provider.Group = null;
			
			bool eventHandlerCalled = false;
			group.ChildGroupAdded += Forbidden<IssueGroup> ("group.ChildGroupAdded");
			group.IssueSummaryAdded += delegate {
				eventHandlerCalled = true;
			};
			
			group.Push (new IssueSummary ());
			Assert.IsTrue (eventHandlerCalled, "The event handler was not called.");
		}
		
		[Test]
		public void ClearStatisticsTest ()
		{
			group.Push (new IssueSummary ());
			
			Assert.AreEqual (1, group.IssueCount, "Incorrect issue count.");
			
			group.ClearStatistics ();
			
			Assert.AreEqual (0, group.IssueCount, "Incorrect issue count after reset.");
			Assert.IsTrue (provider.ResetCalled, "Reset was not called on the provider");

			provider.Group = new IssueGroup (null, "group to be added");
			provider.Group.ChildGroupAdded += Forbidden<IssueGroup> ("provider.Group.ChildGroupAdded");
			bool eventHandlerCalled = false;
			group.ChildGroupAdded += delegate {
				eventHandlerCalled = true;
			};
			group.IssueSummaryAdded += Forbidden<IssueSummary> ("group.IssueSummaryAdded");

			group.Push (new IssueSummary ());			
			
			Assert.IsTrue (eventHandlerCalled, "The ChildGroupAdded event handler was not called.");
		}
	}
}

