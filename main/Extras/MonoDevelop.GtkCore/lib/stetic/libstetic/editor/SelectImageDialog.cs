
using System;
using System.Collections;
using System.IO;
using Gtk;

namespace Stetic.Editor
{
	public class SelectImageDialog: IDisposable
	{
		[Glade.Widget] Gtk.TreeView resourceList;
		[Glade.Widget] Gtk.FileChooserWidget fileChooser;
		[Glade.Widget] Gtk.Entry iconNameEntry;
		[Glade.Widget] Gtk.Notebook notebook;
		[Glade.Widget] Gtk.ScrolledWindow iconScrolledwindow;
		[Glade.Widget] Gtk.Image previewIcon;
		[Glade.Widget] Gtk.Image previewResource;
		[Glade.Widget] Gtk.ComboBox iconSizeCombo;
		[Glade.Widget] Gtk.Entry resourceNameEntry;
		[Glade.Widget] Gtk.Button okButton;
		[Glade.Widget] Gtk.Button buttonAdd;
		[Glade.Widget] Gtk.Button buttonRemove;
		[Glade.Widget ("SelectImageDialog")] Gtk.Dialog dialog;
		
		ThemedIconList iconList;
		
		Gtk.ListStore resourceListStore;
		Gtk.Window parent;
		
		int thumbnailSize = 30;
		Hashtable resources = new Hashtable ();	// Stores resourceName -> thumbnail pixbuf
		Gdk.Pixbuf missingThumbnail;
		IResourceProvider resourceProvider;
		string importedImageFile;
		Stetic.IProject project;
		
		public SelectImageDialog (Gtk.Window parent, Stetic.IProject project)
		{
			this.parent = parent;
			this.project = project;
			Glade.XML xml = new Glade.XML (null, "stetic.glade", "SelectImageDialog", null);
			xml.Autoconnect (this);
			
			// Stock icon list
			
			iconList = new ThemedIconList ();
			iconList.SelectionChanged += new EventHandler (OnIconSelectionChanged);
			iconScrolledwindow.AddWithViewport (iconList);
			
			// Icon Sizes
			
			foreach (IconSize s in Enum.GetValues (typeof(Gtk.IconSize))) {
				if (s != IconSize.Invalid)
					iconSizeCombo.AppendText (s.ToString ());
			}
			iconSizeCombo.Active = 0;
			
			// Resource list
			
			resourceListStore = new Gtk.ListStore (typeof(Gdk.Pixbuf), typeof(string), typeof(string));
			resourceList.Model = resourceListStore;
			
			Gtk.TreeViewColumn col = new Gtk.TreeViewColumn ();
			
			Gtk.CellRendererPixbuf pr = new Gtk.CellRendererPixbuf ();
			pr.Xpad = 3;
			col.PackStart (pr, false);
			col.AddAttribute (pr, "pixbuf", 0);
			
			Gtk.CellRendererText crt = new Gtk.CellRendererText ();
			col.PackStart (crt, true);
			col.AddAttribute (crt, "markup", 1);
			
			resourceList.AppendColumn (col);
			resourceProvider = project.ResourceProvider;
			if (resourceProvider == null) {
				buttonAdd.Sensitive = false;
				buttonRemove.Sensitive = false;
			}
			FillResources ();
			resourceList.Selection.Changed += OnResourceSelectionChanged;
			
			if (project.FileName != null)
				fileChooser.SetCurrentFolder (project.ImagesRootPath);

			fileChooser.SelectionChanged += delegate (object s, EventArgs a) {
				UpdateButtons ();
			};

			fileChooser.FileActivated += delegate (object s, EventArgs a) {
				if (Icon != null) {
					if (Validate ())
						dialog.Respond (Gtk.ResponseType.Ok);
				}
			};
			
			okButton.Clicked += OnOkClicked;

			UpdateButtons ();
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
		
		public ImageInfo Icon {
			get {
				if (notebook.Page == 0) {
					if (iconNameEntry.Text.Length == 0)
						return null;
					return ImageInfo.FromTheme (iconNameEntry.Text, SelectedIconSize);
				} else if (notebook.Page == 1) {
					if (resourceNameEntry.Text.Length == 0)
						return null;
					return ImageInfo.FromResource (resourceNameEntry.Text);
				} else {
					if (importedImageFile != null)
						return ImageInfo.FromFile (importedImageFile);
					if (fileChooser.Filename == null || fileChooser.Filename.Length == 0 || !File.Exists (fileChooser.Filename))
						return null;
					return ImageInfo.FromFile (fileChooser.Filename);
				}
			}
			set {
				if (value == null)
					return;
				if (value.Source == ImageSource.Theme) {
					iconNameEntry.Text = value.Name;
					SelectedIconSize = value.ThemeIconSize;
					notebook.Page = 0;
				} else if (value.Source == ImageSource.Resource) {
					notebook.Page = 1;
					resourceNameEntry.Text = value.Name;
				} else {
					fileChooser.SetFilename (value.Name);
					notebook.Page = 2;
				}
			}
		}
		
		Gtk.IconSize SelectedIconSize {
			get { return (IconSize) iconSizeCombo.Active + 1; }
			set { iconSizeCombo.Active = ((int) value) - 1; }
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
				iconNameEntry.Text = iconList.Selection;
			}
		}
		
