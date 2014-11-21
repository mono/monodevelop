//
// PackageManagementProgressMonitor.cs
//
// Author:
//       Matt Ward <matt.ward@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.PackageManagement
{
	public class PackageManagementProgressMonitor : IProgressMonitor, IAsyncOperation
	{
		IProgressMonitor consoleMonitor;
		IProgressMonitor statusMonitor;
		List<string> warnings = new List<string> ();
		List<string> errors = new List<string> ();

		public IProgressMonitor ConsoleMonitor {
			get { return consoleMonitor; }
		}

		public IConsole Console {
			get { return (IConsole)this.consoleMonitor; }
		}

		public PackageManagementProgressMonitor (IProgressMonitor consoleMonitor, IProgressMonitor statusMonitor)
		{
			this.consoleMonitor = consoleMonitor;
			this.statusMonitor = statusMonitor;

			consoleMonitor.CancelRequested += OnCancelRequested;
			statusMonitor.CancelRequested += OnCancelRequested;
		}

		public void BeginTask (string name, int totalWork)
		{
			statusMonitor.BeginTask (name, totalWork);
		}

		public void BeginStepTask (string name, int totalWork, int stepSize)
		{
			statusMonitor.BeginStepTask (name, totalWork, stepSize);
		}

		public void EndTask ()
		{
			statusMonitor.EndTask ();
		}

		public void Step (int work)
		{
			statusMonitor.Step (work);
		}

		public TextWriter Log
		{
			get { return consoleMonitor.Log; }
		}

		public void ReportSuccess (string message)
		{
			consoleMonitor.ReportSuccess (message);
			statusMonitor.ReportSuccess (message);
		}

		public void ReportWarning (string message)
		{
			warnings.Add (message);
			statusMonitor.ReportWarning (message);
		}

		public void ReportError (string message, Exception ex)
		{
			errors.Add (message);
			statusMonitor.ReportError (message, ex);
		}

		public void Dispose ()
		{
			consoleMonitor.CancelRequested -= OnCancelRequested;
			statusMonitor.CancelRequested -= OnCancelRequested;

			// Do not report warnings if there are errors otherwise the warnings will
			// appear at the end of the Package Console and hide the error which 
			// should be the last line of text visible to the user.
			if (errors.Count == 0) {
				ReportAllWarningsButLastToConsole ();
			}

			ReportAllErrorsButLastToConsole ();

			consoleMonitor.Dispose ();
			statusMonitor.Dispose ();
		}

		void ReportAllWarningsButLastToConsole ()
		{
			warnings = warnings.Distinct ().ToList ();
			RemoveLastItem (warnings);
			warnings.ForEach (warning => consoleMonitor.ReportWarning (warning));
		}

		void ReportAllErrorsButLastToConsole ()
		{
			RemoveLastItem (errors);
			errors.ForEach (error => consoleMonitor.ReportError (error, null));
		}

		static void RemoveLastItem (List<string> items)
		{
			if (items.Count > 0) {
				items.RemoveAt (items.Count - 1);
			}
		}

		public bool IsCancelRequested
		{
			get {
				return consoleMonitor.IsCancelRequested || statusMonitor.IsCancelRequested;
			}
		}

		public object SyncRoot {
			get { return this; }
		}

		void OnCancelRequested (IProgressMonitor sender)
		{
			AsyncOperation.Cancel ();
		}

		public IAsyncOperation AsyncOperation
		{
			get { return this; }
		}

		void IAsyncOperation.Cancel ()
		{
			consoleMonitor.AsyncOperation.Cancel ();
		}

		void IAsyncOperation.WaitForCompleted ()
		{
			consoleMonitor.AsyncOperation.WaitForCompleted ();
		}

		public bool IsCompleted {
			get { return consoleMonitor.AsyncOperation.IsCompleted; }
		}

		bool IAsyncOperation.Success { 
			get { return consoleMonitor.AsyncOperation.Success; }
		}

		bool IAsyncOperation.SuccessWithWarnings { 
			get { return consoleMonitor.AsyncOperation.SuccessWithWarnings; }
		}

		public event MonitorHandler CancelRequested {
			add { consoleMonitor.CancelRequested += value; }
			remove { consoleMonitor.CancelRequested -= value; }
		}

		public event OperationHandler Completed {
			add { consoleMonitor.AsyncOperation.Completed += value; }
			remove { consoleMonitor.AsyncOperation.Completed -= value; }
		}
	}
}

