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

using MonoDevelop.Core;

namespace MonoDevelop.Core.Gui
{
	public class DispatchService
	{
		static ArrayList arrBackgroundQueue;
		static ArrayList arrGuiQueue;
		static Thread thrBackground;
		static uint iIdle = 0;
		static GLib.TimeoutHandler handler;
		static Thread guiThread;
		static GuiSyncContext guiContext;
		static internal bool DispatchDebug;
		const string errormsg = "An exception was thrown while dispatching a method call in the UI thread.";

		static DispatchService ()
		{
			guiContext = new GuiSyncContext ();

			guiThread = Thread.CurrentThread;
			
			handler = new GLib.TimeoutHandler (guiDispatcher);
			arrBackgroundQueue = new ArrayList ();
			arrGuiQueue = new ArrayList ();
			thrBackground = new Thread (new ThreadStart (backgroundDispatcher));
			thrBackground.IsBackground = true;
			thrBackground.Priority = ThreadPriority.Lowest;
			thrBackground.Start ();
			DispatchDebug = Environment.GetEnvironmentVariable ("MONODEVELOP_DISPATCH_DEBUG") != null;
		}

		public static void GuiDispatch (MessageHandler cb)
		{
			QueueMessage (new GenericMessageContainer (cb, false));
		}

		public static void GuiDispatch (StatefulMessageHandler cb, object state)
		{
			QueueMessage (new StatefulMessageContainer (cb, state, false));
		}

		public static void GuiSyncDispatch (MessageHandler cb)
		{
			if (IsGuiThread) {
				cb ();
				return;
			}

			GenericMessageContainer mc = new GenericMessageContainer (cb, true);
			lock (mc) {
				QueueMessage (mc);
				Monitor.Wait (mc);
			}
			if (mc.Exception != null)
				throw new Exception (errormsg, mc.Exception);
		}
		
		public static void GuiSyncDispatch (StatefulMessageHandler cb, object state)
		{
			if (IsGuiThread) {
				cb (state);
				return;
			}

			StatefulMessageContainer mc = new StatefulMessageContainer (cb, state, true);
			lock (mc) {
				QueueMessage (mc);
				Monitor.Wait (mc);
			}
			if (mc.Exception != null)
				throw new Exception (errormsg, mc.Exception);
		}
		
		public static void RunPendingEvents ()
		{
			Gdk.Threads.Enter();
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();
			Gdk.Threads.Leave();
			guiDispatcher ();
		}
		
		static void QueueMessage (object msg)
		{
			lock (arrGuiQueue) {
				arrGuiQueue.Add (msg);
				if (iIdle == 0)
					iIdle = GLib.Timeout.Add (0, handler);
			}
		}
		
		public static bool IsGuiThread
		{
			get { return guiThread == Thread.CurrentThread; }
		}
		
		public static void AssertGuiThread ()
		{
			if (guiThread != Thread.CurrentThread)
				throw new InvalidOperationException ("This method can only be called in the GUI thread");
		}
		
		public static Delegate GuiDispatch (Delegate del)
		{
			return guiContext.CreateSynchronizedDelegate (del);
		}
		
		public static T GuiDispatch<T> (T theDelegate)
		{
			Delegate del = (Delegate)(object)theDelegate;
			return (T)(object)guiContext.CreateSynchronizedDelegate (del);
		}
		
		public static void BackgroundDispatch (MessageHandler cb)
		{
			arrBackgroundQueue.Add (new GenericMessageContainer (cb, false));
		}

		public static void BackgroundDispatch (StatefulMessageHandler cb, object state)
		{
			arrBackgroundQueue.Add (new StatefulMessageContainer (cb, state, false));
			//thrBackground.Resume ();
		}
		
		public static void ThreadDispatch (MessageHandler cb)
		{
			GenericMessageContainer smc = new GenericMessageContainer (cb, false);
			Thread t = new Thread (new ThreadStart (smc.Run));
			t.IsBackground = true;
			t.Start ();
		}

		public static void ThreadDispatch (StatefulMessageHandler cb, object state)
		{
			StatefulMessageContainer smc = new StatefulMessageContainer (cb, state, false);
			Thread t = new Thread (new ThreadStart (smc.Run));
			t.IsBackground = true;
			t.Start ();
		}

		static bool guiDispatcher ()
		{
			GenericMessageContainer msg;
			int iterCount;
			
			lock (arrGuiQueue) {
				iterCount = arrGuiQueue.Count;
				if (iterCount == 0) {
					iIdle = 0;
					return false;
				}
			}
			
			for (int n=0; n<iterCount; n++) {
				lock (arrGuiQueue) {
					if (arrGuiQueue.Count == 0) {
						iIdle = 0;
						return false;
					}
					msg = (GenericMessageContainer) arrGuiQueue [0];
					arrGuiQueue.RemoveAt (0);
				}
				
				msg.Run ();
				
				if (msg.IsSynchronous)
					lock (msg) Monitor.PulseAll (msg);
				else if (msg.Exception != null)
					HandlerError (msg);
			}
			
			lock (arrGuiQueue) {
				if (arrGuiQueue.Count == 0) {
					iIdle = 0;
					return false;
				} else
					return true;
			}
		}

		static void backgroundDispatcher ()
		{
			// FIXME: use an event to avoid active wait
			while (true) {
				if (arrBackgroundQueue.Count == 0) {
					Thread.Sleep (500);
					//thrBackground.Suspend ();
					continue;
				}
				GenericMessageContainer msg = null;
				lock (arrBackgroundQueue) {
					msg = (GenericMessageContainer)arrBackgroundQueue[0];
					arrBackgroundQueue.RemoveAt (0);
				}
				if (msg != null) {
					msg.Run ();
					if (msg.Exception != null)
						HandlerError (msg);
				}
			}
		}
		
		static void HandlerError (GenericMessageContainer msg)
		{
			if (msg.CallerStack != null) {
				LoggingService.LogError ("{0} {1}\nCaller stack:{2}", errormsg, msg.Exception.ToString (), msg.CallerStack);
			}
			else
				LoggingService.LogError ("{0} {1}\nCaller stack not available. Define the environment variable MONODEVELOP_DISPATCH_DEBUG to enable caller stack capture.", errormsg, msg.Exception.ToString ());
		}
	}

	public delegate void MessageHandler ();
	public delegate void StatefulMessageHandler (object state);

	class GenericMessageContainer
	{
		MessageHandler callback;
		protected Exception ex;
		protected bool isSynchronous;
		protected string callerStack;

		protected GenericMessageContainer () { }

		public GenericMessageContainer (MessageHandler cb, bool isSynchronous)
		{
			callback = cb;
			this.isSynchronous = isSynchronous;
			if (DispatchService.DispatchDebug) callerStack = Environment.StackTrace;
		}

		public virtual void Run ()
		{
			try {
				callback ();
			}
			catch (Exception e) {
				ex = e;
			}
		}
		
		public Exception Exception
		{
			get { return ex; }
		}
		
		public bool IsSynchronous
		{
			get { return isSynchronous; }
		}
		
		public string CallerStack
		{
			get { return callerStack; }
		}
	}

	class StatefulMessageContainer : GenericMessageContainer
	{
		object data;
		StatefulMessageHandler callback;

		public StatefulMessageContainer (StatefulMessageHandler cb, object state, bool isSynchronous)
		{
			data = state;
			callback = cb;
			this.isSynchronous = isSynchronous;
			if (DispatchService.DispatchDebug) callerStack = Environment.StackTrace;
		}
		
		public override void Run ()
		{
			try {
				callback (data);
			}
			catch (Exception e) {
				ex = e;
			}
		}
	}

}
