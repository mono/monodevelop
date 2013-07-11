
using System;
using System.Collections;
using Gtk;
using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using Mono.Addins;
using MonoDevelop.Ide;
using MonoDevelop.Projects;

namespace MonoDevelop.VersionControl.Dialogs
{
	partial class CommitDialog : Gtk.Dialog
	{
		ListStore store;
		ArrayList selected = new ArrayList ();
		ArrayList extensions = new ArrayList ();
		ChangeSet changeSet;
		string oldMessage;

		public CommitDialog (ChangeSet changeSet)
		{
			Build ();

			store = new ListStore(typeof (Gdk.Pixbuf), typeof (string), typeof (string), typeof(bool), typeof(object));
			fileList.Model = store;
			this.changeSet = changeSet;
			oldMessage = changeSet.GlobalComment;

			CellRendererText crt = new CellRendererText ();
			var crp = new CellRendererPixbuf ();
			TreeViewColumn colStatus = new TreeViewColumn ();
			colStatus.Title = GettextCatalog.GetString ("Status");
			colStatus.PackStart (crp, false);
			colStatus.PackStart (crt, true);
			colStatus.Spacing = 2;
			colStatus.AddAttribute (crp, "pixbuf", 0);
			colStatus.AddAttribute (crt, "text", 1);
			CellRendererToggle cellToggle = new CellRendererToggle();
			cellToggle.Toggled += new ToggledHandler(OnCommitToggledHandler);
			TreeViewColumn colCommit = new TreeViewColumn ("", cellToggle, "active", 3);
			TreeViewColumn colFile = new TreeViewColumn (GettextCatalog.GetString ("File"), new CellRendererText(), "text", 2);
			
			fileList.AppendColumn(colCommit);
			fileList.AppendColumn(colStatus);
			fileList.AppendColumn(colFile);
			
			colCommit.Visible = false;
			
			object[] exts = AddinManager.GetExtensionObjects ("/MonoDevelop/VersionControl/CommitDialogExtensions", false);
			bool separatorRequired = false;
			
			foreach (object ob in exts) {
				CommitDialogExtension ext = ob as CommitDialogExtension;
				if (ext == null) {
					MessageService.ShowError ("Commit extension type " + ob.GetType() + " must be a subclass of CommitDialogExtension");
					continue;
				}
				if (ext.Initialize (changeSet)) {
					if (separatorRequired) {
						HSeparator sep = new HSeparator ();
						sep.Show ();
						vboxExtensions.PackEnd (sep, false, false, 0);
					}
					vboxExtensions.PackEnd (ext, false, false, 0);
					extensions.Add (ext);
					ext.AllowCommitChanged += HandleAllowCommitChanged;
					separatorRequired = true;
				} else
					ext.Destroy ();
			}
			HandleAllowCommitChanged (null, null);

			foreach (ChangeSetItem info in changeSet.Items) {
				Gdk.Pixbuf statusicon = VersionControlService.LoadIconForStatus (info.Status);
				string lstatus = VersionControlService.GetStatusLabel (info.Status);
				string localpath;

				if (info.IsDirectory)
					localpath = (!info.LocalPath.IsChildPathOf (changeSet.BaseLocalPath)?
									".":
									(string) info.LocalPath.ToRelative (changeSet.BaseLocalPath));
				else
					localpath = System.IO.Path.GetFileName((string) info.LocalPath);

				if (localpath.Length > 0 && localpath[0] == System.IO.Path.DirectorySeparatorChar) localpath = localpath.Substring(1);
				if (localpath == "") { localpath = "."; } // not sure if this happens
				
				store.AppendValues (statusicon, lstatus, localpath, true, info);
				selected.Add (info.LocalPath);
			}

			if (string.IsNullOrEmpty (changeSet.GlobalComment)) {
				AuthorInformation aInfo;
				CommitMessageFormat fmt = VersionControlService.GetCommitMessageFormat (changeSet, out aInfo);
				Message = changeSet.GenerateGlobalComment (fmt, aInfo);
			}
			else
				Message = changeSet.GlobalComment;
				
			textview.Buffer.Changed += OnTextChanged;
			
			// Focus the text view and move the insert point to the beginning. Makes it easier to insert
			// a comment header.
			textview.Buffer.MoveMark (textview.Buffer.InsertMark, textview.Buffer.StartIter);
			textview.Buffer.MoveMark (textview.Buffer.SelectionBound, textview.Buffer.StartIter);
			textview.GrabFocus ();
			textview.Buffer.MarkSet += OnMarkSet;
		}

