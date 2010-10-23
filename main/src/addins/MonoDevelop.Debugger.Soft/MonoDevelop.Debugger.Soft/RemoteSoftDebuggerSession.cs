// 
// RemoteSoftDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc.
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
using Mono.Debugger.Soft;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Reflection;
using MonoDevelop.Core;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Soft
{

	public abstract class RemoteSoftDebuggerSession : SoftDebuggerSession
	{
		ProcessInfo[] procs;
		Gtk.Dialog dialog;
		string appName;
		Func<bool> retryConnection;
		
		/// <summary>Subclasses must implement this to start the session </summary>
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			throw new NotImplementedException ();
		}
		
		/// <summary>Starts the debugger listening for a connection over TCP/IP</summary>
		protected void StartListening (RemoteDebuggerStartInfo dsi)
		{
			IPEndPoint dbgEP, conEP;
			PreConnectionInit (dsi, out dbgEP, out conEP);
			
			var callback = HandleConnectionCallbackErrors (ListenCallback);
			OnConnecting (VirtualMachineManager.BeginListen (dbgEP, conEP, callback));
			ShowConnectingDialog (dsi);
		}
		
		/// <summary>Starts the debugger connecting to a remote IP</summary>
		protected void StartConnecting (RemoteDebuggerStartInfo dsi, int maxAttempts, int timeBetweenAttempts)
		{
			if (timeBetweenAttempts < 0 || timeBetweenAttempts > 10000)
				throw new ArgumentException ("timeBetweenAttempts");
			
			IPEndPoint dbgEP, conEP;
			PreConnectionInit (dsi, out dbgEP, out conEP);
			
			var callback = HandleConnectionCallbackErrors (ConnectCallback);
			
			retryConnection = () => {
				if (maxAttempts == 1 || Exited) {
					return false;
				}
				if (maxAttempts > 1)
					maxAttempts--;
				try {
					if (timeBetweenAttempts > 0)
						System.Threading.Thread.Sleep (timeBetweenAttempts);
					
					OnConnecting (VirtualMachineManager.BeginConnect (dbgEP, conEP, callback));
				} catch (Exception ex2) {
					retryConnection = null;
					OnConnectionError (ex2);
					return false;
				}
				return true;
			};
			
			ShowConnectingDialog (dsi);
			
			OnConnecting (VirtualMachineManager.BeginConnect (dbgEP, conEP, callback));
		}
		
		void PreConnectionInit (RemoteDebuggerStartInfo dsi, out IPEndPoint dbgEP, out IPEndPoint conEP)
		{
			if (appName != null)
				throw new InvalidOperationException ("Cannot initialize connection more than once");
			
			appName = dsi.AppName;
			RegisterUserAssemblies (dsi.UserAssemblyNames);
			
			dbgEP = new IPEndPoint (dsi.Address, dsi.DebugPort);
			conEP = dsi.RedirectOutput? new IPEndPoint (dsi.Address, dsi.OutputPort) : null;
			
			if (!String.IsNullOrEmpty (dsi.LogMessage))
				LogWriter (false, dsi.LogMessage + "\n");
		}
		
		protected override void OnConnectionError (Exception ex)
		{
			if (retryConnection != null) {
				var sx = ex as SocketException;
				if (sx != null) {
					bool retry = sx.ErrorCode == 10061; //connection refused
					if (retry && retryConnection ())
						return;
				}
				retryConnection = null;
			}
			
			base.OnConnectionError (ex);
		}
		
		void PressOk ()
		{
			Gtk.Application.Invoke (delegate {
				if (dialog != null)
					dialog.Respond (Gtk.ResponseType.Ok);
			});
		}
		
		//get rid of the dialog if there's an exception while connecting
		protected override bool HandleException (Exception ex)
		{
			if (dialog != null) {
				Gtk.Application.Invoke (delegate {
					if (dialog != null)
						dialog.Respond (Gtk.ResponseType.Ok);
				});
			}
			return base.HandleException (ex);
		}
		
		void ListenCallback (IAsyncResult ar)
		{
			HandleConnection (VirtualMachineManager.EndListen (ar));
			PressOk ();
		}
		
		void ConnectCallback (IAsyncResult ar)
		{
			HandleConnection (VirtualMachineManager.EndConnect (ar));
			retryConnection = null;
			PressOk ();
		}
		
		//[Obsolete]
		protected virtual string GetListenMessage (RemoteDebuggerStartInfo dsi)
		{
			return GettextCatalog.GetString ("Waiting for debugger to connect...");
		}
		
		protected virtual string GetConnectingMessage (RemoteDebuggerStartInfo dsi)
		{
			//ignore the Obsolete warning
			#pragma warning disable 0612
			return GetListenMessage (dsi);
			#pragma warning restore 0612
		}
		
		void ShowConnectingDialog (RemoteDebuggerStartInfo dsi)
		{
			string message = GetConnectingMessage (dsi);
			
			Gtk.Application.Invoke (delegate {
				if (VirtualMachine != null || Exited)
					return;
				
				dialog = new Gtk.Dialog () {
					Title = "Waiting for debugger"
				};
				
				var label = new Gtk.Alignment (0.5f, 0.5f, 1f, 1f) {
					Child = new Gtk.Label (message),
					BorderWidth = 12
				};
				dialog.VBox.PackStart (label);
				label.ShowAll ();	
				
				dialog.AddButton ("Cancel", Gtk.ResponseType.Cancel);
				
				int response = MonoDevelop.Ide.MessageService.ShowCustomDialog (dialog);
				
				dialog = null;
				
				if (response != (int) Gtk.ResponseType.Ok) {
					EndSession ();
				}
			});
		}
		
		protected override void EndSession ()
		{
			if (dialog != null) {
				Gtk.Application.Invoke (delegate {
					if (dialog != null)
						dialog.Respond (Gtk.ResponseType.Cancel);
				});
			}
			base.EndSession ();
		}
		
		protected override void OnResumed ()
		{
			procs = null;
			base.OnResumed ();
		}
		
		protected override ProcessInfo[] OnGetProcesses ()
		{
			if (procs == null)
				procs = new ProcessInfo[] { new ProcessInfo (0, appName) };
			return procs;
		}
		
		protected override void EnsureExited ()
		{
			//no-op, we can't kill remote processes
		}
	}
	
	public class RemoteDebuggerStartInfo : DebuggerStartInfo
	{
		public IPAddress Address { get; private set; }
		public int DebugPort { get; private set; }
		public int OutputPort { get; private set; }
		public bool RedirectOutput { get; private set; }
		public string AppName { get; set; }
		public List<AssemblyName> UserAssemblyNames { get; set; }
		
		public RemoteDebuggerStartInfo (string appName, IPAddress address, int debugPort)
			: this (appName, address, debugPort, false, 0) {}
		
		public RemoteDebuggerStartInfo (string appName, IPAddress address, int debugPort, int outputPort)
			: this (appName, address, debugPort, outputPort > 0, outputPort) {}

		RemoteDebuggerStartInfo (string appName, IPAddress address, int debugPort,  bool redirectOutput, int outputPort)
		{
			SoftDebuggerEngine.EnsureSdbLoggingService ();
			this.AppName = appName;
			this.Address = address;
			this.DebugPort = debugPort;
			this.OutputPort = outputPort;
			this.RedirectOutput = redirectOutput;
		}
		
		internal string LogMessage { get; private set; }
		
		public void SetUserAssemblies (IList<string> files)
		{
			string error;
			UserAssemblyNames = SoftDebuggerEngine.GetAssemblyNames (files, out error);
			LogMessage = error;
		}
	}
}
