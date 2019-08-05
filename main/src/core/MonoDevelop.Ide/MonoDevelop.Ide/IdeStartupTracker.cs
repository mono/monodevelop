//
// IdeStartupTracker.cs
//
// Author:
//       iain <iaholmes@microsoft.com>
//
// Copyright (c) 2019 
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
using System.Collections.Generic;
using System.Diagnostics;

using MonoDevelop.Core;

using MonoDevelop.Ide.Desktop;
using MonoDevelop.Ide.Extensions;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.TypeSystem;

namespace MonoDevelop.Ide
{
	internal class IdeStartupTracker
	{
		static Lazy<IdeStartupTracker> startupTracker = new Lazy<IdeStartupTracker> (() => new IdeStartupTracker ());
		static internal IdeStartupTracker StartupTracker => startupTracker.Value;

		const long ttcDuration = 3 * TimeSpan.TicksPerSecond; // Wait 3 seconds before ignoring TTC events

		Stopwatch startupTimer = new Stopwatch ();
		Stopwatch startupSectionTimer = new Stopwatch ();
		Stopwatch timeToCodeTimer = new Stopwatch ();
		Stopwatch timeToCodeSolutionTimer = new Stopwatch ();
		Stopwatch timeToCodeWorkspaceTimer;

		Dictionary<string, long> sectionTimings = new Dictionary<string, long> ();
		TimeToCodeMetadata ttcMetadata;

		IdeStartupTracker ()
		{

		}

		internal void Start ()
		{
			// Using a Stopwatch instead of a TimerCounter since calling
			// TimerCounter.BeginTiming here would occur before any timer
			// handlers can be registered. So instead the startup duration is
			// set as a metadata property on the Counters.Startup counter.
			startupTimer.Start ();
			startupSectionTimer.Start ();
			timeToCodeTimer.Start ();
		}

		internal void Stop (StartupInfo startupInfo)
		{
			startupTimer.Stop ();
			startupSectionTimer.Stop ();

			var result = IdeServices.DesktopService.PlatformTelemetry;
			if (result == null) {
				return;
			}

			StartupCompleted (startupInfo, result);
			sectionTimings = null;
		}

		internal void StartupCompleted (StartupInfo startupInfo, IPlatformTelemetryDetails platformTelemetryDetails)
		{
			var startupMetadata = GetStartupMetadata (startupInfo, platformTelemetryDetails);
			Counters.Startup.Inc (startupMetadata);

			// Start TTC timer
			ttcMetadata = new TimeToCodeMetadata {
				StartupTime = startupMetadata.CorrectedStartupTime
			};
			ttcMetadata.AddProperties (startupMetadata);

			LoggingService.LogDebug ("TTC starting");
		}

		internal void MarkSection (string name)
		{
			sectionTimings [name] = startupSectionTimer.ElapsedMilliseconds;
			startupSectionTimer.Restart ();
		}

		enum StartupType
		{
			Normal = 0x0,
			ConfigurationChange = 0x1,
			FirstLaunch = 0x2,
			DebuggerPresent = 0x10,
			CommandExecuted = 0x20,
			LaunchedAsDebugger = 0x40,
			FirstLaunchSetup = 0x80,

			// Monodevelop specific
			FirstLaunchAfterUpgrade = 0x10000
		}

		StartupMetadata GetStartupMetadata (StartupInfo startupInfo, IPlatformTelemetryDetails platformDetails)
		{
			var assetType = StartupAssetType.FromStartupInfo (startupInfo);
			StartupType startupType = StartupType.Normal;

			if (startupInfo.Restarted && !IdeApp.IsInitialRunAfterUpgrade) {
				startupType = StartupType.ConfigurationChange; // Assume a restart without upgrading was the result of a config change
			} else if (IdeApp.IsInitialRun) {
				startupType = StartupType.FirstLaunch;
			} else if (IdeApp.IsInitialRunAfterUpgrade) {
				startupType = StartupType.FirstLaunchAfterUpgrade;
			} else if (Debugger.IsAttached) {
				startupType = StartupType.DebuggerPresent;
			}

			return new StartupMetadata {
				CorrectedStartupTime = startupTimer.ElapsedMilliseconds,
				StartupType = Convert.ToInt32 (startupType),
				AssetTypeId = assetType.Id,
				AssetTypeName = assetType.Name,
				IsInitialRun = IdeApp.IsInitialRun,
				IsInitialRunAfterUpgrade = IdeApp.IsInitialRunAfterUpgrade,
				TimeSinceMachineStart = (long)platformDetails.TimeSinceMachineStart.TotalMilliseconds,
				TimeSinceLogin = (long)platformDetails.TimeSinceLogin.TotalMilliseconds,
				Timings = sectionTimings,
				StartupBehaviour = IdeApp.Preferences.StartupBehaviour.Value
			};
		}

		internal void TrackTimeToCode (TimeToCodeMetadata.DocumentType documentType)
		{
			LoggingService.LogDebug ("Tracking TTC");
			if (this.timeToCodeTimer == null || timeToCodeSolutionTimer == null) {
				LoggingService.LogDebug ("Ignoring TTC");
				return;
			}

			timeToCodeTimer.Stop ();
			timeToCodeSolutionTimer.Stop ();

			if (ttcMetadata == null) {
				timeToCodeSolutionTimer = null;
				timeToCodeTimer = null;
				throw new Exception ("SendTimeToCode called before initialisation completed");
			}

			LoggingService.LogDebug ("Processing TTC");
			ttcMetadata.SolutionLoadTime = timeToCodeSolutionTimer.ElapsedMilliseconds;

			ttcMetadata.CorrectedDuration = timeToCodeTimer.ElapsedMilliseconds;
			ttcMetadata.Type = documentType;

			Counters.TimeToCode.Inc ("SolutionLoaded", ttcMetadata);

			timeToCodeSolutionTimer = null;

			timeToCodeWorkspaceTimer = Stopwatch.StartNew ();
			MonoDevelopWorkspace.LoadingFinished += CompleteTimeToIntellisense;
		}

		void CompleteTimeToIntellisense (object sender, EventArgs args)
		{
			// Reuse ttcMetadata, as it already has other information set.
			MonoDevelopWorkspace.LoadingFinished -= CompleteTimeToIntellisense;

			timeToCodeWorkspaceTimer.Stop ();
			ttcMetadata.IntellisenseLoadTime = timeToCodeWorkspaceTimer.ElapsedMilliseconds;
			ttcMetadata.CorrectedDuration += timeToCodeWorkspaceTimer.ElapsedMilliseconds;

			Counters.TimeToIntellisense.Inc ("IntellisenseLoaded", ttcMetadata);
		}

		internal bool StartTimeToCodeLoadTimer ()
		{
			if (timeToCodeTimer.ElapsedTicks - startupTimer.ElapsedTicks > ttcDuration) {
				LoggingService.LogDebug ($"Not starting TTC timer: {timeToCodeTimer.ElapsedTicks - startupTimer.ElapsedTicks}");
				return false;
			}
			LoggingService.LogDebug ("Starting TTC timer");
			timeToCodeSolutionTimer.Start ();

			return true;
		}

	}
}
