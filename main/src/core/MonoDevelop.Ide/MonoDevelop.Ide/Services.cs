// Services.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2005 Novell, Inc (http://www.novell.com)
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
//
//


using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Tasks;
using MonoDevelop.Projects;
using MonoDevelop.Core.Instrumentation;
using MonoDevelop.Ide.Editor.Extension;
using System.Collections.Generic;

namespace MonoDevelop.Ide
{
	internal class Services
	{
		public static ProjectService ProjectService {
			get { return MonoDevelop.Projects.Services.ProjectService; }
		}
	}
	
	internal static class Counters
	{
		internal static TimerCounter Initialization = InstrumentationService.CreateTimerCounter ("IDE Initialization", "IDE", id:"Ide.Initialization");
		internal static Counter OpenDocuments = InstrumentationService.CreateCounter ("Open documents", "IDE");
		internal static Counter DocumentsInMemory = InstrumentationService.CreateCounter ("Documents in memory", "IDE");
		internal static Counter PadsLoaded = InstrumentationService.CreateCounter ("Pads loaded", "IDE");
		internal static TimerCounter CommandTargetScanTime = InstrumentationService.CreateTimerCounter ("Command target scan", "Timing", 0.3, false);
		internal static TimerCounter<OpenWorkspaceItemMetadata> OpenWorkspaceItemTimer = InstrumentationService.CreateTimerCounter<OpenWorkspaceItemMetadata> ("Solution opened in the IDE", "IDE", id:"Ide.Shell.SolutionOpened");
		internal static TimerCounter<OpenDocumentMetadata> OpenDocumentTimer = InstrumentationService.CreateTimerCounter<OpenDocumentMetadata> ("Open document", "IDE", id:"Ide.Shell.OpenDocument");
		internal static TimerCounter DocumentOpened = InstrumentationService.CreateTimerCounter ("Document opened", "IDE", id:"Ide.Shell.DocumentOpened");
		internal static Counter AutoSavedFiles = InstrumentationService.CreateCounter ("Autosaved Files", "Text Editor");
		internal static TimerCounter BuildItemTimer = InstrumentationService.CreateTimerCounter ("Project/Solution built in the IDE", "IDE", id:"Ide.Shell.ProjectBuilt");
		internal static Counter PadShown = InstrumentationService.CreateCounter ("Pad focused", "IDE", id:"Ide.Shell.PadShown");
		internal static TimerCounter SaveAllTimer = InstrumentationService.CreateTimerCounter ("Save all documents", "IDE", id:"Ide.Shell.SaveAll");
		internal static TimerCounter CloseWorkspaceTimer = InstrumentationService.CreateTimerCounter ("Workspace closed", "IDE", id:"Ide.Shell.CloseWorkspace");
		internal static Counter<StartupMetadata> Startup = InstrumentationService.CreateCounter<StartupMetadata> ("IDE Startup", "IDE", id:"Ide.Startup");
		internal static TimerCounter CompositionAddinLoad = InstrumentationService.CreateTimerCounter ("MEF Composition Addin Load", "IDE", id: "Ide.Startup.Composition.ExtensionLoad");
		internal static TimerCounter CompositionDiscovery = InstrumentationService.CreateTimerCounter ("MEF Composition From Discovery", "IDE", id:"Ide.Startup.Composition.Discovery");
		internal static TimerCounter CompositionCacheControl = InstrumentationService.CreateTimerCounter ("MEF Composition Control Cache", "IDE", id: "Ide.Startup.Composition.ControlCache");
		internal static TimerCounter CompositionCache = InstrumentationService.CreateTimerCounter ("MEF Composition From Cache", "IDE", id: "Ide.Startup.Composition.Cache");
		internal static TimerCounter CompositionSave = InstrumentationService.CreateTimerCounter ("MEF Composition Save", "IDE", id: "Ide.CompositionSave");
		internal static TimerCounter AnalysisTimer = InstrumentationService.CreateTimerCounter ("Code Analysis", "IDE", id:"Ide.CodeAnalysis");
		internal static TimerCounter ProcessCodeCompletion = InstrumentationService.CreateTimerCounter ("Process Code Completion", "IDE", id: "Ide.ProcessCodeCompletion", logMessages:false);
		internal static Counter<CompletionStatisticsMetadata> CodeCompletionStats = InstrumentationService.CreateCounter<CompletionStatisticsMetadata> ("Code Completion Statistics", "IDE", id:"Ide.CodeCompletionStatistics");
		internal static Counter<TimeToCodeMetadata> TimeToCode = InstrumentationService.CreateCounter<TimeToCodeMetadata> ("Time To Code", "IDE", id: "Ide.TimeToCode");
		internal static bool TrackingBuildAndDeploy;
		internal static TimerCounter<CounterMetadata> BuildAndDeploy = InstrumentationService.CreateTimerCounter<CounterMetadata> ("Build and Deploy", "IDE", id: "Ide.BuildAndDeploy");

