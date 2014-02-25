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
using NUnit.Framework;
using MonoDevelop.Components.AutoTest;
using System.Linq;

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
			Session.ExecuteCommand (FileCommands.CloseAllFiles);
		}

		public static FilePath GetActiveDocumentFilename ()
		{
			return Session.GetGlobalValue<FilePath> ("MonoDevelop.Ide.IdeApp.Workbench.ActiveDocument.FileName");
		}

		public static void BuildSolution ()
		{
			RunAndWaitForTimer (
				() => Session.ExecuteCommand (ProjectCommands.BuildSolution),
				"MonoDevelop.Ide.Counters.BuildItemTimer"
			);

			var status = GetStatusMessage ();
			Assert.AreEqual (status, "Build successful.");
		}

		static void WaitUntil (Func<bool> done, int timeout = 20000, int pollStep = 200)
		{
			do {
				if (done ())
					return;
				timeout -= pollStep;
				Thread.Sleep (pollStep);
			} while (timeout > 0);

			throw new Exception ("Timed out waiting for event");
		}

		//no saner way to do this
		public static string GetStatusMessage (int timeout = 20000)
		{
			//wait for any queued messages to pop
			WaitUntil (
				() => Session.GetGlobalValue<int> ("MonoDevelop.Ide.IdeApp.Workbench.Toolbar.statusArea.messageQueue.Count") == 0,
				timeout
			);
			return (string) Session.GetGlobalValue ("MonoDevelop.Ide.IdeApp.Workbench.Toolbar.statusArea.renderArg.CurrentText");
		}

		public static void RunAndWaitForTimer (Action action, string counter, int timeout = 20000)
		{
			var c = Session.GetGlobalValue<TimerCounter> (counter);
			var tt = c.TotalTime;

			action ();

			WaitUntil (() => c.TotalTime > tt, timeout);
		}

		public static void CreateProject (string name, string category, string kind, FilePath directory)
		{
			Session.ExecuteCommand (FileCommands.NewProject);
			Session.WaitForWindow ("MonoDevelop.Ide.Projects.NewProjectDialog");

			Session.SelectWidget ("lst_template_types");
			Session.SelectTreeviewItem (category);

			Session.SelectWidget ("boxTemplates");
			var cells = Session.GetTreeviewCells ();
			var cellName = cells.First (c => c!= null && c.StartsWith (kind + "\n", StringComparison.Ordinal));
			Session.SelectTreeviewItem (cellName);

			Gui.EnterText ("txt_name", name);
			Gui.EnterText ("entry_location", directory);

			RunAndWaitForTimer (
				() => Gui.PressButton ("btn_new"),
				"MonoDevelop.Ide.Counters.OpenDocumentTimer"
			);
		}
	}

}
