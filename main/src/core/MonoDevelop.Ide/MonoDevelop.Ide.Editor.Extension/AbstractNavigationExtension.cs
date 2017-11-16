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
using Xwt;
using System.Linq;

namespace MonoDevelop.Ide.Editor.Extension
{
	public abstract class AbstractNavigationExtension : TextEditorExtension
	{
		uint timerId;
		CancellationTokenSource src = new CancellationTokenSource ();

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

		//static uint snooperId;


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
			if (IdeApp.Workbench?.RootWindow == null)
				return;
			// snooperId =
				Gtk.Key.SnooperInstall (TooltipKeySnooper);
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
			this.Editor.MouseMoved += Editor_MouseMoved;

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
		List<IDocumentLine> visibleLines = new List<IDocumentLine> ();
		void ShowLinks ()
		{
			HideLinks ();
			try {
				Editor_MouseMoved (this, null);
			} catch (Exception e) {
				LoggingService.LogError ("Error while retrieving nav links.", e);
			}
		}

		double x, y;
		async void Editor_MouseMoved (object sender, MouseMovedEventArgs e)
		{
			if (e != null) {
				x = e.X;
				y = e.Y;
			}
			CancelRequestLinks ();
			if (!IsHoverNavigationValid (Editor))
				return;
			var token = src.Token;
			if (LinksShown) {
				var lineNumber = Editor.PointToLocation (x, y).Line;
				var line = Editor.GetLine (lineNumber);
				if (visibleLines.Any (line.Equals)) {
					return;
				}
				visibleLines.Add (line);

				IEnumerable<NavigationSegment> segments;
				try {
					segments = await RequestLinksAsync (line.Offset, line.Length, token).ConfigureAwait (false);
				} catch (OperationCanceledException) {
					return;
				} catch (Exception ex) {
					LoggingService.LogError ("Error while requestling navigation links", ex);
					return;
				}
				if (segments == null || token.IsCancellationRequested)
					return;
				await Runtime.RunInMainThread(delegate {
					try {
						foreach (var segment in segments) {
							if (token.IsCancellationRequested) {
								return;
							}
							var marker = Editor.TextMarkerFactory.CreateLinkMarker (Editor, segment.Offset, segment.Length, delegate { segment.Activate (); });
							marker.OnlyShowLinkOnHover = true;
							Editor.AddMarker (marker);
							markers.Add (marker);
						}
					} catch (Exception ex) {
						LoggingService.LogError ("Error while creating navigation line markers", ex);
					}
				});
			}
		}

		internal static bool IsHoverNavigationValid (TextEditor editor)
		{
			return !editor.IsSomethingSelected;
		}

		void CancelRequestLinks ()
		{
			src.Cancel ();
			src = new CancellationTokenSource ();
		}

		void HideLinks ()
		{
			foreach (var m in markers) {
				Editor.RemoveMarker (m);
			}
			markers.Clear ();
			visibleLines.Clear ();
		}

		void RemoveTimer ()
		{
			if (timerId != 0) {
				GLib.Source.Remove (timerId);
				timerId = 0;
			}
		}

		public override void Dispose ()
		{
			CancelRequestLinks ();
			LinksShownChanged -= AbstractNavigationExtension_LinksShownChanged;
			DocumentContext.DocumentParsed -= DocumentContext_DocumentParsed;
			this.Editor.MouseMoved -= Editor_MouseMoved;
			HideLinks ();
			RemoveTimer ();
			base.Dispose ();
		}
	}
}