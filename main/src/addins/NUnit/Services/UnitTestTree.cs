//
// UnitTestTree.cs
//
// Author:
//       Sergey Khabibullin <sergey@khabibullin.com>
//
// Copyright (c) 2014 Sergey Khabibullin
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
//

using System;
using MonoDevelop.Projects;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace MonoDevelop.NUnit
{
	public class UnitTestTree: UnitTestTreeBranchNode, ITestDiscoverySink
	{
		readonly IWorkspaceObject owner;
		readonly ITestDiscoverer discoverer;

		/*public override string Title {
			get {
				return string.Format ("{0} ({1})", Name, Tag);
			}
		}*/

		public UnitTestTree (IWorkspaceObject owner, string tag, List<TestCaseWrapper> wrappers, ITestExecutionDispatcher dispatcher)
			: base (String.Format("{0} ({1})", owner.Name, tag), tag, Prepare(wrappers), 0, dispatcher)
		{
			this.owner = owner;
		}

		public UnitTestTree (IWorkspaceObject owner, string tag, ITestDiscoverer discoverer, ITestExecutionDispatcher dispatcher)
			: this (owner, tag, new List<TestCaseWrapper>(), dispatcher)
		{
			this.discoverer = discoverer;
		}

		protected override void OnCreateTests ()
		{
			if (discoverer != null) {
				wrappers.Clear ();
				discoverer.Discover (owner, null, this);
				Prepare (wrappers);
			}

			base.OnCreateTests ();
		}

		void ITestDiscoverySink.SendTest (TestCase testCase)
		{
			wrappers.Add (new TestCaseWrapper (testCase));
		}

		static List<TestCaseWrapper> Prepare(List<TestCaseWrapper> wrappers)
		{
			wrappers.Sort ();
			// TODO: check that the list is a valid tree
			return wrappers;
		}
	}

	public class UnitTestTreeBranchNode: UnitTestGroup
	{
		readonly string tag;
		readonly ITestExecutionDispatcher dispatcher;
		readonly int level;
		protected List<TestCaseWrapper> wrappers;

		public string Tag {
			get {
				return tag;
			}
		}

		public UnitTestTreeBranchNode (string name, string tag, List<TestCaseWrapper> wrappers, int level,
			ITestExecutionDispatcher dispatcher) : base (name)
		{
			this.dispatcher = dispatcher;
			this.wrappers = wrappers;
			this.level = level;
			this.tag = tag;
		}

		protected override void OnCreateTests ()
		{
			// build tree using 'name parts' array
			var groups = wrappers.GroupBy (d => d.NameParts [level]);
			Tests.Clear ();

			foreach (var group in groups) {
				var list = new List<TestCaseWrapper> (group);
				// we can safely assume that all items at group
				// have the same NameParts elements up to current level
				// so we will just take the first one
				var arr = list [0].NameParts;

				UnitTest test;
				// if the group has only one element and it does not have
				// children then the test is a leaf node otherwise it's a branch
				if (list.Count == 1 && arr.Length == level + 1)
					test = new UnitTestTreeLeafNode (arr [level], tag, list, dispatcher);
				else
					test = new UnitTestTreeBranchNode (arr [level], tag, list, level + 1, dispatcher);

				Tests.Add (test);
			}
		}

		int childrenStarted;
		int childrenFinished;
		UnitTestResult combinedResult;

		void ResetExecutionInfo ()
		{
			childrenStarted = 0;
			childrenFinished = 0;
			combinedResult = new UnitTestResult();
		}

		public void OnChildTestStart (UnitTest unitTest, TestContext context, UnitTest origin)
		{
			// event chain should not go beyond the test that started the execution
			if (origin != this && childrenStarted == 0) {
				// we should notify the parent test before the monitor
				var parent = Parent as UnitTestTreeBranchNode;
				if (parent != null) {
					parent.OnChildTestStart (this, context, origin);
				}

				context.Monitor.BeginTest (this);
				Status = TestStatus.Running;
			}

			childrenStarted++;
		}

		public void OnChildTestResult (UnitTest unitTest, UnitTestResult result, TestContext context, UnitTest origin)
		{
			childrenFinished++;
			combinedResult.Add (result);

			// event chain should not go beyond the test that started the execution
			if (origin != this && childrenFinished == Tests.Count) {
				// if every child test has finished then this test has finished too
				context.Monitor.EndTest (this, combinedResult);
				Status = TestStatus.Ready;
				RegisterResult (context, combinedResult);

				// notify the parent test
				var parent = Parent as UnitTestTreeBranchNode;
				if (parent != null) {
					parent.OnChildTestResult (this, combinedResult, context, origin);
				}
			}
		}

		class ChildTestsSession: ITestExecutionHandler
		{
			readonly TestContext context;
			readonly UnitTest origin;

			Dictionary<string, UnitTestTreeLeafNode> owners = new Dictionary<string, UnitTestTreeLeafNode> ();
			Dictionary<string, UnitTestResult> results = new Dictionary<string, UnitTestResult> ();

			public ChildTestsSession (UnitTestTreeBranchNode test, TestContext context)
			{
				this.context = context;
				this.origin = test;
				test.ResetExecutionInfo ();
				CollectTestCases (test.Tests);
			}

			void CollectTestCases (UnitTestCollection tests)
			{
				foreach (var test in tests) {
					if (test is UnitTestTreeBranchNode) {
						var branch = (UnitTestTreeBranchNode)test;
						branch.ResetExecutionInfo ();
						CollectTestCases (branch.Tests);
					} else {
						var unitTest = ((UnitTestTreeLeafNode)test);
						var testCase = unitTest.TestCase;
						owners.Add (testCase.Name, unitTest);
					}
				}
			}

			void ITestExecutionHandler.RecordStart (TestCase testCase)
			{
				var unitTest = owners [testCase.Name];
				// notify the parent before starting the test
				unitTest.MessageParentAboutStart (context, origin);
				context.Monitor.BeginTest (unitTest);
				if (!results.ContainsKey (testCase.Name))
					results.Add (testCase.Name, new UnitTestResult ());
			}

			void ITestExecutionHandler.RecordResult (TestCaseResult testCaseResult)
			{
				var unitTest = owners [testCaseResult.TestCase.Name];
				var unitTestResult = (UnitTestResult)testCaseResult;
				results [testCaseResult.TestCase.Name].Add (unitTestResult);
				context.Monitor.EndTest (unitTest, unitTestResult);
			}

			void ITestExecutionHandler.RecordEnd (TestCase testCase)
			{
				var unitTest = owners [testCase.Name];
				var unitTestResult = results [testCase.Name];
				// end the test, only then notify the parent
				unitTest.RegisterResult (context, unitTestResult);
				//context.Monitor.EndTest (unitTest, unitTestResult);
				results.Remove (testCase.Name);
				owners.Remove (testCase.Name);
				unitTest.MessageParentAboutResult (unitTestResult, context, origin);
			}

			public void Run (ITestExecutionDispatcher dispatcher)
			{
				dispatcher.DispatchExecution (owners.Select (o => o.Value.TestCase).ToList (), context, this);
			}

			public void HandleNotEndedTests ()
			{
				foreach (var pair in owners) {
					var unitTest = pair.Value;
					if (unitTest.Status != TestStatus.Running) {
						// notify the parent before starting the test
						unitTest.MessageParentAboutStart (context, origin);
						context.Monitor.BeginTest (unitTest);
					}

					var unitTestResult = new UnitTestResult ();

					// end the test, only then notify the parent
					unitTest.RegisterResult (context, unitTestResult);
					context.Monitor.EndTest (unitTest, unitTestResult);
					unitTest.MessageParentAboutResult (unitTestResult, context, origin);
				}
			}

		}

		protected override UnitTestResult OnRun (TestContext testContext)
		{
			OnBeginTest (testContext);

			ChildTestsSession session = new ChildTestsSession (this, testContext);
			try {
				session.Run (dispatcher);
			} finally {
				session.HandleNotEndedTests ();
				OnEndTest (testContext);
			}

			return combinedResult;
		}
	}

	public class UnitTestTreeLeafNode: UnitTest, ITestExecutionHandler
	{
		readonly ITestExecutionDispatcher dispatcher;
		readonly TestCase testCase;
		readonly string tag;

		public string Tag {
			get {
				return tag;
			}
		}

		public TestCase TestCase {
			get {
				return testCase;
			}
		}

		public UnitTestTreeLeafNode (string name, string tag, List<TestCaseWrapper> wrappers, ITestExecutionDispatcher dispatcher)
			: base (name)
		{
			this.dispatcher = dispatcher;
			this.testCase = wrappers [0].TestCase;
			this.tag = tag;
		}

		public void MessageParentAboutStart (TestContext context, UnitTest origin)
		{
			Status = TestStatus.Running;
			((UnitTestTreeBranchNode)Parent).OnChildTestStart (this, context, origin);
		}

		public void MessageParentAboutResult (UnitTestResult result, TestContext context, UnitTest origin)
		{
			Status = TestStatus.Ready;
			((UnitTestTreeBranchNode)Parent).OnChildTestResult (this, result, context, origin);
		}

		UnitTestResult unitTestResult;

		void ITestExecutionHandler.RecordStart (TestCase testCase)
		{
		}

		void ITestExecutionHandler.RecordResult (TestCaseResult testCaseResult)
		{
			unitTestResult.Add((UnitTestResult)testCaseResult);
		}

		void ITestExecutionHandler.RecordEnd (TestCase testCase)
		{
		}

		protected override UnitTestResult OnRun (TestContext context)
		{
			unitTestResult = new UnitTestResult ();
			dispatcher.DispatchExecution (new TestCase[] { testCase }, context, this);
			UnitTestResult result = unitTestResult;
			unitTestResult = null;
			RegisterResult (context, result);
			return result;
		}
	}
}

