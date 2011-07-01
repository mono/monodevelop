// 
// UpdateService.cs
//  
// Author:
//       Lluis Sanchez <lluis@xamarin.com>
// 
// Copyright (c) 2011 Xamarin Inc.
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

using MonoDevelop.Core.Setup;
using MonoDevelop.Core;
using System;
using Mono.Addins;
using MonoDevelop.Core.ProgressMonitoring;
using System.Threading;

namespace MonoDevelop.Ide.Updater
{
	public static class UpdateService
	{
		static UpdateService ()
		{
			NotifyAddinUpdates = true;
		}
		
		public static bool AutoCheckForUpdates {
			get {
				return PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.CkeckForUpdates", true);
			}
			set {
				PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.CkeckForUpdates", value);
			}
		}
		
		public static UpdateSpanUnit UpdateSpanUnit {
			get {
				return PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.UpdateSpanUnit", UpdateSpanUnit.Day);
			}
			set {
				PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.UpdateSpanUnit", value);
			}
		}
		
		public static int UpdateSpanValue {
			get {
				return PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.UpdateSpanValue", 1);
			}
			set {
				PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.UpdateSpanValue", value);
			}
		}
		
		public static UpdateLevel UpdateLevel {
			get {
				return PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.UpdateLevel", UpdateLevel.Stable);
			}
			set {
				PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.UpdateLevel", value);
			}
		}
		
		public static string TestMode {
			get {
				string testMode = Environment.GetEnvironmentVariable ("MONODEVELOP_UPDATER_TEST");
				if (!string.IsNullOrEmpty (testMode))
					return testMode;
				else
					return PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.TestMode", "");
			}
		}
		
		public static bool TestModeEnabled {
			get { return TestMode.Length > 0 && TestMode.ToLower () != "false"; }
		}
		
		public static bool NotifyAddinUpdates { get; set; }
		
		internal static void ScheduledCheckForUpdates ()
		{
			if (!AutoCheckForUpdates)
				return;
			
			DateTime lastUpdate = PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.LastCheck", DateTime.MinValue);
			
			bool check = false;
			if (UpdateSpanUnit == UpdateSpanUnit.Hour) {
				lastUpdate = lastUpdate.Date;
				check = (DateTime.Now - lastUpdate).TotalHours >= UpdateSpanValue;
			} else if (UpdateSpanUnit == UpdateSpanUnit.Day) {
				lastUpdate = lastUpdate.Date;
				check = (DateTime.Now - lastUpdate).TotalDays >= UpdateSpanValue;
			} else {
				lastUpdate = new DateTime (lastUpdate.Year, lastUpdate.Month, 1, 0, 0, 0);
				check = DateTime.Now >= lastUpdate.AddMonths (UpdateSpanValue);
			}
				
			if (check)
				CheckForUpdates ();
		}
		
		public static void CheckForUpdates ()
		{
			PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.LastCheck", DateTime.Now);
			PropertyService.SaveProperties ();
			var handlers = AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/Updater/UpdateHandlers");
			
			IProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor ("Looking for updates", "md-software-update"); 

			Thread t = new Thread (delegate () {
				CheckUpdates (mon, handlers);
			});
			t.Name = "Addin updater";
			t.Start ();
		}
		
		static void CheckUpdates (IProgressMonitor monitor, object[] handlers)
		{
			using (monitor) {
				monitor.BeginTask ("Looking for updates", handlers.Length);
				foreach (IUpdateHandler uh in handlers) {
					try {
						uh.CheckUpdates (monitor);
					} catch (Exception ex) {
						LoggingService.LogError ("Updates check failed for handler of type '" + uh.GetType () + "'", ex);
					}
					monitor.Step (1);
				}
				monitor.EndTask ();
			}
		}
	}
	
	public enum UpdateSpanUnit
	{
		Hour,
		Day,
		Month
	}
}
