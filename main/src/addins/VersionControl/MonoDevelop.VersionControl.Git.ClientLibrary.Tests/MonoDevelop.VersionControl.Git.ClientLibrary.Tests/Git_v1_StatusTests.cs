//
// Git_v1_StatusTests.cs
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
	public class Git_v1_StatusTests
	{
		[TestCase]
		public void ParsePathTests ()
		{
			string test = "AM test/path\0AM test2/path2\0";

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v1);
			handler.OnOutput (test);
			var dict = handler.FileList;
			Assert.IsTrue (dict.ContainsKey ("test/path"));
			Assert.IsTrue (dict.ContainsKey ("test2/path2"));
		}

		[TestCase]
		public void ParsePathTests_noPaths ()
		{
			var test = "AM \0AM test2/path2\0";
			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v1);
			handler.OnOutput (test);
			var dict = handler.FileList;
			Assert.IsTrue (dict.ContainsKey (""));
			Assert.IsTrue (dict.ContainsKey ("test2/path2"));
		}

		[TestCase]
		public void ParseOrigPathTests ()
		{
			string test = "AM test/path\0AM test3/path3 -> test2/path2\0";

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v1);
			handler.OnOutput (test);
			var dict = handler.FileList;
			Assert.IsTrue (dict.ContainsKey ("test/path"));
			Assert.IsTrue (dict.ContainsKey ("test2/path2"));
			var state = dict ["test2/path2"];
			Assert.AreEqual ("test3/path3", state.OriginalPath);
		}
	}
}
