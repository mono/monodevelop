
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
				
				string localpath = (!info.LocalPath.IsChildPathOf (changeSet.BaseLocalPath)?
				                    ".":
				                    (string) info.LocalPath.ToRelative (changeSet.BaseLocalPath)); 
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
			
			// Focus the text view and move the insert point to the begining. Makes it easier to insert
			// a comment header.
			textview.Buffer.MoveMark (textview.Buffer.InsertMark, textview.Buffer.StartIter);
			textview.Buffer.MoveMark (textview.Buffer.SelectionBound, textview.Buffer.StartIter);
			textview.GrabFocus ();
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
			if (type == Gtk.ResponseType.Ok) {
			
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
			} else {
				changeSet.GlobalComment = oldMessage;
			}
			base.OnResponse (type);
		}
		
		void OnTextChanged (object s, EventArgs args)
		{
			changeSet.GlobalComment = Message;
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
