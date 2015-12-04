﻿//
// VCSUtils.cs
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
using MonoDevelop.Ide.Commands;

namespace UserInterfaceTests
{
	public class VCSBase : CreateBuildTemplatesTestBase
	{
		public enum VersionControlType
		{
			Git,
			Subversion
		}

		protected string CheckoutOrClone (string repoUrl, string cloneToLocation = null, VersionControlType cvsType = VersionControlType.Git, int cloneTimeoutSecs = 180)
		{
			cloneToLocation = cloneToLocation ?? Util.CreateTmpDir ("clone");
			ReproStep ("Click on Version Control > Checkout from Menu Bar");
			Session.ExecuteCommand (MonoDevelop.VersionControl.Commands.Checkout);

			WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog"),
			               "Select Repository window should open",
			               "Select Reprository window did not open");
			TakeScreenShot ("Checkout-Window-Ready");

			ReproStep (string.Format ("Select Type to '{0}'", cvsType));
			Assert.IsTrue (Session.SelectElement (c => c.Marked ("repCombo").Model ().Text (cvsType.ToString ())));

			ReproStep (string.Format ("Enter URL as '{0}'", repoUrl));
			Assert.IsTrue (Session.EnterText (c => c.Textfield ().Marked ("repositoryUrlEntry"), repoUrl));

			Assert.IsTrue (Session.EnterText (c => c.Textfield ().Marked ("entryFolder"), cloneToLocation));
			Session.WaitForElement (c => c.Textfield ().Marked ("entryFolder").Text (cloneToLocation));

			TakeScreenShot ("Before-Clicking-OK");
			ReproStep ("Click OK");
			Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog").Children ().Button ().Marked ("buttonOk")));

			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.Dialogs.ProgressDialog"), 15000);
			TakeScreenShot ("CheckoutClone-In-Progress");
			ReproStep ("Wait for Clone to Finish");
			WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.Dialogs.ProgressDialog"),
			                string.Format ("Clone should finish within {0} seconds", cloneTimeoutSecs),
			                string.Format ("Clone failed to finish within {0} seconds", cloneTimeoutSecs),
			                cloneTimeoutSecs * 1000);

