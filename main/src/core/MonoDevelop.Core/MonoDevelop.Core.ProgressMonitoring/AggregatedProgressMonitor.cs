//
// AggregatedProgressMonitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace MonoDevelop.Core.ProgressMonitoring
{
	[Flags]
	public enum MonitorAction
	{
		None = 0x00,
		WriteLog = 0x01,
		ReportError = 0x02,
		ReportWarning = 0x04,
		ReportSuccess = 0x08,
		Dispose = 0x10,
		Tasks = 0x20,
		Cancel = 0x40,
		SlaveCancel = 0x80,	// when the slave is cancelled, the whole aggregated monitor is cancelled.
		All =  0xff
	}
	
	public class AggregatedProgressMonitor: ProgressMonitor
	{
		ProgressMonitor masterMonitor;
		List<MonitorInfo> monitors = new List<MonitorInfo> ();
		CancellationTokenSource cancelSource;
		
		class MonitorInfo {
			public MonitorAction ActionMask;
			public ProgressMonitor Monitor;
			public CancellationTokenRegistration CancellationTokenRegistration;
		}
		
		public ProgressMonitor MasterMonitor {
			get { return this.masterMonitor; }
		}
		
		
		public AggregatedProgressMonitor (): this (new ProgressMonitor ())
		{
		}
		
		public AggregatedProgressMonitor (ProgressMonitor masterMonitor, params ProgressMonitor[] slaveMonitors): this (masterMonitor, null, slaveMonitors)
		{
		}

		internal AggregatedProgressMonitor (ProgressMonitor masterMonitor, CancellationTokenSource cancelSource, params ProgressMonitor[] slaveMonitors)
		{
			this.cancelSource = cancelSource ?? new CancellationTokenSource ();
			this.masterMonitor = masterMonitor;
			AddSlaveMonitor (masterMonitor, MonitorAction.All);
			foreach (ProgressMonitor mon in slaveMonitors)
				AddSlaveMonitor (mon);
		}
		
		public new void AddSlaveMonitor (ProgressMonitor slaveMonitor)
		{
			AddSlaveMonitor (slaveMonitor, MonitorAction.All);
		}
		
		public void AddSlaveMonitor (ProgressMonitor slaveMonitor, MonitorAction actionMask)
		{
			MonitorInfo smon = new MonitorInfo ();
			smon.ActionMask = actionMask;
			smon.Monitor = slaveMonitor;
			monitors.Add (smon);
			if ((actionMask & MonitorAction.SlaveCancel) != 0)
				smon.CancellationTokenRegistration = slaveMonitor.CancellationToken.Register (OnSlaveCancelRequested);
		}

		protected override void OnBeginTask (string name, int totalWork, int stepWork)
		{
			if (stepWork == -1) {
				foreach (MonitorInfo info in monitors)
					if ((info.ActionMask & MonitorAction.Tasks) != 0)
						info.Monitor.BeginTask (name, totalWork);
			} else {
				foreach (MonitorInfo info in monitors)
					if ((info.ActionMask & MonitorAction.Tasks) != 0) {
						info.Monitor.BeginStep (stepWork);
						info.Monitor.BeginTask (name, totalWork);
					}
			}
		}

		protected override void OnEndTask (string name, int totalWork, int stepWork)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0)
					info.Monitor.EndTask ();
		}

		protected override void OnStep (string message, int work)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0)
					info.Monitor.Step (message, work);
		}

		protected override ProgressMonitor CreateAsyncStepMonitor ()
		{
			return new AggregatedProgressMonitor ();
		}

		protected override void OnBeginAsyncStep (string message, int work, ProgressMonitor stepMonitor)
		{
			var am = (AggregatedProgressMonitor) stepMonitor;
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0)
					am.AddSlaveMonitor (info.Monitor.BeginAsyncStep (message, work));
		}

		protected override void OnWriteLog (string message)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.WriteLog) != 0)
					info.Monitor.Log.Write (message);
		}

		protected override void OnWriteErrorLog (string message)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.WriteLog) != 0)
					info.Monitor.ErrorLog.Write (message);
		}

		protected override void OnSuccessReported (string message)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.ReportSuccess) != 0)
					info.Monitor.ReportSuccess (message);
		}

		protected override void OnWarningReported (string message)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.ReportWarning) != 0)
					info.Monitor.ReportWarning (message);
		}

		protected override void OnErrorReported (string message, Exception exception)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.ReportError) != 0)
					info.Monitor.ReportError (message, exception);
		}

		public override void Dispose ()
		{
			foreach (MonitorInfo info in monitors) {
				if ((info.ActionMask & MonitorAction.Dispose) != 0)
					info.Monitor.Dispose ();
				if ((info.ActionMask & MonitorAction.SlaveCancel) != 0)
					info.CancellationTokenRegistration.Dispose ();
			}
		}

		void OnSlaveCancelRequested ()
		{
			cancelSource.Cancel ();
		}
	}
}
