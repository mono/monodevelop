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
using System.Collections;
using System.IO;

namespace MonoDevelop.Services
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
	
	public class AggregatedProgressMonitor: IProgressMonitor, IAsyncOperation
	{
		IProgressMonitor masterMonitor;
		ArrayList monitors = new ArrayList ();
		LogTextWriter logger;
		
		class MonitorInfo {
			public MonitorAction ActionMask;
			public IProgressMonitor Monitor;
		}
		
		public AggregatedProgressMonitor (): this (new NullProgressMonitor ())
		{
		}
		
		public AggregatedProgressMonitor (IProgressMonitor masterMonitor)
		{
			this.masterMonitor = masterMonitor;
			AddSlaveMonitor (masterMonitor, MonitorAction.All);
			logger = new LogTextWriter ();
			logger.TextWritten += new LogTextEventHandler (OnWriteLog);
		}
		
		public void AddSlaveMonitor (IProgressMonitor slaveMonitor)
		{
			AddSlaveMonitor (slaveMonitor, MonitorAction.All);
		}
		
		public void AddSlaveMonitor (IProgressMonitor slaveMonitor, MonitorAction actionMask)
		{
			MonitorInfo smon = new MonitorInfo ();
			smon.ActionMask = actionMask;
			smon.Monitor = slaveMonitor;
			monitors.Add (smon);
			if ((actionMask & MonitorAction.SlaveCancel) != 0)
				slaveMonitor.CancelRequested += new MonitorHandler (OnSlaveCancelRequested);
		}
		
		public void BeginTask (string name, int totalWork)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0)
					info.Monitor.BeginTask (name, totalWork);
		}
		
		public void EndTask ()
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0)
					info.Monitor.EndTask ();
		}
		
		public void Step (int work)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Tasks) != 0)
					info.Monitor.Step (work);
		}
		
		public TextWriter Log
		{
			get { return logger; }
		}
		
		void OnWriteLog (string text)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.WriteLog) != 0)
					info.Monitor.Log.Write (text);
		}
		
		public void ReportSuccess (string message)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.ReportSuccess) != 0)
					info.Monitor.ReportSuccess (message);
		}
		
		public void ReportWarning (string message)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.ReportWarning) != 0)
					info.Monitor.ReportWarning (message);
		}
		
		public void ReportError (string message, Exception ex)
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.ReportError) != 0)
					info.Monitor.ReportError (message, ex);
		}
		
		public void Dispose ()
		{
			foreach (MonitorInfo info in monitors) {
				if ((info.ActionMask & MonitorAction.Dispose) != 0)
					info.Monitor.Dispose ();
				if ((info.ActionMask & MonitorAction.SlaveCancel) != 0)
					info.Monitor.CancelRequested -= new MonitorHandler (OnSlaveCancelRequested);
			}
		}
		
		public bool IsCancelRequested
		{
			get {
				foreach (MonitorInfo info in monitors)
					if ((info.ActionMask & MonitorAction.SlaveCancel) != 0) {
						if (info.Monitor.IsCancelRequested) return true;
					}
				return false;
			}
		}
		
		public object SyncRoot {
			get { return this; }
		}
		
		void OnSlaveCancelRequested (IProgressMonitor sender)
		{
			AsyncOperation.Cancel ();
		}
		
		public IAsyncOperation AsyncOperation
		{
			get { return this; }
		}
		
		void IAsyncOperation.Cancel ()
		{
			foreach (MonitorInfo info in monitors)
				if ((info.ActionMask & MonitorAction.Cancel) != 0)
					info.Monitor.AsyncOperation.Cancel ();
		}
		
		void IAsyncOperation.WaitForCompleted ()
		{
			masterMonitor.AsyncOperation.WaitForCompleted ();
		}
		
		public bool IsCompleted {
			get { return masterMonitor.AsyncOperation.IsCompleted; }
		}
		
		bool IAsyncOperation.Success { 
			get { return masterMonitor.AsyncOperation.Success; }
		}
		
		public event MonitorHandler CancelRequested {
			add { masterMonitor.CancelRequested += value; }
			remove { masterMonitor.CancelRequested -= value; }
		}
			
		public event OperationHandler Completed {
			add { masterMonitor.AsyncOperation.Completed += value; }
			remove { masterMonitor.AsyncOperation.Completed -= value; }
		}
	}
}
