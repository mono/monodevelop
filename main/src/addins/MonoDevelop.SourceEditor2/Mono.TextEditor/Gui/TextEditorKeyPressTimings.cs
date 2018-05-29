//
// TextEditorKeyPressTimings.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://xamarin.com)
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
using System.Diagnostics;
using System.Collections.Generic;
using Microsoft.VisualStudio.Text.Implementation;
using System.Linq;
using MonoDevelop.SourceEditor;
using MonoDevelop.Ide;

namespace Mono.TextEditor
{
	class TextEditorKeyPressTimings
	{
		TimeSpan maxTime;
		TimeSpan totalTime;
		TimeSpan? firstTime;
		int count;

		// The length of time it takes to process a key is the time
		// from the key being pressed to the character being drawn on screen
		// As the expose event is on a fixed cycle, it's possible that multiple characters
		// can be pressed before the expose event happens again.
		// We need to keep a track of keys that have been pressed and are waiting to be drawn on screen
		//

		const int numberOfCountSpaces = 100;
		long[] activeCounts = new long[numberOfCountSpaces];
		int activeCountIndex = 0;
		int droppedEvents = 0;

		public void StartTimer (Gdk.EventKey evt)
		{
			if (activeCountIndex == numberOfCountSpaces) {
				// just drop these events now
				droppedEvents++;
				return;
			}

			droppedEvents = 0;

			// evt.Time is the time in ms since system startup (on macOS at least)
			activeCounts[activeCountIndex++] = evt.Time;
		}

		void AddTime (TimeSpan duration)
		{
			if (duration > maxTime) {
				maxTime = duration;
			}

			if (!firstTime.HasValue) {
				firstTime = duration;
			}

			totalTime += duration;
			count++;
		}

		/// <summary>
		/// Overhead to a key press of using StartTimer and EndTimer is normally about 0.0017ms
		///
		/// Note that the first ever key press in the text editor this can add up to
		/// ~0.1ms but this is small compared with the text editor key press itself
		/// which can take ~800ms for the first ever key press in the text editor for
		/// the current IDE session.
		/// </summary>
		public void EndTimer (bool complete = false)
		{
			if (activeCountIndex == 0) {
				return;
			}

			var telemetry = DesktopService.PlatformTelemetry;
			if (telemetry == null) {
				activeCountIndex = 0;
				return;
			}

			var sinceStartup = (long)telemetry.TimeSinceMachineStart.TotalMilliseconds;

			if (complete) {
				for (int i = 0; i < activeCountIndex; i++) {
					var ts = activeCounts[i];
					var durationMs = sinceStartup - ts;

					AddTime (new TimeSpan (durationMs * TimeSpan.TicksPerMillisecond));
				}

				activeCountIndex = 0;
			} else {
				// Some keypresses do not trigger a draw event, so we process them once
				// they are finished and remove them from the activeCounts list
				var ts = activeCounts[--activeCountIndex];
				var durationMs = sinceStartup - ts;

				AddTime (new TimeSpan (durationMs * TimeSpan.TicksPerMillisecond));
			}
		}

		public void ReportTimings (Mono.TextEditor.TextDocument document)
		{
			if (count == 0) {
				// No timings recorded.
				return;
			}

			string extension = document.FileName.Extension;

			var metadata = new Dictionary<string, string> ();
			if (!string.IsNullOrEmpty (extension))
				metadata ["Extension"] = extension;

			var average = totalTime.TotalMilliseconds / count;
			metadata ["Average"] = average.ToString ();
			metadata ["First"] = firstTime.Value.TotalMilliseconds.ToString ();
			metadata ["Maximum"] = maxTime.TotalMilliseconds.ToString ();

			// Do we want to track the number of dropped events?
			// If there are any dropped events, something major happened to halt the event loop
			metadata ["Dropped"] = droppedEvents.ToString ();
			MonoDevelop.SourceEditor.Counters.Typing.Inc (metadata);
		}
	}
}
