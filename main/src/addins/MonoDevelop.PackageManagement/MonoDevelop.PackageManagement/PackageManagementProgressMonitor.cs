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

using System.Collections.Generic;
using System.Linq;
using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using System.Threading;

namespace MonoDevelop.PackageManagement
{
	internal class PackageManagementProgressMonitor : ProgressMonitor
	{
		OutputProgressMonitor consoleMonitor;
		CancellationTokenRegistration consoleMonitorReg;
		CancellationTokenRegistration statusMonitorReg;

		public ProgressMonitor ConsoleMonitor {
			get { return consoleMonitor; }
		}

		public OperationConsole Console {
			get { return consoleMonitor.Console; }
		}

		public PackageManagementProgressMonitor (
			OutputProgressMonitor consoleMonitor,
			ProgressMonitor statusMonitor,
			CancellationTokenSource cancellationTokenSource)
			: base (cancellationTokenSource)
		{
			AddFollowerMonitor (statusMonitor);
			this.consoleMonitor = consoleMonitor;

			consoleMonitorReg = consoleMonitor.CancellationToken.Register (OnCancelRequested);
			statusMonitorReg = statusMonitor.CancellationToken.Register (OnCancelRequested);
		}

		protected override void OnWriteLog (string message)
		{
			consoleMonitor.Log.Write (message);
		}

		protected override void OnWriteErrorLog (string message)
		{
			consoleMonitor.ErrorLog.Write (message);
		}

		public override void Dispose ()
		{
			consoleMonitorReg.Dispose ();
			statusMonitorReg.Dispose ();

			foreach (var m in SuccessMessages)
				consoleMonitor.ReportSuccess (m);

			// Do not report warnings if there are errors otherwise the warnings will
			// appear at the end of the Package Console and hide the error which 
			// should be the last line of text visible to the user.
			if (Errors.Length == 0) {
				ReportAllWarningsButLastToConsole ();
			}

			ReportAllErrorsButLastToConsole ();

			consoleMonitor.Dispose ();

			base.Dispose ();
		}

		void ReportAllWarningsButLastToConsole ()
		{
			var warnings = Warnings.Distinct ().ToList ();
			RemoveLastItem (warnings);
			warnings.ForEach (warning => consoleMonitor.ReportWarning (warning));
		}

		void ReportAllErrorsButLastToConsole ()
		{
			var errors = Errors.ToList ();
			RemoveLastItem (errors);
			errors.ForEach (error => consoleMonitor.ReportError (error.Message, error.Exception));
		}

		static void RemoveLastItem<T> (List<T> items)
		{
			if (items.Count > 0) {
				items.RemoveAt (items.Count - 1);
			}
		}

		public object SyncRoot {
			get { return this; }
		}

		void OnCancelRequested ()
		{
			consoleMonitor.Log.WriteLine (GettextCatalog.GetString ("Cancelling operation..."));
			CancellationTokenSource.Cancel ();
		}
	}
}

