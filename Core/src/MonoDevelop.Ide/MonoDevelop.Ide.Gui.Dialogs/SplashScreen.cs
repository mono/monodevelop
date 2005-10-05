using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.Gui.Dialogs {
	
	public class SplashScreenForm : Gtk.Window, IProgressMonitor
	{
		static SplashScreenForm splashScreen = new SplashScreenForm();
		static ProgressBar progress;
		static VBox vbox;
		ProgressTracker tracker = new ProgressTracker ();
		
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
			Gdk.Pixbuf bitmap = new Gdk.Pixbuf(Assembly.GetCallingAssembly(), "SplashScreen.png");
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
			RunMainLoop ();
		}

		public static void SetMessage (string Message)
		{
			progress.Text = Message;
			RunMainLoop ();
		}
		
		static void RunMainLoop ()
		{
			while (Gtk.Application.EventsPending()) {
				Gtk.Application.RunIteration (false);
			}
		}
		
		void IProgressMonitor.BeginTask (string name, int totalWork)
		{
			tracker.BeginTask (name, totalWork);
			SetMessage (name);
		}
		
		void IProgressMonitor.EndTask ()
		{
			tracker.EndTask ();
			SetProgress (tracker.GlobalWork);
		}
		
		void IProgressMonitor.Step (int work)
		{
			tracker.Step (work);
			SetProgress (tracker.GlobalWork);
		}
		
		TextWriter IProgressMonitor.Log {
			get { return Console.Out; }
		}
		
		void IProgressMonitor.ReportWarning (string message)
		{
		}
		
		void IProgressMonitor.ReportSuccess (string message)
		{
		}
		
		void IProgressMonitor.ReportError (string message, Exception exception)
		{
		}
		
		bool IProgressMonitor.IsCancelRequested {
			get { return false; }
		}
		
		public event MonitorHandler CancelRequested;
		
		// The returned IAsyncOperation object must be thread safe
		IAsyncOperation IProgressMonitor.AsyncOperation {
			get { return null; }
		}
		
		object IProgressMonitor.SyncRoot {
			get { return this; }
		}
		
		public override void Dispose ()
		{
			Hide ();
			base.Dispose ();
		}
	}
}
