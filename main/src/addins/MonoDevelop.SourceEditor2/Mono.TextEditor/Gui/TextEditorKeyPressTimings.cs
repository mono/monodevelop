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
using MonoDevelop.Ide;
using System.Collections.Immutable;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Editor;
using MonoDevelop.Ide.Desktop;

namespace Mono.TextEditor
{
	class TextEditorKeyPressTimings
	{
		static readonly ImmutableArray<int> bucketUpperLimit = ImmutableArray.Create<int> (
			8, 16, 32, 64, 128, 256, 512, 1024
		);
		readonly BucketTimings bucketTimings = new BucketTimings (bucketUpperLimit);
		TimeSpan totalTimeMarginDrawing;
		TimeSpan totalTimeExtensionKeyPress;
		TimeSpan totalTimeAnimationDrawing;
		TimeSpan totalTimeCaretDrawing;

		TimeSpan openTime;

		TimeSpan maxTime;
		TimeSpan totalTime;
		TimeSpan? firstTime;
		int count;
		int lengthAtStart, lineCountAtStart;

		// The length of time it takes to process a key is the time
		// from the key being pressed to the character being drawn on screen
		// As the expose event is on a fixed cycle, it's possible that multiple characters
		// can be pressed before the expose event happens again.
		// We need to keep a track of keys that have been pressed and are waiting to be drawn on screen
		//

		const int numberOfCountSpaces = 100;
		readonly TimeSpan [] activeCounts = new TimeSpan [numberOfCountSpaces];
		int activeCountIndex = 0;
		int droppedEvents = 0;

		readonly IPlatformTelemetryDetails telemetry;

		public TimeSpan GetCurrentTime ()
		{
			if (telemetry == null) {
				return TimeSpan.Zero;
			}
			return telemetry.TimeSinceMachineStart;
		}

		public TextEditorKeyPressTimings (TextDocument document)
		{
			telemetry = DesktopService.PlatformTelemetry;

			openTime = GetCurrentTime ();

			if (document != null) {
				lengthAtStart = document.Length;
				lineCountAtStart = document.LineCount;
			}
		}

		public void AddMarginDrawingTime (TimeSpan duration)
		{
			totalTimeMarginDrawing += duration;
		}

		public void AddExtensionKeypressTime (TimeSpan duration)
		{
			totalTimeExtensionKeyPress += duration;
		}

		public void AddAnimationDrawingTime (TimeSpan duration)
		{
			totalTimeAnimationDrawing += duration;
		}

		public void AddCaretDrawingTime (TimeSpan duration)
		{
			totalTimeCaretDrawing += duration;
		}

		public void StartTimer (Gdk.EventKey eventKey)
		{
			if (telemetry == null)
				StartTimer (TimeSpan.FromMilliseconds (eventKey.Time));
			else
				StartTimer (telemetry.GetEventTime (eventKey));
		}

		public void StartTimer (TimeSpan eventTime)
		{
			if (activeCountIndex == numberOfCountSpaces) {
				// just drop these events now
				droppedEvents++;
				return;
			}

			droppedEvents = 0;

			// evt.Time is the time in ms since system startup (on macOS at least)
			activeCounts[activeCountIndex++] = eventTime;
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

			bucketTimings.Add (duration);
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

			var currentTime = GetCurrentTime ();
			if (currentTime == TimeSpan.Zero) {
				activeCountIndex = 0;
				return;
			}

			if (complete) {
				for (int i = 0; i < activeCountIndex; i++)
					AddTime (currentTime - activeCounts[i]);

				activeCountIndex = 0;
			} else {
				// Some keypresses do not trigger a draw event, so we process them once
				// they are finished and remove them from the activeCounts list
				AddTime (currentTime - activeCounts[--activeCountIndex]);
			}
		}

		internal TypingTimingMetadata GetTypingTimingMetadata (string extension, ITextEditorOptions options, int lengthAtEnd, int lineCountAtEnd)
		{
			double totalMillis = totalTime.TotalMilliseconds;

			var average = totalMillis / count;
			var metadata = new TypingTimingMetadata {
				Average = average,
				First = firstTime.Value.TotalMilliseconds,
				Maximum = maxTime.TotalMilliseconds,
				Dropped = droppedEvents,
				PercentAnimation = totalTimeAnimationDrawing.TotalMilliseconds / totalMillis * 100,
				PercentDrawCaret = totalTimeCaretDrawing.TotalMilliseconds / totalMillis * 100,
				PercentDrawMargin = totalTimeMarginDrawing.TotalMilliseconds / totalMillis * 100,
				PercentExtensionKeypress = totalTimeExtensionKeyPress.TotalMilliseconds / totalMillis * 100,
				SessionKeypressCount = count,
				SessionLength = GetCurrentTime ().TotalMilliseconds - openTime.TotalMilliseconds,
				LengthAtStart = lengthAtStart,
				LengthDelta = lengthAtEnd - lengthAtStart,
				LineCountAtStart = lineCountAtStart,
				LineCountDelta = lineCountAtEnd - lineCountAtStart,
			};

			if (options != null) {
				metadata.FoldMarginShown = options.ShowFoldMargin;
				metadata.NumberMarginShown = options.ShowLineNumberMargin;
				metadata.ShowIconMargin = options.ShowIconMargin;
				metadata.ShowWhiteSpaces = options.ShowWhitespaces;
				metadata.IncludeWhitespaces = options.IncludeWhitespaces;
			}

			if (!string.IsNullOrEmpty (extension))
				metadata.Extension = extension;

			bucketTimings.AddTo (metadata);

			return metadata;
		}

		public void ReportTimings (Mono.TextEditor.TextDocument document, ITextEditorOptions options)
		{
			if (count == 0) {
				// No timings recorded.
				return;
			}

			string extension = document.FileName.Extension;

			var metadata = GetTypingTimingMetadata (extension, options, document.Length, document.LineCount);
			MonoDevelop.SourceEditor.Counters.Typing.Inc (metadata);
		}
	}

	class TypingTimingMetadata : CounterMetadata
	{
		public TypingTimingMetadata ()
		{
		}

		public string Extension {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}

		public double Average {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double First {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double Maximum {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public int Dropped {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}

		public double PercentDrawMargin {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double PercentExtensionKeypress {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double PercentDrawCaret {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double PercentAnimation {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public bool FoldMarginShown {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}

		public bool NumberMarginShown {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}

		public bool ShowIconMargin {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}

		public ShowWhitespaces ShowWhiteSpaces {
			get => GetProperty<ShowWhitespaces> ();
			set => SetProperty (value);
		}

		public IncludeWhitespaces IncludeWhitespaces {
			get => GetProperty<IncludeWhitespaces> ();
			set => SetProperty (value);
		}

		public double SessionKeypressCount {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double SessionLength {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public int LengthAtStart {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}

		public int LengthDelta {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}

		public int LineCountAtStart {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}

		public int LineCountDelta {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}
	}

}
