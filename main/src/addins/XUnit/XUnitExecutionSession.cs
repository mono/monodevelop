//
// XUnitExecutionSession.cs
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

namespace MonoDevelop.XUnit
{
	/// <summary>
	/// Execution session is used by executor to report results
	/// back to monitor. Session reports it's status to parent
	/// session when it changes.
	/// </summary>
	public class XUnitExecutionSession: IDisposable
	{
		UnitTest unitTest;
		UnitTestResult result;
		XUnitExecutionSession parentSession;
		List<XUnitExecutionSession> childSessions;
		int childSessionsStarted = 0;
		int childSessionsFinished = 0;
		bool isActive = false;

		public UnitTestResult Result {
			get {
				return result;
			}
		}

		public XUnitExecutionSession (UnitTest unitTest)
		{
			this.unitTest = unitTest;
			result = new UnitTestResult ();
			childSessions = new List<XUnitExecutionSession> ();
		}

		public void Begin (TestContext context)
		{
			if (!isActive) {
				// notify the parent session before starting the test itself
				if (parentSession != null)
					parentSession.OnChildSessionStarting (this, context);

				context.Monitor.BeginTest (unitTest);
				unitTest.Status = TestStatus.Running;

				isActive = true;
			}
		}

		public void End (TestContext context)
		{
			if (isActive) {
				context.Monitor.EndTest (unitTest, result);
				unitTest.Status = TestStatus.Ready;
				unitTest.RegisterResult (context, result);

				// notify the parent session after finishing the test itself
				if (parentSession != null)
					parentSession.OnChildSessionFinished (this, context);

				isActive = false;
			}
		}

		public void AddChildSession (XUnitExecutionSession childSession)
		{
			childSession.parentSession = this;
			childSessions.Add (childSession);
		}

		void OnChildSessionStarting (XUnitExecutionSession childSession, TestContext context)
		{
			// begin session if the first child session started
			if (++childSessionsStarted == 1)
				Begin (context);
		}

		void OnChildSessionFinished (XUnitExecutionSession childSession, TestContext context)
		{
			result.Add (childSession.result);
			// end session if the last child session finished
			if (++childSessionsFinished == childSessions.Count)
				End (context);
		}

		public void Dispose ()
		{
			foreach (var session in childSessions) {
				session.Dispose ();
			}
			childSessions = null;
		}
	}

	public interface IExecutableTest
	{
		XUnitExecutionSession CreateExecutionSession ();
	}
}

