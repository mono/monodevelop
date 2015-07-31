//
// GitRepositoryConfigurationTests.cs
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

using System;
using NUnit.Framework;

namespace UserInterfaceTests
{
	[TestFixture]
	[Category ("GitConfig")]
	public class GitRepositoryConfigurationTests : GitBase
	{
		string gtkSharpUrl = "git@github.com:mono/gtk-sharp.git";

		#region Branch Tab

		[Test]
		public void CheckBranchButtonsSensitivity ()
		{
			TestClone (gtkSharpUrl);
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Branches");

			TakeScreenShot ("Asserting-Edit-Delete-Switch-Button-Disabled");
			AssertBranchesButtonSensitivity (false, false, false);
			SelectBranch ("<b>master</b>");
			TakeScreenShot ("Asserting-Edit-Switch-Button-Enabled");
			AssertBranchesButtonSensitivity (true, false, false);
			CreateNewBranch ("new-branch");
			SelectBranch ("new-branch");
			TakeScreenShot ("Asserting-Edit-Delete-Switch-Button-Enabled");
			AssertBranchesButtonSensitivity (true, true, true);

			CloseRepositoryConfiguration ();
		}

		[Test]
		public void CreateNewBranchTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ();
			CreateNewBranch ("new-branch");
			CloseRepositoryConfiguration ();
		}

