//
// CoreExtensions.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MonoDevelop.Core;

namespace System
{
	public static class CoreExtensions
	{
		public static IEnumerable<T> Concat<T> (this IEnumerable<T> e, T item)
		{
			return e.Concat (Enumerable.Repeat (item, 1));
		}

		public static int FindIndex<T> (this IEnumerable<T> e, Func<T, bool> predicate)
		{
			bool found = false;
			int index = e.TakeWhile (i => {
				found = predicate(i);
				return !found;
			}).Count ();

			return found ? index : -1;
		}

		public static int IndexOf<T> (this IEnumerable<T> e, T item)
		{
			bool found = false;
			int index = e.TakeWhile (i => {
				found = EqualityComparer<T>.Default.Equals (i, item);
				return !found;
			}).Count ();

			return found ? index : -1;
		}

		static TSource MaxValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer, out bool hasValue)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));

			TSource result = default (TSource);
			TCompare value = default (TCompare);
			hasValue = false;
			foreach (TSource item in source) {
				var x = compareSelector (item);
				if (hasValue) {
					if (comparer.Compare (x, value) > 0) {
						value = x;
						result = item;
					}
				} else {
					value = x;
					result = item;
					hasValue = true;
				}
			}
			return result;
		}

		public static TSource MaxValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector) where TCompare : IComparable<TCompare>
		{
			bool hasValue;
			TSource result = MaxValue (source, compareSelector, Comparer<TCompare>.Default, out hasValue);
			if (hasValue)
				return result;
			throw new InvalidOperationException (string.Format ("{0} contains no elements", nameof (source)));
		}

		public static TSource MaxValueOrDefault<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector) where TCompare : IComparable<TCompare>
		{
			bool hasValue;
			return MaxValue (source, compareSelector, Comparer<TCompare>.Default, out hasValue);
		}

		public static TSource MaxValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer)
		{
			bool hasValue;
			TSource result = MaxValue (source, compareSelector, comparer, out hasValue);
			if (hasValue)
				return result;
			throw new InvalidOperationException (string.Format ("{0} contains no elements", nameof (source)));
		}

		public static TSource MaxValueOrDefault<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer)
		{
			bool hasValue;
			return MaxValue (source, compareSelector, comparer, out hasValue);
		}

		static TSource MinValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer, out bool hasValue)
		{
			if (source == null)
				throw new ArgumentNullException (nameof (source));

			TSource result = default (TSource);
			TCompare value = default (TCompare);
			hasValue = false;
			foreach (TSource item in source) {
				var x = compareSelector (item);
				if (hasValue) {
					if (comparer.Compare (x, value) < 0) {
						value = x;
						result = item;
					}
				} else {
					value = x;
					result = item;
					hasValue = true;
				}
			}
			return result;
		}

		public static TSource MinValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector) where TCompare : IComparable<TCompare>
		{
			bool hasValue;
			TSource result = MinValue (source, compareSelector, Comparer<TCompare>.Default, out hasValue);
			if (hasValue)
				return result;
			throw new InvalidOperationException (string.Format ("{0} contains no elements", nameof (source)));
		}

		public static TSource MinValueOrDefault<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector) where TCompare : IComparable<TCompare>
		{
			bool hasValue;
			return MinValue (source, compareSelector, Comparer<TCompare>.Default, out hasValue);
		}

		public static TSource MinValue<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer)
		{
			bool hasValue;
			TSource result = MinValue (source, compareSelector, comparer, out hasValue);

			if (hasValue)
				return result;
			throw new InvalidOperationException (string.Format ("{0} contains no elements", nameof (source)));
		}

		public static TSource MinValueOrDefault<TSource, TCompare> (this IEnumerable<TSource> source, Func<TSource, TCompare> compareSelector, IComparer<TCompare> comparer)
		{
			bool hasValue;
			return MinValue (source, compareSelector, comparer, out hasValue);
		}

		public static Exception FlattenAggregate (this Exception ex)
		{
			return (ex as AggregateException)?.Flatten () ?? ex;
		}

		public static Func<R> Memoize<R> (this Func<R> f)
		{
			R value = default (R);
			bool hasValue = false;

			return () => {
				if (!hasValue) {
					hasValue = true;
					value = f ();
				}
				return value;
			};
		}

		public static Func<T1, R> Memoize<T1, R> (this Func<T1, R> f)
		{
			var map = new Dictionary<T1, R> ();
			return a => {
				if (map.TryGetValue (a, out R value))
					return value;
				value = f (a);
				map.Add (a, value);
				return value;
			};
		}

		public static Func<T1, R> MemoizeWithLock<T1, R> (this Func<T1, R> f)
		{
			var map = new Dictionary<T1, R> ();
			return a => {
				lock (map) {
					if (map.TryGetValue (a, out R value))
						return value;
					value = f (a);
					map.Add (a, value);
					return value;
				}
			};
		}

		public static Func<T1, T2, R> Memoize<T1, T2, R> (this Func<T1, T2, R> f)
		{
			var map = new Dictionary<ValueTuple<T1, T2>, R> ();
			return (a, b) => {
				var key = ValueTuple.Create (a, b);
				if (map.TryGetValue (key, out R value))
					return value;
				value = f (a, b);
				map.Add (key, value);
				return value;
			};
		}

		public static Func<T1, T2, R> MemoizeWithLock<T1, T2, R> (this Func<T1, T2, R> f)
		{
			var map = new Dictionary<ValueTuple<T1, T2>, R> ();
			return (a, b) => {
				var key = ValueTuple.Create (a, b);
				lock (map) {
					if (map.TryGetValue (key, out R value))
						return value;
					value = f (a, b);
					map.Add (key, value);
					return value;
				}
			};
		}

		static class MemoizeUtil<Value>
		{
			public static Dictionary<Key, Value> Create<Key> (Key justForFancyTypeInference)
			{
				return new Dictionary<Key, Value> ();
			}
		}

		/// <summary>
		/// Use this method to explicitly indicate that you don't care
		/// about the result of an async call
		/// </summary>
		/// <param name="task">The task to forget</param>
		public static void Ignore (this Task task)
		{
			task.ContinueWith (t => {
				LoggingService.LogError ("Async operation failed", t.Exception);
			}, TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously);
		}

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

