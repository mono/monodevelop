// 
// MonoDroidExecutionHandler.cs
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
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;

using System.IO;
using System.Threading;
using System.Text;
using System.Diagnostics;
using MonoDevelop.Ide;

namespace MonoDevelop.MonoDroid
{
	public class MonoDroidExecutionHandler : IExecutionHandler
	{
		public MonoDroidExecutionHandler ()
		{
		}
		
		public MonoDroidExecutionHandler (AndroidDevice target)
		{
			this.DeviceTarget = target;
		}		
		
		public AndroidDevice DeviceTarget { get; private set; }

		public bool CanExecute (ExecutionCommand command)
		{
			var cmd = command as MonoDroidExecutionCommand;
			if (cmd == null)
				return false;
			return true;
		}
		
		public IProcessAsyncOperation Execute (ExecutionCommand command, IConsole console)
		{
			var cmd = (MonoDroidExecutionCommand) command;
			return MonoDroidFramework.Toolbox.StartActivity (cmd.Device, cmd.Activity,
				console.Out, console.Error);
		}
	}

	public class MonoDroidProcess : IProcessAsyncOperation, IDisposable
	{
		AndroidDevice device;
		string packageName;
		AdbGetProcessIdOperation getPidOp;
		ManualResetEvent endHandle = new ManualResetEvent (false);
		volatile int pid = UNASSIGNED_PID;

		const int UNASSIGNED_PID = -1;
		const int WAIT_TIME = 1000;

		public MonoDroidProcess (AndroidDevice device, string activity, string packageName)
		{
			this.device = device;
			this.packageName = packageName;
			StartTracking ();
		}

		void StartTracking ()
		{
			getPidOp = new AdbGetProcessIdOperation (device, packageName);
			getPidOp.Completed += RefreshPid;
		}

		void RefreshPid (IAsyncOperation op)
		{
			if (!op.Success) {
				SetCompleted (false);
				return;
			}

			AdbGetProcessIdOperation adbOp = (AdbGetProcessIdOperation)op;
			if (pid == UNASSIGNED_PID) {
				// Ignore if the activity is still starting, and thus doesn't show up in 'ps'
				if (adbOp.ProcessId > 0)
					pid = adbOp.ProcessId;
			} else {
				if (adbOp.ProcessId == 0 || pid != adbOp.ProcessId) {
					SetCompleted (false);
					return;
				}
			}
			adbOp.Dispose ();

			Thread.Sleep (WAIT_TIME);
			getPidOp = new AdbGetProcessIdOperation (device, packageName);
			getPidOp.Completed += RefreshPid;
		}

		public int ExitCode {
			get {
				// TODO - Get the exit code from logcat.
				return 0;
			}
		}

		public int ProcessId {
			get {
				return pid;
			}
		}

		public void Cancel ()
		{
			lock (lockObj) {
				if (IsCompleted)
					return;

				// Make sure our master tracking operation is finished first
				if (getPidOp != null && !getPidOp.IsCompleted) {
					try {
						getPidOp.Cancel ();
					} catch {}
				}

				// Try to kill the activity if we were able to actually get its pid
				if (pid != UNASSIGNED_PID) {
					try {
						new AdbShellOperation (device, "kill " + ProcessId);
					} catch {}
				}
			}

			SetCompleted (false);
		}

		public void WaitForCompleted ()
		{
			lock (lockObj) {
				if (IsCompleted)
					return;
			}

			endHandle.WaitOne ();
		}

		void SetCompleted (bool success)
		{
			lock (lockObj) {
				if (IsCompleted)
					throw new InvalidOperationException ("Already completed");

				endHandle.Set ();
				IsCompleted = true;
			}

			try {
				if (completedEvent != null)
					completedEvent (this);

				StopOperations ();
				Dispose ();
			} catch (Exception ex) {
				LoggingService.LogError ("Unhandled error completing MonoDroidProcess", ex);
			}
		}

		void StopOperations ()
		{
			if (getPidOp != null && !getPidOp.IsCompleted) {
				getPidOp.Cancel ();
			}
		}

		public bool IsCompleted { get; private set; }

		public bool Success { get { return IsCompleted && ExitCode <= 0; } }
		public bool SuccessWithWarnings { get { return Success; } }

		object lockObj = new object ();
		OperationHandler completedEvent;

		public event OperationHandler Completed {
			add {
				lock (lockObj) {
					completedEvent += value;
				}
			}
			remove {
				lock (lockObj) {
					completedEvent -= value;
				}
			}
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		void Dispose (bool disposing)
		{
			if (disposing) {
				if (getPidOp != null) {
					getPidOp.Dispose ();
					getPidOp = null;
				}
			}
		}

		~MonoDroidProcess ()
		{
			Dispose (false);
		}
	}
}
