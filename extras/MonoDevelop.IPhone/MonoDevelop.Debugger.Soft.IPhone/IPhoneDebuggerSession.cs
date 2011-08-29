// 
// IPhoneDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2009 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.IPhone;
using System.IO;
using MonoDevelop.Core;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Mono.Debugging.Soft;

namespace MonoDevelop.Debugger.Soft.IPhone
{
	public class IPhoneDebuggerSession : SoftDebuggerSession
	{
		System.Diagnostics.Process simProcess;
		IPhoneUsbConnection usbConnection;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (IPhoneDebuggerStartInfo) startInfo;
			var cmd = dsi.ExecutionCommand;
			if (cmd.Simulator)
				StartSimulatorProcess (cmd);
			
			if (dsi.UsbDebugging) {
				usbConnection = dsi.UsbConnection;
				StartConnecting (dsi);
			} else {
				StartListening (dsi);
			}
		}
		
		protected override string GetConnectingMessage (DebuggerStartInfo dsi)
		{
			var iphDsi = (IPhoneDebuggerStartInfo) dsi;
			string message = GettextCatalog.GetString ("Waiting for debugger to connect...",
				iphDsi.Address, iphDsi.DebugPort);
			if (!iphDsi.ExecutionCommand.Simulator)
				message += "\n" + GettextCatalog.GetString ("Please start the application on the device.");
			return message;
		}
		
		protected override void EndSession ()
		{
			base.EndSession ();
			EndSimProcess ();
			EndUsbDebugging ();
		}

		//FIXME: hook up the app's stdin and stdout, and mtouch's stdin and stdout
		void StartSimulatorProcess (IPhoneExecutionCommand cmd)
		{
			var psi = IPhoneExecutionHandler.CreateMtouchSimStartInfo (cmd, false);
			psi.RedirectStandardInput = true;
			simProcess = System.Diagnostics.Process.Start (psi);
			
			simProcess.Exited += delegate {
				EndSession ();
				simProcess = null;
			};
			
			TargetExited += delegate {
				EndSimProcess ();
			};
			IPhoneUtility.MakeSimulatorGrabFocus ();
		}
		
		void EndSimProcess ()
		{
			if (simProcess == null)
				return;
			if (!simProcess.HasExited) {
				try {
					simProcess.StandardInput.WriteLine ();
				} catch {}
			}
			GLib.Timeout.Add (10000, delegate {
				if (!simProcess.HasExited)
					simProcess.Kill ();
				return false;
			});
		}

		void EndUsbDebugging ()
		{
			if (usbConnection != null) {
				usbConnection.Dispose ();
				usbConnection = null;
			}
		}

		protected override void OnStarted (ThreadInfo t)
		{
			base.OnStarted (t);
			if (simProcess != null)
				IPhoneUtility.MakeSimulatorGrabFocus ();
		}
		
		protected override void OnContinue ()
		{
			base.OnContinue ();
			if (simProcess != null)
				IPhoneUtility.MakeSimulatorGrabFocus ();
		}
		
		protected override void OnExit ()
		{
			base.OnExit ();
			EndSimProcess ();
			EndUsbDebugging ();
		}
	}
	
	class IPhoneDebuggerStartInfo : SoftDebuggerStartInfo
	{
		public bool UsbDebugging { get { return UsbConnection != null; } }
 		public IPhoneExecutionCommand ExecutionCommand { get; private set; }
		public int DebugPort { get; private set; }
		public IPAddress Address { get; private set; }
		public IPhoneUsbConnection UsbConnection { get; private set; }
		
		public IPhoneDebuggerStartInfo (IPAddress address, int debugPort, int outputPort, IPhoneExecutionCommand cmd)
			: base (new SoftDebuggerListenArgs (cmd.AppPath.FileNameWithoutExtension, address, debugPort, outputPort))
		{
			DebugPort = debugPort;
			Address = address;
			ExecutionCommand = cmd;
		}
		
		public IPhoneDebuggerStartInfo (IPhoneUsbConnection usb, IPhoneExecutionCommand cmd)
			: base (new SoftDebuggerConnectArgs (cmd.AppPath.FileNameWithoutExtension, usb.IPAddress, usb.DebugPort, usb.OutputPort))
		{
			UsbConnection = usb;
			DebugPort = usb.DebugPort;
			Address = usb.IPAddress;
			ExecutionCommand = cmd;
		}
	}
}
