
using System;
using System.Collections;
using System.IO;
using Gtk;
using Mono.Unix;

namespace Stetic.Editor
{
	public class SelectIconDialog: IDisposable
	{
		[Glade.Widget] Gtk.Entry stockIconEntry;
		[Glade.Widget] Gtk.Notebook notebook;
		[Glade.Widget] Gtk.ScrolledWindow iconScrolledwindow;
		[Glade.Widget] Gtk.ScrolledWindow customIconScrolledwindow;
		[Glade.Widget] Gtk.Image previewIcon;
		[Glade.Widget] Gtk.Button okButton;
		[Glade.Widget] Gtk.Widget labelWarningIcon;
		[Glade.Widget ("SelectIconDialog")] Gtk.Dialog dialog;
		
		StockIconList iconList;
		ProjectIconList customIconList;
		
		Gtk.Window parent;
		Stetic.IProject project;
		
		public SelectIconDialog (Gtk.Window parent, Stetic.IProject project)
		{
			this.parent = parent;
			this.project = project;
			
			Glade.XML xml = new Glade.XML (null, "stetic.glade", "SelectIconDialog", null);
			xml.Autoconnect (this);
			
			// Stock icon list
			
			iconList = new StockIconList ();
			iconList.SelectionChanged += new EventHandler (OnIconSelectionChanged);
			iconScrolledwindow.AddWithViewport (iconList);
			
			// Custom icon list
			
			customIconList = new ProjectIconList (project, project.IconFactory);
			customIconList.SelectionChanged += new EventHandler (OnCustomIconSelectionChanged);
			customIconScrolledwindow.AddWithViewport (customIconList);
			dialog.ShowAll ();

			UpdateIconSelection ();
			UpdateButtons ();
		}
		
		public int Run ()
		{
			dialog.Show ();
			dialog.TransientFor = parent;
			return dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
		
		public string Icon {
			get {
				if (notebook.Page == 0) {
					if (stockIconEntry.Text.Length == 0)
						return null;
					return stockIconEntry.Text;
				} else {
					return customIconList.Selection;
				}
			}
			set {
				if (value == null)
					return;
					
				if (project.IconFactory.GetIcon (value) != null) {
					notebook.Page = 1;
					customIconList.Selection = value;
				} else {
					stockIconEntry.Text = value;
					iconList.Selection = value;
					notebook.Page = 0;
				}
			}
		}
		
		void UpdateButtons ()
		{
			okButton.Sensitive = Icon != null;
		}
		
		protected void OnCurrentPageChanged (object s, Gtk.SwitchPageArgs args)
		{
			UpdateButtons ();
		}
		
		void OnIconSelectionChanged (object s, EventArgs args)
		{
			if (iconList.Selection != null) {
				stockIconEntry.Text = iconList.Selection;
			}
		}
		
		void OnCustomIconSelectionChanged (object s, EventArgs args)
		{
			UpdateButtons ();
		}
		
		void UpdateIconSelection ()
		{
			labelWarningIcon.Visible = stockIconEntry.Text.Length > 0 && (!stockIconEntry.Text.StartsWith ("gtk-"));
				
			Gdk.Pixbuf icon = null;
			if (stockIconEntry.Text.Length > 0) {
				icon = WidgetUtils.LoadIcon (stockIconEntry.Text, Gtk.IconSize.Menu);
			}
			if (icon == null)
				icon = WidgetUtils.MissingIcon;
			previewIcon.Pixbuf = icon;
		}
		
		protected void OnIconNameChanged (object ob, EventArgs args)
		{
			UpdateIconSelection ();
			UpdateButtons ();
		}
		
		protected void OnAddIcon (object ob, EventArgs args)
		{
			ProjectIconSet icon = new ProjectIconSet ();
			using (EditIconDialog dlg = new EditIconDialog (project, icon)) {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					project.IconFactory.Icons.Add (icon);
					customIconList.Refresh ();
					customIconList.Selection = icon.Name;
					project.Modified = true;
				}
			}
		}
		
		protected void OnRemoveIcon (object ob, EventArgs args)
		{
			string name = customIconList.Selection;
			ProjectIconSet icon = project.IconFactory.GetIcon (name);
			if (icon != null) {
				Gtk.MessageDialog md = new Gtk.MessageDialog (dialog, Gtk.DialogFlags.Modal, Gtk.MessageType.Question, Gtk.ButtonsType.YesNo, string.Format (Catalog.GetString ("Are you sure you want to delete the icon '{0}'"), icon.Name));
				if (md.Run () == (int) Gtk.ResponseType.Yes) {
					project.IconFactory.Icons.Remove (icon);
					customIconList.Refresh ();
					project.Modified = true;
				}
				md.Destroy ();
			}
		}
		
		protected void OnEditIcon (object ob, EventArgs args)
		{
			string name = customIconList.Selection;
			ProjectIconSet icon = project.IconFactory.GetIcon (name);
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
