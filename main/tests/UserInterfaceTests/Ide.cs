//
// Ide.cs
//
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using System.Threading;
using System.Linq;

using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.AutoTest;

using NUnit.Framework;
using System.Collections.Generic;

namespace UserInterfaceTests
{
	public static class Ide
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public static void OpenFile (FilePath file)
		{
			Session.RunAndWaitForTimer (() => Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workbench.OpenDocument", (string)file, true), "Ide.Shell.DocumentOpened");
			Assert.AreEqual (file, GetActiveDocumentFilename ());
		}

		public static void CloseAll (bool exit = true)
		{
			Session.ExecuteCommand (FileCommands.SaveAll);
			Session.ExecuteCommand (FileCommands.CloseWorkspace);
			if (exit)
				Session.ExitApp ();
		}

		public static FilePath GetActiveDocumentFilename ()
		{
			return Session.GetGlobalValue<FilePath> ("MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument.FileName");
		}

		public static bool BuildSolution (bool isPass = true, int timeoutInSecs = 360)
		{
			Session.RunAndWaitForTimer (() => Session.ExecuteCommand (ProjectCommands.BuildSolution), "Ide.Shell.ProjectBuilt", timeoutInSecs * 1000);
			return isPass == Workbench.IsBuildSuccessful (timeoutInSecs);
		}

		public static void WaitUntil (Func<bool> done, int timeout = 20000, int pollStep = 200, Func<string> timeoutMessage = null)
		{
			try {
				PollTimer (timeout, pollStep, done);
			} catch (TimeoutException) {
				// Replace the exception with another one.
				var suffix = timeoutMessage != null ? " Message: " + timeoutMessage () : string.Empty;
				throw new TimeoutException ("Timed out waiting for Function: " + done.Method.Name + suffix);
			}
		}

		public static bool ClickButtonAlertDialog (string buttonText)
		{
			if (Platform.IsMac) {
				Ide.WaitUntil (() => Session.Query (c => c.Marked ("Visual Studio").Marked ("AppKit.NSPanel")).Any ());
				return Session.ClickElement (c => c.Marked ("AppKit.NSButton").Text (buttonText));
			} else {
				Ide.WaitUntil (() => Session.Query (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.Dialogs.GtkAlertDialog")).Any ());
				return Session.ClickElement (c => c.Window ().Marked ("MonoDevelop.Ide.Gui.Dialogs.GtkAlertDialog").Children ().Button ().Text (buttonText));
			}
		}

		public static void RunAndWaitForTimer (Action action, string counter, int timeout = 20000)
		{
			var c = Session.GetGlobalValue<TimerCounter> (counter);
			var tt = c.TotalTime;

			action ();

			WaitUntil (() => c.TotalTime > tt, timeout,
					   timeoutMessage: () => "Counter:" + counter + " T1:" + c.TotalTime + " T2:" + tt + " Timeout:" + timeout);
		}


		public readonly static Action WaitForPackageUpdateOrSaved = delegate {
			try {
				WaitForPackageUpdate ();
			} catch (TimeoutException e1) {
				try {
					WaitForSolutionLoaded ();
				} catch (TimeoutException e2) {
					throw new TimeoutException (string.Format ("{0}\n{1}", e1.Message, e2.Message), e1);
				}
			}
		};

		public readonly static Action EmptyAction = delegate { };

		static readonly string[] waitForNuGetMessages = {
			"Package updates are available.",
			"Packages are up to date.",
			"No updates found but warnings were reported.",
			"Packages successfully added.",
			"Packages successfully updated.",
			"Packages added with warnings.",
			"Packages updated with warnings."};
		
		public readonly static Action WaitForPackageUpdate = delegate {
			WaitForStatusMessage (waitForNuGetMessages, timeoutInSecs: 180, pollStepInSecs: 1);
		};

		public static void WaitForPackageUpdateExtra (List<string> otherMessages)
		{
			otherMessages.AddRange (waitForNuGetMessages);
			WaitForStatusMessage (otherMessages.ToArray (), timeoutInSecs: 180, pollStepInSecs: 1);
		}

		public readonly static Action WaitForSolutionCheckedOut = delegate {
			WaitForStatusMessage (new [] {"Solution checked out", "Solution Loaded."}, timeoutInSecs: 360, pollStepInSecs: 1);
		};

		public readonly static Action WaitForSolutionLoaded = delegate {
			WaitForStatusMessage (new [] {"Project saved.", "Solution loaded."}, timeoutInSecs: 120, pollStepInSecs: 1);
		};

		public static void WaitForStatusMessage (string[] statusMessage, int timeoutInSecs = 240, int pollStepInSecs = 1)
		{
			PollStatusMessage (statusMessage, timeoutInSecs, pollStepInSecs);
		}

		public static void WaitForNoStatusMessage (string[] statusMessage, int timeoutInSecs = 240, int pollStepInSecs = 1)
		{
			PollStatusMessage (statusMessage, timeoutInSecs, pollStepInSecs, false);
		}

		static void PollStatusMessage (string[] statusMessage, int timeoutInSecs, int pollStepInSecs, bool waitForMessage = true)
		{
			Ide.WaitUntil (() => {
				string actualStatusMessage = string.Empty;
				try {
					actualStatusMessage = Workbench.GetStatusMessage ();
					return waitForMessage == (statusMessage.Contains (actualStatusMessage, StringComparer.OrdinalIgnoreCase));
				} catch (TimeoutException e) {
					throw new TimeoutException (
						string.Format ("Timed out. Found status message '{0}'\nand expected one of these:\n\t {1}",
							actualStatusMessage, string.Join ("\n\t", statusMessage)), e);
				}
			},
			pollStep: pollStepInSecs * 1000,
			timeout: timeoutInSecs * 1000,
			timeoutMessage: () => "GetStatusMessage=" + Workbench.GetStatusMessage ());
		}

		static readonly List<string> globalIgnoreStatusMessages = new List<string> {
			"Saving...",
			"Restoring packages for solution...",
			"Restoring packages before update...",
			"Restoring packages for project...",
			"Updating packages in solution...",
			"Updating packages in project..."
		};

		public static void WaitForIdeIdle (uint totalTimeoutInSecs = 100, uint idlePeriodInSecs = 1, string[] ignoreMessages = null)
		{
			var ignoreStatusMessages = globalIgnoreStatusMessages.ToList ();
			if (ignoreMessages != null)
				ignoreStatusMessages.AddRange (ignoreMessages);

			var initialStatusMessage = Workbench.GetStatusMessage (waitForNonEmpty: false);
			PollTimer ((int)totalTimeoutInSecs * 1000, (int)idlePeriodInSecs * 1000, () => {
				var finalStatusMessage = Workbench.GetStatusMessage (waitForNonEmpty: false);
				var isIdle = string.Equals (initialStatusMessage, finalStatusMessage) && !ignoreStatusMessages.Contains (finalStatusMessage);

				if (!isIdle)
					initialStatusMessage = finalStatusMessage;
				return isIdle;
			});
		}

		static void PollTimer (int timeout, int interval, Func<bool> checkIsDone)
		{
			uint retriesLeft = (uint)Math.Ceiling ((double)timeout / interval);
			var resetEvent = new ManualResetEvent (false);

			var timer = new System.Timers.Timer {
				Interval = interval,
				AutoReset = true
			};

			using (timer)
			using (resetEvent) {
				bool didTimeout = false;

				timer.Elapsed += (sender, e) => {
					if (retriesLeft == 0) {
						didTimeout = true;
						resetEvent.Set ();
						return;
					}

					bool done = checkIsDone ();
					if (!done) {
						retriesLeft--;
					} else
						resetEvent.Set ();
				};

				timer.Start ();
				resetEvent.WaitOne ();
				timer.Stop ();

				if (didTimeout)
					throw new TimeoutException ("Timeout waiting for IDE to be ready and idle");
			}
		}
	}

}
