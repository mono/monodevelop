//
// GitAskPassTests.cs
//
// Author:
//       Mike Krüger <mikkrg@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation. All rights reserved.
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
using GitAskPass;
using System.IO;
using System.Diagnostics;
using MonoDevelop.VersionControl.Git.ClientLibrary;
using System.Threading;
using System.IO.Pipes;

namespace MonoDevelop.VersionControl.Git.AskPass
{
	[TestFixture]
	public class GitAskPassTests
	{
		string Start (string argument, Action<StreamStringReadWriter> callback, int exitCode = 0)
		{
			var passApp = Path.Combine (Path.GetDirectoryName (typeof (GitProcess).Assembly.Location), "../AddIns/VersionControl/GitAskPass");
			var startInfo = new ProcessStartInfo (passApp);
			startInfo.RedirectStandardOutput = true;
			startInfo.UseShellExecute = false;
			startInfo.Arguments = "\"" + argument + "\"";
			Console.WriteLine (startInfo.Arguments);
			var pipe = "testPipe" + Thread.CurrentThread.ManagedThreadId;
			startInfo.EnvironmentVariables.Add ("MONODEVELOP_GIT_ASKPASS_PIPE", pipe);
			using (var server = new NamedPipeServerStream (pipe)) {
				server.BeginWaitForConnection (stream => {
					callback (new StreamStringReadWriter (server));
				}, server);
				if (!File.Exists (passApp))
					Assert.Fail ("GitAskPass not found " + passApp);
				var process = Process.Start (startInfo);
				process.WaitForExit (5000);
				server.Close ();

				Assert.AreEqual (exitCode, process.ExitCode);
				return process.StandardOutput.ReadToEnd ().TrimEnd ();
			}
		}

		[Test]
		public void TestUsernameRequest ()
		{
			var result = Start ("Username for 'fooBar':", stream => {
				Assert.AreEqual ("Username", stream.ReadLine ());
				Assert.AreEqual ("fooBar", stream.ReadLine ());
				stream.WriteLine ("test_user");
			});
			Assert.AreEqual ("test_user", result);
		}

		[Test]
		public void TestPasswordRequest ()
		{
			var result = Start ("Password for 'fooBar':", stream => {
				Assert.AreEqual ("Password", stream.ReadLine ());
				Assert.AreEqual ("fooBar", stream.ReadLine ());
				stream.WriteLine ("test_pass");
			});
			Assert.AreEqual ("test_pass", result);
		}

		[Test]
		public void TestContinueConnectingMessage ()
		{
			var result = Start ("Are you sure you want to continue connecting (yes/no)?", stream => {
				Assert.AreEqual ("Continue connecting", stream.ReadLine ());
				stream.WriteLine ("yes");

			});
			Assert.AreEqual ("yes", result);
		}

		[Test]
		public void TestSshPassword ()
		{
			var result = Start ("Foo Bar's password:", stream => {
				Assert.AreEqual ("SSHPassword", stream.ReadLine ());
				stream.WriteLine ("Foo Bar");

			});
			Assert.AreEqual ("Foo Bar", result);
		}

		[Test]
		public void TestKeyPassphrase ()
		{
			var result = Start ("Enter passphrase for key 'FooBar':", stream => {
				Assert.AreEqual ("SSHPassPhrase", stream.ReadLine ());
				stream.WriteLine ("FooBar");

			});
			Assert.AreEqual ("FooBar", result);
		}

		[Test]
		public void TestError ()
		{
			var result = Start ("Some gibberish text.", stream => {
				Assert.AreEqual ("Error", stream.ReadLine ());
				stream.WriteLine ("Some gibberish text.");
			}, exitCode: 2);
		}
	}
}


