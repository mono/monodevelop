//
// ProgressMonitorManager.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
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



using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.FindInFiles;
using System.Threading;

namespace MonoDevelop.Ide.Gui
{
	[DefaultServiceImplementation(typeof(IdeProgressMonitorManager))]
	public abstract class ProgressMonitorManager : Service
	{
		public ProgressMonitor GetBuildProgressMonitor ()
		{
			return OnGetBuildProgressMonitor ();
		}

		public ProgressMonitor GetCleanProgressMonitor ()
		{
			return OnGetCleanProgressMonitor ();
		}

		public ProgressMonitor GetRebuildProgressMonitor ()
		{
			return OnGetRebuildProgressMonitor ();
		}

		public OutputProgressMonitor GetRunProgressMonitor ()
		{
			return GetRunProgressMonitor (null);
		}

		public OutputProgressMonitor GetRunProgressMonitor (string titleSuffix)
		{
			return OnGetRunProgressMonitor (titleSuffix);
		}

		public OutputProgressMonitor GetToolOutputProgressMonitor (bool bringToFront, CancellationTokenSource cs = null)
		{
			return OnGetToolOutputProgressMonitor (bringToFront, cs);
		}

		public ProgressMonitor GetLoadProgressMonitor (bool lockGui)
		{
			return OnGetLoadProgressMonitor (lockGui);
		}

		public ProgressMonitor GetProjectLoadProgressMonitor (bool lockGui)
		{
			return OnGetProjectLoadProgressMonitor (lockGui);
		}

		public ProgressMonitor GetSaveProgressMonitor (bool lockGui)
		{
			return OnGetSaveProgressMonitor (lockGui);
		}

		public OperationConsole CreateConsole (bool closeOnDispose, CancellationToken cancellationToken)
		{
			return OnCreateConsole (closeOnDispose, cancellationToken);
		}

		public ProgressMonitor GetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs, bool showTaskTitle, bool lockGui, Pad statusSourcePad, bool showCancelButton)
		{
			return OnGetStatusProgressMonitor (title, icon, showErrorDialogs, showTaskTitle, lockGui, statusSourcePad, showCancelButton);
		}

		public ProgressMonitor GetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs, bool showTaskTitle = true, bool lockGui = false, Pad statusSourcePad = null)
		{
			return GetStatusProgressMonitor (title, icon, showErrorDialogs, showTaskTitle, lockGui, statusSourcePad, showCancelButton: false);
		}

		public ProgressMonitor GetBackgroundProgressMonitor (string title, IconId icon)
		{
			return OnGetBackgroundProgressMonitor (title, icon);
		}

		public OutputProgressMonitor GetOutputProgressMonitor (string title, IconId icon, bool bringToFront, bool allowMonitorReuse, bool visible = true)
		{
			return GetOutputProgressMonitor (null, title, icon, bringToFront, allowMonitorReuse, visible);
		}

		public OutputProgressMonitor GetOutputProgressMonitor (string id, string title, IconId icon, bool bringToFront, bool allowMonitorReuse, bool visible = true)
		{
			return GetOutputProgressMonitor (id, title, icon, bringToFront, allowMonitorReuse, null, visible);
		}

		public OutputProgressMonitor GetOutputProgressMonitor (string id, string title, IconId icon, bool bringToFront, bool allowMonitorReuse, string titleSuffix, bool visible = true)
		{
			return OnGetOutputProgressMonitor (id, title, icon, bringToFront, allowMonitorReuse, titleSuffix, visible);
		}

		public SearchProgressMonitor GetSearchProgressMonitor (bool bringToFront, bool focusPad = false, CancellationTokenSource cancellationTokenSource = null)
		{
			return OnGetSearchProgressMonitor (bringToFront, focusPad, cancellationTokenSource);
		}

		public OperationConsoleFactory ConsoleFactory {
			get { return OnGetConsoleFactory (); }
		}

		protected abstract OperationConsoleFactory OnGetConsoleFactory ();

		protected abstract ProgressMonitor OnGetBuildProgressMonitor ();

		protected abstract ProgressMonitor OnGetCleanProgressMonitor ();

		protected abstract ProgressMonitor OnGetRebuildProgressMonitor ();

		protected abstract OutputProgressMonitor OnGetRunProgressMonitor (string titleSuffix);

		protected abstract OutputProgressMonitor OnGetToolOutputProgressMonitor (bool bringToFront, CancellationTokenSource cs);

		protected abstract ProgressMonitor OnGetLoadProgressMonitor (bool lockGui);

		protected abstract ProgressMonitor OnGetProjectLoadProgressMonitor (bool lockGui);

		protected abstract ProgressMonitor OnGetSaveProgressMonitor (bool lockGui);

		protected abstract OperationConsole OnCreateConsole (bool closeOnDispose, CancellationToken cancellationToken);

		protected abstract ProgressMonitor OnGetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs, bool showTaskTitle, bool lockGui, Pad statusSourcePad, bool showCancelButton);

		protected abstract ProgressMonitor OnGetBackgroundProgressMonitor (string title, IconId icon);

		protected abstract OutputProgressMonitor OnGetOutputProgressMonitor (string id, string title, IconId icon, bool bringToFront, bool allowMonitorReuse, string titleSuffix, bool visible);

		protected abstract SearchProgressMonitor OnGetSearchProgressMonitor (bool bringToFront, bool focusPad, CancellationTokenSource cancellationTokenSource);
	}
}
