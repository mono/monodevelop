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
			MonoDroidFramework.Toolbox.StartActivity (cmd.Device, cmd.Activity);
			return NullProcessAsyncOperation.Success;
		}
	}

	public class MonoDroidProcess : IProcessAsyncOperation, IDisposable
	{
		AndroidDevice device;
		string packageName;
		AdbGetProcessIdOperation getPidOp;
		AdbOperation trackLogOp;
		Action<string> stdout;
		Action<string> stderr;
		ManualResetEvent endHandle = new ManualResetEvent (false);
		volatile int pid = UNASSIGNED_PID;

		const int UNASSIGNED_PID = -1;
		const int WAIT_TIME = 1000;

		public MonoDroidProcess (AndroidDevice device, string activity, string packageName,
			Action<string> stdout, Action<string> stderr) : 
			this (device, activity, packageName, stdout, stderr, null)
		{
		}

		public MonoDroidProcess (AndroidDevice device, string activity, string packageName,
			Action<string> stdout, Action<string> stderr, IAsyncOperation startOp)
		{
			this.device = device;
			this.packageName = packageName;
			this.stdout = stdout;
			this.stderr = stderr;

			if (startOp == null) {
				StartTracking ();
				return;
			}

			// Our launch intent.
			startOp.Completed += delegate (IAsyncOperation op) {
				if (!op.Success)
					SetCompleted (false);
				else
					StartTracking ();
			};
		}

		void StartTracking ()
		{
			getPidOp = new AdbGetProcessIdOperation (device, packageName);
			getPidOp.Completed += RefreshPid;

			trackLogOp = new AdbTrackLogOperation (device, ProcessLogLine, "*:S", "stdout:*", "stderr:*");
			trackLogOp.Completed += delegate (IAsyncOperation op) {
				if (!op.Success) {
					SetCompleted (false);
				}
			};
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

			GLib.Timeout.Add (WAIT_TIME, delegate {
				getPidOp = new AdbGetProcessIdOperation (device, packageName);
				getPidOp.Completed += RefreshPid;
				return false;
			});
		}

		void ProcessLogLine (string line)
		{
			string result, tag;
			int pid;
			if (!ParseLine (line, out pid, out tag, out result))
				throw new FormatException ("Could not recognize logcat output: '" + line + "'");

			if (pid != this.pid)
				return;

			switch (tag) {
				case "stdout":
					stdout (result);
					break;
				case "stderr":
					stderr (result);
					break;
			}
		}

		bool ParseLine (string line, out int pid, out string tag, out string result)
		{
			// Default (brief) FORMAT: P/Tag (PID): Actual log information
			int pos = 0;
			int len, start;
			tag = result = null;
			pid = 0;

			// Ignore the priority char -and its respective '/'- for now
			pos += 2;

			// Tag
			start = pos;
			len = 0;
			while (pos < line.Length && Char.IsLetter (line [pos++]))
				len++;

			if (len == 0)
				return false;

			tag = line.Substring (start, len);

			// Optional whitespace
			while (pos < line.Length && line [pos] == ' ')
				pos++;

			// Opening brace
			pos++;

			// Optional whitespace
			while (pos < line.Length && line [pos] == ' ')
				pos++;

			// PID section
			start = pos;
			len = 0;
			while (Char.IsDigit (line [pos++]))
				len++;

			if (len == 0)
				return false;
				
			pid = Int32.Parse (line.Substring (start, len));

			// Closing brace + space char
			pos += 2;

			if (pos >= line.Length)
				return false;

			result = line.Substring (pos, line.Length - pos);
			return true;
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
					return;

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
			if (trackLogOp != null && !trackLogOp.IsCompleted) {
				trackLogOp.Cancel ();
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
				if (trackLogOp != null) {
					trackLogOp.Dispose ();
					trackLogOp = null;
				}
			}
		}

		~MonoDroidProcess ()
		{
			Dispose (false);
		}
	}
}
