//
// GitProcessTests.cs
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
using System.Threading.Tasks;
using NUnit.Framework.Internal;

namespace MonoDevelop.VersionControl.Git.ClientLibrary.Tests
{
	[TestFixture]
	public class GitProcessTests
	{
		[TestCase ("error: Error")]
		[TestCase ("    error: Error")]
		public void IsErrorTests (string errorLine)
		{
			Assert.IsTrue (GitProcess.IsError (errorLine));
		}

		[TestCase ("")]
		[TestCase ("    ")]
		[TestCase ("some line…")]
		public void IsNoErrorTests (string errorLine)
		{
			Assert.IsFalse (GitProcess.IsError (errorLine));
		}

		[TestCase]
		public void TestStartRootPathNullCheck ()
		{
			try {
				new GitProcess ().StartAsync (null, new GitCallbackHandler (), false);
			} catch (ArgumentNullException e) {
				Assert.AreEqual ("arguments", e.ParamName);
				return;
			}
			Assert.Fail ("No exception thrown.");
		}

		[TestCase]
		public void TestGitCallbackHandlerNullCheck ()
		{
			try {
				var args = new GitArguments (".");
				args.AddArgument ("Foo");
				new GitProcess ().StartAsync (args, null, false);
			} catch (ArgumentNullException e) {
				Assert.AreEqual ("callbackHandler", e.ParamName);
				return;
			}
			Assert.Fail ("No exception thrown.");
		}

		[TestCase]
		public async Task TestGitStartupFail ()
		{
			var args = new GitArguments (".");
			args.AddArgument ("needs to fail");
			var result = await new GitProcess ().StartAsync (args, new GitCallbackHandler (), false);
			Assert.AreEqual (1, result.ExitCode);
		}
	}
}
