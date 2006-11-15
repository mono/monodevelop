
using System;
using System.Collections.Specialized;
using System.Collections;
using Gtk;
using VersionControl.Service;
using MonoDevelop.Core;

namespace VersionControl.AddIn.Dialogs
{
	public class CommitDialog : Gtk.Dialog
	{
		protected Gtk.TreeView fileList;
		ListStore store;
		ArrayList selected = new ArrayList ();
		ChangeSet changeSet;
		protected Gtk.TextView textview;

		public CommitDialog (ChangeSet changeSet)
		{
			Stetic.Gui.Build(this, typeof(CommitDialog));
			store = new ListStore(typeof (Gdk.Pixbuf), typeof (string), typeof (string), typeof(bool), typeof(object));
			fileList.Model = store;
			this.changeSet = changeSet;

			CellRendererText crt = new CellRendererText ();
			CellRendererPixbuf crp = new CellRendererPixbuf ();
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

			foreach (ChangeSetItem info in changeSet.Items) {
				Gdk.Pixbuf statusicon = VersionControlProjectService.LoadIconForStatus (info.Status);
				string lstatus = VersionControlProjectService.GetStatusLabel (info.Status);
				
				string localpath = info.LocalPath.Substring (changeSet.BaseLocalPath.Length);
				if (localpath.Length > 0 && localpath[0] == System.IO.Path.DirectorySeparatorChar) localpath = localpath.Substring(1);
				if (localpath == "") { localpath = "."; } // not sure if this happens
				
				store.AppendValues (statusicon, lstatus, localpath, true, info);
				selected.Add (info.LocalPath);
			}
			
			if (changeSet.GlobalComment.Length == 0)
				Message = changeSet.GenerateGlobalComment (70);
			else
				Message = changeSet.GlobalComment;
		}
		
		protected override void OnResponse (Gtk.ResponseType type)
		{
			if (type == Gtk.ResponseType.Ok) {
				ArrayList todel = new ArrayList ();
				foreach (ChangeSetItem it in changeSet.Items) {
					if (!selected.Contains (it.LocalPath))
						todel.Add (it.LocalPath);
				}
				foreach (string file in todel)
					changeSet.RemoveFile (file);
			}
			base.OnResponse (type);
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
		
		public override void Dispose ()
		{
			base.Dispose ();
			Destroy ();
		}
	}
}
