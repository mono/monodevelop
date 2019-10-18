
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide;
using System.Collections.Generic;
using System.Threading.Tasks;
using MonoDevelop.VersionControl.Dialogs;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MonoDevelop.VersionControl
{
	class CommitCommand
	{
		public static async Task CommitAsync (Repository vc, ChangeSet changeSet)
		{
			try {
				VersionControlService.NotifyPrepareCommit (vc, changeSet);
				if (!await VerifyUnsavedChangesAsync (changeSet))
					return;
				CommitDialog dlg = new CommitDialog (vc, changeSet);
				try {
					MessageService.RunCustomDialog (dlg);
				} finally {
					dlg.Destroy ();
					dlg.Dispose ();
				}
				VersionControlService.NotifyAfterCommit (vc, changeSet, false);
			}
			catch (Exception ex) {
				MessageService.ShowError (GettextCatalog.GetString ("Version control command failed."), ex);
			}
		}

		static async Task<bool> VerifyUnsavedChangesAsync (ChangeSet changeSet)
		{
			bool allowCommit = true;
			// In case we have local unsaved files with changes, ask the user to save them.
			List<Document> docList = new List<Document> ();
			foreach (var item in IdeApp.Workbench.Documents) {
				if (item.IsDirty)
					docList.Add (item);
			}

			if (docList.Count != 0) {
				AlertButton dontSaveButton = new AlertButton (GettextCatalog.GetString ("Don't Save"));
				AlertButton response = MessageService.GenericAlert (
					Stock.Question,
					GettextCatalog.GetString ("You are trying to commit files which have unsaved changes."),
					GettextCatalog.GetString ("Do you want to save the changes before committing?"),
					new AlertButton [] {
						AlertButton.Cancel,
						dontSaveButton,
						AlertButton.Save
					}
				);

				if (response == AlertButton.Cancel) {
					return false;
				}

				if (response == dontSaveButton) {
					allowCommit = true;
				}

				if (response == AlertButton.Save) {
					// Go through all the items and save them.
					foreach (var item in docList) {
						await item.Save ();
						if (!changeSet.ContainsFile (item.FileName) && allowCommit)
							allowCommit = false;
					}

					// Check if save failed on any item.
					foreach (var item in docList)
						if (item.IsDirty) {
							MessageService.ShowMessage (GettextCatalog.GetString (
								"Some files could not be saved."));
							return false;
						}
				}
			}

			return allowCommit;
		}

		
	}
}
