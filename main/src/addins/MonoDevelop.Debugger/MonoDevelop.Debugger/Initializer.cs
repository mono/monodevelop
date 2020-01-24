// Initializer.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.IO;
using System.Threading.Tasks;

using Mono.Debugging.Client;

using MonoDevelop.Ide;
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Core.Web;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;

namespace MonoDevelop.Debugger
{
	public class Initializer: CommandHandler
	{
		Document disassemblyDoc;
		DisassemblyView disassemblyView;
		Document noSourceDoc;
		NoSourceView noSourceView;
		string symbolCachePath;
		bool changingFrame;

		protected override void Run ()
		{
			DebuggingService.CallStackChanged += OnStackChanged;
			DebuggingService.CurrentFrameChanged += OnFrameChanged;
			DebuggingService.DisassemblyRequested += OnShowDisassembly;

			IdeApp.CommandService.RegisterGlobalHandler (new GlobalRunMethodHandler ());
			symbolCachePath = UserProfile.Current.CacheDir.Combine ("Symbols");
		}

		void OnStackChanged (object s, EventArgs a)
		{
			if (disassemblyDoc == null || IdeApp.Workbench.ActiveDocument != disassemblyDoc) {
				// If the disassembly view is not selected, set a frame with source code
				SetSourceCodeFrame ();
			}
		}
		
		async void OnFrameChanged (object s, EventArgs a)
		{
			if (changingFrame)
				return;

			changingFrame = true;

			try {
				if (disassemblyDoc != null && DebuggingService.IsFeatureSupported (DebuggerFeatures.Disassembly))
					disassemblyView.Update ();

				var frame = DebuggingService.CurrentFrame;
				if (frame == null)
					return;

				var debuggerOptions = DebuggingService.GetUserOptions ();
				FilePath file = frame.SourceLocation.FileName;
				int line = frame.SourceLocation.Line;

				if (line != -1) {
					if (!file.IsNullOrEmpty && File.Exists (file)) {
						var doc = await IdeApp.Workbench.OpenDocument (file, null, line, 1, OpenDocumentOptions.Debugger);
						if (doc != null)
							return;
					}

					if (frame.SourceLocation.FileHash != null) {
						var newFilePath = SourceCodeLookup.FindSourceFile (file, frame.SourceLocation.FileHash);
						if (newFilePath != null && File.Exists (newFilePath)) {
							frame.UpdateSourceFile (newFilePath);
							var doc = await IdeApp.Workbench.OpenDocument (newFilePath, null, line, 1, OpenDocumentOptions.Debugger);
							if (doc != null)
								return;
						}
					}

					var automaticSourceDownload = debuggerOptions.AutomaticSourceLinkDownload;

					var sourceLink = frame.SourceLocation.SourceLink;
					if (sourceLink != null && automaticSourceDownload != AutomaticSourceDownload.Never) {
						var downloadLocation = sourceLink.GetDownloadLocation (symbolCachePath);
						Document doc = null;
						// ~/Library/Caches/VisualStudio/8.0/Symbols/org/projectname/git-sha/path/to/file.cs
						if (!File.Exists (downloadLocation)) {
							if (automaticSourceDownload == AutomaticSourceDownload.Always)
								doc = await NoSourceView.DownloadAndOpenAsync (frame);
						} else {
							// The file has previously been downloaded for a different solution.
							// We need to map the cached location
							frame.UpdateSourceFile (downloadLocation);
							doc = await IdeApp.Workbench.OpenDocument (downloadLocation, null, line, 1, OpenDocumentOptions.Debugger);
						}
						if (doc != null)
							return;
					}
				}

				bool disassemblySupported = !string.IsNullOrEmpty (frame.AddressSpace) &&
					DebuggingService.CurrentSessionSupportsFeature (DebuggerFeatures.Disassembly);

				if (!disassemblySupported && disassemblyDoc != null) {
					disassemblyDoc.Close ().Ignore ();
					disassemblyDoc = null;
					disassemblyView = null;
				}

				// If disassembly is open don't show NoSourceView
				if (disassemblyDoc == null) {
					if (noSourceDoc == null) {
						noSourceView = new NoSourceView ();
						noSourceView.Update (debuggerOptions, frame, disassemblySupported);
						noSourceDoc = await IdeApp.Workbench.OpenDocument (noSourceView, true);
						noSourceDoc.Closed += delegate {
							noSourceDoc = null;
							noSourceView = null;
						};
					} else {
						noSourceView.Update (debuggerOptions, frame, disassemblySupported);
						noSourceDoc.Select ();
					}
				} else {
					disassemblyDoc.Select ();
				}
			} finally {
				changingFrame = false;
			}
		}

		async void OnShowDisassembly (object s, EventArgs a)
		{
			if (disassemblyDoc == null) {
				disassemblyView = new DisassemblyView ();
				disassemblyDoc = await IdeApp.Workbench.OpenDocument (disassemblyView, true);
				disassemblyDoc.Closed += delegate {
					disassemblyDoc = null;
					disassemblyView = null;
				};
			} else {
				disassemblyDoc.Select ();
			}
			disassemblyView.Update ();
		}
		
		static void SetSourceCodeFrame ()
		{
			var bt = DebuggingService.CurrentCallStack;
			
			if (bt != null) {
				for (int n = 0; n < bt.FrameCount; n++) {
					var sf = bt.GetFrame (n);

					if (!sf.IsExternalCode &&
					    sf.SourceLocation.Line != -1 &&
					    !string.IsNullOrEmpty (sf.SourceLocation.FileName) &&
					    //Uncomment condition below once logic for ProjectOnlyCode in runtime is fixed
					    (/*DebuggingService.CurrentSessionSupportsFeature (DebuggerFeatures.Disassembly) ||*/

						    sf.SourceLocation.SourceLink != null ||
							File.Exists (sf.SourceLocation.FileName) ||
					        SourceCodeLookup.FindSourceFile (sf.SourceLocation.FileName, sf.SourceLocation.FileHash).IsNotNull)) {
						if (n != DebuggingService.CurrentFrameIndex)
							DebuggingService.CurrentFrameIndex = n;
						break;
					}
				}
			}
		}
	}

	class GlobalRunMethodHandler
	{
		// This handler will hide the Run menu if there are no debuggers installed.
		
		[CommandHandler (ProjectCommands.Run)]
		public void OnRun ()
		{
		}

		[CommandUpdateHandler (ProjectCommands.Run)]
		public void OnRunUpdate (CommandInfo cinfo)
		{
			if (!DebuggingService.IsDebuggingSupported)
				cinfo.Visible = false;
			else
				cinfo.Bypass = true;
		}
	}
}
