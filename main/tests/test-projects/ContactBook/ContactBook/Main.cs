// project created on 23/08/2006 at 16:37
using System;
using Gtk;

namespace ContactBook
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Application.Init ();
			MainWindow win = new MainWindow ();
			win.Show ();
			Application.Run ();
		}
	}
}