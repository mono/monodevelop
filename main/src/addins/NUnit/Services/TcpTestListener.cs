//
// TcpTestListener.cs
//
// Author:
//       Alan McGovern <alan.mcgovern@gmail.com>
//
// Copyright (c) 2013 Alan McGovern
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
using System.Net;
using System.Net.Sockets;
using MonoDevelop.NUnit.External;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;

namespace MonoDevelop.NUnit.External
{
	class TcpTestListener : IDisposable
	{
		string testSuiteName;
		string rootTestName;

		public bool HasReceivedConnection {
			get; private set;
		}

		List<Tuple<string,UnitTestResult>> suiteStack = new List<Tuple<string, UnitTestResult>> ();
		IRemoteEventListener listener;

		TcpListener TcpListener {
			get; set;
		}

		public int Port {
			get { return ((IPEndPoint) TcpListener.LocalEndpoint).Port; }
		}


		public TcpTestListener (IRemoteEventListener listener, string suiteName)
		{
			this.testSuiteName = suiteName;
			this.listener = listener;
			bool rootSuiteStarted = false;

			TcpListener = new TcpListener (new IPEndPoint (IPAddress.Loopback, 0));
			TcpListener.Start ();
			Task.Factory.StartNew (() => {
				try {
					using (var client = TcpListener.AcceptTcpClient ())
					using (var socketStream = client.GetStream ())
					using (var reader = new StreamReader (socketStream, Encoding.UTF8)) {

						string line = null;
						while ((line = reader.ReadLine ()) != null) {
							var element = XElement.Parse (line);
							string testName = element.Attribute ("name").Value;
							var action = element.Name.LocalName;

							if (testSuiteName.Length == 0 && !rootSuiteStarted) {
								// Running the whole assembly
								rootTestName = testName;
								rootSuiteStarted = true;
								continue;
							}
							if (testSuiteName == testName && !rootSuiteStarted) {
								// Running a test suite
								rootTestName = testName;
								rootSuiteStarted = true;
								listener.SuiteStarted ("<root>");
								continue;
							}

							if (!rootSuiteStarted)
								continue;

							switch (action) {
							case "suite-started":
								UpdateTestSuiteStatus (testName, false); break;
							case "test-started":
								UpdateTestSuiteStatus (testName, true);
								listener.TestStarted (testName); break;
							case "test-finished":
								var res = CreateResult (element);
								AddTestResult (res);
								listener.TestFinished (testName, res); break;
							case "suite-finished":
								if (testName == rootTestName) {
									FinishSuites (0);
									listener.SuiteFinished ("<root>", CreateResult (element));
									rootSuiteStarted = false;
								}
								break;
							}
						}
					}
				} catch (Exception ex) {
					LoggingService.LogError ("Exception in test listener", ex);
				} finally {
					TcpListener.Stop ();
				}
			});
		}

		public void Dispose ()
		{
			TcpListener.Stop ();
		}

		void UpdateTestSuiteStatus (string name, bool isTest)
		{
			if (testSuiteName.Length > 0)
				name = name.Substring (testSuiteName.Length + 1);
			string[] parts = name.Split ('.');
			int len = isTest ? parts.Length - 1 : parts.Length;
			for (int n = 0; n < len; n++) {
				if (n >= suiteStack.Count) {
					StartSuite (parts[n]);
				} else if (parts [n] != suiteStack [n].Item1) {
					FinishSuites (n);
					StartSuite (parts[n]);
				}
			}
		}

		void FinishSuites (int stackLevel)
		{
			if (stackLevel + 1 < suiteStack.Count)
				FinishSuites (stackLevel + 1);

			if (stackLevel >= suiteStack.Count)
				return;

			var tname = GetTestSuiteName (stackLevel);
			var res = suiteStack [stackLevel].Item2;

			suiteStack.RemoveAt (stackLevel);

			listener.SuiteFinished (tname, res);
		}

		void StartSuite (string name)
		{
			suiteStack.Add (new Tuple<string, UnitTestResult> (name, new UnitTestResult ()));
			name = GetTestSuiteName (suiteStack.Count - 1);
			listener.SuiteStarted (name);
		}

		void AddTestResult (UnitTestResult res)
		{
			foreach (var r in suiteStack)
				r.Item2.Add (res);
		}

		string GetTestSuiteName (int stackLevel)
		{
			var info = suiteStack [stackLevel];

			string name;
			if (stackLevel > 0) {
				var prefix = string.Join (".", suiteStack.Select (s => s.Item1).Take (stackLevel));
				name = prefix + "." + info.Item1;
			} else
				name = info.Item1;

			if (testSuiteName.Length > 0)
				name = testSuiteName + "." + name;
			return name;
		}

		UnitTestResult CreateResult (XElement element)
		{
			var result = (ResultStatus)Enum.Parse (typeof(ResultStatus), element.Attribute ("result").Value);
			var passed = int.Parse (element.Attribute ("passed").Value);
			var failures = int.Parse (element.Attribute ("failures").Value);
			var ignored = int.Parse (element.Attribute ("ignored").Value);
			var inconclusive = int.Parse (element.Attribute ("inconclusive").Value);

			var message = (string)element.Attribute ("message");
			var stackTrace = (string)element.Attribute ("stack-trace");

			return new UnitTestResult {
				Status = result,
				Passed = passed,
				Failures = failures,
				Ignored = ignored,
				Inconclusive = inconclusive,
				Message = message,
				StackTrace = stackTrace
			};
		}
	}
}

