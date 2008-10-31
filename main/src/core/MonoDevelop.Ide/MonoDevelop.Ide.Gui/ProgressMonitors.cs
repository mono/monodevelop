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
		
		/******************************/

		internal void Initialize ()
		{
			CreateMonitorPad (GettextCatalog.GetString ("Build Output"), MonoDevelop.Core.Gui.Stock.BuildCombine, false, true, false);
		}
		
		public IProgressMonitor GetBuildProgressMonitor ()
		{
			bool front = (bool) PropertyService.Get ("SharpDevelop.ShowOutputWindowAtBuild", true);
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
			DefaultMonitorPad monitorPad = CreateMonitorPad (title, icon, bringToFront, allowMonitorReuse, true);
			return new OutputProgressMonitor (monitorPad, title, icon);
		}
		
		DefaultMonitorPad CreateMonitorPad (string title, string icon, bool bringToFront, bool allowMonitorReuse, bool show)
		{
			Pad pad = null;
			if (icon == null)
				icon = MonoDevelop.Core.Gui.Stock.OutputIcon;

			int instanceCount = -1;
			if (allowMonitorReuse) {
				lock (outputMonitors) {
					// Look for an available pad
					for (int n=0; n<outputMonitors.Count; n++) {
						Pad mpad = (Pad) outputMonitors [n];
						DefaultMonitorPad mon = (DefaultMonitorPad) mpad.Content;
						if (mon.TypeTag == title) {
							if (mon.InstanceNum > instanceCount)
								instanceCount = mon.InstanceNum;
							if (mon.AllowReuse) {
								pad = mpad;
								break;
							}
						}
					}
				}
				if (pad != null) {
					if (bringToFront) pad.BringToFront ();
					return (DefaultMonitorPad) pad.Content;
				}
			}

			instanceCount++;
			DefaultMonitorPad monitorPad = new DefaultMonitorPad (title, icon, instanceCount);
			
			string newPadId = "OutputPad - " + title + " - " + instanceCount;
			string basePadId = "OutputPad - " + title + " - 0";
			
			if (instanceCount > 0)
				title += " (" + (instanceCount+1) + ")";

			if (show)
				pad = IdeApp.Workbench.ShowPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", icon);
			else
				pad = IdeApp.Workbench.AddPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", icon);
			
			pad.Sticky = true;
			outputMonitors.Add (pad);
			
			if (instanceCount > 0) {
				// Additional output pads will be destroyed when hidden
				pad.Window.PadHidden += delegate {
					outputMonitors.Remove (pad);
					pad.Destroy ();
				};
			}
			if (bringToFront)
				pad.BringToFront ();

			return monitorPad;
		}
		
		public ISearchProgressMonitor GetSearchProgressMonitor (bool bringToFront)
		{
			Pad pad = null;
			string title = GettextCatalog.GetString ("Search Results");
			
			int instanceNum = -1;
			lock (searchMonitors) {
				// Look for an available pad
				for (int n=0; n<searchMonitors.Count; n++) {
					Pad mpad = (Pad) searchMonitors [n];
					SearchResultPad rp = (SearchResultPad) mpad.Content;
					if (rp.InstanceNum > instanceNum)
						instanceNum = rp.InstanceNum;
					if (rp.AllowReuse) {
						pad = mpad;
						break;
					}
				}
			}
			if (pad != null) {
				if (bringToFront) pad.BringToFront ();
				return new SearchProgressMonitor ((SearchResultPad) pad.Content, pad.Title);
			}
			
			instanceNum++;
			
			string newPadId = "SearchPad - " + title + " - " + instanceNum;
			string basePadId = "SearchPad - " + title + " - 0";
			
			if (instanceNum > 0)
				title += " (" + (instanceNum+1) + ")";
			
			SearchResultPad monitorPad = new SearchResultPad (instanceNum);
			pad = IdeApp.Workbench.ShowPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", MonoDevelop.Core.Gui.Stock.FindIcon);
			pad.Sticky = true;
		    searchMonitors.Add (pad);

			if (searchMonitors.Count > 1) {
				// Additional search pads will be destroyed when hidden
				pad.Window.PadHidden += delegate {
					searchMonitors.Remove (pad);
					pad.Destroy ();
				};
			}
			
			if (bringToFront)
			    pad.BringToFront ();

			return new SearchProgressMonitor (monitorPad, pad.Title);
		}
	}
}
