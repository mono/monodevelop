//
// ProgressMonitor.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@xamarin.com>
//
// Copyright (c) 2014 Xamarin, Inc (http://www.xamarin.com)
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
using System.Linq;
using System.IO;
using System.Threading;
using MonoDevelop.Core.ProgressMonitoring;
using System.Collections.Generic;

namespace MonoDevelop.Core
{
	public class ProgressMonitor: IDisposable
	{
		ProgressTask currentTask;
		ProgressTask parentRootTask;
		ProgressTask rootTask;

		LogTextWriter logWriter;
		LogTextWriter errorLogWriter;
		TextWriter customLogWriter;
		TextWriter customErrorLogWriter;

		int openStepWork = -1;
		ProgressMonitor parentMonitor;
		SynchronizationContext context;

		List<ProgressError> errors = new List<ProgressError> ();
		List<string> warnings = new List<string> ();
		List<string> messages = new List<string> ();

		List<ProgressMonitor> slaveMonitors;

		public ProgressMonitor (): this (null, null)
		{
		}

		public ProgressMonitor (SynchronizationContext context): this (context, null)
		{
		}

		public ProgressMonitor (CancellationTokenSource cancellationTokenSource): this (null, cancellationTokenSource)
		{
		}

		public ProgressMonitor (SynchronizationContext context, CancellationTokenSource cancellationTokenSource)
		{
			this.cancellationTokenSource = cancellationTokenSource;
			this.context = context;
			logWriter = new LogTextWriter ();
			logWriter.TextWritten += OnWriteLog;

			errorLogWriter = new LogTextWriter ();
			errorLogWriter.TextWritten += OnWriteErrorLog;
		}

		void SetParentTask (ProgressMonitor parent, ProgressTask task, int work)
		{
			parentMonitor = parent;
			currentTask = parentRootTask = task;
			openStepWork = work;
		}

		public virtual void Dispose ()
		{
			var t = parentRootTask;
			parentRootTask = null;
			while (currentTask != t && currentTask != null)
				EndTask ();

			if (context != null)
				context.Post ((o) => OnCompleted (), null);
			else
				OnCompleted ();

			if (slaveMonitors != null) {
				foreach (var m in slaveMonitors)
					m.Dispose ();
			}
		}

		protected void AddSlaveMonitor (ProgressMonitor monitor)
		{
			if (slaveMonitors == null)
				slaveMonitors = new List<ProgressMonitor> ();
			slaveMonitors.Add (monitor);
			logWriter.ChainWriter (monitor.Log);
			errorLogWriter.ChainWriter (monitor.ErrorLog);
		}

		protected void RemoveSlaveMonitor (ProgressMonitor monitor)
		{
			if (slaveMonitors == null)
				return;
			slaveMonitors.Remove (monitor);
			logWriter.UnchainWriter (monitor.Log);
			errorLogWriter.UnchainWriter (monitor.ErrorLog);
		}

		public ProgressTask CurrentTask {
			get {
				var t = currentTask;
				while (t != null && t.Name == null)
					t = t.ParentTask;
				return t;
			}
		}

		public string CurrentTaskName {
			get {
				var t = CurrentTask;
				return t != null ? t.Name ?? "" : "";
			}
		}

		public IEnumerable<ProgressTask> GetRootTasks ()
		{
			var t = rootTask;
			if (t.Name != null)
				return new [] {t};
			return t.GetChildrenTasks ();
		}

		public IDisposable BeginTask (string name, int totalWork)
		{
			var t = new ProgressTask (this, name, totalWork);
			if (openStepWork != -1) {
				t.StepWork = openStepWork;
				openStepWork = -1;
			}
			if (currentTask == null)
				rootTask = t;
			else
				currentTask.AddChild (t);

			currentTask = t;

			if (name != null) {
				if (context != null)
					context.Post ((o) => OnBeginTask (name, totalWork, t.StepWork), null);
				else
					OnBeginTask (name, totalWork, t.StepWork);
			}

			ReportProgressChanged ();

			if (slaveMonitors != null) {
				foreach (var m in slaveMonitors)
					m.BeginTask (name, totalWork);
			}
			return t;
		}

