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
using System.Threading.Tasks;

namespace MonoDevelop.Ide.Updater
{
	public static class UpdateService
	{
		static readonly TimeSpan AutoUpdateSpan = TimeSpan.FromDays (1);

		static UpdateService ()
		{
			NotifyAddinUpdates = true;
			ScheduleUpdateRun ();
		}

		static void ScheduleUpdateRun ()
		{
			new Timer (_ => CheckForUpdates (true), null, AutoUpdateSpan, AutoUpdateSpan);
		}

		public static bool AutoCheckForUpdates {
			get {
				return PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.CheckForUpdates", true) && Runtime.Preferences.EnableUpdaterForCurrentSession;
			}
			set {
				PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.CheckForUpdates", value);
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
				return UpdateService.UpdateChannel.ToUpdateLevel ();
			}
		}

		public static UpdateChannel UpdateChannel {
			get {
				// Returned Saved Update Level
				// If empty, use whatever "UpdateLevel" was set to.
				var updateChannelId = PropertyService.Get<string> ("MonoDevelop.Ide.AddinUpdater.UpdateChannel");
				if (string.IsNullOrEmpty (updateChannelId))
					return UpdateChannel.FromUpdateLevel (PropertyService.Get ("MonoDevelop.Ide.AddinUpdater.UpdateLevel", UpdateLevel.Stable));
				return new UpdateChannel (updateChannelId, updateChannelId, "", 0);
			}
			set {
				PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.UpdateChannel", value.Id);
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
			get { return TestMode.Length > 0 && !string.Equals (TestMode, "false", StringComparison.OrdinalIgnoreCase); }
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
				CheckForUpdates (true);
		}

		public static void CheckForUpdates ()
		{
			CheckForUpdates (false);
		}

		static async void CheckForUpdates (bool automatic)
		{
			if (automatic && !AutoCheckForUpdates)
				return;

			PropertyService.Set ("MonoDevelop.Ide.AddinUpdater.LastCheck", DateTime.Now);
			PropertyService.SaveProperties ();
			var handlers = AddinManager.GetExtensionObjects ("/MonoDevelop/Ide/Updater/UpdateHandlers");

			ProgressMonitor mon = IdeApp.Workbench.ProgressMonitors.GetBackgroundProgressMonitor ("Looking for updates", "md-updates");

			await CheckUpdates (mon, handlers, automatic);
		}

		static async Task CheckUpdates (ProgressMonitor monitor, object[] handlers, bool automatic)
		{
			using (monitor) {
				// The handler to use is the last one declared in the extension point
				if (handlers.Length == 0)
					return;
				try {
					IUpdateHandler uh = (IUpdateHandler) handlers [handlers.Length - 1];
					await uh.CheckUpdates (monitor, automatic);
				} catch (Exception ex) {
					LoggingService.LogError ("Updates check failed for handler of type '" + handlers [handlers.Length - 1].GetType () + "'", ex);
				}
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
