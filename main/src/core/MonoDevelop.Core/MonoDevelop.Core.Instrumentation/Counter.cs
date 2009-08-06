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

namespace MonoDevelop.Core.Instrumentation
{
	public class Counter
	{
		int count;
		int totalCount;
		string name;
		CounterCategory category;
		List<CounterValue> values = new List<CounterValue> ();
		TimeSpan resolution = TimeSpan.FromMilliseconds (0);
		DateTime lastValueTime = DateTime.MinValue;
		
		internal Counter (string name, CounterCategory category)
		{
			this.name = name;
			this.category = category;
		}
		
		public string Name {
			get { return name; }
		}
		
		public CounterCategory Category {
			get { return category; }
		}
		
		public TimeSpan Resolution {
			get { return resolution; }
			set { resolution = value; }
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
		
		public int TotalCount {
			get { return totalCount; }
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
		
		public CounterValue GetValueAt (DateTime time)
		{
			lock (values) {
				if (values.Count == 0 || time < values[0].TimeStamp)
					return new CounterValue (0, 0, time);
				if (time >= values[values.Count - 1].TimeStamp)
					return values[values.Count - 1];
				for (int n=0; n<values.Count; n++) {
					if (values[n].TimeStamp > time)
						return values [n - 1];
				}
			}
			return new CounterValue (0, 0, time);
		}
		
		public CounterValue LastValue {
			get {
				lock (values) {
					if (values.Count > 0)
						return values [values.Count - 1];
					else
						return new CounterValue (0, 0, DateTime.MinValue);
				}
			}
		}
		
		void StoreValue ()
		{
			DateTime now = DateTime.Now;
			if (resolution.Ticks != 0) {
				if (now - lastValueTime < resolution)
					return;
			}
			values.Add (new CounterValue (count, totalCount, now));
		}
		
		public void Inc ()
		{
			Inc (1);
		}
		
		public void Inc (int n)
		{
			if (!InstrumentationService.Enabled)
				return;
			lock (values) {
				count += n;
				totalCount += n;
				StoreValue ();
			}
		}
		
		public void Dec ()
		{
			Dec (1);
		}
		
		public void Dec (int n)
		{
			if (!InstrumentationService.Enabled)
				return;
			lock (values) {
				count -= n;
				StoreValue ();
			}
		}
		
		public static Counter operator ++ (Counter c)
		{
			c.Inc ();
			return c;
		}
		
		public static Counter operator -- (Counter c)
		{
			c.Dec ();
			return c;
		}
		
		public MemoryProbe CreateMemoryProbe ()
		{
			return new MemoryProbe (this);
		}
	}
	
	public struct CounterValue
	{
		int value;
		int totalCount;
		DateTime timestamp;
		
		public CounterValue (int value, int totalCount, DateTime timestamp)
		{
			this.value = value;
			this.timestamp = timestamp;
			this.totalCount = totalCount;
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
	}
}
