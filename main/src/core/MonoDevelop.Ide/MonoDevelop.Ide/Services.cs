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
		internal static TimerCounter OpenWorkspaceItemTimer = InstrumentationService.CreateTimerCounter ("Solution opened in the IDE", "IDE", id:"Ide.Shell.SolutionOpened");
		internal static TimerCounter OpenDocumentTimer = InstrumentationService.CreateTimerCounter ("Document opened", "IDE");
		internal static TimerCounter DocumentOpened = InstrumentationService.CreateTimerCounter ("Document opened", "IDE", id:"Ide.Shell.DocumentOpened");
		internal static Counter AutoSavedFiles = InstrumentationService.CreateCounter ("Autosaved Files", "Text Editor");
		internal static TimerCounter BuildItemTimer = InstrumentationService.CreateTimerCounter ("Project/Solution built in the IDE", "IDE", id:"Ide.Shell.ProjectBuilt");
		internal static Counter PadShown = InstrumentationService.CreateCounter ("Pad focused", "IDE", id:"Ide.Shell.PadShown");
		internal static TimerCounter SaveAllTimer = InstrumentationService.CreateTimerCounter ("Save all documents", "IDE", id:"Ide.Shell.SaveAll");
		internal static TimerCounter CloseWorkspaceTimer = InstrumentationService.CreateTimerCounter ("Workspace closed", "IDE", id:"Ide.Shell.CloseWorkspace");

		internal static TimerCounter ProcessCodeCompletion = InstrumentationService.CreateTimerCounter ("Process Code Completion", "IDE", id: "Ide.ProcessCodeCompletion", logMessages:false);

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

			return reports;
		}
	}
}

