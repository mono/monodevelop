
using System;
using System.IO;
using System.Diagnostics;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace Stetic
{
	internal class ApplicationBackendController: MarshalByRefObject
	{
		bool stopping;
		ApplicationBackend backend;
		string channelId;
		IsolatedApplication app;
		
		ManualResetEvent runningEvent = new ManualResetEvent (false);
		
		public event EventHandler Stopped;
		
		public ApplicationBackendController (IsolatedApplication app, string channelId)
		{
			this.app = app;
			this.channelId = channelId;
		}
		
		public ApplicationBackend Backend {
			get { return backend; }
		}
		
		public Application Application {
			get { return app; }
		}
		
		public void StartBackend ()
		{
			runningEvent.Reset ();
			
			string asm = GetType().Assembly.Location;
			
			BinaryFormatter bf = new BinaryFormatter ();
			ObjRef oref = RemotingServices.Marshal (this);
			MemoryStream ms = new MemoryStream ();
			bf.Serialize (ms, oref);
			string sref = Convert.ToBase64String (ms.ToArray ());
		
			Process process = new Process ();
			process.StartInfo = new ProcessStartInfo ("sh", "-c \"mono --debug " + asm + "\"");
			process.StartInfo.WorkingDirectory = AppDomain.CurrentDomain.BaseDirectory;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardInput = true;
			process.EnableRaisingEvents = true;
			process.Start ();
			process.StandardInput.WriteLine (channelId);
			process.StandardInput.WriteLine (sref);
			process.StandardInput.Flush ();
			process.Exited += OnExited;
			
			if (!runningEvent.WaitOne (10000, false))
				throw new ApplicationException ("Couldn't create a remote process.");
		}
		
		public void StopBackend (bool waitUntilDone)
		{
			stopping = true;
			runningEvent.Reset ();
			backend.Dispose ();
			if (waitUntilDone)
				runningEvent.WaitOne (9000, false);
		}
		
		void OnExited (object o, EventArgs args)
		{
			Gtk.Application.Invoke (OnExitedInGui);
		}
		
		void OnExitedInGui (object o, EventArgs args)
		{
			if (!stopping && Stopped != null)
				Stopped (this, EventArgs.Empty);
			System.Runtime.Remoting.RemotingServices.Disconnect (this);
		}
		
		internal void Connect (ApplicationBackend backend)
		{
			this.backend = backend;
			runningEvent.Set ();
		}
		
		internal void Disconnect (ApplicationBackend backend)
		{
			this.backend = null;
			runningEvent.Set ();
		}

		public override object InitializeLifetimeService ()
		{
			// Will be disconnected when calling Dispose
			return null;
		}
	}
}
