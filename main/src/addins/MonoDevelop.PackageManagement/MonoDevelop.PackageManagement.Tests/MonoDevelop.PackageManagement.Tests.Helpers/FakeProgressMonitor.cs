//
// FakeProgressMonitor.cs
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
using System.IO;
using System.Text;
using MonoDevelop.Core;
using NUnit.Framework;
using System.Threading;

namespace MonoDevelop.PackageManagement.Tests.Helpers
{
	public class FakeProgressMonitor : ProgressMonitor
	{
		public event MonitorHandler CancelRequested;

		protected virtual void OnCancelRequested (ProgressMonitor monitor)
		{
			var handler = CancelRequested;
			if (handler != null)
				handler (monitor);
		}

		public FakeProgressMonitor ()
		{
			Log = new StringWriter (LoggedMessages);
		}

		protected override void OnBeginTask (string name, int totalWork, int stepWork)
		{
			BeginTaskTotalWork = totalWork;
		}

		public int BeginTaskTotalWork;

		public void BeginStepTask (string name, int totalWork, int stepSize)
		{
		}

		protected override void OnEndTask (string name, int totalWork, int stepWork)
		{
			IsTaskEnded = true;
		}

		public bool IsTaskEnded;

		protected override void OnStep (string message, int work)
		{
			StepCalledCount++;
			TotalStepWork += work;
		}

		public int StepCalledCount;
		public int TotalStepWork;

		protected override void OnWarningReported (string message)
		{
			ReportedWarningMessage = message;
		}

		public string ReportedWarningMessage;

		protected override void OnSuccessReported (string message)
		{
			ReportedSuccessMessage = message;
		}

		public string ReportedSuccessMessage;

		protected override void OnErrorReported (string message, Exception exception)
		{
			ReportedErrorMessage = message;
		}

		public string ReportedErrorMessage;

		public StringBuilder LoggedMessages = new StringBuilder ();

		public void AssertMessageIsLogged (string message)
		{
			string log = LoggedMessages.ToString ();
			Assert.IsTrue (log.Contains (message), log);
		}

		public void AssertMessageIsNotLogged (string message)
		{
			string log = LoggedMessages.ToString ();
			Assert.IsFalse (log.Contains (message), log);
		}

		public bool IsCancelRequested { get; set; }
		public AsyncOperation AsyncOperation { get; set; }
		public object SyncRoot { get; set; }

		public override void Dispose ()
		{
			base.Dispose ();
			IsDisposed = true;
		}

		public bool IsDisposed;

		public void Cancel ()
		{
			CancellationTokenSource.Cancel ();
		}

		public void SetCancellationTokenSource (CancellationTokenSource cancellationTokenSource)
		{
			CancellationTokenSource = cancellationTokenSource;
		}
	}
}

