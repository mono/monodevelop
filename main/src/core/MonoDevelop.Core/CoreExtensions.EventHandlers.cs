//
// CoreExtensions.EventHandlers.cs
//
// Author:
//       Marius Ungureanu <maungu@microsoft.com>
//
// Copyright (c) 2019 Microsoft Inc.
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
using System.Reflection;
using MonoDevelop.Core;

namespace System
{
	public static partial class CoreExtensions
	{
		/// <summary>
		/// Invokes a callback catching and reporting exceptions thrown by handlers
		/// </summary>
		/// <typeparam name="T">Type of the event arguments</typeparam>
		/// <param name="events">Event to invoke</param>
		/// <param name="sender">Sender of the event</param>
		/// <param name="args">Arguments of the event</param>
		public static void SafeInvoke<T> (this EventHandler<T> events, object sender, T args)
		{
			foreach (var ev in events.GetInvocationList ()) {
				try {
					((EventHandler<T>)ev) (sender, args);
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);
				}
			}
		}

		/// <summary>
		/// Invokes a callback catching and reporting exceptions thrown by handlers
		/// </summary>
		/// <param name="events">Event to invoke</param>
		/// <param name="sender">Sender of the event</param>
		/// <param name="args">Arguments of the event</param>
		public static void SafeInvoke (this EventHandler events, object sender, EventArgs args)
		{
			foreach (var ev in events.GetInvocationList ()) {
				try {
					((EventHandler)ev) (sender, args);
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);
				}
			}
		}

		internal static void TimeInvoke<T> (this EventHandler<T> events, object sender, T args)
		{
			var sw = new Diagnostics.Stopwatch ();
			foreach (var ev in events.GetInvocationList ()) {
				try {
					sw.Restart ();
					((EventHandler<T>)ev) (sender, args);
					sw.Stop ();

					lock (timings) {
						if (!timings.TryGetValue (sender, out var timingPair)) {
							timings [sender] = timingPair = new Dictionary<Reflection.MethodInfo, TimeSpan> ();
						}

						if (!timingPair.TryGetValue (ev.Method, out var previousTime))
							previousTime = TimeSpan.Zero;

						timingPair [ev.Method] = previousTime.Add (sw.Elapsed);
					}
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);
				}
			}
		}

		static readonly Dictionary<object, Dictionary<MethodInfo, TimeSpan>> timings = new Dictionary<object, Dictionary<MethodInfo, TimeSpan>> ();

		internal static void TimingsReport ()
		{
			foreach (var kvp in timings) {
				var source = kvp.Key;
				var values = kvp.Value;

				Console.WriteLine ("Source {0}", source);
				foreach (var timeReport in values.OrderByDescending (x => x.Value)) {
					Console.WriteLine ("{0} - {1} {2}", timeReport.Value.ToString (), timeReport.Key.DeclaringType, timeReport.Key);
				}
			}
		}

		internal static void TimeInvoke (this EventHandler events, object sender, EventArgs args)
		{
			var sw = new Diagnostics.Stopwatch ();
			foreach (var ev in events.GetInvocationList ()) {
				try {
					sw.Restart ();
					((EventHandler)ev) (sender, args);
					sw.Stop ();

					lock (timings) {
						if (!timings.TryGetValue (sender, out var timingPair)) {
							timings [sender] = timingPair = new Dictionary<MethodInfo, TimeSpan> (MethodInfoEqualityComparer.Instance);
						}

						if (!timingPair.TryGetValue (ev.Method, out var previousTime))
							previousTime = TimeSpan.Zero;

						timingPair [ev.Method] = previousTime.Add (sw.Elapsed);
					}
				} catch (Exception ex) {
					LoggingService.LogInternalError (ex);
				}
			}
		}

		sealed class MethodInfoEqualityComparer : IEqualityComparer<MethodInfo>
		{
			public static MethodInfoEqualityComparer Instance = new MethodInfoEqualityComparer ();
			public bool Equals (MethodInfo x, MethodInfo y)
				=> x.Name == y.Name
				&& x.DeclaringType == y.DeclaringType;

			public int GetHashCode (MethodInfo obj)
				=> obj.Name.GetHashCode () ^ obj.DeclaringType.GetHashCode ();
		}
	}
}
