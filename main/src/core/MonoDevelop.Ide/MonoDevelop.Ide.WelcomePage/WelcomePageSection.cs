//
// WelcomePageSection.cs
//
// Author:
//       lluis <${AuthorEmail}>
//
// Copyright (c) 2012 lluis
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
using System;
using Gtk;
using System.Xml.Linq;
using MonoDevelop.Core;
using MonoDevelop.Components;
using MonoDevelop.Components.AtkCocoaHelper;

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageSection: EventBox
	{
		string title;

		static string headerFormat;

		Label label;
		Alignment root = new Alignment (0, 0, 1f, 1f);
		protected Gtk.Alignment ContentAlignment { get; private set; }
		protected Gtk.Alignment TitleAlignment { get; private set; }

		static WelcomePageSection ()
		{
			UpdateStyle ();
			Gui.Styles.Changed += (sender, e) => UpdateStyle();
		}

		static void UpdateStyle ()
		{
			var face = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;
			headerFormat = Styles.GetFormatString (face, Styles.WelcomeScreen.Pad.LargeTitleFontSize, Styles.WelcomeScreen.Pad.LargeTitleFontColor);
		}

		public WelcomePageSection (string title = null)
		{
			if (!string.IsNullOrEmpty (title)) {
				Accessible.SetTitle (title);
			}

			this.title = title;
			VisibleWindow = false;
			root.Accessible.SetShouldIgnore (true);
			Add (root);

			root.Show ();

			uint p = Styles.WelcomeScreen.Pad.ShadowSize * 2;
			root.SetPadding (p, p, p, p);

			TitleAlignment = new Alignment (0f, 0f, 1f, 1f);
			TitleAlignment.Accessible.SetShouldIgnore (true);
			p = Styles.WelcomeScreen.Pad.Padding;
			TitleAlignment.SetPadding (p, Styles.WelcomeScreen.Pad.LargeTitleMarginBottom, p, p);

			ContentAlignment = new Alignment (0f, 0f, 1f, 1f);
			ContentAlignment.SetPadding (0, p, p, p);
			ContentAlignment.Accessible.SetShouldIgnore (true);

			Gui.Styles.Changed += UpdateStyle;
		}

		void UpdateStyle (object sender, EventArgs args)
		{
			if (label != null)
				label.Markup = string.Format (headerFormat, title);
			QueueDraw ();
		}

		public void SetContent (Gtk.Widget w)
		{
			ContentAlignment.Add (w);

			if (string.IsNullOrEmpty (title)) {
				ContentAlignment.TopPadding = Styles.WelcomeScreen.Pad.Padding;
				root.Add (ContentAlignment);
				return;
			}

			var box = new VBox ();
			box.Accessible.SetShouldIgnore (true);

			label = new Label () { Markup = string.Format (headerFormat, title), Xalign = (uint) 0 };
			TitleAlignment.Add (label);
			box.PackStart (TitleAlignment, false, false, 0);
			box.PackStart (ContentAlignment, false, false, 0);
			root.Add (box);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var ctx = Gdk.CairoHelper.Create (evnt.Window)) {
				ctx.LineWidth = 1;
				var rect = new Gdk.Rectangle (Allocation.X, Allocation.Y, Allocation.Width, Allocation.Height);

				var shadowColor = CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.ShadowColor);
				int inset = 2;
				var ss = Styles.WelcomeScreen.Pad.ShadowSize; 
				var r = new Cairo.Rectangle (rect.X + ss + 0.5, rect.Y + ss + 0.5, rect.Width - ss * 2 - 1, rect.Height - ss * 2 - 1);
				var sr = new Cairo.Rectangle (r.X + inset, r.Y + inset + Styles.WelcomeScreen.Pad.ShadowVerticalOffset, r.Width - inset * 2, r.Height - inset * 2);
				int size = Styles.WelcomeScreen.Pad.ShadowSize;
				double alpha = 0.2;
				double alphaDec = 0.2 / (double)size;
				for (int n=0; n<size; n++) {
					sr = new Cairo.Rectangle (sr.X - 1, sr.Y - 1, sr.Width + 2, sr.Height + 2);
					CairoExtensions.RoundedRectangle (ctx, sr.X, sr.Y, sr.Width, sr.Height, 4);
					shadowColor.A = alpha;
					ctx.SetSourceColor (shadowColor);
					ctx.Stroke ();
					alpha -= alphaDec;
				}

				CairoExtensions.RoundedRectangle (ctx, r.X, r.Y, r.Width, r.Height, 4);
				ctx.SetSourceColor (CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.BackgroundColor));
				ctx.FillPreserve ();
				ctx.SetSourceColor (CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.BorderColor));
				ctx.Stroke ();
			}

			PropagateExpose (Child, evnt);
			return true;
		}

		public static string FormatText (string fontFace, int fontSize, Pango.Weight weight, string color, string text)
		{
			var format = Styles.GetFormatString (fontFace, fontSize, color, weight);
			return string.Format (format, GLib.Markup.EscapeText (text));
		}

		public static void DispatchLink (string uri)
		{
			try {
				if (uri.StartsWith ("project://")) {
					string file = uri.Substring ("project://".Length);
					Gdk.ModifierType mtype = GtkWorkarounds.GetCurrentKeyModifiers ();
					bool inWorkspace = (mtype & Gdk.ModifierType.ControlMask) != 0;
					if (Platform.IsMac && !inWorkspace)
						inWorkspace = (mtype & Gdk.ModifierType.Mod2Mask) != 0;

					// Notify the RecentFiles that this item does not exist anymore.
					// Possible other solution would be to check the recent projects list on focus in
					// and update them accordingly.
					if (!System.IO.File.Exists (file)) {
						var res = MessageService.AskQuestion (
							GettextCatalog.GetString ("{0} could not be opened", file),
							GettextCatalog.GetString ("Do you want to remove the reference to it from the Recent list?"),
							AlertButton.No, AlertButton.Yes);
						if (res == AlertButton.Yes)
							FileService.NotifyFileRemoved (file);
						return;
					}

					IdeApp.Workspace.OpenWorkspaceItem (file, !inWorkspace);
				} else if (uri.StartsWith ("monodevelop://")) {
					var cmdId = uri.Substring ("monodevelop://".Length);
					IdeApp.CommandService.DispatchCommand (cmdId, MonoDevelop.Components.Commands.CommandSource.WelcomePage);
				} else {
					DesktopService.ShowUrl (uri);
				}
			} catch (Exception ex) {
				LoggingService.LogInternalError (GettextCatalog.GetString ("Could not open the url '{0}'", uri), ex);
			}
		}

		// Accessible widgets can say what other widget acts as their title
		// so use this to set the Section title to be that title
		//
		// The content cannot automatically be set because it might be ignored
		// by accessibility
		//
		// This must be called after setContent, otherwise label will be null
		protected void SetTitledWidget (Widget widget)
		{
			if (label == null) {
				return;
			}

			widget.Accessible.SetTitleUIElement (label.Accessible);
			label.Accessible.AddElementToTitle (widget.Accessible);
		}

		protected void RemoveAccessibiltyTitledWidget (Widget widget)
		{
			if (label == null) {
				return;
			}

			label.Accessible.RemoveElementFromTitle (widget.Accessible);
		}
	}
}

