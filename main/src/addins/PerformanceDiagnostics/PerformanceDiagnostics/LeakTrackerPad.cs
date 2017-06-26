//
// LeakTrackerPad.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 2017
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
using System.Linq;
using MonoDevelop.Components;
using MonoDevelop.Ide.Gui;

namespace PerformanceDiagnosticsAddIn
{
	public class LeakTrackerPad : PadContent
	{
		Control control;
		uint timeoutId = 0;
		public LeakTrackerPad ()
		{
			timeoutId = GLib.Timeout.Add (2000, HandleTimeoutHandler);
		}

		protected override void Initialize (IPadWindow window)
		{
			base.Initialize (window);
			window.PadContentShown += OnPadContentShown;
		}

		void OnPadContentShown (object sender, EventArgs args)
		{
			// Force collection of objects.
			LeakHelpers.GetSummary (steady: false);
			HandleTimeoutHandler ();
		}

		bool HandleTimeoutHandler ()
		{
			try {
				var (live, delta) = LeakHelpers.GetStatistics ();

				var liveCount = live.Sum (x => x.Value);
				var deltaCount = delta.Sum (x => x.Value);

				string title = string.Format ("Live {0} ({1}{2})", liveCount, deltaCount >= 0 ? "+" : "", deltaCount);
				Window.Title = title;
				Window.HasErrors |= deltaCount > 0;
			} catch {
				// maybe the collection was modified, don't bother with updating
			}
			return true;
		}

		public override void Dispose ()
		{
			if (Window != null)
				Window.PadContentShown -= OnPadContentShown;

			if (timeoutId != 0) {
				GLib.Source.Remove (timeoutId);
				timeoutId = 0;
			}

			base.Dispose ();
		}

		public override Control Control {
			get {
				if (control == null)
					control = CreateControl ();
				return control;
			}
		}

		public override string Id {
			get {
				return "PerformanceDiagnosticsAddIn.LeakTrackerPad";
			}
		}

		Control CreateControl ()
		{
			return new Gtk.HBox ();
		}
	}
}
