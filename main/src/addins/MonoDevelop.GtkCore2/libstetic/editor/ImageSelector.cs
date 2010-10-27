
using System;

namespace Stetic.Editor
{
	public class BaseImageCell: PropertyEditorCell
	{
		const int imgSize = 24;
		const int imgPad = 2;
		const int spacing = 5;
		Gdk.Pixbuf image;
		
		protected Gdk.Pixbuf Image {
			get { return image; }
			set { image = value; }
		}
		
		protected int ImageSize {
			get { return 16; }//imgSize - imgPad*2; }
		}
		
		public override void GetSize (int availableWidth, out int width, out int height)
		{
			base.GetSize (availableWidth, out width, out height);
			width += imgSize + spacing;
			height = Math.Max (imgSize, height);
		}
		
		public override void Render (Gdk.Drawable window, Gdk.Rectangle bounds, Gtk.StateType state)
		{
			int iy = bounds.Y + (bounds.Height - imgSize) / 2;
			
			if (image != null) {
				int dy = (imgSize - image.Height) / 2;
				int dx = (imgSize - image.Width) / 2;
				window.DrawPixbuf (Container.Style.BackgroundGC (state), image, 0, 0, bounds.X + dx, iy + dy, -1, -1, Gdk.RgbDither.None, 0, 0);
			}
			
			window.DrawRectangle (Container.Style.DarkGC (state), false, bounds.X, iy, imgSize - 1, imgSize - 1);
			
			bounds.X += imgSize + spacing;
			base.Render (window, bounds, state);
		}
	}
	
	public class ImageSelector: BaseImageCell
	{
		IProject project;
		ImageInfo imageInfo;
		
		protected override void Initialize ()
		{
			base.Initialize ();
			
			if (Property.PropertyType != typeof(ImageInfo))
				throw new ApplicationException ("ImageSelector editor does not support editing values of type " + Property.PropertyType);
			
			if (Instance == null)
				return;

			Stetic.ObjectWrapper w = Stetic.ObjectWrapper.Lookup (Instance);
			project = w.Project;
			imageInfo = (ImageInfo)Value;
			if (imageInfo != null)
				Image = imageInfo.GetThumbnail (project, ImageSize);
			else
				Image = null;
		}
		
		protected override string GetValueText ()
		{
			if (imageInfo == null)
				return "";
			return imageInfo.Label;
		}
		
		protected override IPropertyEditor CreateEditor (Gdk.Rectangle cell_area, Gtk.StateType state)
		{
			return new ImageSelectorEditor ();
		}
	}
	
	public class ImageSelectorEditor: Gtk.HBox, IPropertyEditor
	{
		Gtk.Image image;
		Gtk.Label entry;
		Gtk.Button button;
		Gtk.Button clearButton;
		ImageInfo icon;
		IProject project;
		Gtk.Frame imageFrame;
		
		public ImageSelectorEditor()
		{
			Spacing = 3;
			imageFrame = new Gtk.Frame ();
			imageFrame.Shadow = Gtk.ShadowType.In;
			imageFrame.BorderWidth = 2;
			PackStart (imageFrame, false, false, 0);

			image = new Gtk.Image (GnomeStock.Blank, Gtk.IconSize.Button);
			imageFrame.Add (image);
			
			Gtk.Frame frame = new Gtk.Frame ();
			entry = new Gtk.Label ();
			entry.Xalign = 0;
			frame.Shadow = Gtk.ShadowType.In;
			frame.BorderWidth = 2;
			frame.Add (entry);
			PackStart (frame, true, true, 0);

			clearButton = new Gtk.Button (new Gtk.Image (Gtk.Stock.Clear, Gtk.IconSize.Menu));
			clearButton.Clicked += OnClearImage;
			PackStart (clearButton, false, false, 0);

			button = new Gtk.Button ("...");
			PackStart (button, false, false, 0);
			button.Clicked += button_Clicked;
			ShowAll ();
		}

		void button_Clicked (object obj, EventArgs args)
		{
			Gtk.Window parent = (Gtk.Window)GetAncestor (Gtk.Window.GType);
			using (SelectImageDialog dlg = new SelectImageDialog (parent, project)) {
				dlg.Icon = (ImageInfo) Value;
				if (dlg.Run () == (int) Gtk.ResponseType.Ok)
					Value = dlg.Icon;
			}
		}
		
		void OnClearImage (object obj, EventArgs args)
		{
			Value = null;
		}
		
		// Called once to initialize the editor.
		public void Initialize (PropertyDescriptor prop)
		{
			if (prop.PropertyType != typeof(ImageInfo))
				throw new ApplicationException ("ImageSelector editor does not support editing values of type " + prop.PropertyType);
		}
		
		// Called when the object to be edited changes.
		public void AttachObject (object obj)
		{
			Stetic.ObjectWrapper w = Stetic.ObjectWrapper.Lookup (obj);
			project = w.Project;
		}
		
		// Gets/Sets the value of the editor. If the editor supports
		// several value types, it is the responsibility of the editor 
		// to return values with the expected type.
		public object Value {
			get { return icon; }
			set {
				icon = (ImageInfo) value;
				if (icon != null) {
					entry.Text = icon.Label;
					image.Pixbuf = icon.GetThumbnail (project, 16);
					imageFrame.Show ();
					clearButton.Show ();
				} else {
					imageFrame.Hide ();
					clearButton.Hide ();
					entry.Text = "";
				}

				if (ValueChanged != null)
					ValueChanged (this, EventArgs.Empty);
			}
		}

		// To be fired when the edited value changes.
		public event EventHandler ValueChanged;
	}
}
