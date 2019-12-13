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

#nullable enable

using System;
using System.Collections.Generic;
using System.Threading;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.Serialization;

namespace MonoDevelop.Core.Instrumentation
{
	[Serializable]
	public class TimerCounter : Counter
	{
		double minSeconds;
		TimeSpan totalTime;
		int totalCountWithTime;
		TimeSpan minTime = TimeSpan.MaxValue;
		TimeSpan maxTime;

		public TimerCounter (string name, CounterCategory category) : base (name, category)
		{
		}

		public override CounterDisplayMode DisplayMode => CounterDisplayMode.Line;

		public override string ToString ()
		{
			return string.Format ("[TimerCounter: Name={0} Id={1} Category={2} MinSeconds={3}, TotalTime={4}, AverageTime={5}, MinTime={6}, MaxTime={7}, CountWithDuration={8}]", Name, Id, Category, MinSeconds, TotalTime, AverageTime, MinTime, MaxTime, CountWithDuration);
		}

		public double MinSeconds {
			get { return this.minSeconds; }
			set { this.minSeconds = value; }
		}

		public TimeSpan TotalTime {
			get { return totalTime; }
		}

		public TimeSpan AverageTime {
			get { return totalCountWithTime > 0 ? TimeSpan.FromTicks (totalTime.Ticks / totalCountWithTime) : TimeSpan.Zero; }
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

		public ITimeTracker BeginTiming ()
		{
			return BeginTiming (null, null);
		}

		public ITimeTracker BeginTiming (string message)
		{
			return BeginTiming (message, null);
		}

		[Obsolete ("Use BeginTiming (string, IDictionary<string,object>) instead")]
		public ITimeTracker BeginTiming (IDictionary<string, string>? metadata)
		{
			var converted = metadata.ToDictionary (k => k.Key, k => (object)(k.Value));
			return BeginTiming (null, converted);
		}

		public ITimeTracker BeginTiming (string? message, IDictionary<string, object>? metadata)
		{
			return BeginTiming (message, metadata != null ? new CounterMetadata (metadata) : null, CancellationToken.None);
		}

		internal ITimeTracker<T> BeginTiming<T> (string? message, T? metadata, CancellationToken cancellationToken) where T : CounterMetadata, new()
		{
			if (!Enabled && !LogMessages) {
				return new DummyTimerCounter<T> (metadata);
			}

			var c = new TimeCounter<T> (this, metadata, cancellationToken);

			if (Enabled) {
				lock (values) {
					count++;
					totalCount++;
					int i = StoreValue (message, c, metadata?.Properties);

					var traceList = ((ITimeCounter)c).TraceList;
					if (traceList != null)
						traceList.ValueIndex = i;
				}
			} else {
				if (message != null)
					InstrumentationService.LogMessage (message);
				else
					InstrumentationService.LogMessage ("START: " + Name);
			}
			return c;
		}
	}

	[Serializable]
	public class TimerCounter<T> : TimerCounter where T : CounterMetadata, new()
	{
		public TimerCounter (string name, CounterCategory category) : base (name, category)
		{
		}

		new public ITimeTracker<T> BeginTiming ()
		{
			return base.BeginTiming<T> (null, null, CancellationToken.None);
		}

		public ITimeTracker<T> BeginTiming (CancellationToken cancellationToken)
		{
			return base.BeginTiming<T> (null, null, cancellationToken);
		}

		public ITimeTracker<T> BeginTiming (T metadata)
		{
			return base.BeginTiming<T> (null, metadata, CancellationToken.None);
		}

		public ITimeTracker<T> BeginTiming (T metadata, CancellationToken cancellationToken)
		{
			return base.BeginTiming<T> (null, metadata, cancellationToken);
		}

		new public ITimeTracker<T> BeginTiming (string message)
		{
			return base.BeginTiming<T> (message, null, CancellationToken.None);
		}

		public ITimeTracker<T> BeginTiming (string message, CancellationToken cancellationToken)
		{
			return base.BeginTiming<T> (message, null, cancellationToken);
		}

		public ITimeTracker<T> BeginTiming (string message, T metadata)
		{
			return base.BeginTiming<T> (message, metadata, CancellationToken.None);
		}

		public ITimeTracker<T> BeginTiming (string message, T metadata, CancellationToken cancellationToken)
		{
			return base.BeginTiming<T> (message, metadata, cancellationToken);
		}
	}

	public class CounterMetadata
	{
		internal protected IDictionary<string, object> Properties { get; }

		public CounterMetadata () : this(new Dictionary<string, object> ())
		{
		}

		public CounterMetadata (IDictionary<string, object> properties)
		{
			Properties = properties ?? new Dictionary<string, object> ();
		}

		public CounterMetadata (CounterMetadata original)
		{
			if (original?.Properties != null) {
				this.Properties = new Dictionary<string, object> (original.Properties);
			} else {
				this.Properties = new Dictionary<string, object> ();
			}
		}

		public void AddProperties (CounterMetadata original)
		{
			foreach (var kvp in original.Properties) {
				Properties [kvp.Key] = kvp.Value;
			}
		}

		public CounterResult Result {
			get {
				var rs = GetProperty<string> ();
				if (Enum.TryParse<CounterResult> (rs, out var result)) {
					return result;
				}

				return CounterResult.Unspecified;
			}
			set => SetProperty (value.ToString ());
		}

		public void SetUserFault () => Result = CounterResult.UserFault;
		public void SetFailure () => Result = CounterResult.Failure;
		public void SetSuccess () => Result = CounterResult.Success;
		public void SetUserCancel () => Result = CounterResult.UserCancel;

		protected void SetProperty (object value, [CallerMemberName]string? name = null)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			Properties [name] = value;
		}

		// [return: MaybeNull]
		protected T GetProperty<T> ([CallerMemberName]string? name = null)
		{
			if (name == null)
				throw new ArgumentNullException (nameof (name));

			if (Properties.TryGetValue (name, out var result)) {
				return (T)Convert.ChangeType (result, typeof (T), CultureInfo.InvariantCulture);
			}

			return default (T);
		}

		protected bool ContainsProperty ([CallerMemberName]string? propName = null)
		{
			if (propName == null)
				throw new ArgumentNullException (nameof (propName));

			return Properties.ContainsKey (propName);
		}
	}

	public enum CounterResult
	{
		Unspecified,
		Success,
		Failure,
		UserCancel,
		UserFault
	}
}

