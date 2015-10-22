//
// GitBase.cs
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
using MonoDevelop.Components.AutoTest;
using NUnit.Framework;

namespace UserInterfaceTests
{
	public abstract class GitBase : VCSBase
	{
		static string notString = "not";

		#region Git Repository Configuration

		#region Remotes

		Func<AppQuery, AppQuery> remoteTreeName = c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__Name");
		Func<AppQuery, AppQuery> remoteTreeUrl = c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__Url");
		Func<AppQuery, AppQuery> remoteTreeFullName = c => c.TreeView ().Marked ("treeRemotes").Model ("storeRemotes__FullName");

		protected void AssertRemotesButtonSensitivity (bool editSensitivity, bool removeSensitivity, bool trackSensitivity, bool fetchSensitivity)
		{
			AssertButtonSensitivity ("Add", true);
			AssertButtonSensitivity ("Edit", editSensitivity);
			AssertButtonSensitivity ("Remove", removeSensitivity);
			AssertButtonSensitivity ("Track in Local Branch", trackSensitivity);
			AssertButtonSensitivity ("Fetch", fetchSensitivity);
		}

		protected void SelectRemote (string remoteName, string remoteUrl = null)
		{
			ReproStep (string.Format ("Select a remote named '{0}' {1}", remoteName,
			                          remoteUrl != null ? string.Format (" and Remote URL '{0}'", remoteUrl) : string.Empty));
			Session.WaitForElement (c => remoteTreeName (c).Contains (remoteName));

			try {
				Assert.IsTrue (Session.SelectElement (c => remoteTreeName (c).Contains (remoteName)));
			} catch (AssertionException) {
				ReproFailedStep (string.Format ("Remote Name '{0}' exists", remoteName), string.Format ("Remote Name '{0}' does not exists", remoteName));
				throw;
			}
			if (remoteUrl != null) {
				try {
					Assert.IsTrue (Session.SelectElement (c => remoteTreeUrl (c).Contains (remoteUrl)));
				} catch (AssertionException) {
					ReproFailedStep (string.Format ("Remote URL '{0}' with Name '{1}' exists", remoteUrl, remoteName),
					                 string.Format ("Remote URL '{0}' with Name '{1}' does not exist", remoteUrl, remoteName));
					throw;
				}
			}
			TakeScreenShot (string.Format ("{0}-Remote-Selected", remoteName));
		}

		protected void EditRemote (string newRemoteName, string remoteUrl, string remotePushUrl = null)
		{
			ReproStep ("Click on Edit");
			AddEditRemote ("buttonEditRemote", newRemoteName, remoteUrl, remotePushUrl);
		}

		protected void AddRemote (string newRemoteName, string remoteUrl, string remotePushUrl = null)
		{
			ReproStep ("Click on Add");
			AddEditRemote ("buttonAddRemote", newRemoteName, remoteUrl, remotePushUrl);
		}

		protected void FetchRemoteBranch (string remoteName)
		{
			SelectRemote (remoteName);

			Assert.IsEmpty (Session.Query (c => remoteTreeFullName (c).Contains (remoteName+"/")));
			Assert.IsTrue (Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked ("buttonFetch")));
			TakeScreenShot ("Fetch-Remote");

			SelectRemoteBranch (remoteName);
		}

		protected void SelectRemoteBranch (string remoteName, string remoteBranchName = null)
		{
			Session.ClickElement (c => remoteTreeName (c).Contains (remoteName));
			Assert.IsNotEmpty (Session.Query (c => remoteTreeFullName (c).Contains (remoteName+"/"+remoteBranchName)));

			var expected = string.Format ("Select the Remote with Name '{0}' and Remote Branch Name '{1}'", remoteName, remoteBranchName);
			try {
				ReproStep (expected);
				Assert.IsTrue (Session.SelectElement (c => remoteTreeFullName (c).Contains (remoteName + "/" + remoteBranchName).Index (0)));
			} catch (AssertionException) {
				ReproFailedStep (expected, "Could not "+expected);
				throw;
			}
			TakeScreenShot (string.Format ("{0}-Remote-Branch-Selected", remoteBranchName ?? "First"));
		}

