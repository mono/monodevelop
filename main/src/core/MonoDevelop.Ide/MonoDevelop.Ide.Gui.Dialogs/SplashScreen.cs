using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.Gui.Dialogs {
	
	public class SplashScreenForm : Gtk.Window, IProgressMonitor, IDisposable
	{
		const int SplashFontSize = 10;
		const string SplashFontFamily = "sans-serif"; 
		
		static SplashScreenForm splashScreen;
		static ProgressBar progress;
		static VBox vbox;
		ProgressTracker tracker = new ProgressTracker ();
		Gdk.Pixbuf bitmap;
		static Gtk.Label label;
		bool showVersionInfo;
		
		public static SplashScreenForm SplashScreen {
			get {
				if (splashScreen == null)
					splashScreen = new SplashScreenForm();
				return splashScreen;
			}
		}
		
		//this is a popup so it behaves like other splashes on Windows, i.e. doesn't show up as a second window in the taskbar.
		public SplashScreenForm () : base (Gtk.WindowType.Popup)
		{
			AppPaintable = true;
			this.Decorated = false;
			this.WindowPosition = WindowPosition.Center;
			this.TypeHint = Gdk.WindowTypeHint.Splashscreen;
			this.showVersionInfo = BrandingService.GetBool ("SplashScreen", "ShowVersionInfo") ?? true;
			try {
				using (var stream = BrandingService.GetStream ("SplashScreen.png", true))
					bitmap = new Gdk.Pixbuf (stream);
			} catch (Exception e) {
				LoggingService.LogError ("Can't load splash screen pixbuf 'SplashScreen.png'.", e);
			}
			progress = new ProgressBar();
			progress.Fraction = 0.00;
			progress.HeightRequest = 6;

			vbox = new VBox();
			vbox.BorderWidth = 12;
			label = new Gtk.Label ();
			label.UseMarkup = true;
			label.Xalign = 0;
			vbox.PackEnd (progress, false, true, 0);
			vbox.PackEnd (label, false, true, 3);
			this.Add (vbox);
			if (bitmap != null)
				this.Resize (bitmap.Width, bitmap.Height);
		}
		
		protected override void OnDestroyed ()
		{
			base.OnDestroyed ();
			if (bitmap != null) {
				bitmap.Dispose ();
				bitmap = null;
			}
		}
		
		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			var build = "";
			var version = "v" + BuildVariables.PackageVersionLabel;
			
			var index = version.IndexOf (' ');
			if (index != -1) {
				build = version.Substring (index + 1);
				version = version.Substring (0, index);
			}
			
			if (bitmap != null) {
				using (var context = Gdk.CairoHelper.Create (GdkWindow)) {
					context.Antialias = Cairo.Antialias.Subpixel;
				
					// Render the image first.
					bitmap.RenderToDrawable (GdkWindow, new Gdk.GC (GdkWindow), 0, 0, 0, 0, bitmap.Width, bitmap.Height, Gdk.RgbDither.None, 0, 0);
					
					if (showVersionInfo) {
						var bottomRight = new Cairo.PointD (bitmap.Width - 12, bitmap.Height - 25);
						// Render the alpha/beta text if we're an alpha or beta. If this
						// is rendered, the bottomRight point will be shifted upwards to
						// allow the MonoDevelop version to be rendered above the alpha marker
						if (!string.IsNullOrEmpty (build)) {
							DrawAlphaBetaMarker (context, ref bottomRight, build);
						}
						// Render the MonoDevelop version
						DrawVersionNumber (context, ref bottomRight, version);
					}
				}
			}

			return true;
		}
		
		void DrawVersionNumber (Cairo.Context c, ref Cairo.PointD bottomRight, string text)
		{
			c.SelectFontFace (SplashFontFamily, Cairo.FontSlant.Normal, Cairo.FontWeight.Normal);
			c.SetFontSize (SplashFontSize);
			
			var extents = c.TextExtents (text);
			c.MoveTo (bottomRight.X - extents.Width - 1, bottomRight.Y - extents.Height);
			
			c.Color = new Cairo.Color (1, 1, 1);
			c.ShowText (text);
		}
		
		void DrawAlphaBetaMarker (Cairo.Context c, ref Cairo.PointD bottomRight, string text)
		{
			c.SelectFontFace (SplashFontFamily, Cairo.FontSlant.Normal, Cairo.FontWeight.Bold);
			c.SetFontSize (SplashFontSize);
			
			// Create a rectangle larger than the text so we can have a nice border
			var extents = c.TextExtents (text);
			var x = bottomRight.X - extents.Width * 1.3;
			var y = bottomRight.Y - extents.Height * 1.5;
			var rectangle = new Cairo.Rectangle (x, y, bottomRight.X - x, bottomRight.Y - y);
			
			// Draw the background color the text will be overlaid on
			DrawRoundedRectangle (c, rectangle);
			
			// Calculate the offset the text will need to be at to be centralised
			// in the border
			x = x + extents.XBearing + (rectangle.Width - extents.Width) / 2;
			y = y - extents.YBearing + (rectangle.Height - extents.Height) / 2;
			c.MoveTo (x, y);
			
			// Draw the text
			c.Color = new Cairo.Color (1, 1, 1);
			c.ShowText (text);
			
			bottomRight.Y -= rectangle.Height - 2;
		}
		
		void DrawRoundedRectangle (Cairo.Context c, Cairo.Rectangle rect)
		{
			double x = rect.X;
			double y = rect.Y;
			double width = rect.Width;
			double height = rect.Height;
			double radius = 5;
			
			c.MoveTo (x, y + radius);
			c.Arc (x + radius, y + radius, radius, Math.PI, -Math.PI / 2);
			c.LineTo (x + width - radius, y);
			c.Arc (x + width - radius, y + radius, radius, -Math.PI / 2, 0);
			c.LineTo (x + width, y + height - radius);
			c.Arc (x + width - radius, y + height - radius, radius, 0, Math.PI / 2);
			c.LineTo (x + radius, y + height);
			c.Arc (x + radius, y + height - radius, radius, Math.PI / 2, Math.PI);
			c.ClosePath ();
			
			c.Color = new Cairo.Color (161 / 255.0, 40 / 255.0, 48 / 255.0);
			c.Fill ();
		}

		public static void SetProgress (double Percentage)
		{
			progress.Fraction = Percentage;
			RunMainLoop ();
		}

		public void SetMessage (string Message)
		{
			if (bitmap == null) {
				label.Text = Message;
			} else {
				label.Markup = "<span size='small' foreground='white'>" + Message + "</span>";
			}
			RunMainLoop ();
		}
		
		static void RunMainLoop ()
		{
			DispatchService.RunPendingEvents ();
		}
		
		void IProgressMonitor.BeginTask (string name, int totalWork)
		{
			tracker.BeginTask (name, totalWork);
			SetMessage (tracker.CurrentTask);
		}
		
		void IProgressMonitor.BeginStepTask (string name, int totalWork, int stepSize)
		{
			tracker.BeginStepTask (name, totalWork, stepSize);
			SetMessage (tracker.CurrentTask);
		}
		
		void IProgressMonitor.EndTask ()
		{
			tracker.EndTask ();
			SetProgress (tracker.GlobalWork);
			SetMessage (tracker.CurrentTask);
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
		
		public event MonitorHandler CancelRequested {
			add { }
			remove { }
		}
		
		// The returned IAsyncOperation object must be thread safe
		IAsyncOperation IProgressMonitor.AsyncOperation {
			get { return null; }
		}
		
		object IProgressMonitor.SyncRoot {
			get { return this; }
		}
		
		void IDisposable.Dispose ()
		{
			Destroy ();
			splashScreen = null;
		}
	}
}
