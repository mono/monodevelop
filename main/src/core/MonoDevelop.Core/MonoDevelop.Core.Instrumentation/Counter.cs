// 
// Counter.cs
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
using System.IO;
using System.Linq;

namespace MonoDevelop.Core.Instrumentation
{
	[Serializable]
	public class Counter: MarshalByRefObject
	{
		internal int count;
		internal int totalCount;
		int lastStoredCount;
		string name;
		bool logMessages;
		CounterCategory category;
		protected List<CounterValue> values = new List<CounterValue> ();
		TimeSpan resolution = TimeSpan.FromMilliseconds (0);
		DateTime lastValueTime = DateTime.MinValue;
		CounterDisplayMode displayMode = CounterDisplayMode.Block;
		bool disposed;
		bool storeValues;
		bool enabled;
		string id;
		
		List<InstrumentationConsumer> handlers = new List<InstrumentationConsumer> ();

		public bool StoreValues {
			get {
				return storeValues;
			}
			set {
				storeValues = value;
			}
		}

		public bool Enabled {
			get { return enabled; }
		}
		
		internal List<InstrumentationConsumer> Handlers {
			get {
				InstrumentationService.InitializeHandlers ();
				return handlers; 
			}
		}
		
		internal void UpdateStatus ()
		{
			InstrumentationService.InitializeHandlers ();
			enabled = InstrumentationService.Enabled || Handlers.Count > 0;
			storeValues = InstrumentationService.Enabled;
		}
	
		internal Counter (string name, CounterCategory category)
		{
			this.name = name;
			this.category = category;
		}

		public override string ToString ()
		{
			return string.Format ("[Counter: Name={0}, Enabled={1}, Id={2}, Category={3}, Count={4}, TotalCount={5}, LastValue={6}]", Name, Enabled, Id, Category, Count, TotalCount, LastValue);
		}

		public string Name {
			get { return name; }
		}

		public string Id {
			get { return id ?? Name; }
			internal set { id = value; }
		}
		
		public CounterCategory Category {
			get { return category; }
		}
		
		public TimeSpan Resolution {
			get { return resolution; }
			set { resolution = value; }
		}

		public bool LogMessages {
			get { return this.logMessages; }
			set { this.logMessages = value; }
		}
		
		public int Count {
			get { return count; }
			set {
				lock (values) {
					if (value > count)
						totalCount += value - count;
					count = value;
				}
			}
		}
		
		public bool Disposed {
			get { return disposed; }
			internal set { disposed = value; }
		}
		
		public int TotalCount {
			get { return totalCount; }
		}
		
		public CounterDisplayMode DisplayMode {
			get { return this.displayMode; }
			set { this.displayMode = value; }
		}
		
		public IEnumerable<CounterValue> GetValues ()
		{
			lock (values) {
				return new List<CounterValue> (values);
			}
		}
		
		public IEnumerable<CounterValue> GetValuesAfter (DateTime time)
		{
			List<CounterValue> res = new List<CounterValue> ();
			lock (values) {
				for (int n=values.Count - 1; n >= 0; n--) {
					CounterValue val = values [n];
					if (val.TimeStamp > time)
						res.Add (val);
					else
						break;
				}
			}
			res.Reverse ();
			return res;
		}
		
		public IEnumerable<CounterValue> GetValuesBetween (DateTime startTime, DateTime endTime)
		{
			List<CounterValue> res = new List<CounterValue> ();
			lock (values) {
				if (values.Count == 0 || startTime > values[values.Count - 1].TimeStamp)
					return res;
				for (int n=0; n<values.Count; n++) {
					CounterValue val = values[n];
					if (val.TimeStamp > endTime)
						break;
					if (val.TimeStamp >= startTime)
						res.Add (val);
				}
			}
			return res;
		}
		
		public CounterValue GetValueAt (DateTime time)
		{
			lock (values) {
				if (values.Count == 0 || time < values[0].TimeStamp)
					return new CounterValue (0, 0, time, null);
				if (time >= values[values.Count - 1].TimeStamp)
					return values[values.Count - 1];
				for (int n=0; n<values.Count; n++) {
					if (values[n].TimeStamp > time)
						return values [n - 1];
				}
			}
			return new CounterValue (0, 0, time, null);
		}
		
