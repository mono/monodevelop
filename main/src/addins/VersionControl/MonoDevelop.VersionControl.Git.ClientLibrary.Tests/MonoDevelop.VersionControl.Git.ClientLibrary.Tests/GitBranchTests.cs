//
// GitBranchTests.cs
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
using System.IO;
using System.Diagnostics;
using System.Linq;

namespace MonoDevelop.VersionControl.Git.ClientLibrary.Tests
{
	public class GitTestBase
	{
		protected static async Task RunTest (Func<string, Task> action)
		{
			var path = TestUtil.CreateTempDirectory ();
			try {
				var result = await GitInit.InitAsync (path);
				Assert.IsTrue (result.Success, "git init failed:" + result.ErrorMessage);
				await action (path);
			} finally {
				if (Directory.Exists (path))
					Directory.Delete (path, true);
			}
		}
	}

	[TestFixture]
	public class GitBranchTests : GitTestBase
	{
		[Test]
		public Task GetCurrentBranchTest ()
		{
			return RunTest (async root => {
				var currentBranch = await BranchUtil.GetCurrentBranchAsync (root);
				Assert.AreEqual ("master", currentBranch.Name);
			});
		}

		[Test]
		public Task CreateTagTest ()
		{
			return RunTest (async root => {
				Setup (root);
				var result = await BranchUtil.CreateNewTagAsync (root, "newTag");
				Assert.IsTrue (result.Success, result.ErrorMessage);
			});
		}

		[Test]
		public Task CreateExistingTagTest ()
		{
			return RunTest (async root => {
				Setup (root);
				var result = await BranchUtil.CreateNewTagAsync (root, "newTag");
				Assert.IsTrue (result.Success, result.ErrorMessage);

				result = await BranchUtil.CreateNewTagAsync (root, "newTag");
				Assert.IsFalse (result.Success, "has to fail.");
			});
		}

		[Test]
		public Task DeleteTagTest ()
		{
			return RunTest (async root => {
				Setup (root);
				var result = await BranchUtil.CreateNewTagAsync (root, "newTag");
				Assert.IsTrue (result.Success, result.ErrorMessage);

				result = await BranchUtil.DeleteTagAsync (root, "newTag");
				Assert.IsTrue (result.Success, result.ErrorMessage);
			});
		}

		[Test]
		public Task DeleteNonExistingTagTest ()
		{
			return RunTest (async root => {
				Setup (root);
				var result = await BranchUtil.DeleteTagAsync (root, "notExist");
				Assert.IsFalse (result.Success, result.ErrorMessage);
			});
		}

		[Test]
		public Task ListAllTagsTest ()
		{
			return RunTest (async root => {
				Setup (root);
				var result = await BranchUtil.CreateNewTagAsync (root, "newTag");
				Assert.IsTrue (result.Success, result.ErrorMessage);
				result = await BranchUtil.CreateNewTagAsync (root, "newTag2");
				Assert.IsTrue (result.Success, result.ErrorMessage);
				result = await BranchUtil.CreateNewTagAsync (root, "otherTag");
				Assert.IsTrue (result.Success, result.ErrorMessage);

				var allTags = await BranchUtil.GetAllTagsAsync (root);
				Assert.IsTrue (allTags.Count == 3);

				Assert.IsTrue (allTags.Single (t => t.Name == "newTag") != null);
				Assert.IsTrue (allTags.Single (t => t.Name == "newTag2") != null);
				Assert.IsTrue (allTags.Single (t => t.Name == "otherTag") != null);
			});
		}

		void Setup (string root)
		{
			var path = Path.Combine (root, "foo.txt");
			File.WriteAllText (path, "Test content.");
			var si = new ProcessStartInfo ("git") {
				WorkingDirectory = root,
				Arguments = "add foo.txt"
			};
			var p = Process.Start (si);
			p.WaitForExit ();
			Assert.AreEqual (0, p.ExitCode);

			si = new ProcessStartInfo ("git") {
				WorkingDirectory = root,
				Arguments = "commit -a -m 'first commit.'"
			};
			p = Process.Start (si);
			p.WaitForExit ();
			Assert.AreEqual (0, p.ExitCode);
		}
	}
}
