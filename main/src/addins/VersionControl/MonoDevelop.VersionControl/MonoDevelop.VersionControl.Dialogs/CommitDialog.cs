//
// CommitDialog.cs
//
// Authors:
//       Thiago Becker    (GSoC)
//       Rafael Giorgetti (GSoC)
//       Lluis Sanchez Gual <lluis@novell.com>
//       Michael Hutchinson  <mhutchinson@novell.com>
//       Ungureanu Marius <teromario@yahoo.com>
//
// Copyright (c) 2006-2011 Novell, Inc (http://www.novell.com)
// Copyright (c) 2013 Xamarin (http://www.xamarin.com)
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
using System.Collections;
using Gtk;
using MonoDevelop.Core;
 
using MonoDevelop.Ide.Gui;
using Mono.Addins;
using MonoDevelop.Ide;
using MonoDevelop.Projects;
using MonoDevelop.Components;
using System.Collections.Generic;
using System.Linq;

namespace MonoDevelop.VersionControl.Dialogs
{
	partial class CommitDialog : Gtk.Dialog
	{
		ListStore store;
		List<FilePath> selected = new List<FilePath> ();
		List<CommitDialogExtension> extensions = new List<CommitDialogExtension> ();
		ChangeSet changeSet;
		string oldMessage;
		bool responseSensitive;

		public CommitDialog (ChangeSet changeSet)
		{
			Build ();

			store = new ListStore(typeof (Xwt.Drawing.Image), typeof (string), typeof (string), typeof(bool), typeof(object));
			fileList.Model = store;
			fileList.SearchColumn = -1; // disable the interactive search
			this.changeSet = changeSet;
			oldMessage = changeSet.GlobalComment;

			CellRendererText crt = new CellRendererText ();
			var crp = new CellRendererImage ();
			TreeViewColumn colStatus = new TreeViewColumn ();
			colStatus.Title = GettextCatalog.GetString ("Status");
			colStatus.PackStart (crp, false);
			colStatus.PackStart (crt, true);
			colStatus.Spacing = 2;
			colStatus.AddAttribute (crp, "image", 0);
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
					LoggingService.LogError ("Commit extension type " + ob.GetType() + " must be a subclass of CommitDialogExtension");
					continue;
				}
				if (ext.Initialize (changeSet)) {
					var newTitle = ext.FormatDialogTitle (changeSet, Title);
					if (newTitle != null)
						Title = newTitle;

					ext.CommitMessageTextViewHook (textview);
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
				Xwt.Drawing.Image statusicon = VersionControlService.LoadIconForStatus (info.Status);
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
			responseSensitive = !string.IsNullOrEmpty (Message);
			
			// Focus the text view and move the insert point to the beginning. Makes it easier to insert
			// a comment header.
			textview.Buffer.MoveMark (textview.Buffer.InsertMark, textview.Buffer.StartIter);
			textview.Buffer.MoveMark (textview.Buffer.SelectionBound, textview.Buffer.StartIter);
			textview.GrabFocus ();
			textview.Buffer.MarkSet += OnMarkSet;

			SetResponseSensitive (ResponseType.Ok, responseSensitive);
		}

		void HandleAllowCommitChanged (object sender, EventArgs e)
		{
			bool allowCommit = responseSensitive;
			foreach (CommitDialogExtension ext in extensions)
				allowCommit &= ext.AllowCommit;
			SetResponseSensitive (Gtk.ResponseType.Ok, allowCommit);
		}
		
		protected override void OnResponse (Gtk.ResponseType type)
		{
			if (type != Gtk.ResponseType.Ok) {
				changeSet.GlobalComment = oldMessage;
			} else if (!ButtonCommitClicked ())
				return;

			base.OnResponse (type);
		}

		protected override void OnDestroyed ()
		{
			foreach (var ob in extensions) {
				var ext = ob as CommitDialogExtension;
				if (ext != null)
					ext.Destroy ();
			}
			base.OnDestroyed ();
		}

		bool ButtonCommitClicked ()
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
						new AlertButton (GettextCatalog.GetString ("Don't Save")),
						AlertButton.Save
					}
				);

				if (response == AlertButton.Cancel)
					return false;

				if (response == AlertButton.Save) {
					// Go through all the items and save them.
					foreach (var item in docList)
						item.Save ();

					// Check if save failed on any item and abort.
					foreach (var item in docList)
						if (item.IsDirty) {
							MessageService.ShowMessage (GettextCatalog.GetString (
								"Some files could not be saved. Commit operation aborted"));
							return false;
						}
				}

				docList.Clear ();
			}

			// Update the change set
			List<FilePath> todel = new List<FilePath> ();
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
				CommitDialogExtension ext = extensions [n];
				bool res;
				try {
					res = ext.OnBeginCommit (changeSet);
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);
					res = false;
				}
				if (!res) {
					// Commit failed. Rollback the previous extensions
					for (int m=0; m<n; m++) {
						ext = extensions [m];
						try {
							ext.OnEndCommit (changeSet, false);
						} catch {}
					}
					return false;
				}
			}
			return true;
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
			responseSensitive = !string.IsNullOrEmpty (Message);
			SetResponseSensitive (ResponseType.Ok, responseSensitive);
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
			return selected.ToPathStrings ().ToArray ();
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
