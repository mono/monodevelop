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

namespace MonoDevelop.Ide.Editor.Extension
{
	public abstract class AbstractNavigationExtension : TextEditorExtension
	{
		uint snooperId;
		uint timerId;

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

		protected override void Initialize ()
		{
			snooperId = Gtk.Key.SnooperInstall (TooltipKeySnooper);
		}

		protected abstract Task<IEnumerable<NavigationSegment>> RequestLinksAsync (int offset, int length, CancellationToken token);

		int TooltipKeySnooper (Gtk.Widget widget, Gdk.EventKey evnt)
		{
			RemoveTimer ();
			if (evnt != null && evnt.Type == Gdk.EventType.KeyPress && (evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R)) {
				timerId = GLib.Timeout.Add (250, delegate {
					timerId = 0;
					ShowLinks ();
					return false;
				});
			}
			if (evnt != null && evnt.Type == Gdk.EventType.KeyRelease && (evnt.Key == Gdk.Key.Control_L || evnt.Key == Gdk.Key.Control_R)) {
				HideLinks ();
			}
			return 0; //FALSE
		}

		List<ITextSegmentMarker> markers = new List<ITextSegmentMarker> ();

		async void ShowLinks ()
		{
			HideLinks ();
			foreach (var segment in await RequestLinksAsync (0, Editor.Length, default(CancellationToken))) {
				var marker = Editor.TextMarkerFactory.CreateLinkMarker (Editor, segment.Offset, segment.Length, delegate { segment.Activate (); } );
				Editor.AddMarker (marker);
				markers.Add (marker); 
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
			HideLinks ();
			RemoveTimer ();
			if (snooperId != 0)
				Gtk.Key.SnooperRemove (snooperId);
			base.Dispose ();
		}

	}
}