		public CounterValue LastValue {
			get {
				lock (values) {
					if (values.Count > 0)
						return values [values.Count - 1];
					else
						return new CounterValue (0, 0, DateTime.MinValue, null);
				}
			}
		}
		
		internal int StoreValue (string message, ITimeCounter timer, IDictionary<string, object> metadata)
		{
			DateTime now = DateTime.Now;
			if (resolution.Ticks != 0) {
				if (now - lastValueTime < resolution)
					return -1;
			}
			var val = new CounterValue (count, totalCount, count - lastStoredCount, now, message, timer != null ? timer.TraceList : null, metadata);
			lastStoredCount = count;

			if (storeValues)
				values.Add (val);
			if (Handlers.Count > 0) {
				if (timer != null) {
					foreach (var h in handlers) {
						var t = h.BeginTimer ((TimerCounter)this, val);
						if (t != null)
							timer.AddHandlerTracker (t);
					}
				} else {
					foreach (var h in handlers)
						h.ConsumeValue (this, val);
				}
			}
			return values.Count - 1;
		}
		
		internal void RemoveValue (int index)
		{
			lock (values) {
				values.RemoveAt (index);
				for (int n=index; n<values.Count; n++) {
					CounterValue val = values [n];
					val.UpdateValueIndex (n);
				}
			}
		}
		
		public void Inc ()
		{
			Inc (1, null);
		}
		
		public void Inc (string message)
		{
			Inc (1, message);
		}
		
		public void Inc (int n)
		{
			Inc (n, null);
		}

		public void Inc (int n, string message)
		{
			Inc (n, message, (IDictionary<string, object>)null);
		}

		[Obsolete ("Use Inc (int, string, IDictionary<string, object>) instead")]
		public void Inc (string message, IDictionary<string, string> metadata)
		{
			Inc (1, message, metadata);
		}

		[Obsolete ("Use Inc (int, string, IDictionary<string, object>) instead")]
		public void Inc (IDictionary<string, string> metadata)
		{
			Inc (1, null, metadata);
		}

		[Obsolete ("Use Inc (int, string, IDictionary<string, object>) instead")]
		public void Inc (int n, string message, IDictionary<string, string> metadata)
		{
			var converted = metadata.ToDictionary (k => k.Key, k => (object)k.Value);
			Inc (n, message, converted);
		}

		public void Inc (int n, string message, IDictionary<string, object> metadata)
		{
			if (enabled) {
				lock (values) {
					count += n;
					totalCount += n;
					StoreValue (message, null, metadata);
				}
			}
			if (logMessages && message != null)
				InstrumentationService.LogMessage (message);
		}
		
		public void Dec ()
		{
			Dec (1);
		}
		
		public void Dec (string message)
		{
			Dec (1, message);
		}
		
		public void Dec (int n)
		{
			Dec (n, null);
		}

		public void Dec (int n, string message)
		{
			Dec (n, message, (IDictionary<string, object>)null); 
		}

		[Obsolete ("Use Dec (int, string, IDictionary<string, object>) instead")]
		public void Dec (int n, string message, IDictionary<string, string> metadata)
		{
			var converted = metadata.ToDictionary (k => k.Key, k => (object)(k.Value));
			Dec (n, message, converted);
		}

		public void Dec (int n, string message, IDictionary<string, object> metadata)
		{
			if (enabled) {
				lock (values) {
					count -= n;
					StoreValue (message, null, metadata);
				}
			}
			if (logMessages && message != null)
				InstrumentationService.LogMessage (message);
		}
		
		public void SetValue (int value)
		{
			SetValue (value, null);
		}

		public void SetValue (int value, string message)
		{
			SetValue (value, message, (IDictionary<string, object>)null);
		}

		[Obsolete ("Use SetValue (int, string, IDictionary<string, object>) instead")]
		public void SetValue (int value, string message, IDictionary<string, string> metadata)
		{
			var converted = metadata.ToDictionary (k => k.Key, k => (object)(k.Value));
			SetValue (value, message, converted);
		}

