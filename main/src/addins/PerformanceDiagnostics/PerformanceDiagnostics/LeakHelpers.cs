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
using System.Threading.Tasks;

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

		static HashSet<IntPtr> gobjectDict = new HashSet<IntPtr> ();
		internal static HashSet<IntPtr> GObjectDict {
			get {
				return gobjectDict;
			}
		}

		static Dictionary<Type, int> ComputeCurrentCounts ()
		{
			var counts = new Dictionary<Type, int>();
			foreach (var pair in NSObjectDict)
			{
				var obj = pair.Value.Target;
				if (obj == null)
					continue;

				var nsobjType = obj.GetType();
				if (!counts.TryGetValue(nsobjType, out int count))
					count = 0;
				counts[nsobjType] = ++count;
			}

			foreach (var ptr in GObjectDict)
			{
				var obj = GLib.Object.GetObject (ptr);
				if (obj == null)
					continue;

				var gobjType = obj.GetType();
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

				var delta = pair.Value - lastCount;
				if (delta == 0)
					lastCounts.Remove (pair.Key);
				else
					lastCounts [pair.Key] = delta;
			}
			return lastCounts;
		}

		static Dictionary<Type, int> summaryLastCounts;
		static TaskCompletionSource<(string summary, string delta)> tcs;
		static bool steadySummary = false;
		public static Task<(string summary, string delta)> GetSummary (bool steady)
		{
			if (tcs == null) {
				tcs = new TaskCompletionSource<(string summary, string delta)> ();
				steadySummary = steady;
				GLib.Timeout.Add (100, SummaryTimeoutHandler);
			}
			return tcs.Task;
		}

		static int remainingEqualChangedCount = 5;
		static bool SummaryTimeoutHandler ()
		{
			int gobjCount = GObjectDict.Count;
			int nsobjCount = NSObjectDict.Count;

			GC.Collect ();
			GC.Collect ();
			GC.Collect ();
			GC.WaitForPendingFinalizers ();

			bool changed = steadySummary ? gobjCount != GObjectDict.Count || nsobjCount != NSObjectDict.Count : false;
			if (!changed) {
				remainingEqualChangedCount--;
				if (remainingEqualChangedCount == 0) {
					remainingEqualChangedCount = 5;

					var counts = ComputeCurrentCounts ();
					var delta = ComputeDelta (summaryLastCounts, counts);

					summaryLastCounts = counts;
					tcs.SetResult ((GetSummaryString (counts), GetSummaryString (delta, "+")));
					tcs = null; // Make it so we can queue another dump now.
					return false;
				}
			} else {
				remainingEqualChangedCount = 5;
			}
			return true;
		}

		static Dictionary<Type, int> statisticsLastCounts;
		public static (Dictionary<Type, int> liveObjects, Dictionary<Type, int> deltaObjects) GetStatistics ()
		{
			var counts = ComputeCurrentCounts ();
			var delta = ComputeDelta (statisticsLastCounts, counts);

			statisticsLastCounts = counts;
			return (counts, delta);
		}

		internal static string GetSummaryString (Dictionary<Type, int> dict, string plusSymbol = "")
		{
			return string.Join (
				Environment.NewLine,
				dict.OrderByDescending (x => x.Value)
					.Select (x => (x.Value >= 0 ? plusSymbol : string.Empty) + x.Value + "\t\t" + x.Key));
		}
	}
}
