//
// WelcomePageApps.cs
//
// Author:
//       Jason Smith <jason.smith@xamarin.com
//
// Copyright (c) 2012 Xamarin Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using Gtk;
using System;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Net;
using System.Web;
using System.Text.RegularExpressions;

namespace MonoDevelop.Ide.WelcomePage
{
	public class PrebuiltAppData
	{
		public string Title { get; set; }
		public string Description { get; set; }
		public string SupportedPlatforms { get; set; }
		public string DownloadLink { get; set; }
		public string ImageLink { get; set; }
	}

	public class AppModel
	{
		public string Description { get; set; }
		public string Name { get; set; }
		public string DownloadLink { get; set; }
		public Gdk.Pixbuf Image { get; set; }
	}

	public class AppPreviewModel
	{
		List<AppModel> apps;
		AppModel current;

		public event EventHandler CurrentChanged;

		public IEnumerable<AppModel> Apps {
			get {
				return apps.AsEnumerable ();
			}
		}

		public int CurrentIndex {
			get {
				return apps.IndexOf (Current);
			}
		}

		public int LastIndex {
			get {
				return apps.IndexOf (Last);
			}
		}

		public AppModel Current {
			get {
				return current;
			}
			private set {
				if (current == value)
					return;
				Last = current;
				current = value;
				OnCurrentChanged ();
			}
		}

		public AppModel Last { get; private set; }

		public AppPreviewModel ()
		{
			apps = new List<AppModel> ();
		}

		public void Add (AppModel app)
		{
			if (apps.Contains (app))
				return;

			apps.Add (app);
			if (Current == null)
			{
				Current = app;
			}
		}

		public void Remove (AppModel app)
		{
			if (!apps.Remove (app))
				return;

			if (Current == app)
				Current = apps.Any () ? Apps.First () : null;
		}

		public void Next ()
		{
			if (!apps.Any ())
				return;

			int newIndex = (CurrentIndex + 1) % apps.Count;
			Current = apps [newIndex];
		}

		public void Prev ()
		{
			if (!apps.Any ())
				return;

			int newIndex = (apps.Count + CurrentIndex - 1) % apps.Count;
			Current = apps [newIndex];
		}

		void OnCurrentChanged ()
		{
			if (CurrentChanged != null)
				CurrentChanged (this, EventArgs.Empty);
		}
	}

	public class SlidingImage : EventBox, Animatable
	{
		AppPreviewModel model;
		MouseTracker tracker;

		float animationProgress;

		const int ArrowWidth = 10;
		const int ArrowHeight = 17;

		const float ImageStartMove = 0;
		const float ImageEndMove = 0.66f;

		Movement movement;

		enum Movement {
			Left,
			Right,
		}

		bool overLeft;
		bool OverLeft {
			get {
				return overLeft;
			}
			set {
				if (overLeft == value)
					return;

				GdkWindow.Cursor = value ? new Gdk.Cursor (Gdk.CursorType.Hand1) : null;

				overLeft = value;
				QueueDraw ();
			}
		}

		bool overRight;
		bool OverRight {
			get {
				return overRight;
			}
			set {
				if (overRight == value)
					return;

				GdkWindow.Cursor = value ? new Gdk.Cursor (Gdk.CursorType.Hand1) : null;

				overRight = value;
				QueueDraw ();
			}
		}

		public SlidingImage (AppPreviewModel model)
		{
			VisibleWindow = false;
			movement = Movement.Right;
			HeightRequest = 320;
			WidthRequest = 340;
			this.model = model;
			animationProgress = 1.0f;

			Events |= Gdk.EventMask.ButtonReleaseMask | 
					Gdk.EventMask.ButtonPressMask | 
					Gdk.EventMask.PointerMotionMask | 
					Gdk.EventMask.EnterNotifyMask | 
					Gdk.EventMask.LeaveNotifyMask;

			model.CurrentChanged += (sender, e) => {
				if (model.Current == null || model.Last == null) {
					QueueDraw ();
				} else {
					animationProgress = 0;
					new Animation ()
						.Insert (ImageStartMove, ImageEndMove, new Animation ((f) => animationProgress = f, easing: Easing.CubicInOut))
						.Commit (this, "Slide", length: 750);
				}
			};

			tracker = new MouseTracker (this);
			tracker.MouseMoved += (sender, e) => {
				OverLeft = OverLeftButton (tracker.MousePosition.X, tracker.MousePosition.Y);
				OverRight = OverRightButton (tracker.MousePosition.X, tracker.MousePosition.Y);
			};

			tracker.HoveredChanged += (sender, e) => {
				if (!tracker.Hovered) {
					OverLeft = false;
					OverRight = false;
				}
			};
		}

