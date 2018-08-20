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
using System.Threading;
using System.Text;

namespace MonoDevelop.Core.Instrumentation
{
	public interface ITimeTracker: IDisposable
	{
		void Trace (string message);
		void End ();
		TimeSpan Duration { get; }
	}
	
	public interface ITimeTracker<T>: IDisposable, ITimeTracker where T : CounterMetadata
	{
		T Metadata { get; }
	}

	interface ITimeCounter: ITimeTracker
	{
		void AddHandlerTracker (IDisposable t);
		TimerTraceList TraceList { get; }
	}
	
	class DummyTimerCounter<T>: ITimeTracker<T> where T : CounterMetadata
	{
		public DummyTimerCounter (T metadata)
		{
			Metadata = metadata;
		}

		public void Trace (string message)
		{
		}
		
		public void End ()
		{
		}
		
		public void Dispose ()
		{
		}

		public T Metadata { get; private set; }

		public TimeSpan Duration { get; }
	}
	
	class TimeCounter<T>: ITimeTracker<T>, ITimeCounter where T:CounterMetadata, new()
	{
		Stopwatch stopWatch = new Stopwatch ();
		TimerTraceList traceList;
		TimerTrace lastTrace;
		TimerCounter counter;
		object linkedTrackers;
		long lastTraceTime;
		T metadata;
		CancellationToken cancellationToken;

		internal TimeCounter (TimerCounter counter, T metadata, CancellationToken cancellationToken)
		{
			this.counter = counter;
			this.metadata = metadata;
			if (counter.Enabled || metadata != null) {
				// Store metadata in the traces list. The corresponding CounterValue will get whatever
				// metadata is assigned there
				traceList = new TimerTraceList ();
				traceList.Metadata = metadata?.Properties;
			}
			this.cancellationToken = cancellationToken;
			Begin ();
		}

		public T Metadata {
			get {
				if (metadata == null) {
					metadata = new T ();
					if (traceList == null)
						traceList = new TimerTraceList ();
					traceList.Metadata = metadata.Properties;
				}
				return metadata;
			}
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
		
		TimerTraceList ITimeCounter.TraceList {
			get { return this.traceList; }
		}
		
		public void Trace (string message)
		{
			if (traceList != null) {
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
			}
			if (counter?.LogMessages == true) {
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
				LoggingService.LogWarning ("Timer already finished");
				return;
			}

			stopWatch.Stop ();
			Duration = stopWatch.Elapsed;

			if (metadata != null && cancellationToken != CancellationToken.None && cancellationToken.IsCancellationRequested)
				metadata.SetUserCancel ();

			if (counter.LogMessages) {
				var time = stopWatch.ElapsedMilliseconds;
				InstrumentationService.LogMessage (string.Format ("[{0} (+{1})] END: {2}", time, (time - lastTraceTime), counter.Name));
			}

			if (traceList != null) {
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

		public TimeSpan Duration { get; private set; }
	}
	
	[Serializable]
	[DebuggerDisplay ("{DebuggingText}")]
	class TimerTraceList
	{
		public TimerTrace FirstTrace;
		public TimeSpan TotalTime;
		public int ValueIndex;

		// Timer metadata is stored here, since it may change while the timer is alive.
		// CounterValue will take the metadata from here.
		public IDictionary<string, object> Metadata;

		string DebuggingText {
			get {
				var stringBuilder = new StringBuilder ();
				var current = FirstTrace;
				TimerTrace previous = null;
				while (current != null) {
					stringBuilder.Append (previous == null ? "N/A" : (current.Timestamp - previous.Timestamp).ToString (@"ss\.fff"));
					stringBuilder.Append (" : ");
					stringBuilder.AppendLine (current.Message);
					previous = current;
					current = current.Next;
				}
				return stringBuilder.ToString ();
			}
		}
	}
	
	[Serializable]
	public class TimerTrace
	{
		internal TimerTrace Next;
		public DateTime Timestamp { get; set; }
		public string Message { get; set; }
	}
}
