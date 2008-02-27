//
// TestProgressMonitor.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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
using System.Diagnostics;
using System.Text.RegularExpressions;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Dialogs;
using Mono.Addins;
using MonoDevelop.Projects;
using NUnit.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Ide.Gui.Pads;

namespace MonoDevelop.NUnit
{
	public class TestProgressMonitor : ITestProgressMonitor
	{
		IProgressMonitor monitor;
		int failures, success, ignored;
		int totalWork, currentWork;
		DateTime startTime;
		public TestProgressMonitor ()
		{
		}
		
		public void InitializeTestRun (UnitTest test)
		{
			failures = success = ignored = currentWork = 0;
			monitor = IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor (GettextCatalog.GetString ("Test Output"), null, true, true);
			monitor.CancelRequested += CanelRequestedRequest;
			IdeApp.Services.TaskService.Clear ();
			totalWork = test.CountTestCases ();
			IdeApp.Workbench.StatusBar.BeginProgress (GettextCatalog.GetString ("Running tests"));
			startTime = DateTime.Now;
		}
		
		public void FinishTestRun ()
		{
			if (monitor != null) {
				IdeApp.Workbench.StatusBar.EndProgress ();
				IdeApp.Workbench.StatusBar.ShowMessage (GettextCatalog.GetString ("<b>Successful</b>: {0} <b>Failed</b>: {1} <b>Ignored</b>: {2}", this.success, this.failures, this.ignored));
				monitor.Log.WriteLine ("");
				monitor.Log.WriteLine (GettextCatalog.GetString ("Test run finished"));
				monitor.Log.WriteLine (GettextCatalog.GetString ("Total seconds: {0:0.0}", (DateTime.Now - this.startTime).TotalSeconds));
				monitor.Log.Write (String.Format (GettextCatalog.GetPluralString ("{0} was successfull, ", "{0} tests were successful,", this.success), this.success));
				monitor.Log.Write (String.Format (GettextCatalog.GetPluralString (" {0} failed,", " {0} failed,", this.failures), this.failures));
				monitor.Log.Write (String.Format (GettextCatalog.GetPluralString (" {0} ignored", " {0} ignored", this.ignored), this.ignored));
				monitor.Dispose ();
				monitor = null;
				if (failures > 0) {
					Pad errorList = IdeApp.Workbench.GetPad <ErrorListPad> ();
					if (errorList != null)
						errorList.BringToFront ();
				}
			}
		}
		
		public void Cancel ()
		{
			CanelRequestedRequest (monitor);
			FinishTestRun ();
		}
		
		public void BeginTest (UnitTest test)
		{
			if (test is UnitTestGroup)  {
				monitor.BeginTask (GettextCatalog.GetString ("Run tests in suite {0}", test.Name), 1);
				return;
			} 
			monitor.BeginTask (GettextCatalog.GetString ("Run test {0}", test.Name), 1);
			currentWork++;
			IdeApp.Workbench.StatusBar.SetProgressFraction ((double)currentWork / (double)totalWork);
		}
		
		public void EndTest (UnitTest test, UnitTestResult result)
		{
			if (test is UnitTestGroup) {
				monitor.EndTask ();
				return;
			}
			
			if (!String.IsNullOrEmpty (result.ConsoleError)) 
				monitor.Log.WriteLine (result.ConsoleError);
			if (!String.IsNullOrEmpty (result.ConsoleOutput))
				monitor.Log.WriteLine (result.ConsoleOutput);
			
			if (result.IsFailure) {
				
				failures++;
				string msg = !String.IsNullOrEmpty (result.Message) ? result.Message.Trim ().Replace (Environment.NewLine, " ") : "";
				monitor.Log.WriteLine (msg);
				if (!String.IsNullOrEmpty (result.StackTrace))
					monitor.Log.WriteLine (result.StackTrace.Trim ());
				SourceCodeLocation location = GetSourceCodeLocation (test, result.StackTrace);
				if (location != null) {
					Task t = new Task (location.FileName,
					                   msg,
					                   location.Column,
					                   location.Line,
					                   TaskType.Error);
					IdeApp.Services.TaskService.Add (t);
				}
				monitor.Log.WriteLine (GettextCatalog.GetString ("Test failed"));
			} else if (result.IsIgnored) {
				ignored++;
				monitor.Log.WriteLine (GettextCatalog.GetString ("Test ignored"));
			} else if (result.IsSuccess) {
				success++;
				monitor.Log.WriteLine (GettextCatalog.GetString ("Test success"));
			} else {
				monitor.ReportWarning ("test status unknown: " + result.Message);
			}
			monitor.EndTask ();
		}
		
		static SourceCodeLocation GetSourceCodeLocation (UnitTest test, string stackTrace)
		{
			Debug.Assert (test != null);
			if (!String.IsNullOrEmpty (stackTrace)) {
				System.Console.WriteLine(stackTrace);
				Match match = Regex.Match (stackTrace, @"\sin\s(.*?):(\d+)", RegexOptions.Multiline);
				while (match.Success) {
					try	{
						int line = Int32.Parse (match.Groups[2].Value);
						return new SourceCodeLocation (match.Groups[1].Value, line, 1);
					} catch (Exception) {
					}
					match = match.NextMatch ();
				}
			}
			return test.SourceCodeLocation;
		}
			
		
		public void ReportRuntimeError (string message, Exception exception)
		{
			monitor.ReportError (message, exception);
		}
		
		public bool IsCancelRequested { 
			get {
				return monitor.IsCancelRequested;
			}
		}
		
		void CanelRequestedRequest (IProgressMonitor monitor)
		{
			if (CancelRequested != null)
				CancelRequested ();
		}
		
		public event TestHandler CancelRequested;
	}
}