		public void BeginTask (int totalWork)
		{
			BeginTask (null, totalWork);
		}

		public void EndTask ()
		{
			if (currentTask != null && currentTask != parentRootTask) {
				openStepWork = -1;
				var t = currentTask;
				currentTask = t.ParentTask;
				if (currentTask == null)
					rootTask = null;
				t.SetComplete ();
				if (t.Name != null) {
					if (context != null)
						context.Post ((o) => OnEndTask (t.Name, t.TotalWork, t.StepWork), null);
					else
						OnEndTask (t.Name, t.TotalWork, t.StepWork);
				}
			} else
				LoggingService.LogError ("Task not started");

			ReportProgressChanged ();

			if (slaveMonitors != null) {
				foreach (var m in slaveMonitors)
					m.EndTask ();
			}
		}

		internal void EndTask (ProgressTask task)
		{
			while (currentTask != null && currentTask != task)
				EndTask ();
			EndTask ();
		}

		public void Step (int work = 1)
		{
			Step (null, work);
		}

		public void Step (string message, int work = 1)
		{
			if (currentTask == null)
				throw new InvalidOperationException ("Task not started");
			if (work < 0)
				throw new ArgumentException ("work can't be negative");

			ConsumePendingWork ();
			currentTask.Step (message, work);

			if (context != null)
				context.Post ((o) => {
					OnStep (message, work);
					ReportProgressChanged ();
				}, null);
			else {
				OnStep (message, work);
				ReportProgressChanged ();
			}

			if (slaveMonitors != null) {
				foreach (var m in slaveMonitors)
					m.Step (message, work);
			}
		}

		public void BeginStep (int work = 1)
		{
			BeginStep (null, work);
		}

		public void BeginStep (string message, int work = 1)
		{
			if (currentTask == null)
				throw new InvalidOperationException ("Task not started in progress monitor");
			if (work < 0)
				throw new ArgumentException ("work can't be negative");

			ConsumePendingWork ();

			openStepWork = work;
			if (message != null)
				currentTask.Step (message, 0);

			ReportProgressChanged ();

			if (slaveMonitors != null) {
				foreach (var m in slaveMonitors)
					m.BeginStep (message, work);
			}
		}

		public void EndStep ()
		{
			ConsumePendingWork ();

			if (slaveMonitors != null) {
				foreach (var m in slaveMonitors)
					m.EndStep ();
			}
		}

		void ConsumePendingWork ()
		{
			if (openStepWork != -1) {
				currentTask.Step (null, openStepWork);
				openStepWork = -1;
			}
		}

		public ProgressMonitor BeginAsyncStep (int work)
		{
			return BeginAsyncStep (null, work);
		}

		public ProgressMonitor BeginAsyncStep (string message, int work)
		{
			if (currentTask == null)
				throw new InvalidOperationException ("Task not started in progress monitor");
			if (work < 0)
				throw new ArgumentException ("work can't be negative");

			ConsumePendingWork ();
			if (message != null)
				currentTask.Step (message, 0);

			ProgressMonitor m = null;
			if (context != null)
				context.Send ((o) => m = CreateAsyncStepMonitor (), null);
			else
				m = CreateAsyncStepMonitor ();

			m.SetParentTask (this, currentTask, work);

			if (context != null) {
				context.Post ((o) => {
					OnBeginAsyncStep (message, work, m);
					ReportProgressChanged ();
				}, null);
			} else {
				OnBeginAsyncStep (message, work, m);
				ReportProgressChanged ();
			}

			if (slaveMonitors != null) {
				foreach (var sm in slaveMonitors)
					m.AddSlaveMonitor (sm.BeginAsyncStep (message, work));
			}
			return m;
		}