			return cloneToLocation;
		}

		protected void TestClone (string repoUrl, string cloneToLocation = null, VersionControlType cvsType = VersionControlType.Git, int cloneTimeoutSecs = 180)
		{
			var checkoutFolder = CheckoutOrClone (repoUrl, cloneToLocation, cvsType, cloneTimeoutSecs);
			FoldersToClean.Add (checkoutFolder);
		}

		protected void TestGitStash (string stashMsg, int timeoutStashSecs = 10)
		{
			ReproStep ("Click on Version Control > Stash");
			Session.ExecuteCommand (MonoDevelop.VersionControl.Git.Commands.Stash);

			WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.NewStashDialog"), "Stash Dialog should open", "Stash Dialog did not open");
			TakeScreenShot ("Stash-Dialog-Opened");

			ReproStep ("Enter a stash message");
			Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.NewStashDialog").Children ().Textfield ().Marked ("entryComment"), stashMsg);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.NewStashDialog").Children ().Textfield ().Marked ("entryComment").Text (stashMsg));
			TakeScreenShot ("Stash-Message-Entered");

			ReproStep ("Click on OK");
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.NewStashDialog").Children ().Button ().Marked ("buttonOk"));
			Ide.WaitForStatusMessage (new [] { "Changes successfully stashed" }, timeoutStashSecs);
		}

		protected void TestGitUnstash ()
		{
			ReproStep ("Click on Version Control > Pop Stash");
			Session.ExecuteCommand (MonoDevelop.VersionControl.Git.Commands.StashPop);

			WaitForElement (() => Ide.WaitForStatusMessage (new[] {"Stash successfully applied"}, 10), "Stash should apply successfully", "Stash failed to apply");
		}

		protected void TestCommit (string commitMsg)
		{
			ReproStep ("Click on Version Control > Review Solution and Commit from Menu Bar");
			Session.ExecuteCommand (MonoDevelop.VersionControl.Commands.SolutionStatus);

			ReproStep ("Wait for diff to be available");
			WaitForElement (c =>  c.Button ().Marked ("buttonCommit").Sensitivity (true), "Commit button should become enabled", "Commit button was not enabled");

			ReproStep ("Click on Commit Button");
			Session.ClickElement (c => c.Button ().Marked ("buttonCommit"), false);

			WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.CommitDialog"), "Commit Dialog should open", "Commit Dialog did not open");
			TakeScreenShot ("Commit-Dialog-Opened");

			ReproStep ("Enter commit message and click on Commit");
			Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.CommitDialog").Children ().TextView ().Marked ("textview"), commitMsg);
			TakeScreenShot ("Commit-Msg-Entered");
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.CommitDialog").Children ().Button ().Marked ("buttonCommit"), false);
			CheckIfNameEmailNeeded ();
			CheckIfUserConflict ();

			WaitForElement (() => Ide.WaitForStatusMessage (new [] { "Commit operation completed." }),
			                "Status bar should show 'Commit operation completed.'",
			                "Status bar did not show 'Commit operation completed.'");
			TakeScreenShot ("Commit-Completed");

			ReproStep ("Close currently commit tab");
			Session.ExecuteCommand (FileCommands.CloseFile);
			Session.WaitForElement (IdeQuery.TextArea);
		}

		protected void GitCreateAndCommit (TemplateSelectionOptions templateOptions, string commitMessage)
		{
			CreateProject (templateOptions, 
				new ProjectDetails (templateOptions),
				new GitOptions { UseGit = true, UseGitIgnore = true });

			Session.WaitForElement (IdeQuery.TextArea);
			TestCommit (commitMessage);
		}

		protected string MakeSomeChangesAndSaveAll (string waitForFile = null)
		{
			if (waitForFile != null) {
				WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench").Property ("TabControl.CurrentTab.Text", waitForFile),
				                string.Format ("File '{0}' should open", waitForFile),
				                string.Format ("File {0} did not open", waitForFile));
			}

			Session.WaitForElement (IdeQuery.TextArea);
			TakeScreenShot ("Ready-To-Make-Changes");
			Session.SelectElement (IdeQuery.TextArea);
			ReproStep ("Make some random changes to the file");
			for (int i = 0; i < 10; i++) {
				Session.ExecuteCommand (TextEditorCommands.InsertNewLine);
				Session.ExecuteCommand (TextEditorCommands.InsertTab);
			}
			TakeScreenShot ("Made-Changes-To-Doc");

			ReproStep ("Click on File > Save All from Menu Bar");
			Session.ExecuteCommand (FileCommands.SaveAll);
			TakeScreenShot ("Inserted-Newline-SaveAll-Called");

			return "Entered new blank line";
		}

		protected void CheckIfNameEmailNeeded ()
		{
			try {
				Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserGitConfigDialog"));
				TakeScreenShot ("Git-User-Not-Configured");
				EnterGitUserConfig ("John Doe", "john.doe@example.com");
			} catch (TimeoutException e) { }
		}

		protected void EnterGitUserConfig (string gitUser, string gitEmail)
		{
			Session.ToggleElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserGitConfigDialog").Children ().CheckButton ().Marked ("repoConfigRadio"), true);

			Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserGitConfigDialog").Children ().Textfield ().Marked ("usernameEntry"), gitUser);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserGitConfigDialog").Children ().Textfield ().Marked ("usernameEntry").Text (gitUser));

			Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserGitConfigDialog").Children ().Textfield ().Marked ("emailEntry"), gitEmail);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserGitConfigDialog").Children ().Textfield ().Marked ("emailEntry").Text (gitEmail));

			TakeScreenShot ("Git-User-Email-Filled");
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserGitConfigDialog").Children ().Button ().Marked ("buttonOk"));
			if (Session.Query (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserInfoConflictDialog")).Length > 0) {
				TakeScreenShot ("Provided-User-Details-Mismatch");
				Session.ToggleElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserInfoConflictDialog").Children ().CheckButton ().Marked ("radiobutton2"), true);
				TakeScreenShot ("Selected-Use-Git-Config");
				Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.UserInfoConflictDialog").Children ().Button ().Marked ("buttonOk"));
			}
		}

		protected void CheckIfUserConflict ()
		{
			try {
				Session.WaitForElement (c => c.Window ().Marked ("User Information Conflict"));
				Session.ClickElement (c => c.Window ().Marked ("User Information Conflict").Children ().Button ().Text ("OK"));
			} catch (TimeoutException) {
			}
		}

		protected override void OnBuildTemplate (int buildTimeoutInSecs = 180)
		{
		}
	}
}

