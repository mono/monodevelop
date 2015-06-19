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

using NUnit.Framework;
using System;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.AutoTest;

namespace UserInterfaceTests
{
	[TestFixture]
	[Category ("GitConfig")]
	public class GitRepositoryConfigurationTests : GitRepositoryConfigurationBase
	{
		#region Branch Tab

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

		#endregion

		#region Remotes Tab

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
		[Ignore ("When OK is clicked on EditRemoteDialog, it doesn't update the list")]
		public void EditGitRemoteTest ()
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

	public abstract class GitRepositoryConfigurationBase : VCSBase
	{
		#region Remotes

		Func<AppQuery, AppQuery> remoteTreeName = c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__Name");
		Func<AppQuery, AppQuery> remoteTreeUrl = c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__Url");
		Func<AppQuery, AppQuery> remoteTreeFullName = c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__FullName");

		protected void SelectRemote (string remoteName, string remoteUrl = null)
		{
			Session.WaitForElement (c => remoteTreeName (c).Contains (remoteName));
			Assert.IsTrue (Session.SelectElement (c => remoteTreeName (c).Contains (remoteName)));
			if (remoteUrl != null) {
				Assert.IsTrue (Session.SelectElement (c => remoteTreeUrl (c).Contains (remoteUrl)));
			}
			TakeScreenShot (string.Format ("{0}-Remote-Selected", remoteName));
		}

		protected void EditRemote (string newRemoteName, string remoteUrl, string remotePushUrl = null)
		{
			AddEditRemote ("buttonEditRemote", newRemoteName, remoteUrl, remotePushUrl);
		}

		protected void AddRemote (string newRemoteName, string remoteUrl, string remotePushUrl = null)
		{
			AddEditRemote ("buttonAddRemote", newRemoteName, remoteUrl, remotePushUrl);
		}

		protected void FetchRemoteBranch (string remoteName)
		{
			SelectRemote (remoteName);

			Assert.IsEmpty (Session.Query (c => remoteTreeFullName (c).Contains (remoteName+"/")));
			Assert.IsTrue (Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked ("buttonFetch")));
			TakeScreenShot ("Fetch-Remote");

			Session.ClickElement (c => remoteTreeName (c).Contains (remoteName));
			Assert.IsNotEmpty (Session.Query (c => remoteTreeFullName (c).Contains (remoteName+"/")));
			Assert.IsTrue (Session.SelectElement (c => remoteTreeFullName (c).Contains (remoteName+"/").Index (0)));
			TakeScreenShot ("First-Remote-Branch-Selected");
		}

		void AddEditRemote (string buttonName, string newRemoteName, string remoteUrl, string remotePushUrl)
		{
			Assert.IsNotEmpty (Session.Query (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked (buttonName)));
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked (buttonName), false);
			Session.WaitForElement (IdeQuery.EditRemoteDialog);

			Func<AppQuery, AppQuery> EditRemoteDialogChildren = c => IdeQuery.EditRemoteDialog (c).Children ();
			Assert.IsTrue (Session.EnterText (c => EditRemoteDialogChildren (c).Textfield ().Marked ("entryName"), newRemoteName));
			Session.WaitForElement (c =>  EditRemoteDialogChildren (c).Textfield ().Marked ("entryName").Text (newRemoteName));

			Assert.IsTrue (Session.EnterText (c => EditRemoteDialogChildren (c).Textfield ().Marked ("entryUrl"), remoteUrl));
			Session.WaitForElement (c =>  EditRemoteDialogChildren (c).Marked ("entryUrl").Text (remoteUrl));

			Assert.IsTrue (Session.EnterText (c =>  EditRemoteDialogChildren (c).Textfield ().Marked ("entryPushUrl"), remotePushUrl ?? remoteUrl));
			Session.WaitForElement (c =>  EditRemoteDialogChildren (c).Textfield ().Marked ("entryPushUrl").Text (remotePushUrl ?? remoteUrl));
			TakeScreenShot ("Remote-Details-Filled");

			Assert.IsTrue (Session.ClickElement (c =>  EditRemoteDialogChildren (c).Button ().Marked ("buttonOk")));
			Session.WaitForNoElement (IdeQuery.EditRemoteDialog);
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
			TakeScreenShot ("Remote-Edit-Dialog-Closed");
		}

		#endregion

		#region Branches

		Func<AppQuery, AppQuery> branchDisplayName = c => c.TreeView ().Marked ("listBranches").Model ("storeBranches__DisplayName");

		protected void CreateNewBranch (string newBranchName)
		{
			CreateEditBranch ("buttonAddBranch", newBranchName);
		}

		protected void EditBranch (string oldBranchName, string newBranchName)
		{
			SelectBranch (oldBranchName);
			CreateEditBranch ("buttonEditBranch", newBranchName);
		}

		protected void CreateEditBranch (string buttonName, string newBranchName)
		{
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Marked (buttonName), false);
			Session.WaitForElement (IdeQuery.EditBranchDialog);
			TakeScreenShot ("Edit-Branch-Dialog-Opened");

			Session.EnterText (c => IdeQuery.EditBranchDialog (c).Children ().Textfield ().Marked ("entryName"), newBranchName);
			Session.WaitForElement (c => IdeQuery.EditBranchDialog (c).Children ().Textfield ().Marked ("entryName").Text (newBranchName));
			TakeScreenShot ("Branch-Name-Entered");

			Assert.IsTrue (Session.ClickElement (c => IdeQuery.EditBranchDialog (c).Children ().Button ().Marked ("buttonOk")));
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
			TakeScreenShot ("Edit-Branch-Dialog-Opened-Closed");
		}

		protected void SwitchToBranch (string branchName)
		{
			SelectBranch (branchName);
			TakeScreenShot (string.Format ("{0}-Branch-Selected", branchName));
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Marked ("buttonSetDefaultBranch"), false);

			try {
				Session.WaitForElement (IdeQuery.GitConfigurationDialog);
				TakeScreenShot ("Git-User-Not-Configured");
				EnterGitUserConfig ("John Doe", "john.doe@example.com");
			} catch (TimeoutException e) { }

			Assert.IsTrue (IsBranchSwitched (branchName));
			TakeScreenShot (string.Format ("Switched-To-{0}", branchName));
		}

		protected void SwitchTab (string tabName)
		{
			Assert.IsTrue (Session.SelectElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Notebook ().Marked ("notebook1").Text (tabName)));
			TakeScreenShot (string.Format ("Tab-Changed-{0}", GenerateProjectName (tabName)));
		}

		protected void SelectBranch (string branchName)
		{
			Assert.IsTrue (Session.SelectElement (c => branchDisplayName (c).Contains (branchName)));
			TakeScreenShot (string.Format ("Selected-Branch-{0}", branchName));
		}

		protected bool IsBranchSwitched (string branchName)
		{
			return Session.SelectElement (c => branchDisplayName (c).Text ("<b>" + branchName + "</b>"));
		}

		#endregion
	
		protected void OpenRepositoryConfiguration (string selectTab = null)
		{
			Session.ExecuteCommand (MonoDevelop.VersionControl.Git.Commands.ManageBranches);
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
			TakeScreenShot ("Repository-Configuration-Opened");
			if (selectTab != null)
				SwitchTab (selectTab);
		}

		protected void CloseRepositoryConfiguration ()
		{
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Marked ("buttonOk"));
			Session.WaitForNoElement (IdeQuery.GitConfigurationDialog);
		}
	}
}

