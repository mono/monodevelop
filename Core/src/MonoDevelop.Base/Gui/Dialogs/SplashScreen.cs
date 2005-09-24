using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Gtk;

namespace MonoDevelop.Gui.Dialogs {
	
	public class SplashScreenForm : Gtk.Window
	{
		static SplashScreenForm splashScreen = new SplashScreenForm();
		static ProgressBar progress;
		static VBox vbox;
		
		public static SplashScreenForm SplashScreen {
			get {
				return splashScreen;
			}
		}
		
		public SplashScreenForm () : base (Gtk.WindowType.Popup)
		{
			this.Decorated = false;
			this.WindowPosition = WindowPosition.Center;
			this.TypeHint = Gdk.WindowTypeHint.Splashscreen;
			Gdk.Pixbuf bitmap = new Gdk.Pixbuf(Assembly.GetEntryAssembly(), "SplashScreen.png");
			Gtk.Image image = new Gtk.Image (bitmap);
			image.Show ();

			HBox hbox = new HBox();
			Alignment align = new Alignment (0.5f, 1.0f, 0.90f, 1.0f);
			progress = new ProgressBar();
			progress.Fraction = 0.00;
			align.Add (progress);
			hbox.PackStart (align, true, true, 0);
			hbox.ShowAll();

			vbox = new VBox();
			vbox.PackStart(image, true, true, 0);
			vbox.PackStart(hbox, false, true, 5);

			this.Add (vbox);
		}

		public static void SetProgress (double Percentage)
		{
			progress.Fraction = Percentage;
		}

		public static void SetMessage (string Message)
		{
			progress.Text = Message;
		}
	}
}
