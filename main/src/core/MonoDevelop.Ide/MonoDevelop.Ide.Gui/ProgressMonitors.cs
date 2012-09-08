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
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Components.Docking;

namespace MonoDevelop.Ide.Gui
{
	public class ProgressMonitorManager : GuiSyncObject, IConsoleFactory
	{
		ArrayList searchMonitors = new ArrayList ();
		ArrayList outputMonitors = new ArrayList ();
		
		/******************************/

		internal void Initialize ()
		{
		}
		
		public IProgressMonitor GetBuildProgressMonitor ()
		{
			return GetBuildProgressMonitor (GettextCatalog.GetString ("Building..."));
		}
		
		public IProgressMonitor GetCleanProgressMonitor ()
		{
			return GetBuildProgressMonitor (GettextCatalog.GetString ("Cleaning..."));
		}
		
		public IProgressMonitor GetRebuildProgressMonitor ()
		{
			return GetBuildProgressMonitor (GettextCatalog.GetString ("Rebuilding..."));
		}
		
		private IProgressMonitor GetBuildProgressMonitor (string statusText)
		{
			Pad pad = IdeApp.Workbench.GetPad<ErrorListPad> ();
			ErrorListPad errorPad = (ErrorListPad) pad.Content;
			AggregatedProgressMonitor mon = new AggregatedProgressMonitor (errorPad.GetBuildProgressMonitor ());
			mon.AddSlaveMonitor (GetStatusProgressMonitor (statusText, Stock.StatusBuild, false, true, false, pad));
			return mon;
		}
		
		public IProgressMonitor GetRunProgressMonitor ()
		{
			return GetOutputProgressMonitor ("MonoDevelop.Ide.ApplicationOutput", GettextCatalog.GetString ("Application Output"), Stock.RunProgramIcon, true, true);
		}
		
		public IProgressMonitor GetToolOutputProgressMonitor (bool bringToFront)
		{
			return GetOutputProgressMonitor ("MonoDevelop.Ide.ToolOutput", GettextCatalog.GetString ("Tool Output"), Stock.RunProgramIcon, bringToFront, true);
		}
		
		public IProgressMonitor GetLoadProgressMonitor (bool lockGui)
		{
			return GetStatusProgressMonitor (GettextCatalog.GetString ("Loading..."), Stock.StatusSolutionOperation, true, false, lockGui);
		}
		
		public IProgressMonitor GetProjectLoadProgressMonitor (bool lockGui)
		{
			return new GtkProjectLoadProgressMonitor (GetLoadProgressMonitor (lockGui));
		}
		
		public IProgressMonitor GetSaveProgressMonitor (bool lockGui)
		{
			return GetStatusProgressMonitor (GettextCatalog.GetString ("Saving..."), Stock.StatusSolutionOperation, true, false, lockGui);
		}
		
		public IConsole CreateConsole (bool closeOnDispose)
		{
			return (IConsole) GetOutputProgressMonitor ("MonoDevelop.Ide.ApplicationOutput", GettextCatalog.GetString ("Application Output"), Stock.MessageLog, true, true);
		}
		
		/******************************/
		
		
		public IProgressMonitor GetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs)
		{
			return new StatusProgressMonitor (title, icon, showErrorDialogs, true, false, null);
		}
		
