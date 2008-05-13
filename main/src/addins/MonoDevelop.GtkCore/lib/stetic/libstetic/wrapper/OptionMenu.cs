using System;
using System.Collections;
using System.Xml;

// Don't warn that OptionMenu is deprecated. We know that.
#pragma warning disable 612

namespace Stetic.Wrapper {

	public class OptionMenu : Container {

		public override void Wrap (object obj, bool initialized)
		{
			Gtk.OptionMenu omenu = (Gtk.OptionMenu)obj;
			if (omenu.Menu == null) {
				Gtk.Menu menu = new Gtk.Menu ();
				menu.Show ();
				omenu.Menu = menu;
			}

			base.Wrap (obj, initialized);
		}

		protected override void ReadProperties (ObjectReader reader, XmlElement elem)
		{
			int history = (int)GladeUtils.ExtractProperty (elem, "history", -1);
			base.ReadProperties (reader, elem);

			// Fiddle with things to make the optionmenu resize itself correctly
			Gtk.Widget menu = optionmenu.Menu;
			optionmenu.Menu = new Gtk.Menu ();
			optionmenu.Menu = menu;

			if (history != -1)
				Active = history;
			else
				Active = 0;
		}

		// Some versions of glade call the menu an internal child, some don't

		protected override ObjectWrapper ReadInternalChild (ObjectReader reader, XmlElement child_elem)
		{
			if (child_elem.GetAttribute ("internal-child") == "menu")
				return ReadChild (reader, child_elem);
			else
				return base.ReadInternalChild (reader, child_elem);
		}

		protected override ObjectWrapper ReadChild (ObjectReader reader, XmlElement child_elem)
		{
			Widget wrapper = Stetic.Wrapper.Widget.Lookup (optionmenu.Menu);
			reader.ReadObject (wrapper, child_elem["widget"]);
			return wrapper;
		}

		public override IEnumerable GladeChildren {
			get {
				return new Gtk.Widget[] { optionmenu.Menu };
			}
		}

		Gtk.OptionMenu optionmenu {
			get {
				return (Gtk.OptionMenu)Wrapped;
			}
		}

		public int Active {
			get {
				return optionmenu.History;
			}
			set {
				optionmenu.SetHistory ((uint)value);
				EmitNotify ("Active");
			}
		}
	}
}
