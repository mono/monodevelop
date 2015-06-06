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
	}

	public abstract class GitRepositoryConfigurationBase : VCSBase
	{
		protected void OpenRepositoryConfiguration ()
		{
			Session.ExecuteCommand (MonoDevelop.VersionControl.Git.Commands.ManageBranches);
			Session.WaitForElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog"));
			TakeScreenShot ("Repository-Configuration-Opened");
		}

		protected void CloseRepositoryConfiguration ()
		{
			Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Button ().Marked ("buttonOk"));
			Session.WaitForNoElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog"));
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

		void CreateEditBranch (string buttonName, string newBranchName)
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
			Assert.IsTrue (Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.VersionControl.Git.GitConfigurationDialog").Children ().Button ().Marked ("buttonSetDefaultBranch")));
			Assert.IsTrue (IsBranchSwitched (branchName));
			TakeScreenShot (string.Format ("Switched-To-{0}", branchName));
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
	}
}

