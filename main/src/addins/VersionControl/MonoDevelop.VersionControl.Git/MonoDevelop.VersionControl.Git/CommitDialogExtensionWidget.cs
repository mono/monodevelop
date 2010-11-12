// 
// CommitDialogExtensionWidget.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Ide;

namespace MonoDevelop.VersionControl.Git
{
	public class CommitDialogExtensionWidget: CommitDialogExtension
	{
		Gtk.CheckButton pushCheckbox;
		
		public override bool Initialize (ChangeSet changeSet)
		{
			if (changeSet.Repository is GitRepository) {
				pushCheckbox = new Gtk.CheckButton (GettextCatalog.GetString ("Push changes to remote repository after commit"));
				Add (pushCheckbox);
				ShowAll ();
				return true;
			} else
				return false;
		}
		
		public override bool OnBeginCommit (ChangeSet changeSet)
		{
			// In this callback we check if the user information configured in Git
			// matches the user information configured in MonoDevelop. If the configurations
			// don't match, it shows a dialog asking the user what to do.
			
			GitRepository repo = (GitRepository) changeSet.Repository;
			
			Solution sol = null;
			// Locate the solution to which the changes belong
			foreach (Solution s in IdeApp.Workspace.GetAllSolutions ()) {
				if (s.BaseDirectory == changeSet.BaseLocalPath || changeSet.BaseLocalPath.IsChildPathOf (s.BaseDirectory)) {
					sol = s;
					break;
				}
			}
			if (sol == null)
				return true;
			
			string val = sol.UserProperties.GetValue<string> ("GitUserInfo");
			if (val == "UsingMD") {
				// If the solution is configured to use the MD configuration, make sure the Git config is up to date.
				string user;
				string email;
				repo.GetUserInfo (out user, out email);
				if (user != sol.AuthorInformation.Name || email != sol.AuthorInformation.Email)
					repo.SetUserInfo (sol.AuthorInformation.Name, sol.AuthorInformation.Email);
			}
			else if (val != "UsingGIT") {
				string user;
				string email;
				repo.GetUserInfo (out user, out email);
				if (user != sol.AuthorInformation.Name || email != sol.AuthorInformation.Email) {
					// There is a conflict. Ask the user what to do
					string gitInfo = GetDesc (user, email);
					string mdInfo = GetDesc (sol.AuthorInformation.Name, sol.AuthorInformation.Email);
					
					UserInfoConflictDialog dlg = new UserInfoConflictDialog (mdInfo, gitInfo);
					try {
						if (MessageService.RunCustomDialog (dlg) == (int) Gtk.ResponseType.Ok) {
							if (dlg.UseMonoDevelopConfig) {
								repo.SetUserInfo (sol.AuthorInformation.Name, sol.AuthorInformation.Email);
								sol.UserProperties.SetValue ("GitUserInfo", "UsingMD");
							} else
								sol.UserProperties.SetValue ("GitUserInfo", "UsingGIT");
							sol.SaveUserProperties ();
						}
						else
							return false;
					} finally {
						dlg.Destroy ();
					}
				}
			}
			return true;
		}
		
		string GetDesc (string name, string email)
		{
			if (string.IsNullOrEmpty (name) && string.IsNullOrEmpty (email))
				return "Not configured";
			if (string.IsNullOrEmpty (name))
				name = GettextCatalog.GetString ("Name not configured");
			if (string.IsNullOrEmpty (email))
				email = GettextCatalog.GetString ("e-mail not configured");
			return name + ", " + email;
		}
		
		public override void OnEndCommit (ChangeSet changeSet, bool success)
		{
			if (success && pushCheckbox.Active)
				GitService.Push ((GitRepository) changeSet.Repository);
		}
	}
}

