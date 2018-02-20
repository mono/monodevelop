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
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Components.Docking;
using System.Threading;
using MonoDevelop.Ide.Gui.Components;

namespace MonoDevelop.Ide.Gui
{
	public class ProgressMonitorManager : GuiSyncObject
	{
		List<Pad> searchMonitors = new List<Pad> ();
		List<Pad> outputMonitors = new List<Pad> ();
		
		/******************************/

		internal void Initialize ()
		{
		}
		
		public ProgressMonitor GetBuildProgressMonitor ()
		{
			return GetBuildProgressMonitor (GettextCatalog.GetString ("Building..."));
		}
		
		public ProgressMonitor GetCleanProgressMonitor ()
		{
			return GetBuildProgressMonitor (GettextCatalog.GetString ("Cleaning..."));
		}
		
		public ProgressMonitor GetRebuildProgressMonitor ()
		{
			return GetBuildProgressMonitor (GettextCatalog.GetString ("Rebuilding..."));
		}
		
		private ProgressMonitor GetBuildProgressMonitor (string statusText)
		{
			Pad pad = IdeApp.Workbench.GetPad<ErrorListPad> ();
			ErrorListPad errorPad = (ErrorListPad) pad.Content;
			AggregatedProgressMonitor mon = new AggregatedProgressMonitor (errorPad.GetBuildProgressMonitor ());
			mon.AddFollowerMonitor (GetStatusProgressMonitor (statusText, Stock.StatusBuild, false, true, false, pad, true));
			return mon;
		}

		public OutputProgressMonitor GetRunProgressMonitor ()
		{
			return GetRunProgressMonitor (null);
		}
		
		public OutputProgressMonitor GetRunProgressMonitor (string titleSuffix)
		{
			return GetOutputProgressMonitor ("MonoDevelop.Ide.ApplicationOutput", GettextCatalog.GetString ("Application Output"), Stock.PadExecute, false, true, titleSuffix);
		}
		
		public OutputProgressMonitor GetToolOutputProgressMonitor (bool bringToFront, CancellationTokenSource cs = null)
		{
			return GetOutputProgressMonitor ("MonoDevelop.Ide.ToolOutput", GettextCatalog.GetString ("Tool Output"), Stock.PadExecute, bringToFront, true);
		}
		
		public ProgressMonitor GetLoadProgressMonitor (bool lockGui)
		{
			return GetStatusProgressMonitor (GettextCatalog.GetString ("Loading..."), Stock.StatusSolutionOperation, true, false, lockGui);
		}
		
		public ProgressMonitor GetProjectLoadProgressMonitor (bool lockGui)
		{
			return new GtkProjectLoadProgressMonitor (GetLoadProgressMonitor (lockGui));
		}
		
		public ProgressMonitor GetSaveProgressMonitor (bool lockGui)
		{
			return GetStatusProgressMonitor (GettextCatalog.GetString ("Saving..."), Stock.StatusSolutionOperation, true, false, lockGui);
		}
		
		public OperationConsole CreateConsole (bool closeOnDispose, CancellationToken cancellationToken)
		{
			return ((OutputProgressMonitor)GetOutputProgressMonitor ("MonoDevelop.Ide.ApplicationOutput", GettextCatalog.GetString ("Application Output"), Stock.MessageLog, false, true)).Console;
		}

		CustomConsoleFactory customConsoleFactory = new CustomConsoleFactory ();
		public OperationConsoleFactory ConsoleFactory {
			get { return customConsoleFactory; }
		}

		class CustomConsoleFactory: OperationConsoleFactory
		{
			protected override OperationConsole OnCreateConsole (CreateConsoleOptions options)
			{
				return ((OutputProgressMonitor)IdeApp.Workbench.ProgressMonitors.GetOutputProgressMonitor ("MonoDevelop.Ide.ApplicationOutput", GettextCatalog.GetString ("Application Output"), Stock.MessageLog, options.BringToFront, true, titleSuffix:options.Title)).Console;
			}
		}

		/******************************/
		public ProgressMonitor GetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs, bool showTaskTitle, bool lockGui, Pad statusSourcePad, bool showCancelButton)
		{
			return new StatusProgressMonitor (title, icon, showErrorDialogs, showTaskTitle, lockGui, statusSourcePad, showCancelButton);
		}
		
		public ProgressMonitor GetStatusProgressMonitor (string title, IconId icon, bool showErrorDialogs, bool showTaskTitle = true, bool lockGui = false, Pad statusSourcePad = null)
		{
			return GetStatusProgressMonitor (title, icon, showErrorDialogs, showTaskTitle, lockGui, statusSourcePad, showCancelButton: false);
		}
		
