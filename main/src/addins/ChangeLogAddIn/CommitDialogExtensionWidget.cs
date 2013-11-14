// CommitDialogExtensionWidget.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Collections.Generic;
using System.IO;
using Gtk;
using MonoDevelop.VersionControl;
using MonoDevelop.Core;
using MonoDevelop.Projects.Text;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.ChangeLogAddIn
{
	public class CommitDialogExtensionWidget: CommitDialogExtension
	{
		readonly HBox box = new HBox ();
		readonly VBox vbox = new VBox ();
		readonly Button logButton;
		readonly Button optionsButton;
		ChangeSet cset;
		Label msgLabel;
		Label pathLabel;
		bool notConfigured;
		Dictionary<string,ChangeLogEntry> entries;
		int unknownFileCount;
		int uncommentedCount;
		bool requireComment;
		bool enabled;
		
		public CommitDialogExtensionWidget()
		{	
			Add (box);
			box.PackStart (vbox, true, true, 0);
			
			logButton = new Button (GettextCatalog.GetString ("View ChangeLog..."));
			logButton.Clicked += OnClickButton;
			
			optionsButton = new Button (GettextCatalog.GetString ("Options..."));
			optionsButton.Clicked += OnClickOptions;
			
			var aux = new VBox ();
			box.PackStart (aux, false, false, 3);
			var haux = new HBox ();
			haux.Spacing = 6;
			aux.PackStart (haux, false, false, 0);
			haux.PackStart (logButton, false, false, 0);
			haux.PackStart (optionsButton, false, false, 0);
		}
		
		public override bool Initialize (ChangeSet changeSet)
		{	
			cset = changeSet;
			msgLabel = new Label ();
			pathLabel = new Label ();
			msgLabel.Xalign = 0;
			pathLabel.Xalign = 0;
			vbox.PackStart (msgLabel, false, false, 0);
			vbox.PackStart (pathLabel, false, false, 3);
			
			GenerateLogEntries ();
			if (enabled) {
				ShowAll ();
				UpdateStatus ();
			}
			return enabled;
		}
		
		void UpdateStatus ()
		{
			if (!enabled)
				Remove (box);
			
			AllowCommit = !requireComment;
			
			if (!AuthorInformation.Default.IsValid) {
				msgLabel.Markup = "<b><span foreground='red'>" + GettextCatalog.GetString ("ChangeLog entries can't be generated.") + "</span></b>";
				pathLabel.Text = GettextCatalog.GetString ("The name or e-mail of the user has not been configured.");
				logButton.Label = GettextCatalog.GetString ("Configure user data");
				optionsButton.Visible = false;
				notConfigured = true;
				return;
			}
			
			optionsButton.Visible = true;
			logButton.Label = GettextCatalog.GetString ("Details...");
			string warning = "";
			if (uncommentedCount > 0) {
				string fc = GettextCatalog.GetString ("There are {0} files without a comment.\nThe ChangeLog entry for those files will not be generated.", uncommentedCount);
				if (requireComment)
					fc += "\n" + GettextCatalog.GetString ("Some of the projects require that files have comments when they are committed.");
				warning = "<b><span foreground='red'>" + fc + "</span></b>\n";
			}
			if (unknownFileCount > 0) {
				string fc = GettextCatalog.GetPluralString ("{0} ChangeLog file not found. Some changes will not be logged.","{0} ChangeLog files not found. Some changes will not be logged.", unknownFileCount, unknownFileCount);
				msgLabel.Markup = warning + "<b><span foreground='red'>" + fc + "</span></b>";
				pathLabel.Text = GettextCatalog.GetString ("Click on the 'Details' button for more info.");
			} else if (entries.Count == 1) {
				msgLabel.Markup = warning + "<b>" + GettextCatalog.GetString ("The following ChangeLog file will be updated:") + "</b>";
				pathLabel.Ellipsize = Pango.EllipsizeMode.Start;
				foreach (ChangeLogEntry e in entries.Values)
					pathLabel.Text = e.File;
			} else {
				msgLabel.Markup = warning + "<b>" + GettextCatalog.GetString ("{0} ChangeLog files will be updated.", entries.Count) + "</b>";
				pathLabel.Text = GettextCatalog.GetString ("Click on the 'Details' button for more info.");
			}
			notConfigured = false;
		}
		
		public override bool OnBeginCommit (ChangeSet changeSet)
		{
			if (!enabled)
				return true;

			try {
				foreach (ChangeLogEntry ce in entries.Values) {
					if (ce.CantGenerate)
						continue;

					// Make a backup copy of the log file
					if (!ce.IsNew) {
						ce.BackupFile = System.IO.Path.GetTempFileName ();
						FileService.CopyFile (ce.File, ce.BackupFile);
						
						// Read the log file and add the new entry
						TextFile tf = TextFile.ReadFile (ce.File);
						tf.InsertText (0, ce.Message);
						tf.Save ();
					} else {
						File.WriteAllText (ce.File, ce.Message);
					}
					if (!cset.ContainsFile (ce.File)) {
						if (!cset.Repository.GetVersionInfo (ce.File).IsVersioned)
							cset.Repository.Add (ce.File, false, new MonoDevelop.Core.ProgressMonitoring.NullProgressMonitor ());
						cset.AddFile (ce.File);
					}
				}
				return true;
			}
			catch (Exception e) {
				// Restore the makefiles
				LoggingService.LogError (e.ToString ());
				RollbackMakefiles ();
				throw;
			}
		}
		
		void RollbackMakefiles ()
		{
			foreach (ChangeLogEntry ce in entries.Values) {
				if (ce.BackupFile != null && File.Exists (ce.BackupFile)) {
					try {
						FileService.CopyFile (ce.BackupFile, ce.File);
						FileService.DeleteFile (ce.BackupFile);
						ce.BackupFile = null;
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
				else if (ce.IsNew && File.Exists (ce.File)) {
					// Remove generated files
					try {
						FileService.DeleteFile (ce.File);
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
			}
		}
		
		void DeleteBackupFiles ()
		{
			foreach (ChangeLogEntry ce in entries.Values) {
				if (ce.BackupFile != null && File.Exists (ce.BackupFile)) {
					try {
						FileService.DeleteFile (ce.BackupFile);
						ce.BackupFile = null;
					} catch (Exception ex) {
						LoggingService.LogError (ex.ToString ());
					}
				}
			}
		}
		
		public override void OnEndCommit (ChangeSet changeSet, bool success)
		{
			if (!enabled)
				return;
				
			if (!success)
				RollbackMakefiles ();
			else
				DeleteBackupFiles ();
		}
		
		void GenerateLogEntries ()
		{
			entries = new Dictionary<string,ChangeLogEntry> ();
			unknownFileCount = 0;
			enabled = false;
			requireComment = false;
			
			foreach (ChangeSetItem item in cset.Items) {
				SolutionItem parentItem;
				ChangeLogPolicy policy;
				string logf = ChangeLogService.GetChangeLogForFile (cset.BaseLocalPath, item.LocalPath,
				                                                    out parentItem, out policy);
				
				if (logf == string.Empty)
					continue;
				
				enabled = true;
				bool cantGenerate = false;
				
				if (logf == null) {
					cantGenerate = true;
					logf = System.IO.Path.GetDirectoryName (item.LocalPath);
					logf = System.IO.Path.Combine (logf, "ChangeLog");
				}
				
				if (string.IsNullOrEmpty (item.Comment) && !item.IsDirectory) {
					uncommentedCount++;
					requireComment |= policy != null && policy.VcsIntegration == VcsIntegration.RequireEntry;
				}
				
				ChangeLogEntry entry;
				if (!entries.TryGetValue (logf, out entry)) {
					entry = new ChangeLogEntry ();
					entry.AuthorInformation = parentItem != null ? parentItem.AuthorInformation : AuthorInformation.Default;
					entry.MessageStyle = ChangeLogService.GetMessageStyle (parentItem);
					entry.CantGenerate = cantGenerate;
					entry.File = logf;
					if (cantGenerate)
						unknownFileCount++;
				
					entry.IsNew |= !File.Exists (logf);
				
					entries [logf] = entry;
				}
				entry.Items.Add (item);
			}
			
			var format = new CommitMessageFormat ();
			format.TabsAsSpaces = false;
			format.TabWidth = 8;
			format.MaxColumns = 70;
			format.AppendNewlines = 2;
			foreach (ChangeLogEntry entry in entries.Values) {
				format.Style = entry.MessageStyle;
				entry.Message = cset.GeneratePathComment (entry.File, entry.Items, 
					format, entry.AuthorInformation);
			}
		}
		
		void OnClickButton (object s, EventArgs args)
		{
			if (notConfigured) {
				IdeApp.Workbench.ShowGlobalPreferencesDialog (Toplevel as Window, "GeneralAuthorInfo");
				UpdateStatus ();
				GenerateLogEntries ();
				return;
			}
			
			var dlg = new AddLogEntryDialog (entries);
			MessageService.ShowCustomDialog (dlg, (Window) Toplevel);
		}
		
		void OnClickOptions (object s, EventArgs args)
		{
			IdeApp.Workbench.ShowGlobalPreferencesDialog (Toplevel as Window, "GeneralAuthorInfo");
			UpdateStatus ();
			GenerateLogEntries ();
		}
	}
	
	class ChangeLogEntry
	{
		public string File;
		public string BackupFile;
		public string Message;
		public bool CantGenerate;
		public bool IsNew;
		public List<ChangeSetItem> Items = new List<ChangeSetItem> ();
		public AuthorInformation AuthorInformation;
		public CommitMessageStyle MessageStyle;
	}
}
