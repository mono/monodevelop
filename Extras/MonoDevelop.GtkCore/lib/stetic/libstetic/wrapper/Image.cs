using System;
using System.Xml;
using System.Collections;

namespace Stetic.Wrapper {

	public class Image : Misc {
	
		ImageInfo imageInfo;

		public static new Gtk.Image CreateInstance ()
		{
			return new Gtk.Image (Gtk.Stock.Execute, Gtk.IconSize.Dialog);
		}
		
		protected override void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			if (reader.Format == FileFormat.Glade) {
				string file = (string)GladeUtils.ExtractProperty (elem, "pixbuf", "");
				string stock = (string)GladeUtils.ExtractProperty (elem, "stock", "");
				string iconSize = (string)GladeUtils.ExtractProperty (elem, "icon_size", "");
				base.ReadProperties (reader, elem);
				
				if (stock != null && stock.Length > 0) {
					Pixbuf = ImageInfo.FromTheme (stock, (Gtk.IconSize) int.Parse (iconSize));
				} else if (file != null && file != "") {
					Pixbuf = ImageInfo.FromFile (file);
				}
			} else
				base.ReadProperties (reader, elem);
		}
		
		protected override XmlElement WriteProperties (ObjectWriter writer)
		{
			XmlElement elem = base.WriteProperties (writer);
			if (imageInfo != null) {
				if (writer.Format == FileFormat.Glade) {
					// The generated pixbuf property doesn't have a valid value, it needs to be replaced.
					GladeUtils.ExtractProperty (elem, "pixbuf", "");
					switch (imageInfo.Source) {
						case ImageSource.File:
							GladeUtils.SetProperty (elem, "pixbuf", imageInfo.Name);
							break;
						case ImageSource.Theme:
							GladeUtils.SetProperty (elem, "stock", imageInfo.Name);
							GladeUtils.SetProperty (elem, "icon_size", ((int)imageInfo.ThemeIconSize).ToString ());
							break;
						default:
							throw new System.NotSupportedException ("Image source not supported by Glade.");
					}
				}
			}
			return elem;
		}

		Gtk.Image image {
			get {
				return (Gtk.Image)Wrapped;
			}
		}

		void BreakImage ()
		{
			image.IconSize = (int)Gtk.IconSize.Button;
			image.Stock = Gtk.Stock.MissingImage;
		}

		public ImageInfo Pixbuf {
			get { return imageInfo; }
			set {
				imageInfo = value;
				if (imageInfo == null)
					BreakImage ();
				else
					image.Pixbuf = imageInfo.GetImage (Project);
				EmitNotify ("Pixbuf");
			}
		}
	}
}
