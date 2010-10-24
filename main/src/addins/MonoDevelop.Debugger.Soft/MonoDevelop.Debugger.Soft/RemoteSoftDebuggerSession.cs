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
		Gtk.Dialog dialog;
		
		/// <summary>Subclasses must implement this to start the session </summary>
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			throw new NotImplementedException ();
		}
		
		void PressOk ()
		{
			if (dialog == null)
				return;
			Gtk.Application.Invoke (delegate {
				if (dialog != null)
					dialog.Respond (Gtk.ResponseType.Ok);
			});
		}
		
		//get rid of the dialog if there's an exception while connecting
		protected override bool HandleException (Exception ex)
		{
			PressOk ();
			return base.HandleException (ex);
		}
		
		protected override void OnConnectionStarted ()
		{
			PressOk ();
			OnConnected ();
		}
		
		[Obsolete]
		protected virtual void OnConnected ()
		{
		}
		
		protected override void OnConnectionStarting (DebuggerStartInfo dsi, bool retrying)
		{
			if (!retrying)
				ShowConnectingDialog ((RemoteDebuggerStartInfo)dsi);
		}
		
		[Obsolete]
		protected virtual string GetListenMessage (RemoteDebuggerStartInfo dsi)
		{
			return DefaultListenMessage;
		}
		
		protected virtual string GetConnectingMessage (RemoteSoftDebuggerStartInfo dsi)
		{
			//ignore the Obsolete warning
			#pragma warning disable 0612
			if (dsi is RemoteDebuggerStartInfo)
				return GetListenMessage ((RemoteDebuggerStartInfo)dsi);
			#pragma warning restore 0612
			
			return DefaultListenMessage;
		}
		
		string DefaultListenMessage {
			get { return GettextCatalog.GetString ("Waiting for debugger to connect..."); }
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
			PressOk ();
			base.EndSession ();
		}
		
		protected override void EnsureExited ()
		{
			//no-op, we can't kill remote processes
		}
	}
	
	[Obsolete]
	public class RemoteDebuggerStartInfo : RemoteSoftDebuggerStartInfo
	{
		static RemoteDebuggerStartInfo ()
		{
			SoftDebuggerEngine.EnsureSdbLoggingService ();
		}
		
		public RemoteDebuggerStartInfo (string appName, IPAddress address, int debugPort)
			: base (appName, address, debugPort) {}
		
		public RemoteDebuggerStartInfo (string appName, IPAddress address, int debugPort, int outputPort)
			: base (appName, address, debugPort, outputPort) {}
		
		public void SetUserAssemblies (IList<string> files)
		{
			string error;
			UserAssemblyNames = SoftDebuggerEngine.GetAssemblyNames (files, out error);
			LogMessage = error;
		}
	}
}
