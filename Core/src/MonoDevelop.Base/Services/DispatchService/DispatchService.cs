using System;
using System.Threading;
using System.Collections;

using MonoDevelop.Services;
using MonoDevelop.Core.Services;

namespace MonoDevelop.Services
{
	public class DispatchService : AbstractService
	{
		ArrayList arrBackgroundQueue;
		ArrayList arrGuiQueue;
		Thread thrBackground;
		uint iIdle = 0;
		GLib.IdleHandler handler;
		int guiThreadId;
		GuiSyncContext guiContext;
		const string errormsg = "An exception was thrown while dispatching a method call in the UI thread.";
		internal bool DispatchDebug;

		public override void InitializeService ()
		{
			guiContext = new GuiSyncContext ();
			
			guiThreadId = AppDomain.GetCurrentThreadId();
			
			handler = new GLib.IdleHandler (guiDispatcher);
			arrBackgroundQueue = new ArrayList ();
			arrGuiQueue = new ArrayList ();
			thrBackground = new Thread (new ThreadStart (backgroundDispatcher));
			thrBackground.IsBackground = true;
			thrBackground.Priority = ThreadPriority.Lowest;
			thrBackground.Start ();
			DispatchDebug = Environment.GetEnvironmentVariable ("MONODEVELOP_DISPATCH_DEBUG") != null;
		}

		public void GuiDispatch (MessageHandler cb)
		{
			QueueMessage (new GenericMessageContainer (cb, false));
		}

		public void GuiDispatch (StatefulMessageHandler cb, object state)
		{
			QueueMessage (new StatefulMessageContainer (cb, state, false));
		}

		public void GuiSyncDispatch (MessageHandler cb)
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
		
		public void GuiSyncDispatch (StatefulMessageHandler cb, object state)
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
		
		public void RunPendingEvents ()
		{
			while (Gtk.Application.EventsPending ())
				Gtk.Application.RunIteration ();
			guiDispatcher ();
		}

		void QueueMessage (object msg)
		{
			lock (arrGuiQueue) {
				arrGuiQueue.Add (msg);
				if (iIdle == 0)
					iIdle = GLib.Idle.Add (handler);
			}
		}
		
		public bool IsGuiThread
		{
			get { return guiThreadId == AppDomain.GetCurrentThreadId(); }
		}
		
		public void AssertGuiThread ()
		{
			if (guiThreadId != AppDomain.GetCurrentThreadId())
				throw new InvalidOperationException ("This method can only be called in the GUI thread");
		}
		
		public Delegate GuiDispatch (Delegate del)
		{
			return guiContext.CreateSynchronizedDelegate (del);
		}
		
		public void BackgroundDispatch (MessageHandler cb)
		{
			arrBackgroundQueue.Add (new GenericMessageContainer (cb, false));
		}

		public void BackgroundDispatch (StatefulMessageHandler cb, object state)
		{
			arrBackgroundQueue.Add (new StatefulMessageContainer (cb, state, false));
			//thrBackground.Resume ();
		}
		
		public void ThreadDispatch (StatefulMessageHandler cb, object state)
		{
			StatefulMessageContainer smc = new StatefulMessageContainer (cb, state, false);
			Thread t = new Thread (new ThreadStart (smc.Run));
			t.IsBackground = true;
			t.Start ();
		}

		private bool guiDispatcher ()
		{
			ArrayList msgList;
			
			lock (arrGuiQueue) {
				if (arrGuiQueue.Count == 0) {
					iIdle = 0;
					return false;
				}
				msgList = (ArrayList) arrGuiQueue.Clone ();
				arrGuiQueue.Clear ();
			}

			foreach (GenericMessageContainer msg in msgList) {
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
				}
			}

			return true;
		}

		private void backgroundDispatcher ()
		{
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
		
		private void HandlerError (GenericMessageContainer msg)
		{
			Runtime.LoggingService.Info (errormsg);
			Runtime.LoggingService.Info (msg.Exception);
			if (msg.CallerStack != null) {
				Runtime.LoggingService.Info ("\nCaller stack:");
				Runtime.LoggingService.Info (msg.CallerStack);
			}
			else
				Runtime.LoggingService.Info ("\n\nCaller stack not available. Define the environment variable MONODEVELOP_DISPATCH_DEBUG to enable caller stack capture.");
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
			if (Runtime.DispatchService.DispatchDebug) callerStack = Environment.StackTrace;
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
			if (Runtime.DispatchService.DispatchDebug) callerStack = Environment.StackTrace;
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
