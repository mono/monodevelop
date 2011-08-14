using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using MonoDevelop.Components.Extensions;
using System.Windows.Forms;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Platform
{
    class SelectFileDialogHandler : ISelectFileDialogHandler
    {
        public bool Run(SelectFileDialogData data)
        {
			Application.EnableVisualStyles ();
			
			var parentWindow = data.TransientFor ?? MessageService.RootWindow;
            CommonDialog dlg = null;
            if (data.Action == Gtk.FileChooserAction.Open)
                dlg = new OpenFileDialog();
            else if (data.Action == Gtk.FileChooserAction.Save)
                dlg = new SaveFileDialog();
			else if (data.Action == Gtk.FileChooserAction.SelectFolder)
				dlg = new FolderBrowserDialog ();
			
			if (dlg is FileDialog)
				SetCommonFormProperties (data, dlg as FileDialog);
			else
				SetFolderBrowserProperties (data, dlg as FolderBrowserDialog);
			
			using (dlg) {
                WinFormsRoot root = new WinFormsRoot();
                if (dlg.ShowDialog(root) == DialogResult.Cancel) {
					parentWindow.Present ();
                    return false;
				}
				
				if (dlg is FileDialog) {
					var fileDlg = dlg as FileDialog;
					FilePath[] paths = new FilePath [fileDlg.FileNames.Length];
					for (int n=0; n < fileDlg.FileNames.Length; n++)
						paths [n] = fileDlg.FileNames [n];
                    data.SelectedFiles = paths;    
				} else {
					var folderDlg = dlg as FolderBrowserDialog;
					data.SelectedFiles = new [] { new FilePath (folderDlg.SelectedPath) };
				}
				
				parentWindow.Present ();
				return true;
			}
        }
		
		internal static void SetCommonFormProperties (SelectFileDialogData data, FileDialog dialog)
		{
			if (!string.IsNullOrEmpty (data.Title))
				dialog.Title = data.Title;
			
			dialog.AddExtension = true;
			dialog.Filter = GetFilterFromData (data.Filters);
			dialog.FilterIndex = data.DefaultFilter == null ? 1 : GetDefaultFilterIndex (data);
			
			dialog.InitialDirectory = data.CurrentFolder;

			// FileDialog.FileName expects anything but a directory name.
			if (!Directory.Exists (data.InitialFileName))
				dialog.FileName = data.InitialFileName;
			
			OpenFileDialog openDialog = dialog as OpenFileDialog;
			if (openDialog != null)
				openDialog.Multiselect = data.SelectMultiple;
		}

		static int GetDefaultFilterIndex (SelectFileDialogData data)
		{
			var defFilter = data.DefaultFilter;
			int idx = data.Filters.IndexOf (defFilter) + 1;

			// FileDialog doesn't show the file extension when saving a file,
			// so we try to look fo the precise filter if none was specified.
			if (data.Action == Gtk.FileChooserAction.Save && defFilter.Patterns.Contains ("*")) {
				string ext = Path.GetExtension (data.InitialFileName);

				if (!String.IsNullOrEmpty (ext))
					for (int i = 0; i < data.Filters.Count; i++) {
						var filter = data.Filters [i];
						foreach (string pattern in filter.Patterns)
							if (pattern.EndsWith (ext))
								return i + 1;
					}
			}

			return idx;
		}
				
		static void SetFolderBrowserProperties (SelectFileDialogData data, FolderBrowserDialog dialog)
		{
			if (!string.IsNullOrEmpty (data.Title))
				dialog.Description = data.Title;
			
			dialog.SelectedPath = data.CurrentFolder;
		}
		
		static string GetFilterFromData (IList<SelectFileDialogFilter> filters)
		{
			if (filters == null || filters.Count == 0)
				return null;
			
			var sb = new StringBuilder ();
			foreach (var f in filters) {
				if (sb.Length > 0)
					sb.Append ('|');
				
				sb.Append (f.Name);
				sb.Append ('|');
				for (int i = 0; i < f.Patterns.Count; i++) {
					if (i > 0)
						sb.Append (';');
				
					sb.Append (f.Patterns [i]);
				}
			}
			
			return sb.ToString ();
		}
    }
}