		internal static Counter<UnhandledExceptionMetadata> UnhandledExceptions = InstrumentationService.CreateCounter<UnhandledExceptionMetadata> ("Unhandled Exceptions", "IDE", id: "Ide.UnhandledExceptions");
		internal static class ParserService {
			public static TimerCounter FileParsed = InstrumentationService.CreateTimerCounter ("File parsed", "Parser Service");
			public static TimerCounter ObjectSerialized = InstrumentationService.CreateTimerCounter ("Object serialized", "Parser Service");
			public static TimerCounter ObjectDeserialized = InstrumentationService.CreateTimerCounter ("Object deserialized", "Parser Service");
			public static TimerCounter WorkspaceItemLoaded = InstrumentationService.CreateTimerCounter ("Workspace item loaded", "Parser Service");
			public static Counter ProjectsLoaded = InstrumentationService.CreateTimerCounter ("Projects loaded", "Parser Service");
		}

		public static string[] CounterReport ()
		{
			string[] reports = new string[15];
			reports [0] = Initialization.ToString ();
			reports [1] = OpenDocuments.ToString ();
			reports [2] = DocumentsInMemory.ToString ();
			reports [3] = PadsLoaded.ToString ();
			reports [4] = CommandTargetScanTime.ToString ();
			reports [5] = OpenWorkspaceItemTimer.ToString ();
			reports [6] = OpenDocumentTimer.ToString ();
			reports [7] = DocumentOpened.ToString ();
			reports [8] = BuildItemTimer.ToString ();
			reports [9] = PadShown.ToString ();
			reports [10] = ParserService.FileParsed.ToString ();
			reports [11] = ParserService.ObjectSerialized.ToString ();
			reports [12] = ParserService.ObjectDeserialized.ToString ();
			reports [13] = ParserService.WorkspaceItemLoaded.ToString ();
			reports [14] = ParserService.ProjectsLoaded.ToString ();
			reports [15] = Startup.ToString ();

			return reports;
		}
	}

	class AssetMetadata : CounterMetadata
	{
		public int AssetTypeId {
			get => GetProperty<int> ();
			set => SetProperty (value);
		}
		public string AssetTypeName {
			get => GetProperty<string> ();
			set => SetProperty (value);
		}
	}

	class StartupMetadata: AssetMetadata
	{	
		public StartupMetadata ()
		{
		}

		public long CorrectedStartupTime {
			get => GetProperty<long> ();
			set => SetProperty (value);
		}
		public long StartupType {
			get => GetProperty<long> ();
			set => SetProperty (value);
		}
		public bool IsInitialRun {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}
		public bool IsInitialRunAfterUpgrade {
			get => GetProperty<bool> ();
			set => SetProperty (value);
		}
		public long TimeSinceMachineStart {
			get => GetProperty<long> ();
			set => SetProperty (value);
		}
		public long TimeSinceLogin {
			get => GetProperty<long> ();
			set => SetProperty (value);
		}
		public Dictionary<string, long> Timings {
			get => GetProperty<Dictionary<string, long>> ();
			set => SetProperty (value);
		}
	}

	class TimeToCodeMetadata : CounterMetadata
	{
		public long CorrectedDuration {
			get => GetProperty<long> ();
			set => SetProperty (value);
		}

		public long StartupTime {
			get => GetProperty<long> ();
			set => SetProperty (value);
		}

		public long SolutionLoadTime {
			get => GetProperty<long> ();
			set => SetProperty (value);
		}
	}

	class UnhandledExceptionMetadata : CounterMetadata
	{
		public System.Exception Exception {
			get => GetProperty<System.Exception> ();
			set => SetProperty (value);
		}
	}
}

