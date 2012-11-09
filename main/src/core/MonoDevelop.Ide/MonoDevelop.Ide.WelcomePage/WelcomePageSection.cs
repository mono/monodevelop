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

namespace MonoDevelop.Ide.WelcomePage
{
	public class WelcomePageSection: EventBox
	{
		string title;

		static readonly string headerFormat;

		Alignment root = new Alignment (0, 0, 1f, 1f);
		protected Gtk.Alignment ContentAlignment { get; private set; }
		protected Gtk.Alignment TitleAlignment { get; private set; }

		static WelcomePageSection ()
		{
			var face = Platform.IsMac ? Styles.WelcomeScreen.Pad.TitleFontFamilyMac : Styles.WelcomeScreen.Pad.TitleFontFamilyWindows;
			headerFormat = Styles.GetFormatString (face, Styles.WelcomeScreen.Pad.LargeTitleFontSize, Styles.WelcomeScreen.Pad.LargeTitleFontColor);
		}

		public WelcomePageSection (string title = null)
		{
			this.title = title;
			VisibleWindow = false;
			Add (root);
			root.Show ();

			uint p = Styles.WelcomeScreen.Pad.ShadowSize * 2;
			root.SetPadding (p, p, p, p);

			TitleAlignment = new Alignment (0f, 0f, 1f, 1f);
			p = Styles.WelcomeScreen.Pad.Padding;
			TitleAlignment.SetPadding (p, Styles.WelcomeScreen.Pad.LargeTitleMarginBottom, p, p);
			ContentAlignment = new Alignment (0f, 0f, 1f, 1f);
			ContentAlignment.SetPadding (0, p, p, p);
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
			var label = new Gtk.Label () { Markup = string.Format (headerFormat, title), Xalign = (uint) 0 };
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
					ctx.Color = shadowColor;
					ctx.Stroke ();
					alpha -= alphaDec;
				}

				CairoExtensions.RoundedRectangle (ctx, r.X, r.Y, r.Width, r.Height, 4);
				ctx.Color = CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.BackgroundColor);
				ctx.FillPreserve ();
				ctx.Color = CairoExtensions.ParseColor (Styles.WelcomeScreen.Pad.BorderColor);
				ctx.Stroke ();
			}

			PropagateExpose (Child, evnt);
			return true;
		}

		public static string FormatText (string fontFace, int fontSize, bool bold, string color, string text)
		{
			var format = Styles.GetFormatString (fontFace, fontSize, color, bold);
			return string.Format (format, GLib.Markup.EscapeText (text));
		}

		public static void DispatchLink (string uri)
		{
			try {
				if (uri.StartsWith ("project://")) {
					string projectUri = uri.Substring ("project://".Length);			
					Uri fileuri = new Uri (projectUri);
					Gdk.ModifierType mtype = Mono.TextEditor.GtkWorkarounds.GetCurrentKeyModifiers ();
					bool inWorkspace = (mtype & Gdk.ModifierType.ControlMask) != 0;
					IdeApp.Workspace.OpenWorkspaceItem (fileuri.LocalPath, !inWorkspace);
				} else if (uri.StartsWith ("monodevelop://")) {
					var cmdId = uri.Substring ("monodevelop://".Length);
					IdeApp.CommandService.DispatchCommand (cmdId, MonoDevelop.Components.Commands.CommandSource.WelcomePage);
				} else {
					DesktopService.ShowUrl (uri);
				}
			} catch (Exception ex) {
				MessageService.ShowException (ex, GettextCatalog.GetString ("Could not open the url '{0}'", uri));
			}
		}
	}
}

