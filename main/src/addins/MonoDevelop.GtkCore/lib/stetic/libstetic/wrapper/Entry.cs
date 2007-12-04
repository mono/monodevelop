using System;
using System.ComponentModel;

namespace Stetic.Wrapper
{
	public class Entry: Widget
	{
		[DefaultValue ('*')]
		public char InvisibleChar {
			get { return ((Gtk.Entry)Wrapped).InvisibleChar; }
			set { ((Gtk.Entry)Wrapped).InvisibleChar = value; }
		}
	}
	
}
