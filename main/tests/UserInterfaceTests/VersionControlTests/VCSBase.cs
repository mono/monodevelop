//
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
using MonoDevelop.Components.AutoTest;
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
			Session.ExecuteCommand (MonoDevelop.VersionControl.Commands.Checkout);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog"));
			TakeScreenShot ("Checkout-Window-Ready");
			Assert.IsTrue (Session.SelectElement (c => c.Marked ("repCombo").Model ().Text (cvsType.ToString ())));
			Assert.IsTrue (Session.EnterText (c => c.Textfield ().Marked ("repositoryUrlEntry"), repoUrl));
			Assert.IsTrue (Session.EnterText (c => c.Textfield ().Marked ("entryFolder"), cloneToLocation));
			Session.WaitForElement (c => c.Textfield ().Marked ("entryFolder").Text (cloneToLocation));
			TakeScreenShot ("Before-Clicking-OK");
			Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.SelectRepositoryDialog").Children ().Button ().Marked ("buttonOk")));
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.Dialogs.ProgressDialog"), 15000);
			TakeScreenShot ("CheckoutClone-In-Progress");
			Session.WaitForNoElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.Dialogs.ProgressDialog"), cloneTimeoutSecs * 1000);

			return cloneToLocation;
		}

		protected void TestClone (string repoUrl, string cloneToLocation = null, VersionControlType cvsType = VersionControlType.Git, int cloneTimeoutSecs = 180)
		{
			var checkoutFolder = CheckoutOrClone (repoUrl, cloneToLocation, cvsType, cloneTimeoutSecs);
			FoldersToClean.Add (checkoutFolder);
		}

		protected void TestGitStash (string stashMsg, int timeoutStashSecs = 10)
		{
			Session.ExecuteCommand (MonoDevelop.VersionControl.Git.Commands.Stash);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.NewStashDialog"));
			TakeScreenShot ("Stash-Dialog-Opened");
			Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.NewStashDialog").Children ().Textfield ().Marked ("entryComment"), stashMsg);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.NewStashDialog").Children ().Textfield ().Marked ("entryComment").Text (stashMsg));
			TakeScreenShot ("Stash-Message-Entered");
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.NewStashDialog").Children ().Button ().Marked ("buttonOk"));
			Ide.WaitForStatusMessage (new [] { "Changes successfully stashed" }, timeoutStashSecs);
		}

		protected void TestGitUnstash ()
		{
			Session.ExecuteCommand (MonoDevelop.VersionControl.Git.Commands.StashPop);
			Ide.WaitForStatusMessage (new[] {"Stash successfully applied"}, 10);
		}

		protected void TestCommit (string commitMsg)
		{
			Session.ExecuteCommand (MonoDevelop.VersionControl.Commands.SolutionStatus);
			Session.ClickElement (c => c.Button ().Marked ("buttonCommit"), false);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.CommitDialog"));
			TakeScreenShot ("Commit-Dialog-Opened");
			Session.EnterText (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.CommitDialog").Children ().TextView ().Marked ("textview"), commitMsg);
			TakeScreenShot ("Commit-Msg-Entered");
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Dialogs.CommitDialog").Children ().Button ().Marked ("buttonCommit"), false);
			CheckIfNameEmailNeeded ();
			Ide.WaitForStatusMessage (new[] {"Commit operation completed."});
			TakeScreenShot ("Commit-Completed");
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

		protected void GitCreateAndCommit (TemplateSelectionOptions templateOptions, string commitMessage)
		{
			CreateProject (templateOptions, 
				new ProjectDetails (templateOptions),
				new GitOptions { UseGit = true, UseGitIgnore = true });

			Session.WaitForElement (IdeQuery.TextArea);
			TestCommit (commitMessage);

			Session.ExecuteCommand (FileCommands.CloseFile);
			Session.WaitForElement (IdeQuery.TextArea);
		}

		protected string MakeSomeChangesAndSaveAll (string waitForFile = null)
		{
			if (waitForFile != null)
				Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.DefaultWorkbench").Property ("TabControl.CurrentTab.Text", waitForFile));
			Session.WaitForElement (IdeQuery.TextArea);
			TakeScreenShot ("Ready-To-Make-Changes");
			Session.SelectElement (IdeQuery.TextArea);
			for (int i = 0; i < 10; i++) {
				Session.ExecuteCommand (TextEditorCommands.InsertNewLine);
				Session.ExecuteCommand (TextEditorCommands.InsertTab);
			}
			TakeScreenShot ("Made-Changes-To-Doc");
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

		protected override void OnBuildTemplate (int buildTimeoutInSecs = 180)
		{
		}
	}
}

