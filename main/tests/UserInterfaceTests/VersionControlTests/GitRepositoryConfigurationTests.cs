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

		protected void SelectRemote (string remoteName, string remoteUrl = null)
		{
			Session.WaitForElement (c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__Name").Contains (remoteName));
			Assert.IsTrue (Session.SelectElement (c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__Name").Contains (remoteName)));
			if (remoteUrl != null) {
				Assert.IsTrue (Session.SelectElement (c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__Url").Contains (remoteUrl)));
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

			Assert.IsEmpty (Session.Query (c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__FullName").Contains (remoteName+"/")));
			Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Button ().Marked ("buttonFetch")));
			TakeScreenShot ("Fetch-Remote");

			Session.ClickElement (c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__Name").Contains (remoteName));
			Assert.IsNotEmpty (Session.Query (c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__FullName").Contains (remoteName+"/")));
			Assert.IsTrue (Session.SelectElement (c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__FullName").Contains (remoteName+"/").Index (0)));
			TakeScreenShot ("First-Remote-Branch-Selected");
		}

		void AddEditRemote (string buttonName, string newRemoteName, string remoteUrl, string remotePushUrl)
		{
			Assert.IsNotEmpty (Session.Query (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Button ().Marked (buttonName)));
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Button ().Marked (buttonName), false);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog"));
			Assert.IsTrue (Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog").Children ().Textfield ().Marked ("entryName"), newRemoteName));
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog").Children ().Textfield ().Marked ("entryName").Text (newRemoteName));
			Assert.IsTrue (Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog").Children ().Textfield ().Marked ("entryUrl"), remoteUrl));
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog").Children ().Textfield ().Marked ("entryUrl").Text (remoteUrl));
			Assert.IsTrue (Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog").Children ().Textfield ().Marked ("entryPushUrl"), remotePushUrl ?? remoteUrl));
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog").Children ().Textfield ().Marked ("entryPushUrl").Text (remotePushUrl ?? remoteUrl));
			TakeScreenShot ("Remote-Details-Filled");
			Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog").Children ().Button ().Marked ("buttonOk")));
			Session.WaitForNoElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditRemoteDialog"));
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog"));
			TakeScreenShot ("Remote-Edit-Dialog-Closed");
		}

		#endregion

		#region Branches

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
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Button ().Marked (buttonName), false);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditBranchDialog"));
			TakeScreenShot ("Edit-Branch-Dialog-Opened");
			Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditBranchDialog").Children ().Textfield ().Marked ("entryName"), newBranchName);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditBranchDialog").Children ().Textfield ().Marked ("entryName").Text (newBranchName));
			TakeScreenShot ("Branch-Name-Entered");
			Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.EditBranchDialog").Children ().Button ().Marked ("buttonOk")));
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog"));
			TakeScreenShot ("Edit-Branch-Dialog-Opened-Closed");
		}

		protected void SwitchToBranch (string branchName)
		{
			SelectBranch (branchName);
			TakeScreenShot (string.Format ("{0}-Branch-Selected", branchName));
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Button ().Marked ("buttonSetDefaultBranch"), false);
			try {
				Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserGitConfigDialog"));
				TakeScreenShot ("Git-User-Not-Configured");
				EnterGitUserConfig ("John Doe", "john.doe@example.com");
			} catch (TimeoutException e) { }
			Assert.IsTrue (IsBranchSwitched (branchName));
			TakeScreenShot (string.Format ("Switched-To-{0}", branchName));
		}

		protected void SwitchTab (string tabName)
		{
			Assert.IsTrue (Session.SelectElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Notebook ().Marked ("notebook1").Text (tabName)));
			TakeScreenShot (string.Format ("Tab-Changed-{0}", GenerateProjectName (tabName)));
		}

		protected void SelectBranch (string branchName)
		{
			Assert.IsTrue (Session.SelectElement (c => c.TreeView ().Marked ("listBranches").Model ("storeBranches__DisplayName").Contains (branchName)));
			TakeScreenShot (string.Format ("Selected-Branch-{0}", branchName));
		}

		protected bool IsBranchSwitched (string branchName)
		{
			return Session.SelectElement (c => c.TreeView ().Marked ("listBranches").Model ("storeBranches__DisplayName").Text ("<b>" + branchName + "</b>"));
		}

		#endregion
	
		protected void OpenRepositoryConfiguration (string selectTab = null)
		{
			Session.ExecuteCommand (MonoDevelop.VersionControl.Git.Commands.ManageBranches);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog"));
			TakeScreenShot ("Repository-Configuration-Opened");
			if (selectTab != null)
				SwitchTab (selectTab);
		}

		protected void CloseRepositoryConfiguration ()
		{
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Button ().Marked ("buttonOk"));
			Session.WaitForNoElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog"));
		}
	}
}