		void UpdateIconSelection ()
		{
			Gdk.Pixbuf icon = null;
			if (iconNameEntry.Text.Length > 0) {
				icon = WidgetUtils.LoadIcon (iconNameEntry.Text, SelectedIconSize);
			}
			if (icon == null)
				icon = WidgetUtils.MissingIcon;
			previewIcon.Pixbuf = icon;
		}
		
		protected void OnIconSizeChanged (object ob, EventArgs args)
		{
			UpdateIconSelection ();
		}
		
		protected void OnIconNameChanged (object ob, EventArgs args)
		{
			UpdateIconSelection ();
			UpdateButtons ();
		}
		
		void FillResources ()
		{
			resourceListStore.Clear ();
			resources.Clear ();
			if (resourceProvider != null) {
				foreach (ResourceInfo res in resourceProvider.GetResources ()) {
					if (res.MimeType.StartsWith ("image/")) {
						AppendResource (resourceProvider.GetResourceStream (res.Name), res.Name);
					}
				}
			}
		}
		
		void AppendResource (Stream stream, string name)
		{
			try {
				Gdk.Pixbuf pix = new Gdk.Pixbuf (stream);
				stream.Close ();
				string txt = name + "\n<span foreground='darkgrey' size='x-small'>" + pix.Width + " x " + pix.Height + "</span>";
				pix = GetThumbnail (pix);
				resourceListStore.AppendValues (pix, txt, name);
				resources [name] = pix;
			} catch {
				// Doesn't look like a valid image. Just ignore it.
			}
		}
		
		Gdk.Pixbuf GetThumbnail (Gdk.Pixbuf pix)
		{
			if (pix.Width > pix.Height) {
				if (pix.Width > thumbnailSize) {
					float prop = (float) pix.Height / (float) pix.Width;
					return pix.ScaleSimple (thumbnailSize, (int)(thumbnailSize * prop), Gdk.InterpType.Bilinear);
				}
			} else {
				if (pix.Height > thumbnailSize) {
					float prop = (float) pix.Width / (float) pix.Height;
					return pix.ScaleSimple ((int)(thumbnailSize * prop), thumbnailSize, Gdk.InterpType.Bilinear);
				}
			}
			return pix;
		}
		
		void OnResourceSelectionChanged (object obj, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			if (!resourceList.Selection.GetSelected (out model, out iter)) {
				resourceNameEntry.Text = "";
			} else {
				resourceNameEntry.Text = (string) resourceListStore.GetValue (iter, 2);
			}
		}
		
		protected void OnResourceNameChanged (object ob, EventArgs args)
		{
			Gdk.Pixbuf pix = (Gdk.Pixbuf) resources [resourceNameEntry.Text];
			if (pix != null)
				previewResource.Pixbuf = pix;
			else {
				if (missingThumbnail == null)
					missingThumbnail = WidgetUtils.MissingIcon;
				previewResource.Pixbuf = missingThumbnail;
			}
			UpdateButtons ();
		}
		
		protected void OnAddResource (object ob, EventArgs args)
		{
			FileChooserDialog dialog =
				new FileChooserDialog ("Open File", null, FileChooserAction.Open,
						       Gtk.Stock.Cancel, Gtk.ResponseType.Cancel,
						       Gtk.Stock.Open, Gtk.ResponseType.Ok);
			int response = dialog.Run ();
			if (response == (int)Gtk.ResponseType.Ok) {
				ResourceInfo rinfo = resourceProvider.AddResource (dialog.Filename);
				AppendResource (resourceProvider.GetResourceStream (rinfo.Name), rinfo.Name);
				resourceNameEntry.Text = rinfo.Name;
			}
			dialog.Destroy ();
		}
		
		protected void OnRemoveResource (object ob, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			if (resourceList.Selection.GetSelected (out model, out iter)) {
				string res = (string) resourceListStore.GetValue (iter, 2);
				Gtk.MessageDialog msg = new Gtk.MessageDialog (dialog, DialogFlags.Modal, MessageType.Question, ButtonsType.YesNo, "Are you sure you want to delete the resource '{0}'?", res);
				if (msg.Run () == (int) ResponseType.Yes) {
					resourceProvider.RemoveResource (res);
					resourceListStore.Remove (ref iter);
				}
				msg.Destroy ();
			}
		}
		
		bool Validate ()
		{
			if (notebook.Page == 2) {
				if (fileChooser.Filename == null || fileChooser.Filename.Length == 0 || !File.Exists (fileChooser.Filename))
					return true;
				
				importedImageFile = project.ImportFile (fileChooser.Filename);
				if (importedImageFile != null)
					importedImageFile = WidgetUtils.AbsoluteToRelativePath (project.ImagesRootPath, importedImageFile);
				return importedImageFile != null;
			}
			return true;
		}
		
		void OnOkClicked (object s, EventArgs args)
		{
			if (Validate ())
				dialog.Respond (Gtk.ResponseType.Ok);
		}
	}
}