		public IProgressMonitor GetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs, bool showTaskTitle, bool lockGui)
		{
			return new StatusProgressMonitor (title, icon, showErrorDialogs, showTaskTitle, lockGui, null);
		}
		
		public IProgressMonitor GetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs, bool showTaskTitle, bool lockGui, Pad statusSourcePad)
		{
			return new StatusProgressMonitor (title, icon, showErrorDialogs, showTaskTitle, lockGui, statusSourcePad);
		}
		
		public IProgressMonitor GetBackgroundProgressMonitor (string title, IconId icon)
		{
			return new BackgroundProgressMonitor (title, icon);
		}
		
		public IProgressMonitor GetOutputProgressMonitor (string title, IconId icon, bool bringToFront, bool allowMonitorReuse)
		{
			return GetOutputProgressMonitor (null, title, icon, bringToFront, allowMonitorReuse);
		}
		
		public IProgressMonitor GetOutputProgressMonitor (string id, string title, IconId icon, bool bringToFront, bool allowMonitorReuse)
		{
			Pad pad = CreateMonitorPad (id, title, icon, bringToFront, allowMonitorReuse, true);
			pad.Visible = true;
			return ((DefaultMonitorPad) pad.Content).BeginProgress (title);
		}
		
		/// <summary>
		/// Gets the pad that is showing the output of a progress monitor
		/// </summary>
		/// <param name='monitor'>
		/// The monitor.
		/// </param>
		/// <remarks>
		/// For example, if you have a monitor 'm' created with a call to GetOutputProgressMonitor,
		/// GetPadForMonitor (m) will return the output pad.
		/// </remarks>
		public Pad GetPadForMonitor (IProgressMonitor monitor)
		{
			foreach (Pad pad in outputMonitors) {
				DefaultMonitorPad p = (DefaultMonitorPad) pad.Content;
				if (p.CurrentMonitor == monitor)
					return pad;
			}
			return null;
		}
		
		Pad CreateMonitorPad (string id, string title, string icon, bool bringToFront, bool allowMonitorReuse, bool show)
		{
			Pad pad = null;
			if (icon == null)
				icon = Stock.OutputIcon;
			
			if (id == null)
				id = title;

			int instanceCount = -1;
			if (allowMonitorReuse) {
				lock (outputMonitors) {
					// Look for an available pad
					for (int n=0; n<outputMonitors.Count; n++) {
						Pad mpad = (Pad) outputMonitors [n];
						DefaultMonitorPad mon = (DefaultMonitorPad) mpad.Content;
						if (mon.TypeTag == id) {
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
					return pad;
				}
			}

			instanceCount++;
			DefaultMonitorPad monitorPad = new DefaultMonitorPad (id, icon, instanceCount);
			
			string newPadId = "OutputPad-" + id + "-" + instanceCount;
			string basePadId = "OutputPad-" + id + "-0";
			
			if (instanceCount > 0) {
				// Translate the title before adding the count
				title = GettextCatalog.GetString (title);
				title += " (" + (instanceCount+1) + ")";
			}

			if (show)
				pad = IdeApp.Workbench.ShowPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", DockItemStatus.AutoHide, icon);
			else
				pad = IdeApp.Workbench.AddPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", DockItemStatus.AutoHide, icon);
			
			monitorPad.StatusSourcePad = pad;
			pad.Sticky = true;
			outputMonitors.Add (pad);
			
			if (instanceCount > 0) {
				// Additional output pads will be destroyed when hidden
				pad.Window.PadHidden += delegate {
					outputMonitors.Remove (pad);
					pad.Destroy ();
				};
			}
			
			if (bringToFront) {
				pad.Visible = true;
				pad.BringToFront ();
			}

			return pad;
		}
		
		public ISearchProgressMonitor GetSearchProgressMonitor (bool bringToFront)
		{
			return GetSearchProgressMonitor (bringToFront, false);
		}
		
		public ISearchProgressMonitor GetSearchProgressMonitor (bool bringToFront, bool focusPad)
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
				if (bringToFront) pad.BringToFront (focusPad);
				return new SearchProgressMonitor (pad);
			}
			
			instanceNum++;
			
			string newPadId = "SearchPad - " + title + " - " + instanceNum;
			string basePadId = "SearchPad - " + title + " - 0";
			
			if (instanceNum > 0)
				title += " (" + (instanceNum+1) + ")";
			
			SearchResultPad monitorPad = new SearchResultPad (instanceNum) { FocusPad = focusPad };
			
			pad = IdeApp.Workbench.ShowPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", Stock.FindIcon);
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
				pad.BringToFront (focusPad);
			
			return new SearchProgressMonitor (pad);
		}
	}
}
