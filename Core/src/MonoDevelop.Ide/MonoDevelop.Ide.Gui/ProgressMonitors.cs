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


using System;
using System.Collections;

using MonoDevelop.Core;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Search;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.Ide.Gui
{
	public class ProgressMonitorManager : GuiSyncObject, IConsoleFactory
	{
		ArrayList searchMonitors = new ArrayList ();
		ArrayList outputMonitors = new ArrayList ();
		int monitorId = 0;
		
		/******************************/
		
		public IProgressMonitor GetBuildProgressMonitor ()
		{
			bool front = (bool) Runtime.Properties.GetProperty ("SharpDevelop.ShowOutputWindowAtBuild", true);
			AggregatedProgressMonitor mon = new AggregatedProgressMonitor (GetOutputProgressMonitor (GettextCatalog.GetString ("Build Output"), MonoDevelop.Core.Gui.Stock.BuildCombine, front, true));
			mon.AddSlaveMonitor (GetStatusProgressMonitor (GettextCatalog.GetString ("Building..."), MonoDevelop.Core.Gui.Stock.BuildCombine, false));
			return mon;
		}
		
		public IProgressMonitor GetRunProgressMonitor ()
		{
			return GetOutputProgressMonitor (GettextCatalog.GetString ("Application Output"), MonoDevelop.Core.Gui.Stock.RunProgramIcon, true, true);
		}
		
		public IProgressMonitor GetLoadProgressMonitor (bool lockGui)
		{
			return GetStatusProgressMonitor (GettextCatalog.GetString ("Loading..."), Stock.OpenFileIcon, true, false, lockGui);
		}
		
		public IProgressMonitor GetSaveProgressMonitor (bool lockGui)
		{
			return GetStatusProgressMonitor (GettextCatalog.GetString ("Saving..."), Stock.SaveIcon, true, false, lockGui);
		}
		
		public IConsole CreateConsole (bool closeOnDispose)
		{
			return (IConsole) GetOutputProgressMonitor (GettextCatalog.GetString ("Application Output"), MonoDevelop.Core.Gui.Stock.RunProgramIcon, true, true);
		}
		
		/******************************/
		
		
		public IProgressMonitor GetStatusProgressMonitor (string title, string icon, bool showErrorDialogs)
		{
			return new StatusProgressMonitor (title, icon, showErrorDialogs, true, false);
		}
		
		public IProgressMonitor GetStatusProgressMonitor (string title, string icon, bool showErrorDialogs, bool showTaskTitle, bool lockGui)
		{
			return new StatusProgressMonitor (title, icon, showErrorDialogs, showTaskTitle, lockGui);
		}
		
		public IProgressMonitor GetBackgroundProgressMonitor (string title, string icon)
		{
			return new BackgroundProgressMonitor (title, icon);
		}
		
		public IProgressMonitor GetOutputProgressMonitor (string title, string icon, bool bringToFront, bool allowMonitorReuse)
		{
			Pad pad = null;
			if (icon == null)
				icon = MonoDevelop.Core.Gui.Stock.OutputIcon;

			if (allowMonitorReuse) {
				lock (outputMonitors) {
					// Look for an available pad
					for (int n=0; n<outputMonitors.Count; n++) {
						Pad mpad = (Pad) outputMonitors [n];
						if (mpad.Title == title) {
							pad = mpad;
							outputMonitors.RemoveAt (n);
							break;
						}
					}
				}
				if (pad != null) {
					if (bringToFront) pad.BringToFront ();
					return new OutputProgressMonitor ((DefaultMonitorPad) pad.Content, title, icon);
				}
			}
			
			DefaultMonitorPad monitorPad = new DefaultMonitorPad (title, icon);
			monitorId++;
			pad = IdeApp.Workbench.ShowPad (monitorPad, "OutputPad" + monitorId, title, "Bottom", icon);
			if (bringToFront) pad.BringToFront ();

			return new OutputProgressMonitor (monitorPad, title, icon);
		}

		internal void ReleasePad (DefaultMonitorPad pad)
		{
			lock (outputMonitors) {
				outputMonitors.Add (IdeApp.Workbench.FindPad (pad));
			}
		}
		
		public ISearchProgressMonitor GetSearchProgressMonitor (bool bringToFront)
		{
			Pad pad = null;
			string title = GettextCatalog.GetString ("Search Results");
			
			lock (searchMonitors) {
				// Look for an available pad
				for (int n=0; n<searchMonitors.Count; n++) {
					Pad mpad = (Pad) searchMonitors [n];
					if (((SearchResultPad)mpad.Content).AllowReuse) {
						pad = mpad;
						searchMonitors.RemoveAt (n);
						break;
					}
				}
			}
			if (pad != null) {
				if (bringToFront) pad.BringToFront ();
				return new SearchProgressMonitor ((SearchResultPad) pad.Content, title);
			}
			
			SearchResultPad monitorPad = new SearchResultPad ();
			pad = IdeApp.Workbench.ShowPad (monitorPad, "SearchPad" + (monitorId++), GettextCatalog.GetString ("Search Results"), "Bottom", MonoDevelop.Core.Gui.Stock.FindIcon);
			if (bringToFront) pad.BringToFront ();

			return new SearchProgressMonitor (monitorPad, title);
		}

		internal void ReleasePad (SearchResultPad pad)
		{
			lock (searchMonitors) {
				searchMonitors.Add (IdeApp.Workbench.FindPad (pad));
			}
		}
	}
}
