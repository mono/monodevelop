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

			SelectRemoteBranch (remoteName);
		}

		protected void SelectRemoteBranch (string remoteName, string remoteBranchName = null)
		{
			Session.ClickElement (c => remoteTreeName (c).Contains (remoteName));
			Assert.IsNotEmpty (Session.Query (c => remoteTreeFullName (c).Contains (remoteName+"/"+remoteBranchName)));
			Assert.IsTrue (Session.SelectElement (c => remoteTreeFullName (c).Contains (remoteName+"/"+remoteBranchName).Index (0)));
			TakeScreenShot (string.Format ("{0}-Remote-Branch-Selected", remoteBranchName ?? "First"));
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

		protected void DeleteRemote (string remoteName)
		{
			Session.WaitForElement (c => remoteTreeName (c).Contains (remoteName));
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Text ("Remove"), false);
			TakeScreenShot (string.Format ("Remove-Remote-{0}", remoteName));
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
			Session.WaitForElement (c => tagTreeName (c).Text (tagName));
			Assert.IsTrue (Session.SelectElement (c => tagTreeName (c).Text (tagName)), "Failed to select tag: "+tagName);
			TakeScreenShot (string.Format ("{0}-Tag-Selected", tagName));
		}

		protected void DeleteTag (string tagName)
		{
			SelectTag (tagName);
			Assert.IsTrue ((Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked ("buttonRemoveTag"))));
			Session.WaitForNoElement (c => tagTreeName (c).Text (tagName));
		}

		protected void AddNewTag (string tagName, string tagMessage = null, string commitMsg = null)
		{
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog (c).Children ().Button ().Marked ("buttonAddTag"), false);
			Session.WaitForElement (c => c.Window ().Marked ("Select a revision"));

			Session.EnterText (c => c.Window ().Marked ("Select a revision").Children ().Textfield ().Index (0), tagName);
			Session.WaitForElement (c => c.Window ().Marked ("Select a revision").Children ().Textfield ().Index (0).Text (tagName));
			TakeScreenShot ("Tag-Name-Entered");

			if (!string.IsNullOrEmpty (tagMessage)) {
				Session.EnterText (c => c.Window ().Marked ("Select a revision").Children ().Textfield ().Index (1), tagMessage);
				Session.WaitForElement (c => c.Window ().Marked ("Select a revision").Children ().Textfield ().Index (1).Text (tagMessage));
				TakeScreenShot ("Tag-Message-Entered");
			}

			Func<AppQuery, AppQuery> revisionsTreeView = c => c.Window ().Marked ("Select a revision").Children ().TreeView ().Index (0).Model ().Children ();
			if (!string.IsNullOrEmpty (commitMsg)) {
				Session.SelectElement (c => revisionsTreeView (c).Text (commitMsg));
			} else {
				Session.SelectElement (c => revisionsTreeView (c).Index (0));
			}
			TakeScreenShot ("Commit-Message-Selected");

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

			EnterBranchName (newBranchName);
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
			TakeScreenShot ("Edit-Branch-Dialog-Opened-Closed");
		}

		protected void EnterBranchName (string newBranchName)
		{
			Session.EnterText (c => IdeQuery.EditBranchDialog (c).Children ().Textfield ().Marked ("entryName"), newBranchName);
			Session.WaitForElement (c => IdeQuery.EditBranchDialog (c).Children ().Textfield ().Marked ("entryName").Text (newBranchName));
			TakeScreenShot ("Branch-Name-Entered");
			Assert.IsTrue (Session.ClickElement (c => IdeQuery.EditBranchDialog (c).Children ().Button ().Marked ("buttonOk")));
		}

		protected void SwitchToBranch (string branchName)
		{
			SelectBranch (branchName);
			TakeScreenShot (string.Format ("{0}-Branch-Selected", branchName));
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Text ("Switch to Branch"), false);
			CheckIfNameEmailNeeded ();
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

		protected void DeleteBranch (string branchName)
		{
			Assert.IsTrue (Session.SelectElement (c => branchDisplayName (c).Contains (branchName)));
			Session.ClickElement (c => IdeQuery.GitConfigurationDialog(c).Children ().Button ().Text ("Delete"), false);
			TakeScreenShot (string.Format ("Delete-Branch-{0}", branchName));
			Ide.ClickButtonAlertDialog ("Delete");
			Session.WaitForElement (IdeQuery.GitConfigurationDialog);
		}

		protected bool IsBranchSwitched (string branchName)
		{
			try {
				Session.WaitForElement (c => branchDisplayName (c).Text ("<b>" + branchName + "</b>"));
				return true;
			} catch (TimeoutException) {
				return false;
			}
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
			TakeScreenShot ("Git-Repository-Configuration-Closed");
			Session.WaitForNoElement (IdeQuery.GitConfigurationDialog);
		}

		protected void AssertButtonSensitivity (string buttonLabel, bool sensitivity)
		{
			Assert.IsNotEmpty (Session.Query (c => c.Button ().Text (buttonLabel).Sensitivity (sensitivity)),
				string.Format ("{0} button is {1} enabled", buttonLabel, sensitivity ? notString : string.Empty));
		}

		#endregion

		#region Stash Manager

		protected Func<AppQuery, AppQuery> StashEntries = c => c.Window ().Marked (
			"Stash Manager").Children ().TreeView ().Marked ("list").Model ().Children ();

		protected void OpenStashManager ()
		{
			Session.ExecuteCommand ("MonoDevelop.VersionControl.Git.Commands.ManageStashes");
			Session.WaitForElement (c => c.Window ().Marked ("Stash Manager"));
			TakeScreenShot ("StashManager-Opened");
		}

		protected void CloseStashManager ()
		{
			Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Text ("Close"));
			Session.WaitForElement (IdeQuery.TextArea);
			TakeScreenShot ("StashManager-Closed");
		}

		protected void SelectStashEntry (int index = 0)
		{
			Session.WaitForElement (c => StashEntries (c).Index (index));
			Session.SelectElement (c => StashEntries (c).Index (index));
		}

		protected void RemoveStash (int index)
		{
			SelectStashEntry (index);
			TakeScreenShot ("About-To-Click-Remove");
			Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Button ().Text ("Remove"));
			Session.WaitForElement (c => c.Window ().Marked ("Stash Manager"));
		}

		protected void ApplyAndRemoveStash (int index)
		{
			SelectStashEntry (index);
			TakeScreenShot ("About-To-Click-Apply-and-Remove");
			Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Button ().Text ("Apply and Remove"));
		}

		protected void ApplyStash (int index)
		{
			SelectStashEntry (index);
			TakeScreenShot ("About-To-Click-Apply");
			Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Button ().Text ("Apply"));
		}

		protected void ComvertToBranch (int index, string branchName)
		{
			SelectStashEntry (index);
			TakeScreenShot ("About-To-Click-Convert-To-Branch");
			Session.ClickElement (c => c.Window ().Marked ("Stash Manager").Children ().Button ().Text ("Convert to Branch"), false);
			EnterBranchName (branchName);
			Ide.WaitForStatusMessage (new [] { "Stash successfully applied" });
		}

		#endregion
	}
}

