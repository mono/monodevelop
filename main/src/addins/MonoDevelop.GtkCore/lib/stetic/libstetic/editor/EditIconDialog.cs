
using System;
using System.Collections;
using Mono.Unix;

namespace Stetic.Editor
{
	public class EditIconDialog: IDisposable
	{
		[Glade.Widget] Gtk.Entry nameEntry;
		[Glade.Widget] Gtk.TreeView sourceList;
		[Glade.Widget] Gtk.RadioButton radioSingle;
		[Glade.Widget] Gtk.RadioButton radioMultiple;
		[Glade.Widget] Gtk.Label imageLabel;
		[Glade.Widget] Gtk.Image imageImage;
		[Glade.Widget] Gtk.Button okButton;
		[Glade.Widget] Gtk.HBox hboxSingle;
		[Glade.Widget] Gtk.HBox hboxMultiple;
		[Glade.Widget ("EditIconDialog")] Gtk.Dialog dialog;
		
		Gtk.ListStore sourceListStore;
		
		ProjectIconSet iconSet;
		IProject project;
		
		ImageInfo singleIcon;
		
		string[] sizes = { Catalog.GetString ("All Sizes"), "Menu", "SmallToolbar", "LargeToolbar", "Button", "Dnd", "Dialog" };
		string[] states = { Catalog.GetString ("All States"), "Normal", "Active", "Prelight", "Selected", "Insensitive" };
		string[] directions = { Catalog.GetString ("All Directions"), "Ltr", "Rtl" };
		
		public EditIconDialog (IProject project, ProjectIconSet iconSet)
		{
			Glade.XML xml = new Glade.XML (null, "stetic.glade", "EditIconDialog", null);
			xml.Autoconnect (this);
			dialog.Response += OnResponse;
			
			this.project = project;
			this.iconSet = iconSet;
			
			nameEntry.Text = iconSet.Name;
			
			if (iconSet.Sources.Count == 0) {
				radioSingle.Active = true;
				imageLabel.Text = "";
			}
			else if (iconSet.Sources.Count == 1 && iconSet.Sources[0].AllWildcarded) {
				radioSingle.Active = true;
				singleIcon = iconSet.Sources[0].Image;
				if (singleIcon != null) {
					imageLabel.Text = singleIcon.Label;
					imageImage.Pixbuf = singleIcon.GetThumbnail (project, 16);
				} else
					imageLabel.Text = "";
			} else {
				radioMultiple.Active = true;
			}
			
			hboxSingle.Sensitive = radioSingle.Active;
			hboxMultiple.Sensitive = !radioSingle.Active;
			
			// Build the tree
			
			sourceListStore = new Gtk.ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(string), typeof(string), typeof(string), typeof(object));
			sourceList.Model = sourceListStore;
			
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			
			Gtk.CellRendererPixbuf pr = new Gtk.CellRendererPixbuf ();
			col.Title = Catalog.GetString ("Image");
			col.PackStart (pr, false);
			col.AddAttribute (pr, "pixbuf", 0);
			
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "text", 1);
			sourceList.AppendColumn (col);			
			
			col = new Gtk.TreeViewColumn ();
			col.Expand = true;
			col.Title = Catalog.GetString ("Size");
			CellRendererComboBox crtb = new CellRendererComboBox ();
			crtb.Changed += new ComboSelectionChangedHandler (OnSizeComboChanged);
			crtb.Values = sizes;
			col.PackStart (crtb, true);
			col.AddAttribute (crtb, "text", 2);
			sourceList.AppendColumn (col);
			
			col = new Gtk.TreeViewColumn ();
			col.Expand = true;
			col.Title = Catalog.GetString ("State");
			crtb = new CellRendererComboBox ();
			crtb.Changed += new ComboSelectionChangedHandler (OnStateComboChanged);
			crtb.Values = states;
			col.PackStart (crtb, true);
			col.AddAttribute (crtb, "text", 3);
			sourceList.AppendColumn (col);
			
			col = new Gtk.TreeViewColumn ();
			col.Expand = true;
			col.Title = Catalog.GetString ("Direction");
			crtb = new CellRendererComboBox ();
			crtb.Changed += new ComboSelectionChangedHandler (OnDirComboChanged);
			crtb.Values = directions;
			col.PackStart (crtb, true);
			col.AddAttribute (crtb, "text", 4);
			sourceList.AppendColumn (col);
			
			foreach (ProjectIconSource source in iconSet.Sources)
				AddSource (source);
				
