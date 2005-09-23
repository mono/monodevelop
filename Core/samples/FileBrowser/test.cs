using System;
using Gtk;
using GtkSharp;
using MonoDevelop.Gui.Widgets;

class T
{
	static void Main ()
	{
		new T ();
	}

	T ()
	{
		Application.Init ();
		Window win = new Window ("FileBrowser test");
		win.SetDefaultSize (300, 250);
		win.DeleteEvent += new DeleteEventHandler (OnDelete);

		win.Add (new FileBrowser ());

		win.ShowAll ();
		Application.Run ();
	}

	void OnDelete (object o, DeleteEventArgs args)
	{
		Application.Quit ();
	}
}
