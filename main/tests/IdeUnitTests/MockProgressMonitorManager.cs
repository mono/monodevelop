//
// MockProgressMonitorManager.cs
//
// Author:
//       Lluis Sanchez <llsan@microsoft.com>
//
// Copyright (c) 2019 Microsoft
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
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Gui;

namespace IdeUnitTests
{
	public class MockProgressMonitorManager: ProgressMonitorManager
	{
		public ProgressMonitor BackgroundProgressMonitor { get; set; } = new ConsoleProgressMonitor ();
		public ProgressMonitor BuildProgressMonitor { get; set; } = new ConsoleProgressMonitor ();
		public ProgressMonitor CleanProgressMonitor { get; set; } = new ConsoleProgressMonitor ();
		public ProgressMonitor LoadProgressMonitor { get; set; } = new ConsoleProgressMonitor ();
		public ProgressMonitor ProjectLoadProgressMonitor { get; set; } = new ConsoleProgressMonitor ();
		public OutputProgressMonitor OutputProgressMonitor { get; set; } = new MockOutputProgressMonitor ();
		public ProgressMonitor RebuildProgressMonitor { get; set; } = new ConsoleProgressMonitor ();
		public OutputProgressMonitor RunProgressMonitor { get; set; } = new MockOutputProgressMonitor ();
		public ProgressMonitor SaveProgressMonitor { get; set; } = new ConsoleProgressMonitor ();
		public SearchProgressMonitor SearchProgressMonitor { get; set; } = new SearchProgressMonitor ();
		public ProgressMonitor StatusProgressMonitor { get; set; } = new ConsoleProgressMonitor ();
		public OutputProgressMonitor ToolOutputProgressMonitor { get; set; } = new MockOutputProgressMonitor ();

		public new OperationConsoleFactory ConsoleFactory { get; set; }

		protected override OperationConsole OnCreateConsole (bool closeOnDispose, CancellationToken cancellationToken)
		{
			return new MockOperationConsole ();
		}

		protected override ProgressMonitor OnGetBackgroundProgressMonitor (string title, IconId icon)
		{
			return BackgroundProgressMonitor;
		}

		protected override ProgressMonitor OnGetBuildProgressMonitor ()
		{
			return BuildProgressMonitor;
		}

		protected override ProgressMonitor OnGetCleanProgressMonitor ()
		{
			return CleanProgressMonitor;
		}

		protected override OperationConsoleFactory OnGetConsoleFactory ()
		{
			return ConsoleFactory;
		}

		protected override ProgressMonitor OnGetLoadProgressMonitor (bool lockGui)
		{
			return LoadProgressMonitor;
		}

		protected override OutputProgressMonitor OnGetOutputProgressMonitor (string id, string title, IconId icon, bool bringToFront, bool allowMonitorReuse, string titleSuffix, bool visible)
		{
			return OutputProgressMonitor;
		}

		protected override ProgressMonitor OnGetProjectLoadProgressMonitor (bool lockGui)
		{
			return ProjectLoadProgressMonitor;
		}

		protected override ProgressMonitor OnGetRebuildProgressMonitor ()
		{
			return RebuildProgressMonitor;
		}

		protected override OutputProgressMonitor OnGetRunProgressMonitor (string titleSuffix)
		{
			return RunProgressMonitor;
		}

		protected override ProgressMonitor OnGetSaveProgressMonitor (bool lockGui)
		{
			return SaveProgressMonitor;
		}

		protected override SearchProgressMonitor OnGetSearchProgressMonitor (bool bringToFront, bool focusPad, CancellationTokenSource cancellationTokenSource)
		{
			return SearchProgressMonitor;
		}

		protected override ProgressMonitor OnGetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs, bool showTaskTitle, bool lockGui, Pad statusSourcePad, bool showCancelButton)
		{
			return StatusProgressMonitor;
		}

		protected override OutputProgressMonitor OnGetToolOutputProgressMonitor (bool bringToFront, CancellationTokenSource cs)
		{
			return ToolOutputProgressMonitor;
		}
	}
}
