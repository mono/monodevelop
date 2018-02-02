// DispatchService.cs
//
// Author:
//   Todd Berman  <tberman@off.net>
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Todd Berman  <tberman@off.net>
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//

using System;
using System.Threading;
using System.Collections;
using System.Diagnostics;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace MonoDevelop.Ide
{
	public static class DispatchService
	{
		static GuiSyncContext guiContext;

		class GtkSynchronizationContext: SynchronizationContext
		{
			[UnmanagedFunctionPointer (CallingConvention.Cdecl)]
			internal delegate bool GSourceFuncInternal (IntPtr ptr);

			internal class TimeoutProxy
			{
				SendOrPostCallback d;
				object state;
				ManualResetEventSlim resetEvent;

				public TimeoutProxy (SendOrPostCallback d, object state) : this (d, state, null)
				{
				}

				public TimeoutProxy (SendOrPostCallback d, object state, ManualResetEventSlim lockObject)
				{
					this.d = d;
					this.state = state;
					this.resetEvent = lockObject;
				}

				internal static readonly GSourceFuncInternal SourceHandler = HandlerInternal;

				static bool HandlerInternal (IntPtr data)
				{
					var proxy = (TimeoutProxy)((GCHandle)data).Target;

					try {
						proxy.d (proxy.state);
					} catch (Exception e) {
						GLib.ExceptionManager.RaiseUnhandledException (e, false);
					} finally {
						proxy.resetEvent?.Set ();
					}
					return false;
				}
			}

			const int defaultPriority = 0;

			[DllImport ("libglib-2.0-0.dll", CallingConvention = CallingConvention.Cdecl)]
			static extern uint g_timeout_add_full (int priority, uint interval, GSourceFuncInternal d, IntPtr data, GLib.DestroyNotify notify);

			static void AddTimeout (TimeoutProxy proxy)
			{
				var gch = GCHandle.Alloc (proxy);

				g_timeout_add_full (defaultPriority, 0, TimeoutProxy.SourceHandler, (IntPtr)gch, GLib.DestroyHelper.NotifyHandler);
			}

			public override void Post (SendOrPostCallback d, object state)
			{
				var proxy = new TimeoutProxy (d, state);
				AddTimeout (proxy);
			}

			public override void Send (SendOrPostCallback d, object state)
			{
				if (Runtime.IsMainThread) {
					d (state);
					return;
				}

				using (var ob = new ManualResetEventSlim (false)) {
					var proxy = new TimeoutProxy (d, state, ob);

					AddTimeout (proxy);
					ob.Wait ();
				}
			}

			public override SynchronizationContext CreateCopy ()
			{
				return new GtkSynchronizationContext ();
			}
		}

		internal static void Initialize ()
		{
			if (guiContext != null)
				return;
			
			guiContext = new GuiSyncContext ();

			SynchronizationContext = new GtkSynchronizationContext ();
		}

		public static SynchronizationContext SynchronizationContext { get; private set; }
		
		static DateTime lastPendingEvents;
		internal static void RunPendingEvents ()
		{
			// The loop is limited to 1000 iterations as a workaround for an issue that some users
			// have experienced. Sometimes EventsPending starts return 'true' for all iterations,
			// causing the loop to never end.
			//
			// The loop is also limited to running at most twice a second as some of the classes
			// inheriting from BaseProgressMonitor call RunPendingEvents for every method invocation.
			// This means we pump the main loop dozens of times a second resulting in many screen
			// redraws and significantly slow down the running task.

			int maxLength = 20;
			Gdk.Threads.Enter();
			Stopwatch sw = new Stopwatch ();
			sw.Start ();

			// Check for less than zero in case there's a system time change
			var diff = DateTime.UtcNow - lastPendingEvents;
			if (diff > TimeSpan.FromMilliseconds (500) || diff < TimeSpan.Zero) {
				lastPendingEvents = DateTime.UtcNow;
				while (Gtk.Application.EventsPending () && sw.ElapsedMilliseconds < maxLength) {
					Gtk.Application.RunIteration (false);
				}
			}

			sw.Stop ();

			Gdk.Threads.Leave();
		}
		
		#region Animations

		/// <summary>
		/// Runs a delegate at regular intervals 
		/// </summary>
		/// <returns>
		/// An animation object. It can be disposed to stop the animation.
		/// </returns>
		/// <param name='animation'>
		/// The delegate to run. The return value if the number of milliseconds to wait until the delegate is run again.
		/// The execution will stop if the deletgate returns 0
		/// </param>
		public static IDisposable RunAnimation (Func<int> animation)
		{
			var ainfo = new AnimationInfo () {
				AnimationFunc = animation,
				NextDueTime = DateTime.Now
			};

			activeAnimations.Add (ainfo);
			
			// Don't immediately run the animation if we are going to do it in less than 20ms
			if (animationHandle == 0 || currentAnimationSpan > 20)
				ProcessAnimations ();
			return ainfo;
		}
		
		static List<AnimationInfo> activeAnimations = new List<AnimationInfo> ();
		static uint animationHandle;
		static int currentAnimationSpan;

		class AnimationInfo: IDisposable {
			public Func<int> AnimationFunc;
			public DateTime NextDueTime;

			public void Dispose ()
			{
				DispatchService.StopAnimation (this);
			}
		}

		static bool ProcessAnimations ()
		{
			DateTime now = DateTime.Now;

			foreach (var a in activeAnimations.Where (an => an.NextDueTime <= now).ToArray ()) {
				int ms = a.AnimationFunc ();
				if (ms <= 0) {
					activeAnimations.Remove (a);
				} else
					a.NextDueTime = DateTime.Now + TimeSpan.FromMilliseconds (ms);
			}

			if (activeAnimations.Count == 0) {
				// No more animations
				animationHandle = 0;
				return false;
			}

			var nextDueTime = activeAnimations.Min (a => a.NextDueTime);

			int nms = (int) (nextDueTime - DateTime.Now).TotalMilliseconds;
			if (nms < 20)
				nms = 20;

			// Don't re-schedule if the current time span is more or less the same as the previous one
			if (animationHandle != 0 && Math.Abs (nms - currentAnimationSpan) <= 3)
				return true;

			currentAnimationSpan = nms;
			animationHandle = GLib.Timeout.Add ((uint)currentAnimationSpan, ProcessAnimations);
			return false;
		}

		static void StopAnimation (AnimationInfo a)
		{
			activeAnimations.Remove (a);
			if (activeAnimations.Count == 0 && animationHandle != 0) {
				GLib.Source.Remove (animationHandle);
				animationHandle = 0;
			}
		}

		#endregion
	}
}
