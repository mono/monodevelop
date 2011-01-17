// 
// MonoDroidDebuggerSession.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc. (http://www.novell.com)
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
using MonoDevelop.MonoDroid;
using System.IO;
using MonoDevelop.Core;
using System.Net.Sockets;
using System.Net;
using System.Text;
using MonoDevelop.Core.Execution;

namespace MonoDevelop.Debugger.Soft.MonoDroid
{
	public class MonoDroidDebuggerSession : RemoteSoftDebuggerSession
	{
		const int WAIT_BEFORE_CONNECT_MS = 500;
		const int WAIT_BEFORE_RETRY_MS = 800;
		const int DEBUGGER_TIMEOUT_MS = 30 * 1000;
		
		ChainedAsyncOperationSequence launchOp;
		IAsyncOperation trackProcessOp;
		AndroidDevice debugDevice;
		bool debugPropertySet;
		bool alreadyEnded;
		
		protected override void OnRun (DebuggerStartInfo startInfo)
		{
			var dsi = (MonoDroidDebuggerStartInfo) startInfo;
			var cmd = dsi.ExecutionCommand;
			debugDevice = cmd.Device;
			
			bool alreadyForwarded = MonoDroidFramework.DeviceManager.GetDeviceIsForwarded (cmd.Device.ID);
			if (!alreadyForwarded)
				MonoDroidFramework.DeviceManager.SetDeviceLastForwarded (null);
			
			long date = 0;
			int runningProcessId = 0; // Already running activity
			DateTime setPropertyTime = DateTime.MinValue;
			launchOp = new ChainedAsyncOperationSequence (
				new ChainedAsyncOperation<AndroidToolbox.GetDateOperation> () {
					Create = () => MonoDroidFramework.Toolbox.GetDeviceDate (cmd.Device),
					Completed = (op) => {
						if (op.Success) {
							date = op.Date;
							setPropertyTime = DateTime.Now;
						} else {
							this.OnDebuggerOutput (true, GettextCatalog.GetString ("Failed to get date from device"));
							this.OnDebuggerOutput (true, op.GetOutput ());
						}
					},
				},
				new ChainedAsyncOperation<AndroidToolbox.AdbOutputOperation> () {
					Create = () => {
						this.OnDebuggerOutput (false, GettextCatalog.GetString ("Setting debug property") + "\n");
						long expireDate = date + (DEBUGGER_TIMEOUT_MS / 1000);
						string monoOptions = string.Format ("debug={0}:{1}:{2},timeout={3},server=y", dsi.Address, dsi.DebugPort, 0, expireDate);
						return MonoDroidFramework.Toolbox.SetProperty (cmd.Device, "debug.mono.extra", monoOptions);
					},
					Completed = (op) => {
						if (!op.Success) {
							this.OnDebuggerOutput (true, GettextCatalog.GetString ("Failed to set debug property on device"));
							this.OnDebuggerOutput (true, op.GetOutput ());
						} else {
							debugPropertySet = true;
						}
					}
				},
				new ChainedAsyncOperation () {
					Skip = () => alreadyForwarded? "" : null,
					Create = () => {
						this.OnDebuggerOutput (false, GettextCatalog.GetString ("Forwarding debugger port") + "\n");
						return MonoDroidFramework.Toolbox.ForwardPort (cmd.Device, dsi.DebugPort, dsi.DebugPort, DebuggerOutput, DebuggerError);
					},
					Completed = (op) => {
						if (!op.Success) {
							this.OnDebuggerOutput (true, GettextCatalog.GetString ("Failed to forward port on device"));
						}
					}
				},
				new ChainedAsyncOperation () {
					Skip = () => alreadyForwarded? "" : null,
					Create = () => {
						this.OnDebuggerOutput (false, GettextCatalog.GetString ("Forwarding console port") + "\n");
						return MonoDroidFramework.Toolbox.ForwardPort (cmd.Device, dsi.OutputPort, dsi.OutputPort, DebuggerOutput, DebuggerError);
					},
					Completed = (op) => {
						if (!op.Success) {
							this.OnDebuggerOutput (true, GettextCatalog.GetString ("Failed to forward port on device"));
						} else {
							MonoDroidFramework.DeviceManager.SetDeviceLastForwarded (cmd.Device.ID);
						}
					}
				},
				new ChainedAsyncOperation<AdbGetProcessIdOperation> () {
					Create = () => new AdbGetProcessIdOperation (cmd.Device, cmd.PackageName),
					Completed = (op) => {
						if (!op.Success) {
							this.OnDebuggerOutput (true, "Error trying to detect already running process");
						} else if (op.ProcessId > 0) {
							this.OnDebuggerOutput (false, GettextCatalog.GetString ("Already running activity detected, restarting it in debug mode") + "\n");
							runningProcessId = op.ProcessId;
						}
					}
				},
				new ChainedAsyncOperation () {
					Skip = () => runningProcessId <= 0 ? "" : null,
					Create = () => new AdbShellOperation (cmd.Device, "kill " + runningProcessId),
					Completed = (op) => {
						if (!op.Success) {
							this.OnDebuggerOutput (true, GettextCatalog.GetString ("Failed to stop already running activity"));
						}
					}
				},
				new ChainedAsyncOperation () {
					Create = () => MonoDroidFramework.Toolbox.StartActivity (cmd.Device, cmd.Activity, DebuggerOutput, DebuggerError),
					Completed = (op) => {
						if (!op.Success)
							this.OnDebuggerOutput (true, GettextCatalog.GetString ("Failed to start activity"));
					}
				}
			);
			launchOp.Completed += delegate (IAsyncOperation op) {
				if (!op.Success) {
					EndSession ();
					return;
				}
				launchOp = null;
					
				Action<string> stdout = s => OnTargetOutput (false, s);
				Action<string> stderr = s => OnTargetOutput (true, s);

				trackProcessOp = new MonoDroidProcess (cmd.Device, cmd.Activity, cmd.PackageName, stdout, stderr);
				trackProcessOp.Completed += delegate {
					EndSession ();
				};
			
				System.Threading.Thread.Sleep (WAIT_BEFORE_CONNECT_MS);
				
				var msSinceSetProperty = (long) Math.Floor ((DateTime.Now - setPropertyTime).TotalMilliseconds);
				long msTillPropertyExpires = DEBUGGER_TIMEOUT_MS - msSinceSetProperty;
				
				if (msTillPropertyExpires < 100 || msTillPropertyExpires > DEBUGGER_TIMEOUT_MS)
					return;
				
				int retries = (int) Math.Floor ((double)  msTillPropertyExpires / WAIT_BEFORE_RETRY_MS) - 2;
				
				StartConnecting (dsi, retries, WAIT_BEFORE_RETRY_MS);
			};
			
			TargetExited += delegate {
				EndLaunch ();
			};
			
			launchOp.Start ();
		}
		
