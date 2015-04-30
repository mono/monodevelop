//
// IdeApi.cs
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

using MonoDevelop.Core;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.AutoTest;

using NUnit.Framework;

using Gdk;


namespace UserInterfaceTests
{
	public static class Ide
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public static void OpenFile (FilePath file)
		{
			Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workbench.OpenDocument", (FilePath) file, true);
			Assert.AreEqual (file, Ide.GetActiveDocumentFilename ());
		}

		public static FilePath OpenTestSolution (string solution)
		{
			FilePath path = Util.GetSampleProject (solution);

			RunAndWaitForTimer (
				() => Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workspace.OpenWorkspaceItem", (string)path),
				"MonoDevelop.Ide.Counters.OpenWorkspaceItemTimer"
			);

			return path;
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

		public static bool BuildSolution (bool isPass = true)
		{
			Session.RunAndWaitForTimer (() => Session.ExecuteCommand (ProjectCommands.BuildSolution),
				"Ide.Shell.ProjectBuilt", timeout: 60000);
			var status = IsBuildSuccessful ();
			return isPass == status;
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

		//no saner way to do this
		public static string GetStatusMessage (int timeout = 20000)
		{
			if (Platform.IsMac) {
				WaitUntil (
					() => Session.GetGlobalValue<string> ("MonoDevelop.Ide.IdeApp.Workbench.RootWindow.StatusBar.text") != string.Empty,
					timeout
				);
				return (string)Session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.Workbench.RootWindow.StatusBar.text");
			}

			WaitUntil (
				() => Session.GetGlobalValue<int> ("MonoDevelop.Ide.IdeApp.Workbench.RootWindow.StatusBar.messageQueue.Count") == 0,
				timeout
			);
			return (string) Session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.Workbench.RootWindow.StatusBar.renderArg.CurrentText");
		}

		public static bool IsBuildSuccessful ()
		{
			return Session.IsBuildSuccessful ();
		}

		public static void RunAndWaitForTimer (Action action, string counter, int timeout = 20000)
		{
			var c = Session.GetGlobalValue<TimerCounter> (counter);
			var tt = c.TotalTime;

			action ();

			WaitUntil (() => c.TotalTime > tt, timeout);
		}
	}

}