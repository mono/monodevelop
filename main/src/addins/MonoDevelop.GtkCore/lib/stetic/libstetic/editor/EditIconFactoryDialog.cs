
using System;
using System.Collections;
using System.IO;
using Gtk;
using Mono.Unix;

namespace Stetic.Editor
{
	public class EditIconFactoryDialog: IDisposable
	{
		[Glade.Widget] Gtk.ScrolledWindow iconListScrolledwindow;
		[Glade.Widget ("EditIconFactoryDialog")] Gtk.Dialog dialog;
		
		ProjectIconList customIconList;
		
		Gtk.Window parent;
		Stetic.IProject project;
		ProjectIconFactory iconFactory;
		
		public EditIconFactoryDialog (Gtk.Window parent, Stetic.IProject project, ProjectIconFactory iconFactory)
		{
			this.iconFactory = iconFactory;
			this.parent = parent;
			this.project = project;
			
			Glade.XML xml = new Glade.XML (null, "stetic.glade", "EditIconFactoryDialog", null);
			xml.Autoconnect (this);
			
			customIconList = new ProjectIconList (project, iconFactory);
			iconListScrolledwindow.AddWithViewport (customIconList);
		}
		
		public int Run ()
		{
			dialog.ShowAll ();
			dialog.TransientFor = parent;
			return dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
		
		protected void OnAddIcon (object ob, EventArgs args)
		{
			ProjectIconSet icon = new ProjectIconSet ();
			using (EditIconDialog dlg = new EditIconDialog (project, icon)) {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					iconFactory.Icons.Add (icon);
					customIconList.Refresh ();
					customIconList.Selection = icon.Name;
					project.Modified = true;
				}
			}
		}
		
		protected void OnRemoveIcon (object ob, EventArgs args)
		{
			string name = customIconList.Selection;
			ProjectIconSet icon = iconFactory.GetIcon (name);
			if (icon != null) {
				Gtk.MessageDialog md = new Gtk.MessageDialog (dialog, Gtk.DialogFlags.Modal, Gtk.MessageType.Question, Gtk.ButtonsType.YesNo, string.Format (Catalog.GetString ("Are you sure you want to delete the icon '{0}'"), icon.Name));
				if (md.Run () == (int) Gtk.ResponseType.Yes) {
					iconFactory.Icons.Remove (icon);
					customIconList.Refresh ();
					project.Modified = true;
				}
				md.Destroy ();
			}
		}
		
		protected void OnEditIcon (object ob, EventArgs args)
		{
			string name = customIconList.Selection;
			ProjectIconSet icon = iconFactory.GetIcon (name);
			if (icon != null) {
				using (EditIconDialog dlg = new EditIconDialog (project, icon)) {
					if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
						customIconList.Refresh ();
						customIconList.Selection = icon.Name;
						project.Modified = true;
					}
				}
			}
		}
	}
}
