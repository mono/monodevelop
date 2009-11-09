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
using Mono.Debugger;
using Mono.Debugging;
using Mono.Debugging.Client;
using System.Threading;
using System.Diagnostics;
using MonoDevelop.IPhone;
using System.IO;
using MonoDevelop.Core;
using System.Net.Sockets;
using System.Net;

namespace MonoDevelop.Debugger.Soft.IPhone
{


	public class IPhoneDebuggerSession : RemoteSoftDebuggerSession
	{
		Process simProcess;
		string appName;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (IPhoneDebuggerStartInfo) startInfo;
			appName = dsi.ExecutionCommand.AppPath.FileNameWithoutExtension;
			
			var cmd = dsi.ExecutionCommand;
			if (cmd.Simulator) {
				StartSimulatorProcess (cmd);
				StartListening (dsi);
			} else {
				var markerFile = cmd.AppPath.ParentDirectory.Combine (".monotouch_last_uploaded");
				if (File.Exists (markerFile) && File.GetLastWriteTime (markerFile) > File.GetLastWriteTime (cmd.AppPath)) {
					StartListening (dsi);
				} else {
					IPhoneUtility.Upload (cmd.Runtime, cmd.Framework, cmd.AppPath).Completed += delegate(IAsyncOperation op) {
						if (op.Success) {
							TouchUploadMarker (markerFile);
							StartListening (dsi);
						} else {
							EndSession ();
						}
					};
				}
			}
		}
		
		protected override string AppName {
			get { return appName; }
		}
		
		void TouchUploadMarker (FilePath markerFile)
		{
			if (File.Exists (markerFile))
				File.SetLastWriteTime (markerFile, DateTime.Now);
			else
				File.WriteAllText (markerFile, "This file is used to determine when the app was last uploaded to a device");
		}
		
		protected override string GetListenMessage (RemoteDebuggerStartInfo dsi)
		{
			string message = GettextCatalog.GetString ("Waiting for debugger to connect on {0}:{1}...", dsi.Address, dsi.DebugPort);
			if (((IPhoneDebuggerStartInfo)dsi).ExecutionCommand.Simulator)
				message += "\n" + GettextCatalog.GetString ("Please start the application on the device.");
			return message;
		}
		
		protected override void EndSession ()
		{
			base.EndSession ();
			EndSimProcess ();
		}

		//FIXME: hook up the app's stdin and stdout
		void StartSimulatorProcess (IPhoneExecutionCommand cmd)
		{
			string mtouchPath = cmd.Runtime.GetToolPath (cmd.Framework, "mtouch");
			if (string.IsNullOrEmpty (mtouchPath))
				throw new InvalidOperationException ("Cannot execute iPhone application. mtouch tool is missing.");
			
			var psi = new ProcessStartInfo () {
				FileName = mtouchPath,
				UseShellExecute = false,
				Arguments = string.Format ("-launchsim='{0}'", cmd.AppPath),
				RedirectStandardInput = true,
			};
			simProcess = Process.Start (psi);
			
			simProcess.Exited += delegate {
				EndSession ();
				simProcess = null;
			};
			
			TargetExited += delegate {
				EndSimProcess ();
			};
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
		
		protected override void OnExit ()
		{
			base.OnExit ();
			EndSimProcess ();
		}
	}
	
	class IPhoneDebuggerStartInfo : RemoteDebuggerStartInfo
	{
		public IPhoneExecutionCommand ExecutionCommand { get; private set; }
		
		public IPhoneDebuggerStartInfo (IPAddress address, int debugPort, int outputPort, IPhoneExecutionCommand cmd)
			: base (address, debugPort, outputPort)
		{
			ExecutionCommand = cmd;
		}
	}
}