			UpdateButtons ();
		}
		
		public int Run ()
		{
			return dialog.Run ();
		}
		
		public void Dispose ()
		{
			dialog.Destroy ();
		}
		
		void AddSource (ProjectIconSource source)
		{
			string size = source.SizeWildcarded ? sizes[0] : source.Size.ToString ();
			string state = source.StateWildcarded ? states[0] : source.State.ToString ();
			string dir = source.DirectionWildcarded ? directions[0] : source.Direction.ToString ();
			sourceListStore.AppendValues (source.Image.GetThumbnail (project, 16), source.Image.Label, size, state, dir, source.Image);
		}
		
		ProjectIconSource GetSource (Gtk.TreeIter iter)
		{
			ProjectIconSource src = new ProjectIconSource ();
			src.Image = (ImageInfo) sourceListStore.GetValue (iter, 5);
			string s = (string) sourceListStore.GetValue (iter, 2);
			if (s == sizes[0])
				src.SizeWildcarded = true;
			else {
				src.SizeWildcarded = false;
				src.Size = (Gtk.IconSize) Enum.Parse (typeof(Gtk.IconSize), s);
			}
				
			s = (string) sourceListStore.GetValue (iter, 3);
			if (s == states[0])
				src.StateWildcarded = true;
			else {
				src.StateWildcarded = false;
				src.State = (Gtk.StateType) Enum.Parse (typeof(Gtk.StateType), s);
			}
				
			s = (string) sourceListStore.GetValue (iter, 4);
			if (s == directions[0])
				src.DirectionWildcarded = true;
			else {
				src.DirectionWildcarded = false;
				src.Direction = (Gtk.TextDirection) Enum.Parse (typeof(Gtk.TextDirection), s);
			}

			return src;
		}
		
		void Save ()
		{
			iconSet.Name = nameEntry.Text;
			iconSet.Sources.Clear ();
			
			if (radioSingle.Active) {
				ProjectIconSource src = new ProjectIconSource ();
				src.AllWildcarded = true;
				src.Image = singleIcon;
				iconSet.Sources.Add (src);
			} else {
				Gtk.TreeIter iter;
				if (sourceListStore.GetIterFirst (out iter)) {
					do {
						iconSet.Sources.Add (GetSource (iter));
					}
					while (sourceListStore.IterNext (ref iter));
				}
			}
		}
		
		void OnResponse (object o, Gtk.ResponseArgs args)
		{
			if (args.ResponseId == Gtk.ResponseType.Ok)
				Save ();
		}
		
		protected void OnSelectImage (object s, EventArgs args)
		{
			using (SelectImageDialog dlg = new SelectImageDialog (dialog, project)) {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					singleIcon = dlg.Icon;
					imageLabel.Text = singleIcon.Label;
					imageImage.Pixbuf = singleIcon.GetThumbnail (project, 16);
					UpdateButtons ();
				}
			}
		}
		
		protected void OnAddSource (object s, EventArgs args)
		{
			using (SelectImageDialog dlg = new SelectImageDialog (dialog, project)) {
				if (dlg.Run () == (int) Gtk.ResponseType.Ok) {
					ProjectIconSource src = new ProjectIconSource ();
					src.Image = dlg.Icon;
					src.AllWildcarded = true;
					AddSource (src);
					UpdateButtons ();
				}
			}
		}
		
		protected void OnRemoveSource (object s, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			if (sourceList.Selection.GetSelected (out model, out iter)) {
				sourceListStore.Remove (ref iter);
				UpdateButtons ();
			}
		}
		
		protected void OnSingleClicked (object s, EventArgs args)
		{
			hboxSingle.Sensitive = true;
			hboxMultiple.Sensitive = false;
			UpdateButtons ();
		}
		
		protected void OnMultipleClicked (object s, EventArgs args)
		{
			hboxSingle.Sensitive = false;
			hboxMultiple.Sensitive = true;
			UpdateButtons ();
		}
		
		protected void OnNameChanged (object s, EventArgs args)
		{
			UpdateButtons ();
		}
		
		void OnSizeComboChanged (object s, ComboSelectionChangedArgs args)
		{
			UpdateComboValue (args.Path, 2, args.ActiveText);
		}
		
		void OnStateComboChanged (object s, ComboSelectionChangedArgs args)
		{
			UpdateComboValue (args.Path, 3, args.ActiveText);
		}
		
		void OnDirComboChanged (object s, ComboSelectionChangedArgs args)
		{
			UpdateComboValue (args.Path, 4, args.ActiveText);
		}
		
		void UpdateComboValue (string path, int col, string activeText)
		{
			Gtk.TreeIter iter;
			if (sourceListStore.GetIter (out iter, new Gtk.TreePath (path))) {
				sourceListStore.SetValue (iter, col, activeText);
			}
		}
		
		void UpdateButtons ()
		{
			if (nameEntry.Text.Length == 0) {
				okButton.Sensitive = false;
			} else if (radioSingle.Active) {
				okButton.Sensitive = singleIcon != null;
			} else {
				Gtk.TreeIter iter;
				okButton.Sensitive = sourceListStore != null && sourceListStore.GetIterFirst (out iter);
			}
		}
	}
}