		void AddEditRemote (string buttonName, string newRemoteName, string remoteUrl, string remotePushUrl)
		{
			Assert.IsNotEmpty (Session.Query (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked (buttonName)));
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked (buttonName), false);
			Session.WaitForElement (IdeQuery.EditRemoteDialog);

			ReproStep (string.Format ("Enter Remote name as '{0}'", newRemoteName));
			Func<AppQuery, AppQuery> EditRemoteDialogChildren = c => IdeQuery.EditRemoteDialog (c).Children ();
			Assert.IsTrue (Session.EnterText (c => EditRemoteDialogChildren (c).Textfield ().Marked ("entryName"), newRemoteName));
			Session.WaitForElement (c =>  EditRemoteDialogChildren (c).Textfield ().Marked ("entryName").Text (newRemoteName));

			ReproStep (string.Format ("Enter Remote URL as '{0}'", remoteUrl));
			Assert.IsTrue (Session.EnterText (c => EditRemoteDialogChildren (c).Textfield ().Marked ("entryUrl"), remoteUrl));
			Session.WaitForElement (c =>  EditRemoteDialogChildren (c).Marked ("entryUrl").Text (remoteUrl));

			ReproStep (string.Format ("Enter Remote Push URL as '{0}'", remotePushUrl ?? remoteUrl));
			Assert.IsTrue (Session.EnterText (c =>  EditRemoteDialogChildren (c).Textfield ().Marked ("entryPushUrl"), remotePushUrl ?? remoteUrl));
			Session.WaitForElement (c =>  EditRemoteDialogChildren (c).Textfield ().Marked ("entryPushUrl").Text (remotePushUrl ?? remoteUrl));
			TakeScreenShot ("Remote-Details-Filled");

