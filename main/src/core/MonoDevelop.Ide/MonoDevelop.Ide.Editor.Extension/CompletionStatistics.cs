//
// CompletionStatistics.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using System.Collections.Generic;

namespace MonoDevelop.Ide.Editor.Extension
{
	class CompletionStatistics
	{
		TimeSpan maxTime;
		TimeSpan minTime = TimeSpan.MaxValue;
		TimeSpan totalTime;
		TimeSpan? firstTime;
		int timingsCount;
		int failureCount;
		int cancelCount;
		int successCount;

		public void OnSuccess (TimeSpan duration)
		{
			successCount++;
			AddTime (duration);
		}

		public void OnFailure (TimeSpan duration)
		{
			failureCount++;
			AddTime (duration);
		}

		public void OnUserCanceled (TimeSpan duration)
		{
			cancelCount++;
			AddTime (duration);
		}

		void AddTime (TimeSpan duration)
		{
			if (duration > maxTime) {
				maxTime = duration;
			}

			if (duration < minTime) {
				minTime = duration;
			}

			if (!firstTime.HasValue) {
				firstTime = duration;
			}

			totalTime += duration;
			timingsCount++;
		}

		public void Report ()
		{
			if (timingsCount == 0) {
				// No timings recorded.
				return;
			}

			var metadata = new Dictionary<string, string> ();

			var average = totalTime.TotalMilliseconds / timingsCount;
			metadata ["AverageDuration"] = average.ToString ();
			metadata ["FirstDuration"] = firstTime.Value.TotalMilliseconds.ToString ();
			metadata ["MaximumDuration"] = maxTime.TotalMilliseconds.ToString ();
			metadata ["MinimumDuration"] = minTime.TotalMilliseconds.ToString ();
			metadata ["FailureCount"] = failureCount.ToString ();
			metadata ["SuccessCount"] = successCount.ToString ();
			metadata ["CancelCount"] = cancelCount.ToString ();

			Counters.CodeCompletionStats.Inc (metadata);
		}
	}
}
