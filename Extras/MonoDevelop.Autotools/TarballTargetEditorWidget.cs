using System;

using MonoDevelop.Projects;
using MonoDevelop.Core;

using Gtk;

namespace MonoDevelop.Autotools
{
	public class TarballTargetEditorWidget : VBox
	{
		public TarballTargetEditorWidget (TarballDeployTarget target)
		{
			HBox dir_entry = new HBox ();
			
			Label lab = new Label ( GettextCatalog.GetString ("Deploy directory:") );
			dir_entry.PackStart (lab, false, false, 0);
			
			Gnome.FileEntry fe = new Gnome.FileEntry ("tarball-folders","Target Directory");
			fe.GtkEntry.Text = target.TargetDir;
			fe.Directory = true;
			fe.Modal = true;
			fe.UseFilechooser = true;
			fe.FilechooserAction = FileChooserAction.SelectFolder;
			fe.GtkEntry.Changed += delegate (object s, EventArgs args) {
				target.TargetDir = fe.GtkEntry.Text;
			};
			dir_entry.PackStart (fe, true, true, 6);

			PackStart ( dir_entry , false, false, 0 );
			
			HBox config_box = new HBox ();

			Label conlab = new Label ( GettextCatalog.GetString ("Default configuration:") );
			config_box.PackStart (conlab, false, false, 0);
			
			string curr_conf;
			if ( target.DefaultConfiguration == null || target.DefaultConfiguration == "" )
				curr_conf = target.TargetCombine.ActiveConfiguration.Name;
			else curr_conf = target.DefaultConfiguration;
			
			ComboBox configs = ComboBox.NewText ();
			for ( int ii=0; ii < target.TargetCombine.Configurations.Count; ii++ )
			{
				string cc = target.TargetCombine.Configurations [ii].Name;
				configs.AppendText ( cc );
				if ( cc == curr_conf ) configs.Active = ii;
			}
			configs.Changed += delegate (object s, EventArgs args) {
				target.DefaultConfiguration = configs.ActiveText;
			};
			config_box.PackStart ( configs, true, true, 6 );

			PackStart ( config_box, false, false, 6 );

			Label warning = new Label ();
			warning.LineWrap = true;
			string msg = GettextCatalog.GetString ( "Note: Deploying to a tarball will create a set of autotools files in the solution directory.  It will also overwrite any existing autotools files." );
			warning.Markup = "<i>" + msg + "</i>" ;
			PackStart ( warning, false, false, 6 );
			
			ShowAll ();
		}
	}
}