		public ProgressMonitor GetBackgroundProgressMonitor (string title, IconId icon)
		{
			return new BackgroundProgressMonitor (title, icon);
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
			if (!string.IsNullOrEmpty (titleSuffix)) {
				title += " - " + titleSuffix;
			}
			Pad pad = CreateMonitorPad (id, title, icon, bringToFront, allowMonitorReuse, true);
			pad.Visible = visible;
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
		public Pad GetPadForMonitor (ProgressMonitor monitor)
		{
			Runtime.AssertMainThread ();

			foreach (Pad pad in outputMonitors) {
				DefaultMonitorPad p = (DefaultMonitorPad) pad.Content;
				if (p.CurrentMonitor == monitor)
					return pad;
			}
			return null;
		}
		
		internal Pad CreateMonitorPad (string id, string title, string icon, bool bringToFront, bool allowMonitorReuse, bool show)
		{
			Pad pad = null;
			if (icon == null)
				icon = Stock.OutputIcon;

			string originalTitle = title;
			if (id == null)
				id = originalTitle;

			int instanceCount = -1;
			int titleInstanceCount = 0;
			if (allowMonitorReuse) {
				var usedTitleIds = new List<int> ();
				lock (outputMonitors) {
					// Look for an available pad
					for (int n=0; n<outputMonitors.Count; n++) {
						var mpad = outputMonitors [n];
						DefaultMonitorPad mon = (DefaultMonitorPad) mpad.Content;
						if (mon.TypeTag == id) {
							if (mon.InstanceNum > instanceCount)
								instanceCount = mon.InstanceNum;
							if (mon.Title == originalTitle && !mon.AllowReuse)
								usedTitleIds.Add (mon.TitleInstanceNum);
							if (mon.AllowReuse &&
							   (pad == null ||
							    mon.Title == originalTitle)) {//Prefer reusing output with same title(project)
								pad = mpad;
							}
						}
					}
				}
				titleInstanceCount = usedTitleIds.Count;//Set pesimisticly to largest possible number
				for (int i = 0; i < usedTitleIds.Count; i++) {
					if (!usedTitleIds.Contains (i)) {
						titleInstanceCount = i;//Find smallest free number
						break;
					}
				}
				if (titleInstanceCount > 0)
					title = originalTitle + $" ({titleInstanceCount})";
				else
					title = originalTitle;
				if (pad != null) {
					if (bringToFront) pad.BringToFront ();
					pad.Window.Title = title;
					var mon = (DefaultMonitorPad)pad.Content;
					mon.Title = originalTitle;
					mon.TitleInstanceNum = titleInstanceCount;
					return pad;
				}
			}

			instanceCount++;
			DefaultMonitorPad monitorPad = new DefaultMonitorPad (id, icon, instanceCount, originalTitle, titleInstanceCount);
			
			string newPadId = "OutputPad-" + id + "-" + instanceCount;
			string basePadId = "OutputPad-" + id + "-0";

			if (show)
				pad = IdeApp.Workbench.ShowPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", DockItemStatus.AutoHide, icon);
			else
				pad = IdeApp.Workbench.AddPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", DockItemStatus.AutoHide, icon);
			
			monitorPad.StatusSourcePad = pad;
			pad.Sticky = true;
			lock (outputMonitors) {
				outputMonitors.Add (pad);
			}
			
			if (instanceCount > 0) {
				// Additional output pads will be destroyed when hidden
				pad.Window.PadHidden += (s,a) => {
					// Workaround for crash reported in bug #18096. Look like MS.NET can't access private fields
					// when the delegate is invoked through the remoting chain.
					if (!a.SwitchingLayout)
						DestroyPad (pad);
				};
			}
			
			if (bringToFront) {
				pad.Visible = true;
				pad.BringToFront ();
			}

			return pad;
		}

		void DestroyPad (Pad pad)
		{
			lock (outputMonitors) {
				outputMonitors.Remove (pad);
			}
			pad.Destroy ();
		}
		
		public SearchProgressMonitor GetSearchProgressMonitor (bool bringToFront, bool focusPad = false, CancellationTokenSource cancellationTokenSource = null)
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
				return new SearchProgressMonitor (pad, cancellationTokenSource);
			}
			
			instanceNum++;
			
			string newPadId = "SearchPad - " + title + " - " + instanceNum;
			string basePadId = "SearchPad - " + title + " - 0";
			
			if (instanceNum > 0)
				title += " (" + (instanceNum+1) + ")";
			
			SearchResultPad monitorPad = new SearchResultPad (instanceNum) { FocusPad = focusPad };
			
			pad = IdeApp.Workbench.ShowPad (monitorPad, newPadId, title, basePadId + "/Center Bottom", Stock.FindIcon);
			pad.Sticky = true;
			lock (searchMonitors) {
				searchMonitors.Add (pad);

				if (searchMonitors.Count > 1) {
					// This is needed due to ContextBoundObject not being able to do a reflection access on private fields
					var searchMonitorsCopy = searchMonitors;
					// Additional search pads will be destroyed when hidden
					pad.Window.PadHidden += delegate {
						lock (searchMonitorsCopy) {
							searchMonitorsCopy.Remove (pad);
						}
						pad.Destroy ();
					};
				}
			}
			
			if (bringToFront)
				pad.BringToFront (focusPad);
			
			return new SearchProgressMonitor (pad, cancellationTokenSource);
		}
	}
}
