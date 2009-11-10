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

namespace MonoDevelop.Debugger.Soft
{

	public abstract class RemoteSoftDebuggerSession : SoftDebuggerSession
	{
		ProcessInfo[] procs;
		Process simProcess;
		Gtk.Dialog dialog;
		Socket debugSock, outputSock;
		Thread listenThread;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			throw new NotImplementedException ();
		}
		
		protected void StartListening (RemoteDebuggerStartInfo dsi)
		{
			RunListenThread (dsi);
			ShowListenDialog (dsi);
		}
		
		/// <summary>Starts a thread listening for the debugger to connect over TCP/IP</summary>
		void RunListenThread (RemoteDebuggerStartInfo dsi)
		{
			debugSock = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			outputSock = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			
			VirtualMachine vm = null;
			listenThread = new Thread (delegate () {
				try {
					debugSock.Bind (new IPEndPoint (dsi.Address, dsi.DebugPort));
					outputSock.Bind (new IPEndPoint (dsi.Address, dsi.OutputPort));
					outputSock.Listen (10);
					debugSock.Listen (10);
					
					vm = VirtualMachineManager.Listen (outputSock, debugSock);
					OnConnected (vm);

					Gtk.Application.Invoke (delegate {
						if (dialog != null)
							dialog.Respond (Gtk.ResponseType.Ok);
					});
				} catch (ThreadAbortException) {
					Thread.ResetAbort ();
				} catch (SocketException sox) {
					if (sox.ErrorCode != (int)SocketError.Shutdown) {
						MonoDevelop.Core.Gui.MessageService.ShowException (sox, "Socket error: " + sox.Message);
						LoggingService.LogError ("Error binding soft debugger socket", sox);
					}
					EndSession ();
				} catch (Exception ex) {
					LoggingService.LogError ("Unexpected error in soft debugger listening thread", ex);
					EndSession ();
				}
			});
			listenThread.Start ();
		}
		
		protected abstract string GetListenMessage (RemoteDebuggerStartInfo dsi);
		
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
			CloseSockets ();
			base.EndSession ();
		}
		
		protected override ProcessInfo[] OnGetProcesses ()
		{
			if (procs == null)
				procs = new ProcessInfo[] { new ProcessInfo (0, AppName) };
			return procs;
		}
		
		protected abstract string AppName { get; }
		
		protected override void OnExit ()
		{
			base.OnExit ();
			CloseSockets ();
		}
		
		void CloseSockets ()
		{
			if (debugSock != null) {
				try {
					debugSock.Close ();
				} catch {}
			}
			if (outputSock != null) {
				try {
					outputSock.Close ();
				} catch {}
			}
			debugSock = outputSock = null;
			
			//HACK: we still have to do this because the socket.Close doesn't interrupt socket.Accept on Mono
			if (listenThread != null) {
				listenThread.Abort ();
				listenThread = null;
			}
		}
	}
	
	public class RemoteDebuggerStartInfo : DebuggerStartInfo
	{
		public IPAddress Address { get; private set; }
		public int DebugPort { get; private set; }
		public int OutputPort { get; private set; }
		
		public RemoteDebuggerStartInfo (IPAddress address, int debugPort, int outputPort)
		{
			this.Address = address;
			this.DebugPort = debugPort;
			this.OutputPort = outputPort;
		}		
	}
}