		public void ReportWarning (string message)
		{
			if (parentMonitor != null)
				parentMonitor.ReportWarning (message);
			lock (warnings)
				warnings.Add (message);

			if (context != null)
				context.Post ((o) => OnWarningReported (message), null);
			else
				OnWarningReported (message);

			if (slaveMonitors != null) {
				foreach (var sm in slaveMonitors)
					sm.ReportWarning (message);
			}
		}

		public void ReportSuccess (string message)
		{
			if (parentMonitor != null)
				parentMonitor.ReportSuccess (message);
			lock (messages)
				messages.Add (message);

			if (context != null)
				context.Post ((o) => OnSuccessReported (message), null);
			else
				OnSuccessReported (message);

			if (slaveMonitors != null) {
				foreach (var sm in slaveMonitors)
					sm.ReportSuccess (message);
			}
		}

		public void ReportError (string message, Exception exception = null)
		{
			if (parentMonitor != null)
				parentMonitor.ReportError (message, exception);

			var msg = message;
			if (message == null && exception != null)
				msg = exception.Message;

			lock (errors)
				errors.Add (new ProgressError (msg, exception));

			if (context != null)
				context.Post ((o) => OnErrorReported (msg, exception), null);
			else
				OnErrorReported (msg, exception);

			if (slaveMonitors != null) {
				foreach (var sm in slaveMonitors)
					sm.ReportError (message, exception);
			}
		}

		public bool HasErrors {
			get {
				lock (errors)
					return errors.Count > 0;
			}
		}

		public bool HasWarnings {
			get {
				lock (warnings)
					return warnings.Count > 0;
			}
		}

		public string[] SuccessMessages {
			get {
				lock (messages)
					return messages.ToArray ();
			}
		}

		public string[] Warnings {
			get {
				lock (warnings)
					return warnings.ToArray ();
			}
		}

		public ProgressError[] Errors {
			get {
				lock (errors)
					return errors.ToArray ();
			}
		}

		public TextWriter Log {
			get {
				if (parentMonitor != null)
					return parentMonitor.Log;
				return logWriter;
			}
			protected set {
				if (parentMonitor != null)
					throw new InvalidOperationException ("Log writter can't be modified");
				if (customLogWriter != null)
					logWriter.UnchainWriter (customLogWriter);
				customLogWriter = value;
				logWriter.ChainWriter (customLogWriter);
			}
		}

		public TextWriter ErrorLog {
			get {
				if (parentMonitor != null)
					return parentMonitor.ErrorLog;
				return errorLogWriter ?? Log;
			}
			protected set {
				if (parentMonitor != null)
					throw new InvalidOperationException ("Log writter can't be modified");
				if (customErrorLogWriter != null)
					errorLogWriter.UnchainWriter (customErrorLogWriter);
				customErrorLogWriter = value;
				errorLogWriter.ChainWriter (customErrorLogWriter);
			}
		}

		public CancellationToken CancellationToken {
			get {
				if (parentMonitor != null)
					return parentMonitor.CancellationToken;
				else
					return CancellationTokenSource.Token;
			}
		}

		public double Progress {
			get {
				return rootTask != null ? rootTask.Progress : 0;
			}
		}

		public bool ProgressIsUnknown {
			get {
				return rootTask == null;
			}
		}

		CancellationTokenSource cancellationTokenSource;

		protected CancellationTokenSource CancellationTokenSource {
			get {
				if (cancellationTokenSource == null)
					cancellationTokenSource = new CancellationTokenSource ();
				return cancellationTokenSource; 
			} set {
				cancellationTokenSource = value;
			}
		}

		protected virtual void OnBeginTask (string name, int totalWork, int stepWork)
		{
		}

		protected virtual void OnEndTask (string name, int totalWork, int stepWork)
		{
		}

