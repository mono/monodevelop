//
// GitStashManagerTests.cs
//
// Author:
//       Manish Sinha <manish.sinha@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc.
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

using NUnit.Framework;
using System;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.AutoTest;

namespace UserInterfaceTests
{
	[TestFixture]
	[Category ("Git")]
	[Category ("StashManager")]
	public class GitStashManagerTests : GitBase
	{
		[Test]
		[Description ("Create a project with git, commit changes. Make changes and stash. Remove stash from Stash Manager")]
		public void GitRemoveStashTest ()
		{
			CreateProjectAndCommitAndStash ();

			OpenStashManager ();
			RemoveStash (0);
			Assert.IsEmpty (Session.Query (StashEntries));
			CloseStashManager ();
		}

		[Test]
		[Description ("Create a project with git, commit changes. Make changes and stash. Apply and Remove stash from Stash Manager")]
		public void GitApplyAndRemoveStashTest ()
		{
			CreateProjectAndCommitAndStash ();

			OpenStashManager ();
			ApplyAndRemoveStash (0);

			Session.WaitForElement (IdeQuery.TextArea);
			TakeScreenShot ("Stash-Applied");
			OpenStashManager ();

			TakeScreenShot ("Asserting-if-Not-Stash-Present");
			Session.WaitForNoElement (StashEntries);
			CloseStashManager ();
		}

		[Test]
		[Description ("Create a project with git, commit changes. Make changes and stash. Apply stash from Stash Manager")]
		public void GitApplyStashTest ()
		{
			CreateProjectAndCommitAndStash ();

			OpenStashManager ();
			ApplyStash (0);
			OpenStashManager ();

			TakeScreenShot ("Asserting-if-Stash-Still-Present");
			Assert.IsNotEmpty (Session.Query (StashEntries));
			CloseStashManager ();
		}

		[Test]
		[Description ("Create a project with git, commit changes. Make changes and stash. Convert stash to branch from Stash Manager")]
		public void GitStashConvertToBranchTest ()
		{
			CreateProjectAndCommitAndStash ();

			var branchName = "sample-branch";
			OpenStashManager ();
			ComvertToBranch (0, branchName);
			OpenStashManager ();
			TakeScreenShot ("Asserting-if-Stash-Still-Present");
			Assert.IsEmpty (Session.Query (StashEntries));
			CloseStashManager ();

			OpenRepositoryConfiguration ("Branches");
			IsBranchSwitched (branchName);
			CloseRepositoryConfiguration ();
		}

		void CreateProjectAndCommitAndStash ()
		{
			var templateOptions = new TemplateSelectionOptions {
				CategoryRoot = OtherCategoryRoot,
				Category = ".NET",
				TemplateKindRoot = GeneralKindRoot,
				TemplateKind = "Console Project"
			};
			GitCreateAndCommit (templateOptions, "First commit");
			var changeDescription = MakeSomeChangesAndSaveAll ("Program.cs");
			TestGitStash (changeDescription);
			Session.WaitForElement (IdeQuery.TextArea);
			TakeScreenShot ("After-Stash");
		}
	}
}

