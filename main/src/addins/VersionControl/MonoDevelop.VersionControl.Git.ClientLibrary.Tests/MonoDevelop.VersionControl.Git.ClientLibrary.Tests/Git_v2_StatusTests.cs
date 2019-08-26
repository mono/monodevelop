//
// Git_v2_StatusTests.cs
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

namespace MonoDevelop.VersionControl.Git.ClientLibrary.Tests
{
	public class Git_v2_StatusTests
	{
		[TestCase]
		public void ParseUntrackedItems ()
		{
			string test = "? /foo/bar.cs\0";

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v2);
			handler.OnOutput (test);
			var dict = handler.FileList;
			var state = dict ["/foo/bar.cs"];
			Assert.AreEqual (GitStatusCode.Untracked, state.StageState);
		}

		[TestCase]
		public void ParseIgnoredItems ()
		{
			string test = "! /foo/bar.cs\0";

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v2);
			handler.OnOutput (test);
			var dict = handler.FileList;
			var state = dict ["/foo/bar.cs"];
			Assert.AreEqual (GitStatusCode.Ignored, state.StageState);
		}

		[TestCase]
		public void ParseOrdinaryChanges ()
		{
			string test = "1 .M N... 100644 100700 100777 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 README.md\0";

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v2);
			handler.OnOutput (test);
			var dict = handler.FileList;
			var state = dict ["README.md"];
			Assert.AreEqual (GitStatusCode.WorktreeModified, state.StageState);
			Assert.AreEqual (GitSubmoduleState.NoSubmodule, state.SubmoduleState);
			Assert.AreEqual (Convert.ToInt32 ("100644", 8), state.FileMode_Head);
			Assert.AreEqual (Convert.ToInt32 ("100700", 8), state.FileMode_Index);
			Assert.AreEqual (Convert.ToInt32 ("100777", 8), state.FileMode_Worktree);
		}

		[TestCase]
		public void ParseRenamedChanges ()
		{
			string test = "2 R. N... 100644 100644 100644 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 R100 README2.md\0README.md\01 .M N... 100644 100700 100777 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 foo.cs\0";

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v2);
			handler.OnOutput (test);
			var dict = handler.FileList;
			var state = dict ["README2.md"];
			Assert.AreEqual (GitStatusCode.IndexRenamed, state.StageState);
			Assert.AreEqual (GitSubmoduleState.NoSubmodule, state.SubmoduleState);
			Assert.AreEqual (Convert.ToInt32 ("100644", 8), state.FileMode_Head);
			Assert.AreEqual (Convert.ToInt32 ("100644", 8), state.FileMode_Index);
			Assert.AreEqual (Convert.ToInt32 ("100644", 8), state.FileMode_Worktree);
			Assert.IsTrue (state.RenameOrCopyScore.IsRename);
			Assert.AreEqual (100, state.RenameOrCopyScore.Score);
			Assert.AreEqual ("README.md", state.OriginalPath);
			Assert.IsTrue (dict.ContainsKey ("foo.cs"));
		}

		[TestCase]
		public void ParseUnmergedEntries ()
		{
			string test = "u UU N... 100644 100645 100646 100647 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 599bc5aca51701a55c1c8f8586bcbdb8fd56c568 dc8f30622338a15b432458a52d39a9784dafc44f README.md\0";

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v2);
			handler.OnOutput (test);
			var dict = handler.FileList;
			var state = dict ["README.md"];
			Assert.AreEqual (GitStatusCode.IndexUnmerged | GitStatusCode.WorktreeUnmerged, state.StageState);
			Assert.AreEqual (GitSubmoduleState.NoSubmodule, state.SubmoduleState);
			Assert.AreEqual (Convert.ToInt32 ("100644", 8), state.FileMode_Stage1);
			Assert.AreEqual (Convert.ToInt32 ("100645", 8), state.FileMode_Stage2);
			Assert.AreEqual (Convert.ToInt32 ("100646", 8), state.FileMode_Stage3);
			Assert.AreEqual (Convert.ToInt32 ("100647", 8), state.FileMode_Worktree);
		}

		[TestCase]
		public void TestIgnoreUnknownHeaders ()
		{
			string test = "#Doesn't make sense\01 DT N... 100644 100700 100777 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 119b2af8dc31e93206df6b8c8a7f58b930ac0ea8 README.md\0";

			var handler = GitStatusCallbackHandler.CreateCallbackHandler (StatusVersion.v2);
			handler.OnOutput (test);
			var dict = handler.FileList;
			var state = dict ["README.md"];
			Assert.AreEqual (GitStatusCode.IndexDeleted | GitStatusCode.WorktreeTypeChanged, state.StageState);
			Assert.AreEqual (GitSubmoduleState.NoSubmodule, state.SubmoduleState);
			Assert.AreEqual (Convert.ToInt32 ("100644", 8), state.FileMode_Head);
			Assert.AreEqual (Convert.ToInt32 ("100700", 8), state.FileMode_Index);
			Assert.AreEqual (Convert.ToInt32 ("100777", 8), state.FileMode_Worktree);
		}

	}
}
