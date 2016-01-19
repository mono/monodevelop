//
// AbstractNavigationExtension.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using ICSharpCode.NRefactory.Editor;
using System.Threading;
using MonoDevelop.Core;

namespace MonoDevelop.Ide.Editor.Extension
{
	public abstract class AbstractNavigationExtension : TextEditorExtension
	{
		uint timerId;

		#region Key handling
		static bool linksShown;

		static bool LinksShown {
			get {
				return linksShown;
			}
			set {
				if (value == linksShown)
					return;
				linksShown = value;
				OnLinksShownChanged (EventArgs.Empty);
			}
		}

		static void OnLinksShownChanged (EventArgs e)
		{
			LinksShownChanged?.Invoke (null, e);
		}

		public static event EventHandler LinksShownChanged;

		static uint snooperId;


		public class NavigationSegment : ISegment
		{
			public int Offset { get; private set; }

			public int Length { get; private set; }

			public int EndOffset {
				get {
					return Offset + Length;
				}
			}

			public Action Activate { get; private set; }

			public NavigationSegment (int offset, int length, System.Action activate)
			{
				this.Offset = offset;
				this.Length = length;
				this.Activate = activate;
			}
		}

		static AbstractNavigationExtension ()
		{
			snooperId = Gtk.Key.SnooperInstall (TooltipKeySnooper);
			//if (snooperId != 0)
			//	Gtk.Key.SnooperRemove (snooperId);
			IdeApp.Workbench.RootWindow.FocusOutEvent += RootWindow_FocusOutEvent;
		}

		static void RootWindow_FocusOutEvent (object o, Gtk.FocusOutEventArgs args)
		{
			LinksShown = false;
		}

		static int TooltipKeySnooper (Gtk.Widget widget, Gdk.EventKey evnt)
		{
			if (evnt != null && evnt.Type == Gdk.EventType.KeyPress) {
				LinksShown = IsTriggerKey (evnt);
			}
			if (evnt != null && evnt.Type == Gdk.EventType.KeyRelease && IsTriggerKey (evnt)) {
				LinksShown = false;
			}
			return 0; //FALSE
		}

		static bool IsTriggerKey (Gdk.EventKey evnt)
		{
			#if MAC
			return evnt.Key == Gdk.Key.Meta_L || evnt.Key == Gdk.Key.Meta_R;
			#else
			return evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R;
			#endif
		}
		#endregion

		#region Extension API

		protected abstract Task<IEnumerable<NavigationSegment>> RequestLinksAsync (int offset, int length, CancellationToken token);

		#endregion

		protected override void Initialize ()
		{
			LinksShownChanged += AbstractNavigationExtension_LinksShownChanged;
			this.DocumentContext.DocumentParsed += DocumentContext_DocumentParsed;
			this.Editor.LineShown += Editor_LineShown;

			if (LinksShown)
				ShowLinks ();
		}

		void AbstractNavigationExtension_LinksShownChanged (object sender, EventArgs e)
		{
			RemoveTimer ();
			if (LinksShown) {
				timerId = GLib.Timeout.Add (250, delegate {
					timerId = 0;
					ShowLinks ();
					return false;
				});
			} else {
				HideLinks ();
			}
		}

		void DocumentContext_DocumentParsed (object sender, EventArgs e)
		{
			if (LinksShown)
				ShowLinks ();
		}

		List<ITextSegmentMarker> markers = new List<ITextSegmentMarker> ();

		async void ShowLinks ()
		{
			HideLinks ();
			try {
				foreach (var line in Editor.VisibleLines) {
					if (line.Length <= 0)
						continue;
					foreach (var segment in await RequestLinksAsync (line.Offset, line.Length, default (CancellationToken))) {
						var marker = Editor.TextMarkerFactory.CreateLinkMarker (Editor, segment.Offset, segment.Length, delegate { segment.Activate (); });
						marker.OnlyShowLinkOnHover = true;
						Editor.AddMarker (marker);
						markers.Add (marker);
					}
				}
			} catch (Exception e) {
				LoggingService.LogError ("Error while retrieving nav links.", e);
			}
		}

		async void Editor_LineShown (object sender, Ide.Editor.LineEventArgs e)
		{
			if (LinksShown) {
				var line = e.Line;
				foreach (var segment in await RequestLinksAsync (line.Offset, line.Length, default (CancellationToken))) {
					var marker = Editor.TextMarkerFactory.CreateLinkMarker (Editor, segment.Offset, segment.Length, delegate { segment.Activate (); });
					marker.OnlyShowLinkOnHover = true;
					Editor.AddMarker (marker);
					markers.Add (marker);
				}
			}
		}

		void HideLinks ()
		{
			foreach (var m in markers) {
				Editor.RemoveMarker (m);
			}
			markers.Clear ();
		}

		void RemoveTimer ()
		{
			if (timerId != 0)
				GLib.Source.Remove (timerId);
		}

		public override void Dispose ()
		{
			LinksShownChanged -= AbstractNavigationExtension_LinksShownChanged;
			DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
			Editor.LineShown -= Editor_LineShown;
			HideLinks ();
			RemoveTimer ();
			base.Dispose ();
		}

	}
}