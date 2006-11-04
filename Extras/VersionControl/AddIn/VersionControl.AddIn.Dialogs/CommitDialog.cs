
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
		protected Gtk.TextView textview;

		public CommitDialog (VersionInfo[] infos, string basePath, StringCollection selectedFiles)
		{
			Stetic.Gui.Build(this, typeof(CommitDialog));
			store = new ListStore(typeof (Gdk.Pixbuf), typeof (string), typeof (string), typeof(bool), typeof(object));
			fileList.Model = store;

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
			
			foreach (VersionInfo info in infos) {
				Gdk.Pixbuf statusicon = VersionControlProjectService.LoadIconForStatus (info.Status);
				string lstatus = VersionControlProjectService.GetStatusLabel (info.Status);
				
				string localpath = info.LocalPath.Substring (basePath.Length);
				if (localpath.Length > 0 && localpath[0] == System.IO.Path.DirectorySeparatorChar) localpath = localpath.Substring(1);
				if (localpath == "") { localpath = "."; } // not sure if this happens
				
				bool sel = selectedFiles != null ? selectedFiles.Contains (info.LocalPath) : true;
				store.AppendValues (statusicon, lstatus, localpath, sel, info);
				if (sel)
					selected.Add (info.LocalPath);
			}
		}
		
		public string[] GetFilesToCommit ()
		{
			return (string[]) selected.ToArray (typeof(string));
		}
		
		public string Message {
			get { return textview.Buffer.Text; }
		}
		
		void OnCommitToggledHandler(object o, ToggledArgs args)
		{
			TreeIter pos;
			if (!store.GetIterFromString (out pos, args.Path))
				return;
			bool active = !(bool) store.GetValue (pos, 3);
			VersionInfo vinfo = (VersionInfo) store.GetValue (pos, 4);
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