		[Test]
		public void GitSwitchBranchTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ();
			CreateNewBranch ("new-branch");
			SwitchToBranch ("new-branch");
			CloseRepositoryConfiguration ();
		}

		[Test]
		public void GitEditBranchTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ();
			CreateNewBranch ("new-branch");
			SelectBranch ("new-branch");
			EditBranch ("new-branch", "new-new-branch");
			SwitchToBranch ("new-new-branch");
			CloseRepositoryConfiguration ();
		}

		[Test]
		public void GitDeleteBranchTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ();
			CreateNewBranch ("new-branch");
			SelectBranch ("new-branch");
			DeleteBranch ("new-branch");

			CloseRepositoryConfiguration ();
		}

		#endregion

		#region Tag

		[Test]
		public void CheckTagButtonsSensitivity ()
		{
			TestClone (gtkSharpUrl);
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Tags");

			TakeScreenShot ("Asserting-Push-Delete-Button-Disabled");
			AssertTagsButtonSensitivity (false, false);
			SelectTag ("1.0.10");
			TakeScreenShot ("Asserting-Push-Delete-Button-Enabled");
			AssertTagsButtonSensitivity (true, true);

			CloseRepositoryConfiguration ();
		}

		[Test]
		public void AddTag ()
		{
			TestClone (gtkSharpUrl);
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Tags");

			AddNewTag ("bumped", "bumped tag", "build: Bump mono dependency to 3.2.8");
			SelectTag ("bumped");
			TakeScreenShot ("New-Tag-Selected");

			CloseRepositoryConfiguration ();
		}

		[Test]
		public void DeleteTag ()
		{
			TestClone (gtkSharpUrl);
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Tags");
			DeleteTag ("1.0.10");
			CloseRepositoryConfiguration ();
		}

		#endregion

		#region Remotes Tab

		[Test]
		public void CheckRemoteButtonsSensitivity ()
		{
			TestClone (gtkSharpUrl);
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Remote Sources");

			TakeScreenShot ("Asserting-Edit-Remove-Track--Fetch-Button-Disabled");
			AssertRemotesButtonSensitivity (false, false, false, false);
			SelectRemote ("origin");
			TakeScreenShot ("Asserting-Edit-Switch-Button-Enabled");
			AssertRemotesButtonSensitivity (true, true, false, true);
			SelectRemoteBranch ("origin", "master");
			TakeScreenShot ("Asserting-Edit-Switch-Button-Track-Enabled");
			AssertRemotesButtonSensitivity (true, true, true, true);

			CloseRepositoryConfiguration ();
		}

		[Test]
		public void SelectRemoteTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Remote Sources");
			SelectRemote ("origin");
			CloseRepositoryConfiguration ();
		}

		[Test]
		public void AddGitRemoteTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			const string newRemoteName = "second";
			const string newRemoteUrl = "git@github.com:mono/monohotdraw.git";
			OpenRepositoryConfiguration ("Remote Sources");
			AddRemote (newRemoteName, newRemoteUrl);
			SelectRemote (newRemoteName, newRemoteUrl);
			CloseRepositoryConfiguration ();
		}

		[Test]
		public void DeleteGitRemoteTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			const string newRemoteName = "second";
			const string newRemoteUrl = "git@github.com:mono/monohotdraw.git";
			OpenRepositoryConfiguration ("Remote Sources");
			AddRemote (newRemoteName, newRemoteUrl);
			SelectRemote (newRemoteName, newRemoteUrl);
			DeleteRemote (newRemoteName);
			CloseRepositoryConfiguration ();
		}

		[Test]
		[Description ("Edit only Remote Name, don't edit URL or Push URL")]
		public void EditGitRemoteNameTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Remote Sources");

			const string newRemoteName = "second";
			const string newRemoteUrl = "git@github.com:mono/monohotdraw.git";
			AddRemote (newRemoteName, newRemoteUrl);
			SelectRemote (newRemoteName, newRemoteUrl);

			const string updatedRemoteName = "second-origin";
			const string updatedRemoteUrl = "git@github.com:mono/monohotdraw.git";
			EditRemote (updatedRemoteName, updatedRemoteUrl, updatedRemoteUrl);
			SelectRemote (updatedRemoteName, updatedRemoteUrl);
			CloseRepositoryConfiguration ();
		}

		[Test]
		[Description ("Edit only Remote Name and URL, don't edit Push URL")]
		public void EditGitRemoteNameAndUrlTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Remote Sources");

			const string newRemoteName = "second";
			const string newRemoteUrl = "git@github.com:mono/monohotdraw.git";
			AddRemote (newRemoteName, newRemoteUrl);
			SelectRemote (newRemoteName, newRemoteUrl);

			const string updatedRemoteName = "second-origin";
			const string updatedRemoteUrl = "git@github.com:mono/monohotdraw-push.git";
			EditRemote (updatedRemoteName, updatedRemoteUrl, newRemoteUrl);
			SelectRemote (updatedRemoteName, updatedRemoteUrl);
			CloseRepositoryConfiguration ();
		}

		[Test]
		[Description ("Edit only Remote Name and Push URL, don't edit URL")]
		public void EditGitRemoteNameAndPushUrlTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Remote Sources");

			const string newRemoteName = "second";
			const string newRemoteUrl = "git@github.com:mono/monohotdraw.git";
			AddRemote (newRemoteName, newRemoteUrl);
			SelectRemote (newRemoteName, newRemoteUrl);

			const string updatedRemoteName = "second-origin";
			const string updatedRemoteUrl = "git@github.com:mono/monohotdraw.git";
			EditRemote (updatedRemoteName, updatedRemoteUrl, "git@github.com:mono/monohotdraw-push.git");
			SelectRemote (updatedRemoteName, updatedRemoteUrl);
			CloseRepositoryConfiguration ();
		}

		[Test]
		[Description ("Edit only Remote URL and Push URL, don't edit Name")]
		public void EditGitRemoteUrlTest ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			OpenRepositoryConfiguration ("Remote Sources");

			const string newRemoteName = "second";
			const string newRemoteUrl = "git@github.com:mono/monohotdraw.git";
			AddRemote (newRemoteName, newRemoteUrl);
			SelectRemote (newRemoteName, newRemoteUrl);

			const string updatedRemoteUrl = "git@github.com:mono/monohotdraw-push.git";
			EditRemote (newRemoteName, updatedRemoteUrl, updatedRemoteUrl);
			SelectRemote (newRemoteName, updatedRemoteUrl);
			CloseRepositoryConfiguration ();
		}

		[Test]
		public void FetchRemoteBranches ()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			const string newRemoteName = "second";
			const string newRemoteUrl = "git@github.com:mono/monohotdraw.git";
			OpenRepositoryConfiguration ("Remote Sources");
			AddRemote (newRemoteName, newRemoteUrl);
			FetchRemoteBranch (newRemoteName);
			CloseRepositoryConfiguration ();
		}

		[Test]
		public void TrackRemoteBranchInLocalTest()
		{
			TestClone ("git@github.com:mono/jurassic.git");
			Ide.WaitForSolutionCheckedOut ();

			const string newRemoteName = "second";
			const string newRemoteUrl = "git@github.com:mono/monohotdraw.git";
			OpenRepositoryConfiguration ("Remote Sources");
			AddRemote (newRemoteName, newRemoteUrl);
			FetchRemoteBranch (newRemoteName);
			const string localBranch = "local-branch-random-uitest";
			CreateEditBranch ("buttonTrackRemote", localBranch);
			SwitchTab ("Branches");
			SelectBranch (localBranch);
			CloseRepositoryConfiguration ();
		}

		#endregion
	}
}