		protected override void OnRealized ()
		{
			base.OnRealized ();
		}

		bool OverLeftButton (int x, int y)
		{
			Gdk.Rectangle region = new Gdk.Rectangle (10, 
			                                          Allocation.Height / 2 - ArrowHeight, 
			                                          ArrowWidth, 
			                                          ArrowHeight * 2);
			region.Inflate (5, 5);
			if (region.Contains (x, y))
				return true;
			return false;
		}

		bool OverRightButton (int x, int y)
		{
			Gdk.Rectangle region = new Gdk.Rectangle (Allocation.Width - ArrowWidth - 10, 
			                                          Allocation.Height / 2 - ArrowHeight, 
			                                          ArrowWidth, 
			                                          ArrowHeight * 2);
			region.Inflate (5, 5);
			if (region.Contains (x, y))
				return true;
			return false;
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			if (evnt.Button == 1) {
				if (OverLeftButton ((int)evnt.X, (int)evnt.Y)) {
					model.Prev ();
					movement = Movement.Left;
				} else if (OverRightButton ((int)evnt.X, (int)evnt.Y)) {
					model.Next ();
					movement = Movement.Right;
				}
			}
			return base.OnButtonReleaseEvent (evnt);
		}

		void DrawArrows (Cairo.Context context, Gdk.Rectangle region)
		{
			context.MoveTo (region.X + 10 + ArrowWidth, region.Y + region.Height / 2 - ArrowHeight);
			context.LineTo (region.X + 10, region.Y + region.Height / 2);
			context.LineTo (region.X + 10 + ArrowWidth, region.Y + region.Height / 2 + ArrowHeight);

			context.LineWidth = 4;
			context.Color = new Cairo.Color (0, 0, 0, OverLeft ? 1.0 : 0.5);
			context.Stroke ();

			context.MoveTo (region.Right - 10 - ArrowWidth, region.Y + region.Height / 2 - ArrowHeight);
			context.LineTo (region.Right - 10, region.Y + region.Height / 2);
			context.LineTo (region.Right - 10 - ArrowWidth, region.Y + region.Height / 2 + ArrowHeight);

			context.LineWidth = 4;
			context.Color = new Cairo.Color (0, 0, 0, OverRight ? 1.0 : 0.5);
			context.Stroke ();
		}

		void DrawImage (Cairo.Context context, Gdk.Pixbuf pixbuf, Gdk.Point center)
		{
			if (pixbuf == null)
				return;
			Gdk.CairoHelper.SetSourcePixbuf (context, pixbuf, center.X - pixbuf.Width / 2, center.Y - pixbuf.Height / 2);
			context.Paint ();
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				context.Translate (Allocation.X, Allocation.Y);
				context.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				context.Color = new Cairo.Color (1, 1, 1);
				context.Fill ();

				int spacing = (int) (Allocation.Width * 1.2);
				int center = Allocation.Width / 2;
				int direction = movement == Movement.Right ? 1 : -1;
				int currentX = (int) (center + (1.0f - animationProgress) * spacing * direction);
				int lastX = (int) (center - animationProgress * spacing * direction);

				if (model.Current != null)
					DrawImage (context, model.Current.Image, new Gdk.Point (currentX, Allocation.Height / 2));

				if (model.Last != null)
					DrawImage (context, model.Last.Image, new Gdk.Point (lastX, Allocation.Height / 2));

				Gdk.Rectangle region = new Gdk.Rectangle (0, 0, Allocation.Width, Allocation.Height);
				DrawArrows (context, region);
			}

