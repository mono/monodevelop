// 
// TimerCounter.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Core.Instrumentation
{
	public class TimerCounter: Counter
	{
		TimeCounter lastTimer;
		double minSeconds;
		TimeSpan totalTime;
		int totalCountWithTime;
		TimeSpan minTime = TimeSpan.MaxValue;
		TimeSpan maxTime;
		
		public TimerCounter (string name, CounterCategory category): base (name, category)
		{
		}
		
		public double MinSeconds {
			get { return this.minSeconds; }
			set { this.minSeconds = value; }
		}
		
		public TimeSpan TotalTime {
			get { return totalTime; }
		}
		
		public TimeSpan AverageTime {
			get { return totalCountWithTime > 0 ? new TimeSpan (totalTime.Ticks / totalCountWithTime) : TimeSpan.FromTicks (0); }
		}
		
		public TimeSpan MinTime {
			get { return totalCountWithTime > 0 ? this.minTime : TimeSpan.Zero; }
		}

		public TimeSpan MaxTime {
			get { return this.maxTime; }
		}
		
		public int CountWithDuration {
			get { return totalCountWithTime; }
		}
		
		internal void AddTime (TimeSpan time)
		{
			lock (values) {
				totalCountWithTime++;
				totalTime += time;
				if (time < minTime)
					minTime = time;
				if (time > maxTime)
					maxTime = time;
			}
		}
		
		public override void Trace (string message)
		{
			if (InstrumentationService.Enabled) {
				if (lastTimer != null)
					lastTimer.Trace (message);
				else {
					lock (values) {
						StoreValue (message, null);
					}
				}
			}
			if (LogMessages && message != null)
				InstrumentationService.LogMessage (message);
		}
		
		public ITimeTracker BeginTiming ()
		{
			return BeginTiming (null);
		}
		
		public ITimeTracker BeginTiming (string message)
		{
			ITimeTracker timer;
			if (!InstrumentationService.Enabled) {
				timer = dummyTimer;
			} else {
				lock (values) {
					timer = lastTimer = new TimeCounter (this);
					count++;
					int i = StoreValue (message, lastTimer.TraceList);
					lastTimer.TraceList.ValueIndex = i;
				}
			}
			if (LogMessages && message != null)
				InstrumentationService.LogMessage (message);
			return timer;
		}
		
		public void EndTiming ()
		{
			if (InstrumentationService.Enabled && lastTimer != null)
				lastTimer.End ();
		}
		
		static ITimeTracker dummyTimer = new DummyTimerCounter ();
	}
}

