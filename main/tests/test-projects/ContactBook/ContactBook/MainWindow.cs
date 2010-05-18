using System;
using Gtk;

public partial class MainWindow: Gtk.Window
{	
	public MainWindow (): base ("")
	{
		Build ();
	}
	
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{
		Application.Quit ();
		a.RetVal = true;
	}

	protected virtual void OnRunActivated(object sender, System.EventArgs e)
	{
	}

	protected virtual void OnButton1Activated(object sender, System.EventArgs e)
	{
	}

	protected virtual void OnButton2Activated(object sender, System.EventArgs e)
	{
	}

	protected virtual void OnButton2Clicked(object sender, System.EventArgs e)
	{
	}

	protected virtual void OnButton1Clicked(object sender, System.EventArgs e)
	{
	}

	[GLib.ConnectBeforeAttribute]
	protected virtual void OnEntry1KeyPressEvent(object o, Gtk.KeyPressEventArgs args)
	{
		Console.WriteLine (Gdk.Keyval.Name (args.Event.KeyValue));
	}
}
