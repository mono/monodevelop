//
// ExtensibleTestProvider.cs
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
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Projects;

namespace MonoDevelop.NUnit
{
	public abstract class ExtensibleTestProvider: ITestProvider, ITestExecutionDispatcher
	{
		Dictionary<string, ITestDiscoverer> testDiscoverers = new Dictionary<string, ITestDiscoverer> ();

		Dictionary<string, ITestExecutor> testExecutors = new Dictionary<string, ITestExecutor> ();

		public void RegisterTestDiscoverer (string id, ITestDiscoverer discoverer)
		{
			testDiscoverers.Add (id, discoverer);
		}

		public void UnregisterTestDiscoverer (string id)
		{
			testDiscoverers.Remove (id);
		}

		public void RegisterTestExecutor (string id, ITestExecutor executor)
		{
			testExecutors.Add (id, executor);
		}

		public void UnregisterTestExecutor (string id)
		{
			testExecutors.Remove (id);
		}

		class DiscoverySink: ITestDiscoverySink
		{
			readonly List<TestCaseDecorator> testCases;

			readonly string discovererId;

			public DiscoverySink (List<TestCaseDecorator> testCases, string discovererId)
			{
				this.testCases = testCases;
				this.discovererId = discovererId;
			}

			public void SendTestCase(TestCase testCase)
			{
				testCases.Add (new TestCaseDecorator(testCase) { DiscovererId = discovererId });
			}
		}

		public virtual UnitTest CreateUnitTest (IWorkspaceObject entry)
		{
			var testCases = new List<TestCaseDecorator> ();

			foreach (var item in testDiscoverers) {
				var discoverer = item.Value;
				var sink = new DiscoverySink (testCases, item.Key);
				discoverer.Discover (entry, null, sink);
			}

			if (testCases.Count > 0)
				return new UnitTestTree(entry, this, testCases);
			else
				return null;
		}

		void ITestExecutionDispatcher.DispatchExecution (IEnumerable<TestCaseDecorator> decorators,
			TestContext context, ITestExecutionHandler handler)
		{
			// dispatch tests to corresponding executors
			var groups = decorators.GroupBy (d => d.DiscovererId, d => d.DecoratedTestCase);

			foreach (var group in groups) {
				var testExecutor = testExecutors[group.Key];
				testExecutor.Execute (group, null, handler);
			}
		}

		public virtual Type[] GetOptionTypes ()
		{
			return new Type[0];
		}

	}
}