			return base.OnExposeEvent (evnt);
		}
	}

	public class CrossfadeLabel : Gtk.Label, Animatable
	{
		const float TextFadeOutStart = 0;
		const float TextFadeOutEnd = 0.33f;

		const float TextFadeInStart = 0.66f;
		const float TextFadeInEnd = 1;

		double opacity;

		public CrossfadeLabel ()
		{
			opacity = 1;
		}

		public void Crossfade (string markup)
		{
			new Animation ()
				.Insert (TextFadeOutStart, TextFadeOutEnd, new Animation ((f) => opacity = f, 1, 0, Easing.CubicInOut, () => Markup = markup))
				.Insert (TextFadeInStart, TextFadeInEnd, new Animation ((f) => opacity = f, 0, 1, Easing.CubicInOut, null))
				.Commit (this, "Fade", length: 750);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			bool retVal = base.OnExposeEvent (evnt);

			using (var context = Gdk.CairoHelper.Create (evnt.Window)) {
				// Evil should check background color/stuff
				context.Color = new Cairo.Color (1, 1, 1, 1.0 - opacity);
				Gdk.Rectangle region = Allocation;
				region.Inflate (2, 2);
				context.Rectangle (region.ToCairoRect ());
				context.Fill ();
			}

			return retVal;
		}
	}

	public class WelcomePageApps : WelcomePageSection
	{
		CrossfadeLabel title;
		CrossfadeLabel description;
		Button download;
		SlidingImage images;
		AppPreviewModel model;

		const string UpdateUrl = "http://addons-staging.xamarin.com/api/prebuilt_apps";

		public WelcomePageApps (XElement el) : base (el)
		{
			model = new AppPreviewModel ();

			title = new CrossfadeLabel ();
			title.Xalign = 0;
		
			description = new CrossfadeLabel ();
			description.Xalign = 0;
			description.Wrap = true;
			Gtk.Alignment buttonAlign = new Alignment (0.5f, 0.5f, 0, 0);
			download = new Button (new Label ("Download Solution"));
			buttonAlign.Add (download);

			download.Pressed += (sender, e) => {
				if (model.Current == null)
					return;
				DesktopService.ShowUrl (model.Current.DownloadLink);
			};

			images = new SlidingImage (model);

			VBox layout = new VBox ();
			layout.PackStart (images, true, true, 0);
			layout.PackStart (title, false, true, 5);
			layout.PackStart (description, false, true, 5);
			layout.PackStart (buttonAlign, false, false, 5);

			SetContent (layout);

			model.CurrentChanged += (sender, e) => {
				if (model.Current == null)
					return;
				DescriptionText = model.Current.Description;
				TitleText = model.Current.Name;
			};

			UpdateApps ();
		}

		void UpdateApps ()
		{
			System.Threading.ThreadPool.QueueUserWorkItem ((state) => {
				var request = (HttpWebRequest)WebRequest.Create (UpdateUrl);
				
				try {
					//check to see if the online news file has been modified since it was last downloaded
					request.BeginGetResponse (HandleResponse, request);
				} catch (Exception ex) {
					LoggingService.LogWarning ("Welcome Page news file could not be downloaded.", ex);
				}
			});
		}

		void HandleResponse (IAsyncResult ar)
		{
			try {
				var request = (HttpWebRequest) ar.AsyncState;
				//FIXME: limit this size in case open wifi hotspots provide bad data
				var response = (HttpWebResponse) request.EndGetResponse (ar);
				if (response.StatusCode == HttpStatusCode.OK) {
					XmlSerializer serializer = new XmlSerializer (typeof (PrebuiltAppData[]));
					PrebuiltAppData[] data = serializer.Deserialize (response.GetResponseStream ()) as PrebuiltAppData[];

					if (data != null) {
						foreach (var appData in data) {
							AppModel app = new AppModel ();
							app.Name = appData.Title;
							app.Description = appData.Description;
							app.DownloadLink = appData.DownloadLink;
							Gtk.Application.Invoke ((o, e) => {
								model.Add (app);
							});

							ImageLoader loader = new ImageLoader (appData.ImageLink);
							loader.LoadOperation.Completed += (IAsyncOperation op) => {
								if (loader.Pixbuf != null) {
									app.Image = loader.Pixbuf;
									QueueDraw ();
								}
							};
						}
					}
				}
			} catch (Exception ex) {
				LoggingService.LogWarning ("Prebuilt Apps file could not be downloaded.", ex);
			}
		}

		string DescriptionText {
			set {
				string descFormat = Styles.GetFormatString (Styles.WelcomeScreen.Pad.SummaryFontFamily, Styles.WelcomeScreen.Pad.SummaryFontSize, Styles.WelcomeScreen.Pad.TextColor);
				description.Crossfade (string.Format (descFormat, (value ?? "").Replace ("\n"," ")));
			}
		}

		string TitleText {
			set {
				var face = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;
				string linkFormat = Styles.GetFormatString (face, Styles.WelcomeScreen.Pad.MediumTitleFontSize, Styles.WelcomeScreen.Pad.MediumTitleColor);
				title.Crossfade (string.Format (linkFormat, GLib.Markup.EscapeText (value)));
			}
		}
	}
}

