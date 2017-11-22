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
		FollowerCancel = 0x80,	// when the follower is cancelled, the whole aggregated monitor is cancelled.
		ReportObject = 0x100,
		All =  0xfff
	}
	
	public class AggregatedProgressMonitor: ProgressMonitor
	{
		ProgressMonitor leaderMonitor;
		List<MonitorInfo> monitors = new List<MonitorInfo> ();

		class MonitorInfo {
			public MonitorAction ActionMask;
			public ProgressMonitor Monitor;
			public CancellationTokenRegistration CancellationTokenRegistration;
		}
		
		public ProgressMonitor LeaderMonitor {
			get { return this.leaderMonitor; }
		}
		
		
		public AggregatedProgressMonitor (): this (new ProgressMonitor ())
		{
		}
		
		public AggregatedProgressMonitor (ProgressMonitor leaderMonitor, params ProgressMonitor[] followerMonitors): this (leaderMonitor, null, followerMonitors)
		{
		}

		internal AggregatedProgressMonitor (ProgressMonitor leaderMonitor, CancellationTokenSource cancelSource, params ProgressMonitor[] followerMonitors)
		{
			CancellationTokenSource = cancelSource ?? new CancellationTokenSource ();
			this.leaderMonitor = leaderMonitor;
			AddFollowerMonitor (leaderMonitor, MonitorAction.All);
			foreach (ProgressMonitor mon in followerMonitors)
				AddFollowerMonitor (mon);
		}
		
		public new void AddFollowerMonitor (ProgressMonitor followerMonitor)
		{
			AddFollowerMonitor (followerMonitor, MonitorAction.All);
		}
		
		public void AddFollowerMonitor (ProgressMonitor followerMonitor, MonitorAction actionMask)
		{
			MonitorInfo smon = new MonitorInfo ();
			smon.ActionMask = actionMask;
			smon.Monitor = followerMonitor;
			monitors.Add (smon);
			if ((actionMask & MonitorAction.FollowerCancel) != 0)
				smon.CancellationTokenRegistration = followerMonitor.CancellationToken.Register (OnFollowerCancelRequested);
		}

		protected override void OnBeginTask (string name, int totalWork, int stepWork)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0)
					info.Monitor.BeginTask (name, totalWork);
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

		protected override void OnBeginStep (string message, int work)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0)
					info.Monitor.BeginStep (message, work);
		}

		protected override ProgressMonitor CreateAsyncStepMonitor ()
		{
			return new AggregatedProgressMonitor ();
		}

		protected override void OnBeginAsyncStep (string message, int work, ProgressMonitor stepMonitor)
		{
			var am = (AggregatedProgressMonitor) stepMonitor;
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0) {
					var sm = info.Monitor.BeginAsyncStep (message, work);
					sm.ReportGlobalDataToParent = false;
					am.AddFollowerMonitor (sm);
				}
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

		protected override void OnWriteLogObject (object logObject)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.WriteLog) != 0)
					info.Monitor.LogObject (logObject);
		}

		protected override void OnObjectReported (object statusObject)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.ReportObject) != 0)
					info.Monitor.ReportObject (statusObject);
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
			base.Dispose ();
            foreach (MonitorInfo info in monitors) {
				if ((info.ActionMask & MonitorAction.Dispose) != 0)
					info.Monitor.Dispose ();
				if ((info.ActionMask & MonitorAction.FollowerCancel) != 0)
					info.CancellationTokenRegistration.Dispose ();
			}
		}

		void OnFollowerCancelRequested ()
		{
			CancellationTokenSource.Cancel ();
		}
	}
}