		public void SetValue (int value, string message, IDictionary<string, object> metadata)
		{
			if (enabled) {
				lock (values) {
					count = value;
					StoreValue (message, null, metadata);
				}
			}
			if (logMessages && message != null)
				InstrumentationService.LogMessage (message);
		}

		public static Counter operator ++ (Counter c)
		{
			c.Inc (1, null);
			return c;
		}
		
		public static Counter operator -- (Counter c)
		{
			c.Dec (1, null);
			return c;
		}
		
		public MemoryProbe CreateMemoryProbe ()
		{
			return new MemoryProbe (this);
		}
		
		public virtual void Trace (string message)
		{
			if (enabled) {
				lock (values) {
					StoreValue (message, null, (IDictionary<string, object>)null);
				}
			}
			if (logMessages && message != null)
				InstrumentationService.LogMessage (message);
		}
		
		public override object InitializeLifetimeService ()
		{
			return null;
		}

	}

	public class Counter<T>: Counter where T : CounterMetadata, new()
	{
		internal Counter (string name, CounterCategory category): base (name, category)
		{
		}

		public void Inc (string message, T metadata)
		{
			Inc (1, message, metadata.Properties);
		}

		public void Inc (T metadata)
		{
			Inc (1, null, metadata.Properties);
		}

		public void Inc (int n, string message, T metadata)
		{
			Inc (n, message, metadata.Properties);
		}

		public void Dec (string message, T metadata)
		{
			Dec (1, message, metadata.Properties);
		}

		public void Dec (T metadata)
		{
			Dec (1, null, metadata.Properties);
		}

		public void Dec (int n, string message, T metadata)
		{
			Dec (n, message, metadata.Properties);
		}

		public void SetValue (int value, T metadata)
		{
			base.SetValue (value, null, metadata.Properties);
		}

		public void SetValue (int value, string message, T metadata)
		{
			base.SetValue (value, message, metadata.Properties);
		}
	}
	
	[Serializable]
	public struct CounterValue
	{
		int value;
		int totalCount;
		int change;
		DateTime timestamp;
		string message;
		TimerTraceList traces;
		int threadId;
		IDictionary<string, object> metadata;

		internal CounterValue (int value, int totalCount, DateTime timestamp, IDictionary<string, object> metadata)
		{
			this.value = value;
			this.timestamp = timestamp;
			this.totalCount = totalCount;
			this.message = null;
			traces = null;
			threadId = 0;
			change = 0;
			this.metadata = metadata;
		}

		internal CounterValue (int value, int totalCount, int change, DateTime timestamp, string message, TimerTraceList traces, IDictionary<string, object> metadata)
		{
			this.value = value;
			this.timestamp = timestamp;
			this.totalCount = totalCount;
			this.message = message;
			this.traces = traces;
			this.change = change;
			this.threadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
			this.metadata = metadata;
		}

		public DateTime TimeStamp {
			get { return timestamp; }
		}
		
		public int Value {
			get { return this.value; }
		}
		
		public int TotalCount {
			get { return totalCount; }
		}

		public int ValueChange {
			get { return change; }
		}
		
		public int ThreadId {
			get { return this.threadId; }
		}
		
		public string Message {
			get { return message; }
		}
		
		public bool HasTimerTraces {
			get { return traces != null; }
		}

		public IDictionary<string, object> Metadata {
			get {
				// If the value is for a timer, metadata will be stored in the traces list.
				// That's because metadata may be allocated after CounterValue has been
				// created (timer metadata can be set while timing is in progress).
				return metadata ?? traces?.Metadata;
			}
		}
		
		public TimeSpan Duration {
			get {
				if (traces == null)
					return new TimeSpan (0);
				else
					return traces.TotalTime;
			}
		}
		
		public IEnumerable<TimerTrace> GetTimerTraces ()
		{
			if (traces == null)
				yield break;
			TimerTrace trace = traces.FirstTrace;
			while (trace != null) {
				yield return trace;
				trace = trace.Next;
			}
		}
		
		internal void UpdateValueIndex (int newIndex)
		{
			if (traces != null)
				traces.ValueIndex = newIndex;
		}
	}
	
	public enum CounterDisplayMode
	{
		Block,
		Line
	}
}
