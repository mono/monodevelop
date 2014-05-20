//
// DumbDiscoverer.cs
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
using System;
using MonoDevelop.NUnit;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Dumb
{
	public class DumbDiscoverer : ProjectTestDiscoverer
	{
		static Random random = new Random();

		[TestDiscovery(Target.ProjectOutput, "exe", "dll")]
		public override void Discover (IEnumerable<string> files, ITestDiscoveryContext context, ITestDiscoverySink sink)
		{
			for (char i = 'a'; i <= 'j'; i++) {
				for (char j = 'a'; j <= 'j'; j++) {
					for (char k = 'a'; k <= 'z'; k++) {
						sink.SendTestCase (new TestCase (i+"."+j+"."+k));
					}
				}
			}
		}

	}

	public class DumbExecutor : ITestExecutor
	{
		static Random random = new Random();

		public void Execute (IEnumerable<TestCase> testCases, ITestExecutionContext context, ITestExecutionHandler handler)
		{
			var list = testCases.ToList ();
			foreach (var testCase in testCases) {
				if (random.Next (2) % 2 == 0) {
					handler.RecordStart (testCase);

					var result = new TestCaseResult (testCase);

					Thread.Sleep (5);
					if (random.Next (2) % 2 == 0) {
						result.Status = TestCaseResultStatus.Success;
					} else {
						result.Status = TestCaseResultStatus.Failure;
					}

					handler.RecordResult (result);
				}
			}
		}

	}
}

