using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Gtk;

namespace MonoDevelop.Gui.Dialogs {
	
	public class SplashScreenForm : Gtk.Window
	{
		static SplashScreenForm splashScreen = new SplashScreenForm();
		static ArrayList requestedFileList = new ArrayList();
		static ArrayList parameterList = new ArrayList();
		static ProgressBar progress;
		static VBox vbox;
		
		public static SplashScreenForm SplashScreen {
			get {
				return splashScreen;
			}
		}		
		
		public SplashScreenForm() : base (Gtk.WindowType.Popup)
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

		public static string[] GetParameterList()
		{
			return GetStringArray(parameterList);
		}
		
		public static string[] GetRequestedFileList()
		{
			return GetStringArray(requestedFileList);
		}
		
		static string[] GetStringArray(ArrayList list)
		{
			return (string[])list.ToArray(typeof(string));
		}

		public static void SetProgress(double Percentage)
		{
			progress.Fraction = Percentage;
		}

		public static void SetMessage(string Message)
		{
			progress.Text = Message;
		}
		
		public static void SetCommandLineArgs(string[] args)
		{
			requestedFileList.Clear();
			parameterList.Clear();
			
			foreach (string arg in args)
			{
				string a = arg;
				// this does not yet work with relative paths
				if (a[0] == '~')
				{
					a = System.IO.Path.Combine (Environment.GetEnvironmentVariable ("HOME"), a.Substring (1));
				}
				
				if (System.IO.File.Exists (a))
				{
					requestedFileList.Add (a);
					return;
				}
	
				if (a[0] == '-' || a[0] == '/') {
					int markerLength = 1;
					
					if (a.Length >= 2 && a[0] == '-' && a[1] == '-') {
						markerLength = 2;
					}
					
					parameterList.Add(a.Substring (markerLength));
				}
			}
		}
	}
}
