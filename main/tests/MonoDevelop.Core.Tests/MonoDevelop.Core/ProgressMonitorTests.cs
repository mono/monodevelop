//
// ProgressMonitorTests.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Collections.Generic;
using MonoDevelop.Projects;
using NUnit.Framework;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Core
{
	[TestFixture]
	public class ProgressMonitorTests
	{
		[Test]
		public void TestReportObject ()
		{
			var m = new ReportObjectMonitor ();
			var foo = new object ();
			m.ReportObject (foo);
			Assert.IsTrue (m.ReportedObjects.Contains (foo));
		}

		[Test]
		public void TestReportObjectToFollowers ()
		{
			var main = new ProgressMonitor ();
			var m = new ReportObjectMonitor ();
			var agr = new AggregatedProgressMonitor (main, m);
			var foo = new object ();
			agr.ReportObject (foo);
			Assert.IsTrue (m.ReportedObjects.Contains (foo));
		}

		[Test]
		public void TestLogObject ()
		{
			var m = new ReportObjectMonitor ();
			var foo = new object ();
			m.Log.Write ("one");
			m.LogObject (foo);
			m.Log.Write ("two");
			Assert.AreEqual (3, m.LoggedObjects.Count);
			Assert.AreEqual ("one", m.LoggedObjects [0]);
			Assert.AreSame (foo, m.LoggedObjects [1]);
			Assert.AreEqual ("two", m.LoggedObjects [2]);
		}

		[Test]
		public void TestLogObjectToFollowers ()
		{
			var main = new ProgressMonitor ();
			var m = new ReportObjectMonitor ();
			var agr = new AggregatedProgressMonitor (main, m);
			var foo = new object ();
			agr.Log.Write ("one");
			agr.LogObject (foo);
			agr.Log.Write ("two");
			Assert.AreEqual (3, m.LoggedObjects.Count);
			Assert.AreEqual ("one", m.LoggedObjects [0]);
			Assert.AreSame (foo, m.LoggedObjects [1]);
			Assert.AreEqual ("two", m.LoggedObjects [2]);
		}

		[Test]
		public void TestLogObjectSplit ()
		{
			var m = new ReportObjectMonitor ();
			m.BeginTask (2);
			var p1 = m.BeginAsyncStep (1);
			var p2 = m.BeginAsyncStep (1);

			var foo = new object ();
			p2.LogObject (foo);
			p2.Log.Write ("two");
			p1.Log.Write ("one");

			p1.Dispose ();
			p2.Dispose ();

			Assert.AreEqual (3, m.LoggedObjects.Count);
			Assert.AreEqual ("one", m.LoggedObjects [0]);
			Assert.AreSame (foo, m.LoggedObjects [1]);
			Assert.AreEqual ("two", m.LoggedObjects [2]);
		}
	}

	class ReportObjectMonitor: ProgressMonitor
	{
		public List<object> ReportedObjects = new List<object> ();

		public List<object> LoggedObjects = new List<object> ();

        protected override void OnObjectReported(object statusObject)
        {
			ReportedObjects.Add (statusObject);
			base.OnObjectReported(statusObject);
        }

        protected override void OnWriteLog(string message)
        {
			LoggedObjects.Add (message);
			base.OnWriteLog(message);
        }

        protected override void OnWriteLogObject(object logObject)
        {
			LoggedObjects.Add (logObject);
			base.OnWriteLogObject(logObject);
        }
    }
}
