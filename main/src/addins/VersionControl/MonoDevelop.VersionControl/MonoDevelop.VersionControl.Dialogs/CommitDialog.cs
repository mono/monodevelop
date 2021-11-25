﻿//
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
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Gtk;
using Mono.Addins;
using MonoDevelop.Components;
using MonoDevelop.Core;
using MonoDevelop.Projects;

namespace MonoDevelop.VersionControl.Dialogs
{
	partial class CommitDialog : Gtk.Dialog
	{
		Repository vc;
		ListStore store;
		HashSet<FilePath> selected = new HashSet<FilePath> ();
		List<CommitDialogExtension> extensions = new List<CommitDialogExtension> ();
		ChangeSet changeSet;
		string oldMessage;
		bool responseSensitive;

		public CommitDialog (Repository vc, ChangeSet changeSet)
		{
			this.vc = vc;
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
					ext.CommitDialog = this;
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

			LoadChangeset (changeSet.Items);

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

			VersionControlService.FileStatusChanged += OnFileStatusChanged;
		}

		void OnFileStatusChanged (object sender, FileUpdateEventArgs args)
		{
			foreach (FileUpdateEventInfo f in args) {
				OnFileStatusChanged (f);
			}
		}

		async void OnFileStatusChanged (FileUpdateEventInfo args)
		{
			VersionInfo newInfo = null;
			try {
				if (args.FilePath.IsNullOrEmpty)
					return;
				// Reuse remote status from old version info
				var token = destroyTokenSource.Token;
				changeSet.Repository.TryGetVersionInfo (args.FilePath, out newInfo);
				if (token.IsCancellationRequested)
					return;
				await AddFile (newInfo);
			} catch (Exception ex) {
				LoggingService.LogError (ex.ToString ());
			}
			
		}

		async Task AddFile (VersionInfo vinfo)
		{
			if (vinfo != null && (vinfo.HasLocalChanges || vinfo.HasRemoteChanges)) {
				var token = destroyTokenSource.Token;
				await changeSet.AddFileAsync (vinfo.LocalPath, token);
				if (token.IsCancellationRequested)
					return;
				bool added = selected.Add (vinfo.LocalPath);
				if (added)
					AppendFileInfo (vinfo);
			}
		}

		TreeIter AppendFileInfo (VersionInfo info)
		{
			var status = changeSet.Repository.GetVirtualStatusViewStatus (info, true);
			var statusicon = VersionControlService.LoadIconForStatus (status);
			string lstatus = VersionControlService.GetStatusLabel (status);
			string localpath;
			if (info.IsDirectory)
				localpath = (!info.LocalPath.IsChildPathOf (changeSet.BaseLocalPath) ?
								"." :
								(string)info.LocalPath.ToRelative (changeSet.BaseLocalPath));
			else
				localpath = System.IO.Path.GetFileName ((string)info.LocalPath);

			if (localpath.Length > 0 && localpath [0] == System.IO.Path.DirectorySeparatorChar) localpath = localpath.Substring (1);
			if (localpath == "") { localpath = "."; }

			return store.AppendValues (statusicon, lstatus, localpath, true, info);
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
			base.OnResponse (type);

			if (type != Gtk.ResponseType.Ok) {
				changeSet.GlobalComment = oldMessage;
				EndCommit (false);
			} else if (!ButtonCommitClicked ()) {
				return;
			}

			if (type == Gtk.ResponseType.Ok) {
				VersionControlService.NotifyBeforeCommit (vc, changeSet);
				new CommitWorker (vc, changeSet, this).StartAsync ();
				return;
			}
		}

		CancellationTokenSource destroyTokenSource = new CancellationTokenSource ();

		protected override void OnDestroyed ()
		{
			destroyTokenSource.Cancel ();

			VersionControlService.FileStatusChanged -= OnFileStatusChanged;

			foreach (var ob in extensions) {
				var ext = ob as CommitDialogExtension;
				if (ext != null)
					ext.Destroy ();
			}
			base.OnDestroyed ();
		}

		void LoadChangeset (IEnumerable<ChangeSetItem> items)
		{
			fileList.Model = null;
			store.Clear ();

			foreach (ChangeSetItem info in items) {
				var status = changeSet.Repository.GetVirtualStatusViewStatus (info.VersionInfo, true);
				var statusicon = VersionControlService.LoadIconForStatus (status);
				string lstatus = VersionControlService.GetStatusLabel (status);
				string localpath;

				if (info.IsDirectory)
					localpath = (!info.LocalPath.IsChildPathOf (changeSet.BaseLocalPath) ?
									"." :
									(string)info.LocalPath.ToRelative (changeSet.BaseLocalPath));
				else
					localpath = System.IO.Path.GetFileName ((string)info.LocalPath);

				if (localpath.Length > 0 && localpath [0] == System.IO.Path.DirectorySeparatorChar) localpath = localpath.Substring (1);
				if (localpath == "") { localpath = "."; } // not sure if this happens

				store.AppendValues (statusicon, lstatus, localpath, true, info);
				selected.Add (info.LocalPath);
			}

			fileList.Model = store;
		}

		bool ButtonCommitClicked ()
		{
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
						} catch (Exception ex) {
							LoggingService.LogInternalError ("Commit operation failed.", ex);
						}
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

		private class CommitWorker : VersionControlTask
		{
			Repository vc;
			ChangeSet changeSet;
			CommitDialog dlg;
			bool success;

			public CommitWorker (Repository vc, ChangeSet changeSet, CommitDialog dlg)
			{
				this.vc = vc;
				this.changeSet = changeSet;
				this.dlg = dlg;
				OperationType = VersionControlOperationType.Push;
			}

			protected override string GetDescription ()
			{
				return GettextCatalog.GetString ("Committing {0}...", changeSet.BaseLocalPath);
			}

			protected override async Task RunAsync ()
			{
				success = true;
				try {
					// store global comment before commit.
					VersionControlService.SetCommitComment (changeSet.BaseLocalPath, changeSet.GlobalComment, true);

					await vc.CommitAsync (changeSet, Monitor);
					Monitor.ReportSuccess (GettextCatalog.GetString ("Commit operation completed."));

					// Reset the global comment on successful commit.
					VersionControlService.SetCommitComment (changeSet.BaseLocalPath, "", true);
				} catch (Exception ex) {
					LoggingService.LogError ("Commit operation failed", ex);
					Monitor.ReportError (ex.Message, null);
					success = false;
					throw;
				}
			}

			protected override void Finished ()
			{
				dlg.EndCommit (success);
				dlg.Destroy ();
				FileUpdateEventArgs args = new FileUpdateEventArgs ();
				foreach (ChangeSetItem it in changeSet.Items)
					args.Add (new FileUpdateEventInfo (vc, it.LocalPath, it.IsDirectory));

				if (args.Count > 0)
					VersionControlService.NotifyFileStatusChanged (args);

				VersionControlService.NotifyAfterCommit (vc, changeSet, success);
			}
		}
	}
}
