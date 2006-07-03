
using System;
using MonoDevelop.Core;
using MonoDevelop.Projects.Deployment;

namespace MonoDevelop.Projects.Gui.Deployment
{
	class FileDeployTargetEditor: IDeployTargetEditor
	{
		public bool CanEdit (DeployTarget target)
		{
			return target is FileDeployTarget;
		}
		
		public Gtk.Widget CreateEditor (DeployTarget target)
		{
			return new FileDeployTargetEditorWidget ((FileDeployTarget) target);
		}
	}
	
	class FileDeployTargetEditorWidget: Gtk.HBox
	{
		public FileDeployTargetEditorWidget (FileDeployTarget target)
		{
			string label;
			Gtk.FileChooserAction action;
			
			if (target is FileDeployTarget) {
				label = GettextCatalog.GetString ("Deploy file");
				action = Gtk.FileChooserAction.Save;
			} else {
				label = GettextCatalog.GetString ("Deploy directory");
				action = Gtk.FileChooserAction.SelectFolder;
			}
			
			Gtk.Label lab = new Gtk.Label (label + ":");
			PackStart (lab, false, false, 0);
			
			Gnome.FileEntry fe = new Gnome.FileEntry ("target-folders", label);
			fe.GtkEntry.Text = target.Path;
			fe.Modal = true;
			fe.UseFilechooser = true;
			fe.FilechooserAction = action;
			fe.GtkEntry.Changed += delegate (object s, EventArgs args) {
				target.Path = fe.GtkEntry.Text;
			};
			
			PackStart (fe, true, true, 6);
			ShowAll ();
		}
	}
	
}
