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

namespace MonoDevelop.NUnit.External
{
	class TcpTestListener
	{
		TcpListener TcpListener {
			get; set;
		}

		public int Port {
			get { return ((IPEndPoint) TcpListener.LocalEndpoint).Port; }
		}


		public TcpTestListener (IRemoteEventListener listener)
		{
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

							Gtk.Application.Invoke (delegate {
								var testName = element.Attribute ("name").Value;
								if (element.Name.LocalName == "suite-started") {
									listener.SuiteStarted (testName);
								} else if (element.Name.LocalName == "test-started") {
									listener.TestStarted (testName);
								} else if (element.Name.LocalName == "test-finished") {
									listener.TestFinished (testName, CreateResult (element));
								} else if (element.Name.LocalName == "suite-finished") {
									listener.SuiteFinished (testName, CreateResult (element));
								}
							});
						}
					}
				} catch {

				} finally {
					TcpListener.Stop ();
				}
			});
		}

		UnitTestResult CreateResult (XElement element)
		{
			var result = (ResultStatus)Enum.Parse (typeof(ResultStatus), element.Attribute ("result").Value);
			var passed = int.Parse (element.Attribute ("passed").Value);
			var failures = int.Parse (element.Attribute ("failures").Value);
			var ignored = int.Parse (element.Attribute ("ignored").Value);
			var inconclusive = int.Parse (element.Attribute ("inconclusive").Value);

			return new UnitTestResult {
				Status = result,
				Passed = passed,
				Failures = failures,
				Ignored = ignored,
				Inconclusive = inconclusive
			};
		}
	}
}

