
using System;
using System.IO;
using Gtk;
using MonoDevelop.VersionControl;
using MonoDevelop.Core;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.ChangeLogAddIn
{
	public class CommitDialogExtensionWidget: CommitDialogExtension
	{
		HBox box = new HBox ();
		VBox vbox = new VBox ();
		Button logButton;
		string message;
		ChangeSet cset;
		string tmpFile;
		Label msgLabel;
		Label pathLabel;
		bool notConfigured;
		
		public CommitDialogExtensionWidget()
		{
			Add (box);
			box.PackStart (vbox, true, true, 0);
			
			logButton = new Button (GettextCatalog.GetString ("View ChangeLog..."));
			logButton.Clicked += OnClickButton;
			VBox aux = new VBox ();
			aux.PackStart (logButton, false, false, 0);
			box.PackStart (aux, false, false, 3);
		}
		
		public override void Initialize (ChangeSet cset)
		{
			this.cset = cset;
			msgLabel = new Label ();
			pathLabel = new Label ();
			msgLabel.Xalign = 0;
			pathLabel.Xalign = 0;
			vbox.PackStart (msgLabel, false, false, 0);
			vbox.PackStart (pathLabel, false, false, 3);
			ShowAll ();
			UpdateStatus ();
		}
		
		void UpdateStatus ()
		{
			string name = Runtime.Properties.GetProperty ("ChangeLogAddIn.Name", "");
			string email = Runtime.Properties.GetProperty ("ChangeLogAddIn.Email", "");
			string logf = GetChangelogFile (cset.BaseLocalPath);
			
			if (name.Length == 0 || email.Length == 0) {
				msgLabel.Markup = "<b><span foreground='red'>" + GettextCatalog.GetString ("ChangeLog entries can't be generated.") + "</span></b>";
				pathLabel.Text = GettextCatalog.GetString ("The name or e-mail of the user has not been configured.");
				logButton.Label = GettextCatalog.GetString ("Configure user data");
				notConfigured = true;
				return;
			}
			
			logButton.Label = GettextCatalog.GetString ("View ChangeLog...");
			if (logf == null) {
				msgLabel.Markup = "<b><span foreground='red'>" + GettextCatalog.GetString ("There is no ChangeLog file in the path:") + "</span></b>";
				pathLabel.Text = cset.BaseLocalPath;
				logButton.Sensitive = false;
			} else if (!cset.Repository.CanCommit (logf)){
				msgLabel.Markup = GettextCatalog.GetString ("The following ChangeLog file (<b><span foreground='red'>not versioned</span></b>) will be updated:");
				pathLabel.Text = logf;
			} else {
				msgLabel.Markup = GettextCatalog.GetString ("The following ChangeLog file will be updated:");
				pathLabel.Text = logf;
			}
			notConfigured = false;
		}
		
		public override bool OnBeginCommit (ChangeSet changeSet)
		{
			string logf = GetChangelogFile (changeSet.BaseLocalPath);
			if (logf == null)
				return true;

			if (!changeSet.ContainsFile (logf) && changeSet.Repository.CanCommit (logf))
				changeSet.AddFile (logf);
				
			if (message == null)
				message = GetCommitMessage ();
			
			message = message.Trim ('\n');
			message += "\n\n";
			
			// Make a backup copy of the log file
			string auxf = System.IO.Path.GetTempFileName ();
			File.Copy (logf, auxf, true);
			tmpFile = auxf;
			
			// Read the log file and add the new entry
			try {
				TextFile tf = TextFile.ReadFile (logf);
				tf.InsertText (0, message);
				tf.Save ();
			} catch {
				// Restore the log file
				File.Copy (auxf, logf, true);
				throw;
			}
			return true;
		}
		
		public override void OnEndCommit (ChangeSet changeSet, bool success)
		{
			if (!success) {
				if (tmpFile != null && File.Exists (tmpFile)) {
					string logf = GetChangelogFile (changeSet.BaseLocalPath);
					File.Copy (tmpFile, logf, true);
				}
			}
			if (tmpFile != null && File.Exists (tmpFile))
				File.Delete (tmpFile);
		}
		
		void OnClickButton (object s, EventArgs args)
		{
			if (notConfigured) {
				IdeApp.Workbench.ShowGlobalPreferencesDialog (Toplevel as Gtk.Window, "ChangeLogAddInOptions");
				UpdateStatus ();
				return;
			}
			
			using (AddLogEntryDialog dlg = new AddLogEntryDialog ()) {
				if (message != null)
					dlg.Message = message;
				else
					dlg.Message = GetCommitMessage ();

				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					if (message != null || dlg.Message != GetCommitMessage ())
						message = dlg.Message;
				}
			}
		}
		
		string GetChangelogFile (string basePath)
		{
			string logfile = System.IO.Path.Combine (basePath, "ChangeLog");
			if (File.Exists (logfile))
				return logfile;
			else
				return null;
		}
		
		string GetCommitMessage ()
		{
			string msg = cset.GlobalComment;
			if (msg.Length == 0)
				msg = cset.GenerateGlobalComment (75);

			string name = Runtime.Properties.GetProperty ("ChangeLogAddIn.Name", "");
			string email = Runtime.Properties.GetProperty ("ChangeLogAddIn.Email", "");
			string date = DateTime.Now.ToString("yyyy-MM-dd");
			string text = date + "  " + name + " <" + email + "> " + Environment.NewLine + Environment.NewLine;
			
			msg = msg.Trim ('\n');
			text += "\t" + msg.Replace ("\n", "\n\t");

			return text;
		}
	}
}
