﻿//
// LeakHelpers.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2017 2017
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
using System.Linq;

namespace PerformanceDiagnosticsAddIn
{
	static class LeakHelpers
	{
		// NOTE: When invoking these, the debugger will create a strong reference to the object for the entire session, so only invoke this
		// to check for leaks after a running an operation a few times (i.e. debug an app), then checking whether any NSObjects were leaked.
		// Also, place GC.Collect() a few times followed by GC.WaitForPendingFinalizers() before your breakpoint.
		#region Debug inspection helpers
		const System.Reflection.BindingFlags privateStatic = System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic;
		static Dictionary<IntPtr, WeakReference> NSObjectDict {
			get {
				var fieldInfo = typeof (ObjCRuntime.Runtime).GetField ("object_map", privateStatic);
				return (Dictionary<IntPtr, WeakReference>)fieldInfo.GetValue (null);
			}
		}

		static object [] NSObjects {
			get {
				return NSObjectDict.Values
						.Select (x => x.Target)
						.ToArray ();
			}
		}

		static T [] GetNSObjects<T> ()
		{
			return NSObjects
				.OfType<T> ()
				.ToArray ();
		}
		#endregion

		static Dictionary<Type, int> ComputeCurrentCounts ()
		{
			var counts = new Dictionary<Type, int>();
			foreach (var pair in NSObjectDict)
			{
				var value = pair.Value.Target;
				if (value == null)
					continue;

				var nsobjType = value.GetType();
				if (!counts.TryGetValue(nsobjType, out int count))
					count = 0;
				counts[nsobjType] = ++count;
			}

			foreach (var ptr in LeakCheckSafeHandle.alive)
			{
				var gobjType = GLib.Object.GetObject(ptr).GetType();
				if (!counts.TryGetValue(gobjType, out int count))
					count = 0;
				counts[gobjType] = ++count;
			}
			return counts;
		}

		static Dictionary<Type, int> ComputeDelta (Dictionary<Type, int> lastCounts, Dictionary<Type, int> counts)
		{
			if (lastCounts == null)
				return counts;

			// Re-use last counts as we don't need an extra dictionary.
			foreach (var pair in counts) {
				if (!lastCounts.TryGetValue (pair.Key, out int lastCount))
					lastCount = 0;

				lastCounts [pair.Key] = pair.Value - lastCount;
			}
			return lastCounts;
		}

		static Dictionary<Type, int> lastCounts;
		public static (string summary, string delta) GetSummary ()
		{
			var counts = ComputeCurrentCounts();
			var delta = ComputeDelta (lastCounts, counts);

			lastCounts = counts;
			return (GetSummaryString (counts), GetSummaryString(delta, "+"));
		}

		static string GetSummaryString (Dictionary<Type, int> dict, string plusSymbol = "")
		{
			return string.Join (
				Environment.NewLine,
				dict.OrderByDescending (x => x.Value)
					.Select (x => (x.Value >= 0 ? plusSymbol : string.Empty) + x.Value + "\t\t" + x.Key));
		}

		public class LeakCheckSafeHandle : GLib.SafeObjectHandle
		{
			// Maybe capture stacktrace? This would create a lot of debug spill in the log.
			public static readonly HashSet<IntPtr> alive = new HashSet<IntPtr> ();

			public LeakCheckSafeHandle (IntPtr handle) : base (handle)
			{
				alive.Add (handle);
			}

			protected override bool ReleaseHandle ()
			{
				alive.Remove (handle);
				return base.ReleaseHandle ();
			}
		}
	}
}