		protected virtual void OnStep (string message, int work)
		{
		}

		protected virtual void OnBeginAsyncStep (string message, int work, ProgressMonitor stepMonitor)
		{
		}

		protected virtual ProgressMonitor CreateAsyncStepMonitor ()
		{
			return new ProgressMonitor ();
		}

		protected virtual void OnSuccessReported (string message)
		{
		}

		protected virtual void OnWarningReported (string message)
		{
		}

		protected virtual void OnErrorReported (string message, Exception exception)
		{
		}

		protected virtual void OnWriteLog (string message)
		{
		}

		protected virtual void OnWriteErrorLog (string message)
		{
		}

		void ReportProgressChanged ()
		{
			if (context != null)
				context.Post ((o) => OnProgressChanged (), null);
			else
				OnProgressChanged ();

			if (parentMonitor != null)
				parentMonitor.ReportProgressChanged ();
		}

		protected virtual void OnProgressChanged ()
		{
		}

		protected virtual void OnCompleted ()
		{
		}
	}

	public class ProgressTask: IDisposable
	{
		List<ProgressTask> childrenTasks = new List<ProgressTask> ();

		double currentWork;
		double completedChildrenWork;
		ProgressMonitor monitor;

		internal ProgressTask (ProgressMonitor monitor, string name, int totalWork)
		{
			this.monitor = monitor;
			Name = name;
			TotalWork = totalWork;
			StepWork = -1;
		}

		public int StepWork { get; set; }

		bool HasStepWork {
			get { return StepWork != -1; }
		}

		public string Name { get; private set; }

		public double Progress { get; private set; }

		public string StatusMessage { get; internal set; }

		public int TotalWork { get; private set; }

		public ProgressTask ParentTask { get; private set; }

		public ProgressTask[] GetChildrenTasks ()
		{
			List<ProgressTask> children = new List<ProgressTask> ();
			lock (childrenTasks) {
				foreach (var t in childrenTasks) {
					if (t.Name == null)
						children.AddRange (t.GetChildrenTasks ());
					else
						children.Add (t);
				}
			}
			return children.ToArray ();
		}

		internal void AddChild (ProgressTask task)
		{
			task.ParentTask = this;
			lock (childrenTasks)
				childrenTasks.Add (task);
		}

		internal void Step (string message, double work)
		{
			if (message != null)
				StatusMessage = message;

			IncCurrentWork (work);
		}

		internal void SetComplete ()
		{
			lock (childrenTasks) {
				currentWork = TotalWork;
				completedChildrenWork = 0;
				Progress = 1;
				if (ParentTask != null)
					ParentTask.SetChildComplete (this);
			}
		}

		void SetChildComplete (ProgressTask child)
		{
			child.ParentTask = null;
			lock (childrenTasks) {
				if (child.HasStepWork)
					currentWork += child.StepWork;
				childrenTasks.Remove (child);
			}
			UpdateProgressFromChildren ();
		}

		void IncCurrentWork (double work)
		{
			lock (childrenTasks) {
				currentWork += work;
				Progress = Math.Min (currentWork + completedChildrenWork, (double)TotalWork) / (double)TotalWork;
				if (ParentTask != null && HasStepWork)
					ParentTask.UpdateProgressFromChildren ();
			}
		}

		void UpdateProgressFromChildren ()
		{
			lock (childrenTasks) {
				completedChildrenWork = childrenTasks.Where (t => t.HasStepWork).Sum (t => t.Progress * (double)t.StepWork);
				IncCurrentWork (0);
			}
		}

		void IDisposable.Dispose ()
		{
			monitor.EndTask (this);
		}
	}

	public class ProgressError
	{
		Exception ex;
		string message;

		public ProgressError (string message, Exception ex)
		{
			this.ex = ex;
			this.message = message;
		}

		public string Message {
			get { return message; }
		}

		public Exception Exception {
			get { return ex; }
		}
	}
}

