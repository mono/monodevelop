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
			Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workbench.OpenDocument", (string) file, true);
			Assert.AreEqual (file, GetActiveDocumentFilename ());
		}

		public static void CloseAll ()
		{
			Session.ExecuteCommand (FileCommands.CloseWorkspace);
			Session.ExitApp ();
		}

		public static FilePath GetActiveDocumentFilename ()
		{
			return Session.GetGlobalValue<FilePath> ("MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument.FileName");
		}

		public static bool BuildSolution (bool isPass = true, int timeoutInSecs = 360)
		{
			Session.ExecuteCommand (ProjectCommands.BuildSolution);
			return isPass == Workbench.IsBuildSuccessful (timeoutInSecs);
		}

		public static void WaitUntil (Func<bool> done, int timeout = 20000, int pollStep = 200)
		{
			do {
				if (done ())
					return;
				timeout -= pollStep;
				Thread.Sleep (pollStep);
			} while (timeout > 0);

			throw new TimeoutException ("Timed out waiting for Function: "+done.Method.Name);
		}

		public static bool ClickButtonAlertDialog (string buttonText)
		{
			if (Platform.IsMac) {
				Ide.WaitUntil (() => Session.Query (c => c.Marked ("Xamarin Studio").Marked ("AppKit.NSPanel")).Any ());
				return Session.ClickElement (c => c.Marked ("AppKit.NSButton").Text (buttonText));
			}

			throw new PlatformNotSupportedException ("ClickButtonAlertDialog is only supported on Mac");
		}

		public static void RunAndWaitForTimer (Action action, string counter, int timeout = 20000)
		{
			var c = Session.GetGlobalValue<TimerCounter> (counter);
			var tt = c.TotalTime;

			action ();

			WaitUntil (() => c.TotalTime > tt, timeout);
		}

		public readonly static Action EmptyAction = delegate { };

		static string[] waitForNuGetMessages = {
			"Package updates are available.",
			"Packages are up to date.",
			"No updates found but warnings were reported.",
			"Packages successfully added.",
			"Packages successfully updated.",
			"Packages added with warnings.",
			"Packages updated with warnings."};
		
		public readonly static Action WaitForPackageUpdate = delegate {
			WaitForStatusMessage (waitForNuGetMessages, timeoutInSecs: 180, pollStepInSecs: 5);
		};

		public static void WaitForPackageUpdateExtra (List<string> otherMessages)
		{
			otherMessages.AddRange (waitForNuGetMessages);
			WaitForStatusMessage (otherMessages.ToArray (), timeoutInSecs: 180, pollStepInSecs: 5);
		}

		public readonly static Action WaitForSolutionCheckedOut = delegate {
			WaitForStatusMessage (new [] {"Solution checked out", "Solution Loaded."}, timeoutInSecs: 360, pollStepInSecs: 5);
		};

		public readonly static Action WaitForSolutionLoaded = delegate {
			WaitForStatusMessage (new [] {"Project saved.", "Solution loaded."}, timeoutInSecs: 120, pollStepInSecs: 5);
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
				string actualStatusMessage = null;
				try {
					actualStatusMessage = Workbench.GetStatusMessage ();
					return waitForMessage == (statusMessage.Contains (actualStatusMessage, StringComparer.OrdinalIgnoreCase));
				} catch (TimeoutException e) {
					throw new TimeoutException (
						string.Format ("Timed out. Found status message '{0}'\nand expected one of these:\n\t {1}",
							actualStatusMessage, string.Join ("\n\t", statusMessage)), e);
				}
			}, pollStep: pollStepInSecs * 1000, timeout: timeoutInSecs * 1000);
		}
	}

}
