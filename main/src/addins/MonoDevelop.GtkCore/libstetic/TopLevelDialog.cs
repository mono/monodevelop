using System;
using System.Collections.Generic;
using System.Text;
using Gtk;

namespace Stetic
{
	public class TopLevelDialog: TopLevelWindow
	{
		HButtonBox buttonBox;
		VBox vbox;
		HSeparator separator;

		public TopLevelDialog ( )
		{
			vbox = new VBox ();
			separator = new HSeparator ();
			buttonBox = new HButtonBox ();
			vbox.PackEnd (buttonBox, false, false, 0);
			vbox.PackEnd (separator, false, false, 0);
			vbox.ShowAll ();
			Add (vbox);
		}

		public HButtonBox ActionArea {
			get { return buttonBox; }
		}

		public VBox VBox {
			get { return vbox; }
		}

		public bool HasSeparator
		{
			get { return separator.Visible; }
			set { separator.Visible = value; }
		}
	}
}
