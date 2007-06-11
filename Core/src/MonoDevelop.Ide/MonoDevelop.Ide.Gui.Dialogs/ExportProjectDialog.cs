
using System;
using System.IO;
using MonoDevelop.Components;
using MonoDevelop.Ide.Projects;

namespace MonoDevelop.Ide.Gui.Dialogs
{
	public partial class ExportProjectDialog : Gtk.Dialog
	{
		IFileFormat[] formats;
		
		public ExportProjectDialog (IProject entry, IFileFormat selectedFormat)
		{
			this.Build();
			
			formats = ProjectService.GetFileFormatsForObject (entry);
			if (formats != null)
				foreach (IFileFormat format in formats)
					comboFormat.AppendText (format.Name);

			int sel = Array.IndexOf (formats, selectedFormat);
			if (sel == -1) sel = 0;
			comboFormat.Active = sel;
			
			folderEntry.Path = entry.BasePath;
			UpdateControls ();
		}
		
		public IFileFormat Format {
			get { return formats [comboFormat.Active]; }
		}
		
		public string TargetFolder {
			get { return folderEntry.Path; }
		}
		
		void UpdateControls ()
		{
			buttonOk.Sensitive = folderEntry.Path.Length > 0;
		}

		protected virtual void OnFolderEntryPathChanged(object sender, System.EventArgs e)
		{
			UpdateControls ();
		}
	}
}
