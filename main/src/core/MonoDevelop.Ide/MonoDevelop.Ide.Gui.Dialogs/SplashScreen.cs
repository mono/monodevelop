using System;
using System.IO;
using Gtk;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Components;

namespace MonoDevelop.Ide.Gui.Dialogs {

	public class SplashScreenForm : Window, IProgressMonitor, IDisposable
	{
		ConsoleProgressMonitor monitor = new ConsoleProgressMonitor ();

		const int SplashFontSize = 10;
		const string SplashFontFamily = "sans-serif";

		Xwt.Drawing.Image bitmap;
		bool showVersionInfo;

		//this is a popup so it behaves like other splashes on Windows, i.e. doesn't show up as a second window in the taskbar.
		public SplashScreenForm () : base (WindowType.Popup)
		{
			AppPaintable = true;
			this.Decorated = false;
			this.WindowPosition = WindowPosition.Center;
			this.TypeHint = Gdk.WindowTypeHint.Splashscreen;
			this.showVersionInfo = BrandingService.GetBool ("SplashScreen", "ShowVersionInfo") ?? true;

			var file = BrandingService.GetFile ("SplashScreen.png");
			if (file != null)
				bitmap = Xwt.Drawing.Image.FromFile (file);
			else
				bitmap = Xwt.Drawing.Image.FromResource ("SplashScreen.png");

			this.Resize ((int)bitmap.Width, (int)bitmap.Height);
			MessageService.PopupDialog += HandlePopupDialog;
		}

		void HandlePopupDialog (object sender, EventArgs e)
		{
			if (!isDestroyed)
				Destroy ();
		}

		bool isDestroyed;

		protected override void OnDestroyed ()
		{
			isDestroyed = true;
			MessageService.PopupDialog -= HandlePopupDialog;
			base.OnDestroyed ();
			if (bitmap != null) {
				bitmap.Dispose ();
				bitmap = null;
			}
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evt)
		{
			var build = "";
			var version = "v" + BuildInfo.VersionLabel;

			var index = version.IndexOf (' ');
			if (index != -1) {
				build = version.Substring (index + 1);
				version = version.Substring (0, index);
			}

			if (bitmap != null) {
				using (var context = Gdk.CairoHelper.Create (GdkWindow)) {
					context.Antialias = Cairo.Antialias.Subpixel;

					// Render the image first.
					context.DrawImage (this, bitmap, 0, 0);

					if (showVersionInfo) {
						var bottomRight = new Cairo.PointD (bitmap.Width - 10, bitmap.Height - 12);

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

			c.SetSourceRGB (1, 1, 1);
			c.ShowText (text);
		}

		void DrawAlphaBetaMarker (Cairo.Context c, ref Cairo.PointD bottomRight, string text)
		{
			c.SelectFontFace (SplashFontFamily, Cairo.FontSlant.Normal, Cairo.FontWeight.Bold);
			c.SetFontSize (SplashFontSize);

			// Create a rectangle larger than the text so we can have a nice border
			// And round the value so we don't have a blurry rectangle.
			var extents = c.TextExtents (text);
			var x = Math.Round (bottomRight.X - extents.Width * 1.3);
			var y = Math.Round (bottomRight.Y - extents.Height * 2.8);
			var rectangle = new Cairo.Rectangle (x, y, bottomRight.X - x, bottomRight.Y - y);

			// Draw the background color the text will be overlaid on
			DrawRectangle (c, rectangle);

			// Calculate the offset the text will need to be at to be centralised
			// in the border
			x = x + extents.XBearing + (rectangle.Width - extents.Width) / 2;
			y = y - extents.YBearing + (rectangle.Height - extents.Height) / 2;
			c.MoveTo (x, y);

			// Draw the text
			c.SetSourceRGB (1, 1, 1);
			c.ShowText (text);

			bottomRight.Y -= rectangle.Height - 2;
		}

		void DrawRectangle (Cairo.Context c, Cairo.Rectangle rect)
		{
			double x = rect.X;
			double y = rect.Y;
			double width = rect.Width;
			double height = rect.Height;

			c.Rectangle (x, y, width, height);
			c.SetSourceRGB (0, 0, 0);
			c.Fill ();
		}

		static void RunMainLoop ()
		{
			DispatchService.RunPendingEvents ();
		}

		void IProgressMonitor.BeginTask (string name, int totalWork)
		{
			monitor.BeginTask (name, totalWork);
			RunMainLoop ();
		}

		void IProgressMonitor.BeginStepTask (string name, int totalWork, int stepSize)
		{
			monitor.BeginStepTask (name, totalWork, stepSize);
			RunMainLoop ();
		}

		void IProgressMonitor.EndTask ()
		{
			monitor.EndTask ();
			RunMainLoop ();
		}

		void IProgressMonitor.Step (int work)
		{
			monitor.Step (work);
			RunMainLoop ();
		}

		TextWriter IProgressMonitor.Log {
			get { return monitor.Log; }
		}

		void IProgressMonitor.ReportWarning (string message)
		{
			monitor.ReportWarning (message);
		}

		void IProgressMonitor.ReportSuccess (string message)
		{
			monitor.ReportSuccess (message);
		}

		void IProgressMonitor.ReportError (string message, Exception exception)
		{
			monitor.ReportError (message, exception);
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
			get { return monitor.AsyncOperation; }
		}

		object IProgressMonitor.SyncRoot {
			get { return monitor.SyncRoot; }
		}

		void IDisposable.Dispose ()
		{
			if (!isDestroyed)
				Destroy ();
		}
	}
}
