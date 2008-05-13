using System;
using System.Xml;

namespace Stetic.Wrapper {

	public class ImageMenuItem : MenuItem {

		public static new Gtk.ImageMenuItem CreateInstance ()
		{
			// Use the ctor that will create an AccelLabel
			return new Gtk.ImageMenuItem ("");
		}

		protected override void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			Gtk.StockItem stockItem = Gtk.StockItem.Zero;
			bool use_stock = (bool)GladeUtils.ExtractProperty (elem, "use_stock", false);
			if (use_stock) {
				string label = (string)GladeUtils.GetProperty (elem, "label", "");
				stockItem = Gtk.Stock.Lookup (label);
				if (stockItem.Label != null)
					GladeUtils.ExtractProperty (elem, "label", "");
			}
			base.ReadProperties (reader, elem);

			if (stockItem.StockId != null)
				Image = "stock:" + stockItem.StockId;
			if (stockItem.Keyval != 0)
				Accelerator = Gtk.Accelerator.Name (stockItem.Keyval, stockItem.Modifier);
		}

		string image;

		public string Image {
			get {
				return image;
			}
			set {
				image = value;

				Gtk.Widget icon;
				Gtk.StockItem stockItem = Gtk.StockItem.Zero;

				if (image.StartsWith ("stock:"))
					stockItem = Gtk.Stock.Lookup (image.Substring (6));

				if (stockItem.StockId != null) {
					icon = new Gtk.Image (stockItem.StockId, Gtk.IconSize.Menu);
					Label = stockItem.Label;
					UseUnderline = true;
				} else if (image.StartsWith ("file:"))
					icon = new Gtk.Image (image.Substring (5));
				else
					icon = new Gtk.Image (WidgetUtils.MissingIcon);

				((Gtk.ImageMenuItem)Wrapped).Image = icon;

				EmitNotify ("Image");
			}
		}
	}
}
