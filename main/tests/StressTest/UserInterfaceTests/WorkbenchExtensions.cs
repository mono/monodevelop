//
// WorkbenchExtensions.cs
//
// Author:
//       Matt Ward <matt.ward@microsoft.com>
//
// Copyright (c) 2017 Microsoft
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
using System.Linq;
using MonoDevelop.Components.AutoTest;
using MonoDevelop.Core;
using MonoDevelop.Ide.Commands;

namespace UserInterfaceTests
{
	public static class WorkbenchExtensions
	{
		static AutoTestClientSession Session {
			get { return TestService.Session; }
		}

		public static void CloseAllOpenFiles ()
		{
			Session.ExecuteCommand (FileCommands.CloseAllFiles);
		}

		public static void OpenFile (FilePath file)
		{
			Session.RunAndWaitForTimer (() => {
				Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workbench.OpenDocument", file, true);
			}, "Ide.Shell.DocumentOpened");
		}

		public static void OpenFiles (IEnumerable<FilePath> files)
		{
			foreach (FilePath file in files) {
				Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workbench.OpenDocument", file, true);
			}
		}

		public static void SaveFile ()
		{
			Session.ExecuteCommand (FileCommands.Save);
		}

		public static bool RebuildSolution (bool isPass = true, int timeoutInSecs = 360)
		{
			Session.RunAndWaitForTimer (() => Session.ExecuteCommand (ProjectCommands.RebuildSolution), "Ide.Shell.ProjectBuilt", timeoutInSecs * 1000);
			return isPass == Workbench.IsBuildSuccessful (timeoutInSecs);
		}

		public static bool Debug (int timeoutSeconds = 20, int pollStepSecs = 5)
		{
			Session.ExecuteCommand ("MonoDevelop.Debugger.DebugCommands.Debug");
			try {
				Ide.WaitUntil (
					() => !Session.Query (c => IdeQuery.RunButton (c).Property ("Icon", "Stop")).Any (),
					timeout: timeoutSeconds * 1000, pollStep: pollStepSecs * 1000);
				return false;
			} catch (TimeoutException) {
				return true;
			}
		}

		public static void GrabDesktopFocus ()
		{
			Session.GlobalInvoke ("MonoDevelop.Ide.IdeApp.Workbench.GrabDesktopFocus");
		}
	}
}
