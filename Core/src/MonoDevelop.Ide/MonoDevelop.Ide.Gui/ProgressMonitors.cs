
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
			AggregatedProgressMonitor mon = new AggregatedProgressMonitor (GetOutputProgressMonitor ("Build Output", MonoDevelop.Core.Gui.Stock.BuildCombine, front, true));
			mon.AddSlaveMonitor (GetStatusProgressMonitor ("Building...", MonoDevelop.Core.Gui.Stock.BuildCombine, false));
			return mon;
		}
		
		public IProgressMonitor GetRunProgressMonitor ()
		{
			return GetOutputProgressMonitor ("Application Output", MonoDevelop.Core.Gui.Stock.RunProgramIcon, true, true);
		}
		
		public IProgressMonitor GetLoadProgressMonitor ()
		{
			return GetStatusProgressMonitor ("Loading...", Stock.OpenFileIcon, true);
		}
		
		public IProgressMonitor GetSaveProgressMonitor ()
		{
			return GetStatusProgressMonitor ("Saving...", Stock.SaveIcon, true);
		}
		
		public IConsole CreateConsole (bool closeOnDispose)
		{
			return (IConsole) GetOutputProgressMonitor ("Application Output", MonoDevelop.Core.Gui.Stock.RunProgramIcon, true, true);
		}
		
		/******************************/
		
		
		public IProgressMonitor GetStatusProgressMonitor (string title, string icon, bool showErrorDialogs)
		{
			return new StatusProgressMonitor (title, icon, showErrorDialogs);
		}
		
		public IProgressMonitor GetBackgroundProgressMonitor (string title, string icon)
		{
			return new BackgroundProgressMonitor (title, icon);
		}
		
		public IProgressMonitor GetOutputProgressMonitor (string title, string icon, bool bringToFront, bool allowMonitorReuse)
		{
			Pad pad = null;

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
			monitorPad.Id = "OutputPad" + (monitorId++);
			pad = IdeApp.Workbench.ShowPad (monitorPad);
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
					if (((SearchProgressMonitor)mpad.Content).AllowReuse) {
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
			monitorPad.Id = "SearchPad" + (monitorId++);
			pad = IdeApp.Workbench.ShowPad (monitorPad);
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
