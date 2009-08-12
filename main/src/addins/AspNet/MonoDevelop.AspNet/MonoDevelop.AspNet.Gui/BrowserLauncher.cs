// 
// BrowserLauncher.cs
// 
// Author:
//   Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Threading;

using MonoDevelop.Core;

namespace MonoDevelop.AspNet.Gui
{
	
	
	public static class BrowserLauncher
	{
		public static BrowserLauncherOperation LaunchWhenReady (string url, BrowserLaunchHandler browserHandler)
		{
			return new BrowserLauncherOperation (url, browserHandler);
		}
		
		public static void LaunchDefaultBrowser (string url)
		{
			MonoDevelop.Core.Gui.DesktopService.ShowUrl (url);
		}
		
		public static BrowserLauncherOperation LaunchWhenReady (string url)
		{
			return new BrowserLauncherOperation (url, LaunchDefaultBrowser);
		}
	}
	
	public delegate void BrowserLaunchHandler (string url);
	
	public class BrowserLauncherOperation : IAsyncOperation 
	{
		bool completed;
		bool successful;
		readonly string url;
		readonly BrowserLaunchHandler browserHandler;
		Thread launcherThread;
		Exception error;
		
		public BrowserLauncherOperation (string url, BrowserLaunchHandler browserHandler)
		{
			if (browserHandler == null)
				throw new ArgumentNullException ("browserHandler");
			if (url == null)
				throw new ArgumentNullException ("url");
			
			this.url = url;
			this.browserHandler = browserHandler;
			if (url.StartsWith ("http://")) {
				launcherThread = new Thread (new ThreadStart (LaunchWebBrowser));
				launcherThread.Start ();
			} else {
				browserHandler (url);
				successful = true;
				IsCompleted = true;
			}
		}
		
		//confirm we can connect to server before opening browser; wait up to ten seconds
		void LaunchWebBrowser ()
		{
			try {				
				//wait a bit for server to start
				Thread.Sleep (2000);
				
				//try to contact web server several times, because server may take a while to start
				int noOfRequests = 5;
				int timeout = 8000; //ms
				int wait = 1000; //ms
				
				for (int i = 0; i < noOfRequests; i++) {
					System.Net.WebRequest req = null;
					System.Net.WebResponse resp = null;
					
					try {
						req = System.Net.HttpWebRequest.Create (url);
						req.Timeout = timeout;
						resp = req.GetResponse ();
					} catch (System.Net.WebException exp) {
						
						// server has returned 404, 500 etc, which user will still want to see
						if (exp.Status == System.Net.WebExceptionStatus.ProtocolError) {
							resp = exp.Response;
							
						//final request has failed
						} else if (i >= (noOfRequests - 1)) {
							string message = GettextCatalog.GetString ("Could not connect to webserver {0}", url);
							throw new UserException (message, exp.ToString ());
							
						//we still have requests to go, so cancel the current one and sleep for a bit
						} else {
							req.Abort ();
							Thread.Sleep (wait);
							continue;
						}
					}
				
					if (resp != null) {
						//TODO: a choice of browsers
						browserHandler (url);
						successful = true;
						break;
					}
				}
			} catch (ThreadAbortException) {
			} catch (Exception ex) {
				//don't want any exceptions leaking out the top of the thread, as they'd crash the runtime
				LoggingService.LogError ("Unhandled error in browser launcher thread", ex);
				error = ex;
			}
			IsCompleted = true;
		}
		
		public Exception Error {
			get { return error; }
		}

		public void Cancel ()
		{
			//FIXME: should we try something more graceful than a thread abort? Tricky with the 2s waits...
			if (launcherThread != null && launcherThread.IsAlive && launcherThread.ThreadState != ThreadState.AbortRequested)
				launcherThread.Abort ();
		}

		public void WaitForCompleted ()
		{
			if (launcherThread != null && launcherThread.IsAlive)
				launcherThread.Join ();
		}
		
		public event OperationHandler Completed {
			add {
				bool raiseNow = false;
				lock (this) {
					if (IsCompleted)
						raiseNow = true;
					else
						completedEvent += value;
				}
				if (raiseNow)
					value (this);
			}
			remove {
				lock (this) {
					completedEvent -= value;
				}
			}
		}
	
		public bool Success {
			get { return successful; }
		}
		
		public bool SuccessWithWarnings {
			get { return false; }
		}
		
		public bool IsCompleted {
			get { return completed; }
			private set {
				if (!value)
					throw new InvalidOperationException ();
				completed = true;
				if (completedEvent != null)
					completedEvent (this);
			}
		}
		
		event OperationHandler completedEvent;
	}
}