		void DebuggerOutput (object sender, string message)
		{
			OnDebuggerOutput (true, message);
		}
		
		void DebuggerError (object sender, string message)
		{
			OnDebuggerOutput (false, message);
		}
		
		protected override void EndSession ()
		{
			base.EndSession ();
			EndLaunch ();
		}
		
		void EndLaunch ()
		{
			if (alreadyEnded)
				return;
			alreadyEnded = true;

			if (launchOp != null && !launchOp.IsCompleted) {
				try {
					launchOp.Cancel ();
					launchOp = null;
				} catch {}
			}

			if (trackProcessOp != null && !trackProcessOp.IsCompleted) {
				// This operation should finish by itself, but make sure it's actually done.
				try {
					trackProcessOp.Cancel ();
					trackProcessOp = null;
				} catch {}
			}

			if (debugPropertySet) {
				try {
					MonoDroidFramework.Toolbox.SetProperty (debugDevice, "debug.mono.extra", String.Empty);
				} catch {}
			}
		}

		protected override void OnExit ()
		{
			base.OnExit ();
			EndLaunch ();
		}
		
		protected override bool ShouldRetryConnection (Exception exc, int attemptNumber)
		{
			if (exc is IOException)
				return true;

			return base.ShouldRetryConnection (exc, attemptNumber);
		}
	}
	
	class MonoDroidDebuggerStartInfo : RemoteDebuggerStartInfo
	{
		public MonoDroidExecutionCommand ExecutionCommand { get; private set; }
		
		public MonoDroidDebuggerStartInfo (IPAddress address, MonoDroidExecutionCommand cmd)
			: base (cmd.PackageName, address, cmd.DebugPort, cmd.OutputPort)
		{
			ExecutionCommand = cmd;
		}
	}
}
