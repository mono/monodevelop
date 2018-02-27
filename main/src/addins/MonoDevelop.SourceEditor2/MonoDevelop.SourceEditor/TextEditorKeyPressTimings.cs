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

namespace MonoDevelop.SourceEditor
{
	class TextEditorKeyPressTimings
	{
		Stopwatch stopwatch = new Stopwatch ();

		TimeSpan maxTime;
		TimeSpan totalTime;
		TimeSpan? firstTime;
		int count;

		public void StartTimer ()
		{
			stopwatch.Start ();
		}

		/// <summary>
		/// Overhead to a key press of using StartTimer and EndTimer is normally less
		/// than 0.001ms.
		///
		/// Note that the first ever key press in the text editor this can add up to
		/// ~0.1ms but this is small compared with the text editor key press itself
		/// which can take ~800ms for the first ever key press in the text editor for
		/// the current IDE session.
		/// </summary>
		public void EndTimer ()
		{
			stopwatch.Stop ();

			var duration = stopwatch.Elapsed;
			if (duration > maxTime) {
				maxTime = duration;
			}

			if (!firstTime.HasValue) {
				firstTime = duration;
			}

			totalTime += duration;
			count++;

			stopwatch.Reset ();
		}

		public void ReportTimings (SourceEditorView sourceEditorView)
		{
			if (count == 0) {
				// No timings recorded.
				return;
			}

			string extension = sourceEditorView.Document.FileName.Extension;

			var metadata = new Dictionary<string, string> ();
			if (!string.IsNullOrEmpty (extension))
				metadata ["Extension"] = extension;

			var average = totalTime.TotalMilliseconds / count;
			metadata ["Average"] = average.ToString ();
			metadata ["First"] = firstTime.Value.TotalMilliseconds.ToString ();
			metadata ["Maximum"] = maxTime.TotalMilliseconds.ToString ();

			Counters.Typing.Inc (metadata);
		}
	}
}
