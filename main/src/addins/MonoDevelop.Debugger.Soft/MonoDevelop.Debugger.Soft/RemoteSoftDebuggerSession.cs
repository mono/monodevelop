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
using Mono.Debugger;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using System.IO;
using MonoDevelop.Core;
using System.Net.Sockets;
using System.Net;
using System.Collections.Generic;
using System.Reflection;

namespace MonoDevelop.Debugger.Soft
{

	public abstract class RemoteSoftDebuggerSession : SoftDebuggerSession
	{
		ProcessInfo[] procs;
		Gtk.Dialog dialog;
		string appName;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			throw new NotImplementedException ();
		}
		
		/// <summary>Starts the debugger listening for a connection over TCP/IP</summary>
		protected void StartListening (RemoteDebuggerStartInfo dsi)
		{
			appName = dsi.AppName;
			RegisterUserAssemblies (dsi.UserAssemblyNames);
			
			IPEndPoint dbgEP = new IPEndPoint (dsi.Address, dsi.DebugPort);
			IPEndPoint conEP = dsi.RedirectOutput? new IPEndPoint (dsi.Address, dsi.OutputPort) : null;
			
			OnConnecting (VirtualMachineManager.BeginListen (dbgEP, conEP, HandleCallbackErrors (ListenCallback)));
			ShowListenDialog (dsi);
		}
		
		void ListenCallback (IAsyncResult ar)
		{
			OnConnected (VirtualMachineManager.EndListen (ar));
			Gtk.Application.Invoke (delegate {
				if (dialog != null)
					dialog.Respond (Gtk.ResponseType.Ok);
			});
		}
		
		protected virtual string GetListenMessage (RemoteDebuggerStartInfo dsi)
		{
			return GettextCatalog.GetString ("Waiting for debugger to connect...");
		}
		
		void ShowListenDialog (RemoteDebuggerStartInfo dsi)
		{
			string message = GetListenMessage (dsi);
			
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
				
				int response = MonoDevelop.Core.Gui.MessageService.ShowCustomDialog (dialog);
				dialog.Destroy ();
				
				if (response != (int) Gtk.ResponseType.Ok) {
					EndSession ();
				}
				dialog = null;
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
			: this (appName, address, debugPort, true, outputPort) {}

		RemoteDebuggerStartInfo (string appName, IPAddress address, int debugPort,  bool redirectOutput, int outputPort)
		{
			this.AppName = appName;
			this.Address = address;
			this.DebugPort = debugPort;
			this.OutputPort = outputPort;
			this.RedirectOutput = true;
		}
		
		public void SetUserAssemblies (IList<string> files)
		{
			UserAssemblyNames = SoftDebuggerStartInfo.GetAssemblyNames (files);
		}
	}
}
