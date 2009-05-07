using System;
using System.Collections.Generic;
using System.Text;

namespace Stetic
{
	public class TopLevelWindow: Gtk.EventBox
	{
		string title;
		Gdk.WindowTypeHint typeHint;
		bool modal;
		bool resizable = true;

		public event EventHandler PropertyChanged;

		public string Title {
			get { return title; }
			set {
				title = value;
				NotifyChange ();
			}
		}

		public Gdk.WindowTypeHint TypeHint {
			get { return typeHint; }
			set {
				typeHint = value;
				NotifyChange ();
			}
		}

		public bool Modal {
			get { return modal; }
			set
			{
				modal = value;
				NotifyChange ();
			}
		}

		public bool Resizable
		{
			get { return resizable; }
			set
			{
				resizable = value;
				NotifyChange ();
			}
		}

		void NotifyChange ( )
		{
			if (PropertyChanged != null)
				PropertyChanged (this, EventArgs.Empty);
		}
	}
}
