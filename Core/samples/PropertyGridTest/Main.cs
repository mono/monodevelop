using Gtk;
using GtkSharp;
using Pango;

using System;
using System.IO;
using System.Drawing;

using MonoDevelop.Gui.Widgets;

namespace MonoDevelop.Test
{

	public class PropertyGridTest
	{
		PropertyGrid propgrid;
		Window window;
		
		public PropertyGridTest () {
			Application.Init ();
			
			Window win = new Gtk.Window ("PropertyGridTest");
			window = win;
			win.DeleteEvent += new DeleteEventHandler (Main_Closed);
			
			propgrid = new PropertyGrid (this);

			win.Add(propgrid);
			win.ShowAll ();
			Application.Run ();
			
		}

		private void Main_Closed (object o, DeleteEventArgs e)
		{
			Quit ();
		}
		
		private void Quit() {
			Application.Quit();
		}
		
		public static int Main (string[] args)
		{
			PropertyGridTest shell = new PropertyGridTest();
			return 0;
		}
	}
}