		void HandleAllowCommitChanged (object sender, EventArgs e)
		{
			bool allowCommit = true;
			foreach (CommitDialogExtension ext in extensions)
				allowCommit &= ext.AllowCommit;
			SetResponseSensitive (Gtk.ResponseType.Ok, allowCommit);
		}
		
		protected override void OnResponse (Gtk.ResponseType type)
		{
			if (type != Gtk.ResponseType.Ok) {
				changeSet.GlobalComment = oldMessage;
			}
			base.OnResponse (type);
		}

		protected void OnButtonCommitClicked (object sender, System.EventArgs e)
		{
			// In case we have local unsaved files with changes, throw a dialog for the user.
			System.Collections.Generic.List<Document> docList = new System.Collections.Generic.List<Document> ();
			foreach (var item in IdeApp.Workbench.Documents) {
				if (!item.IsDirty || !selected.Contains (item.FileName))
					continue;
				docList.Add (item);
			}

			if (docList.Count != 0) {
				AlertButton response = MessageService.GenericAlert (
					MonoDevelop.Ide.Gui.Stock.Question,
					GettextCatalog.GetString ("You are trying to commit files which have unsaved changes."),
					GettextCatalog.GetString ("Do you want to save the changes before committing?"),
					new AlertButton[] {
						AlertButton.Cancel,
						new AlertButton ("Don't Save"),
						AlertButton.Save
					}
				);

				if (response == AlertButton.Cancel)
					return;

				if (response == AlertButton.Save) {
					// Go through all the items and save them.
					foreach (var item in docList)
						item.Save ();

					// Check if save failed on any item and abort.
					foreach (var item in docList)
						if (item.IsDirty) {
							MessageService.ShowMessage (GettextCatalog.GetString (
								"Some files could not be saved. Commit operation aborted"));
							return;
						}
				}

				docList.Clear ();
			}

			// Update the change set
			ArrayList todel = new ArrayList ();
			foreach (ChangeSetItem it in changeSet.Items) {
				if (!selected.Contains (it.LocalPath))
					todel.Add (it.LocalPath);
			}
			foreach (string file in todel)
				changeSet.RemoveFile (file);
			changeSet.GlobalComment = Message;
			
			// Perform the commit
			
			int n;
			for (n=0; n<extensions.Count; n++) {
				CommitDialogExtension ext = (CommitDialogExtension) extensions [n];
				bool res;
				try {
					res = ext.OnBeginCommit (changeSet);
				} catch (Exception ex) {
					MessageService.ShowException (ex);
					res = false;
				}
				System.Console.WriteLine ("RES: " + res);
				if (!res) {
					// Commit failed. Rollback the previous extensions
					for (int m=0; m<n; m++) {
						ext = (CommitDialogExtension) extensions [m];
						try {
							ext.OnEndCommit (changeSet, false);
						} catch {}
					}
					return;
				}
				Hide ();
			}
			Respond (Gtk.ResponseType.Ok);
		}

		void UpdatePositionLabel (TextIter iter)
		{
			int row = iter.Line + 1;
			int col = iter.LineOffset + 1;
			label3.Text = String.Format ("{0}/{1}", row, col);
		}

		void OnMarkSet (object o, MarkSetArgs e)
		{
			if (e.Mark != textview.Buffer.InsertMark)
				return;

			UpdatePositionLabel (e.Location);
		}
		
		void OnTextChanged (object s, EventArgs args)
		{
			changeSet.GlobalComment = Message;
			UpdatePositionLabel (textview.Buffer.GetIterAtOffset (textview.Buffer.CursorPosition));
		}
		
		public void EndCommit (bool success)
		{
			foreach (CommitDialogExtension ext in extensions) {
				try {
					ext.OnEndCommit (changeSet, success);
				} catch {}
			}
		}
		
		public string[] GetFilesToCommit ()
		{
			return (string[]) selected.ToArray (typeof(string));
		}
		
		public string Message {
			get { return textview.Buffer.Text; }
			set { textview.Buffer.Text = value; }
		}
		
		void OnCommitToggledHandler(object o, ToggledArgs args)
		{
			TreeIter pos;
			if (!store.GetIterFromString (out pos, args.Path))
				return;
			bool active = !(bool) store.GetValue (pos, 3);
			ChangeSetItem vinfo = (ChangeSetItem) store.GetValue (pos, 4);
			store.SetValue (pos, 3, active);
			if (active)
				selected.Add (vinfo.LocalPath);
			else
				selected.Remove (vinfo.LocalPath);
		}
	}
}