			ReproStep ("Click on OK");
			Assert.IsTrue (Session.ClickElement (c =>  EditRemoteDialogChildren (c).Button ().Marked ("buttonOk")));
			Session.WaitForNoElement (IdeQuery.EditRemoteDialog);
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
			TakeScreenShot ("Remote-Edit-Dialog-Closed");
		}

		protected void DeleteRemote (string remoteName)
		{
			Session.WaitForElement (c => remoteTreeName (c).Contains (remoteName));
			ReproStep ("Click on Remove");
            Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Text ("Remove"), false);
			TakeScreenShot (string.Format ("Remove-Remote-{0}", remoteName));

			ReproStep ("When prompted to confirm, click Delete");
			Ide.ClickButtonAlertDialog ("Delete");
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
		}

		#endregion

		#region Tags

		Func<AppQuery, AppQuery> tagTreeName = c => c.TreeView ().Marked ("listTags").Model ("storeTags__Name");

		protected void AssertTagsButtonSensitivity (bool pushSensitivity, bool deleteSensitivity)
		{
			AssertButtonSensitivity ("New", true);
			AssertButtonSensitivity ("Push", pushSensitivity);
			AssertButtonSensitivity ("Delete", deleteSensitivity);
		}

		protected void SelectTag (string tagName)
		{
			WaitForElement (c => tagTreeName (c).Text (tagName),
			                string.Format ("Tag '{0}' should be available", tagName),
			                string.Format ("Tag '{0}' it not available", tagName));
			try {
				Assert.IsTrue (Session.SelectElement (c => tagTreeName (c).Text (tagName)), "Failed to select tag: " + tagName);
			} catch (AssertionException) {
				ReproFailedStep (string.Format ("Tag '{0}' should be selected", tagName),
				                 string.Format ("Tag '{0}' cannot be selected", tagName));
				throw;
			}
			TakeScreenShot (string.Format ("{0}-Tag-Selected", tagName));
		}

		protected void DeleteTag (string tagName)
		{
			SelectTag (tagName);
			ReproStep ("Click Delete");
			try {
				Assert.IsTrue ((Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked ("buttonRemoveTag"))));
			} catch (AssertionException) {
				ReproFailedStep (string.Format ("Tag '{0}' should be removed", tagName), string.Format ("Tag '{0}' could not be removed", tagName));
				throw;
			}
			Session.WaitForNoElement (c => tagTreeName (c).Text (tagName));
		}

		protected void AddNewTag (string tagName, string tagMessage = null, string commitMsg = null)
		{
			ReproStep ("Click on New");
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked ("buttonAddTag"), false);

			ReproStep ("Wait for 'Select a Revision' dialog to open");
			try {
				Session.WaitForElement (c => c.Window ().Marked ("Select a revision"));
			} catch (AssertionException) {
				ReproFailedStep ("'Select a Revision' dialog should open", "'Select a Revision' dialog did not open");
				throw;
			}

			ReproStep ("Enter the Tag Name");
			Session.EnterText (c => c.Window ().Marked ("Select a revision").Children ().Textfield ().Index (0), tagName);
			Session.WaitForElement (c => c.Window ().Marked ("Select a revision").Children ().Textfield ().Index (0).Text (tagName));
			TakeScreenShot ("Tag-Name-Entered");

			if (!string.IsNullOrEmpty (tagMessage)) {
				ReproStep ("Enter a Tag Message");
				Session.EnterText (c => c.Window ().Marked ("Select a revision").Children ().Textfield ().Index (1), tagMessage);
				Session.WaitForElement (c => c.Window ().Marked ("Select a revision").Children ().Textfield ().Index (1).Text (tagMessage));
				TakeScreenShot ("Tag-Message-Entered");
			}

			Func<AppQuery, AppQuery> revisionsTreeView = c => c.Window ().Marked ("Select a revision").Children ().TreeView ().Index (0).Model ().Children ();
			if (!string.IsNullOrEmpty (commitMsg)) {
				ReproStep (string.Format ("Select the commit with message '{0}'", commitMsg));
				Session.SelectElement (c => revisionsTreeView (c).Text (commitMsg));
			} else {
				ReproStep ("Select the first commit");
				Session.SelectElement (c => revisionsTreeView (c).Index (0));
			}
			TakeScreenShot ("Commit-Message-Selected");

			ReproStep ("Click OK");
			Session.ClickElement (c => c.Window ().Marked ("Select a revision").Children ().Button ().Text ("Ok"));
			try {
				Session.WaitForElement (IdeQuery.GitConfigurationDialog);
				TakeScreenShot ("Git-User-Not-Configured");
				EnterGitUserConfig ("John Doe", "john.doe@example.com");
			} catch (TimeoutException e) { }
			Session.WaitForElement (c => IdeQuery.GitConfigurationDialog (c));
			TakeScreenShot ("Ok-Clicked");
		}

		#endregion

		#region Branches

		Func<AppQuery, AppQuery> branchDisplayName = c => c.TreeView ().Marked ("listBranches").Model ("storeBranches__DisplayName");

		protected void AssertBranchesButtonSensitivity (bool editSensitivity, bool deleteSensitivity, bool switchSensitivity)
		{
			AssertButtonSensitivity ("New", true);
			AssertButtonSensitivity ("Edit", editSensitivity);
			AssertButtonSensitivity ("Delete", deleteSensitivity);
			AssertButtonSensitivity ("Switch to Branch", switchSensitivity);
		}

		protected void CreateNewBranch (string newBranchName)
		{
			ReproStep ("Click New");
			CreateEditBranch ("buttonAddBranch", newBranchName);
		}

		protected void EditBranch (string oldBranchName, string newBranchName)
		{
			SelectBranch (oldBranchName);
			ReproStep ("Click Edit");
			CreateEditBranch ("buttonEditBranch", newBranchName);
		}

		protected void CreateEditBranch (string buttonName, string newBranchName)
		{
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Marked (buttonName), false);

			ReproStep ("Wait for Branch Properties dialog");
			WaitForElement (IdeQuery.EditBranchDialog, "Branch Properties dialog opens", "Branch Properties dialog does not open");
			TakeScreenShot ("Edit-Branch-Dialog-Opened");

			EnterBranchName (newBranchName);
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
			TakeScreenShot ("Edit-Branch-Dialog-Opened-Closed");
		}

		protected void EnterBranchName (string newBranchName)
		{
			ReproStep ("Enter branch name");
			Session.EnterText (c => IdeQuery.EditBranchDialog (c).Children ().Textfield ().Marked ("entryName"), newBranchName);
			Session.WaitForElement (c => IdeQuery.EditBranchDialog (c).Children ().Textfield ().Marked ("entryName").Text (newBranchName));
			TakeScreenShot ("Branch-Name-Entered");

			ReproStep ("Click OK");
			Assert.IsTrue (Session.ClickElement (c => IdeQuery.EditBranchDialog (c).Children ().Button ().Marked ("buttonOk")));
		}

		protected void SwitchToBranch (string branchName)
		{
			SelectBranch (branchName);
			TakeScreenShot (string.Format ("{0}-Branch-Selected", branchName));

			ReproStep ("Click on 'Switch to Branch'");
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Text ("Switch to Branch"), false);
			CheckIfNameEmailNeeded ();
			CheckIfUserConflict ();
			ReproStep ("Check if the selected branch is bold");
			try {
				Assert.IsTrue (IsBranchSwitched (branchName));
			} catch (AssertionException) {
				ReproFailedStep ("The selected branch should be bold", "The selected branch is not bold");
				throw;
			}
			TakeScreenShot (string.Format ("Switched-To-{0}", branchName));
		}

		protected void SwitchTab (string tabName)
		{
			ReproStep (string.Format ("Select the '{0}' tab", tabName));
			try {
				Assert.IsTrue (Session.SelectElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Notebook ().Marked ("notebook1").Text (tabName)));
			} catch (AssertionException) {
				ReproFailedStep (string.Format ("Tab '{0}' is selected", tabName), string.Format ("Tab '{0}' is not selected", tabName));
				throw;
			}
			TakeScreenShot (string.Format ("Tab-Changed-{0}", GenerateProjectName (tabName)));
		}

		protected void SelectBranch (string branchName)
		{
			ReproStep (string.Format ("Select the '{0}' branch", branchName));
			try {
				Assert.IsTrue (Session.SelectElement (c => branchDisplayName (c).Contains (branchName)));
			} catch (AssertionException) {
				ReproFailedStep (string.Format ("Branch '{0}' is selected", branchName), string.Format ("Branch '{0}' is not selected", branchName));
				throw;
			}
			TakeScreenShot (string.Format ("Selected-Branch-{0}", branchName.ToPathSafeString ()));
		}

		protected void DeleteBranch (string branchName)
		{
			SelectBranch (branchName);
			ReproStep ("Press Delete");
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Text ("Delete"), false);
			TakeScreenShot (string.Format ("Delete-Branch-{0}", branchName));
			ReproStep ("If prompted for confirmation, press Delete");
			Ide.ClickButtonAlertDialog ("Delete");
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
		}

		protected bool IsBranchSwitched (string branchName)
		{
			try {
				WaitForElement (c => branchDisplayName (c).Text ("<b>" + branchName + "</b>"),
				                string.Format ("Branch '{0}' is checked out", branchName),
				                string.Format ("Branch '{0}' is not checked out", branchName));
				return true;
			} catch (TimeoutException) {
				return false;
			}
		}

		#endregion

		protected void OpenRepositoryConfiguration (string selectTab = null)
		{
			ReproStep ("Click Version Control > Manage Branches and Remotes");
			Session.ExecuteCommand (MonoDevelop.VersionControl.Git.Commands.ManageBranches);

			WaitForElement (IdeQuery.GitConfigurationDialog,
			                "Git Repository Configuration Dialog should open",
			                "Git Repository Configuration Dialog did not open");
			TakeScreenShot ("Repository-Configuration-Opened");

			if (selectTab != null)
				SwitchTab (selectTab);
		}

		protected void CloseRepositoryConfiguration ()
		{
			ReproStep ("Click on Close button of Git Repository Configuration Dialog");
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Marked ("buttonOk"));
			TakeScreenShot ("Git-Repository-Configuration-Closed");
			Session.WaitForNoElement (IdeQuery.GitConfigurationDialog);
		}

		protected void AssertButtonSensitivity (string buttonLabel, bool sensitivity)
		{
			var expected = string.Format ("{0} button is {1} enabled", buttonLabel, !sensitivity ? notString : string.Empty);
			var actual = string.Format ("{0} button is {1} enabled", buttonLabel, sensitivity ? notString : string.Empty);
			try {
				Assert.IsNotEmpty (Session.Query (c => c.Button ().Text (buttonLabel).Sensitivity (sensitivity)), actual);
			} catch (AssertionException) {
				ReproFailedStep (expected, actual);
				throw;
			}
		}

		#endregion

		#region Stash Manager

		protected Func<AppQuery, AppQuery> StashEntries = c => c.Window ().Marked (
			"Stash Manager").Children ().TreeView ().Marked ("list").Model ().Children ();

		protected void OpenStashManager ()
		{
			ReproStep ("Click on Version Control > Manage Stashes");
			Session.ExecuteCommand ("MonoDevelop.VersionControl.Git.Commands.ManageStashes");
			WaitForElement (c => c.Window ().Marked ("Stash Manager"), "Stash Manager dialog should open", "Stash Manager dialog did not open");
			TakeScreenShot ("StashManager-Opened");
		}

		protected void CloseStashManager ()
		{
			ReproStep ("On Stash Manager, click Close button");
			Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Text ("Close"));
			Session.WaitForElement (IdeQuery.TextArea);
			TakeScreenShot ("StashManager-Closed");
		}

		protected void SelectStashEntry (int index = 0)
		{
			ReproStep ("Select the stash entry #{0}", index+1);
			WaitForElement (c => StashEntries (c).Index (index), "Select stash entry: "+index+1, "Could not select that stash entry");
			Session.SelectElement (c => StashEntries (c).Index (index));
		}

		protected void RemoveStash (int index)
		{
			SelectStashEntry (index);
			TakeScreenShot ("About-To-Click-Remove");
			try {
				ReproStep ("Click on Remove");
				Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Button ().Text ("Remove")));
			} catch (AssertionException) {
				ReproFailedStep ("Stash should be removed", "Stash failed to remove");
				throw;
			}
			Session.WaitForElement (c => c.Window ().Marked ("Stash Manager"));
		}

		protected void ApplyAndRemoveStash (int index)
		{
			SelectStashEntry (index);
			TakeScreenShot ("About-To-Click-Apply-and-Remove");
			try {
				ReproStep ("Click on 'Apply and Remove'");
				Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Button ().Text ("Apply and Remove")));
			} catch (AssertionException) {
				ReproFailedStep ("Stash should be applied and removed from the list", "Stash failed to applied and removed from the list");
				throw;
			}
		}

		protected void ApplyStash (int index)
		{
			SelectStashEntry (index);
			TakeScreenShot ("About-To-Click-Apply");
			try {
				ReproStep ("Click on Apply");
				Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Button ().Text ("Apply")));
			} catch (AssertionException) {
				ReproFailedStep ("Stash should be applied", "Stash failed to apply");
				throw;
			}
		}

		protected void ComvertToBranch (int index, string branchName)
		{
			SelectStashEntry (index);
			TakeScreenShot ("About-To-Click-Convert-To-Branch");
			ReproStep ("Click on 'Convert to Branch'");
			Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Button ().Text ("Convert to Branch"), false);
			EnterBranchName (branchName);
			Ide.WaitForStatusMessage (new [] { "Stash successfully applied" });
		}

		#endregion
	}
}

