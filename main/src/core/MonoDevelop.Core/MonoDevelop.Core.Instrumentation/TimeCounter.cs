// 
// TimeCounter.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;

namespace MonoDevelop.Core.Instrumentation
{
	public interface ITimeTracker: IDisposable
	{
		void Trace (string message);
		void End ();
	}
	
	class DummyTimerCounter: ITimeTracker
	{
		public void Trace (string message)
		{
		}
		
		public void End ()
		{
		}
		
		public void Dispose ()
		{
		}
	}
	
	class TimeCounter: ITimeTracker
	{
		Stopwatch stopWatch = new Stopwatch ();
		TimerTraceList traceList;
		TimerTrace lastTrace;
		TimerCounter counter;
		object linkedTrackers;
		long lastTraceTime;

		internal TimeCounter (TimerCounter counter)
		{
			this.counter = counter;
			if (counter.Enabled)
				traceList = new TimerTraceList ();
			Begin ();
		}

		public void AddHandlerTracker (IDisposable t)
		{
			if (linkedTrackers == null)
				linkedTrackers = t;
			else if (!(linkedTrackers is List<IDisposable>)) {
				var list = new List<IDisposable> ();
				list.Add ((IDisposable)linkedTrackers);
				list.Add (t);
			} else
				((List<IDisposable>)linkedTrackers).Add (t);
		}
		
		internal TimerTraceList TraceList {
			get { return this.traceList; }
		}
		
		public void Trace (string message)
		{
			if (counter.Enabled) {
				TimerTrace t = new TimerTrace ();
				t.Timestamp = DateTime.Now;
				t.Message = message;
				if (lastTrace == null)
					lastTrace = traceList.FirstTrace = t;
				else {
					lastTrace.Next = t;
					lastTrace = t;
				}
				traceList.TotalTime = t.Timestamp - traceList.FirstTrace.Timestamp;
			} else {
				var time = stopWatch.ElapsedMilliseconds;
				InstrumentationService.LogMessage (string.Format ("[{0} (+{1})] {2}", time, (time - lastTraceTime), message));
				lastTraceTime = time;
			}
		}
		
		internal void Begin ()
		{
			stopWatch.Start ();
		}
		
		public void End ()
		{
			if (!stopWatch.IsRunning) {
				Console.WriteLine ("Timer already finished");
				return;
			}

			stopWatch.Stop ();

			if (counter.LogMessages) {
				var time = stopWatch.ElapsedMilliseconds;
				InstrumentationService.LogMessage (string.Format ("[{0} (+{1})] END: {2}", time, (time - lastTraceTime), counter.Name));
			}

			if (counter.Enabled) {
				traceList.TotalTime = TimeSpan.FromMilliseconds (stopWatch.ElapsedMilliseconds);
				if (traceList.TotalTime.TotalSeconds < counter.MinSeconds)
					counter.RemoveValue (traceList.ValueIndex);
				else
					counter.AddTime (traceList.TotalTime);
			}

			counter = null;

			if (linkedTrackers is List<IDisposable>) {
				foreach (var t in (List<IDisposable>)linkedTrackers)
					t.Dispose ();
			} else if (linkedTrackers != null)
				((IDisposable)linkedTrackers).Dispose ();
			stopWatch.Reset ();
		}
		
		void IDisposable.Dispose ()
		{
			End ();
		}
	}
	
	[Serializable]
	class TimerTraceList
	{
		public TimerTrace FirstTrace;
		public TimeSpan TotalTime;
		public int ValueIndex;
	}
	
	[Serializable]
	public class TimerTrace
	{
		internal TimerTrace Next;
		public DateTime Timestamp { get; set; }
		public string Message { get; set; }
	}
}
