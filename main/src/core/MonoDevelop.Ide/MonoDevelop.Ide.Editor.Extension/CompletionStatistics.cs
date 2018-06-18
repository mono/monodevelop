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
using MonoDevelop.Core.Instrumentation;

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

			var average = totalTime.TotalMilliseconds / timingsCount;

			var metadata = new CompletionStatisticsMetadata {
				AverageDuration = average,
				FirstDuration = firstTime.Value.TotalMilliseconds,
				MaximumDuration = maxTime.TotalMilliseconds,
				MinimumDuration = minTime.TotalMilliseconds,
				FailureCount = failureCount,
				SuccessCount = successCount,
				CancelCount = cancelCount
			};

			Counters.CodeCompletionStats.Inc (metadata);
		}
	}

	class CompletionStatisticsMetadata : CounterMetadata
	{
		public CompletionStatisticsMetadata ()
		{
		}

		public double AverageDuration {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double FirstDuration {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double MaximumDuration {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public double MinimumDuration {
			get => GetProperty<double> ();
			set => SetProperty (value);
		}

		public int FailureCount {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}

		public int SuccessCount {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}

		public int CancelCount {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}
	}
}
