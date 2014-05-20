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
	public class UnitTestTree: UnitTestTreeBranchNode
	{
		public UnitTestTree (IWorkspaceObject owner, ITestExecutionDispatcher dispatcher, List<TestCaseDecorator> decorators)
			: base (owner.Name, dispatcher, Prepare(decorators), 0)
		{
		}

		public UnitTestTree (string name, IWorkspaceObject ownerSolutionItem)
			: base (name, ownerSolutionItem)
		{
		}

		static List<TestCaseDecorator> Prepare(List<TestCaseDecorator> decorators)
		{
			if (decorators.Count < 1)
				throw new ArgumentException ("TestCase list cannot be empty");

			decorators.Sort ();
			// TODO: check that the list is a valid tree
			return decorators;
		}
	}

	public class UnitTestTreeBranchNode: UnitTestGroup
	{
		public ITestExecutionDispatcher Dispatcher { get; set; }

		public UnitTestTreeBranchNode (string name, ITestExecutionDispatcher dispatcher, List<TestCaseDecorator> decorator,
			int level) : base (name)
		{
			this.Dispatcher = dispatcher;
			// build tree using 'name parts' array
			var groups = decorator.GroupBy (d => d.NameParts [level]);

			foreach (var group in groups) {
				var list = new List<TestCaseDecorator> (group);
				// we can safely assume that all items at group
				// have the same NameParts elements up to current level
				// so we will just take the first one
				var arr = list [0].NameParts;

				UnitTest test;
				// if the group has only one element and it does not have
				// children then the test is a leaf node otherwise it's a branch
				if (list.Count == 1 && arr.Length == level + 1)
					test = new UnitTestTreeLeafNode (arr [level], dispatcher, list);
				else
					test = new UnitTestTreeBranchNode (arr [level], dispatcher, list, level + 1);

				test.SetParent (this);
				Tests.Add (test);
			}
		}

		public UnitTestTreeBranchNode (string name, IWorkspaceObject ownerSolutionItem)
			: base (name, ownerSolutionItem)
		{
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

		class ChildTestsSession : ITestExecutionHandler
		{
			readonly TestContext context;
			readonly UnitTest origin;

			HashSet<TestCaseDecorator> decorators = new HashSet<TestCaseDecorator>();

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
						decorators.Add (unitTest.TestCaseDecorator);
					}
				}
			}

			void ITestExecutionHandler.RecordStart (TestCase testCase)
			{
				var unitTest = testCase.Decorator.OwnerUnitTest;
				// notify the parent before starting the test
				unitTest.MessageParentAboutStart (context, origin);
				context.Monitor.BeginTest (unitTest);
			}

			void ITestExecutionHandler.RecordResult (TestCaseResult testCaseResult)
			{
				var unitTest = testCaseResult.TestCase.Decorator.OwnerUnitTest;
				var unitTestResult = (UnitTestResult)testCaseResult;
				// end the test, only then notify the parent
				unitTest.RegisterResult (context, unitTestResult);
				context.Monitor.EndTest (unitTest, unitTestResult);
				decorators.Remove (testCaseResult.TestCase.Decorator);
				unitTest.MessageParentAboutResult (unitTestResult, context, origin);
			}

			public void Run (ITestExecutionDispatcher dispatcher)
			{
				dispatcher.DispatchExecution (decorators, context, this);
			}

			public void HandleNotExecutedTests ()
			{
				foreach (var decorator in decorators) {
					var unitTest = decorator.OwnerUnitTest;
					if (unitTest.Status != TestStatus.Running) {
						// notify the parent before starting the test
						unitTest.MessageParentAboutStart (context, origin);
						context.Monitor.BeginTest (unitTest);
					}

					var unitTestResult = new UnitTestResult { Skipped = 1 };

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

			ChildTestsSession session = null;
			try {
				session = new ChildTestsSession(this, testContext);
				session.Run (Dispatcher);
			} finally {
				session.HandleNotExecutedTests ();
				OnEndTest (testContext);
			}

			return combinedResult;
		}
	}

	public class UnitTestTreeLeafNode: UnitTest, ITestExecutionHandler
	{
		readonly ITestExecutionDispatcher dispatcher;

		readonly TestCaseDecorator decorator;

		public TestCaseDecorator TestCaseDecorator {
			get {
				return decorator;
			}
		}

		public UnitTestTreeLeafNode (string name, ITestExecutionDispatcher dispatcher, List<TestCaseDecorator> decorators)
			: base (name)
		{
			this.dispatcher = dispatcher;
			this.decorator = decorators [0];
			this.decorator.OwnerUnitTest = this;
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
			unitTestResult = (UnitTestResult) testCaseResult;
		}

		protected override UnitTestResult OnRun (TestContext context)
		{
			dispatcher.DispatchExecution (new TestCaseDecorator[] { decorator }, context, this);
			UnitTestResult result = unitTestResult ?? new UnitTestResult { Skipped = 1 };
			unitTestResult = null;
			RegisterResult (context, result);
			return result;
		}
	}
}

