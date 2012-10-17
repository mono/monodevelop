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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using Mono.Debugging.Client;
using MonoDevelop.Ide.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;

namespace MonoDevelop.Debugger
{
	public class Initializer: CommandHandler
	{
		Document disassemblyDoc;
		DisassemblyView disassemblyView;
		
		protected override void Run ()
		{
			DebuggingService.CallStackChanged += OnStackChanged;
			DebuggingService.CurrentFrameChanged += OnFrameChanged;
			DebuggingService.ExecutionLocationChanged += OnExecLocationChanged;
			DebuggingService.DisassemblyRequested += OnShowDisassembly;

			IdeApp.CommandService.RegisterGlobalHandler (new GlobalRunMethodHandler ());
		}
		
		void OnExecLocationChanged (object s, EventArgs a)
		{
		}
		
		void OnStackChanged (object s, EventArgs a)
		{
			if (disassemblyDoc == null || IdeApp.Workbench.ActiveDocument != disassemblyDoc) {
				// If the disassembly view is not selected, set a frame with source code
				SetSourceCodeFrame ();
			}
		}
		
		void OnFrameChanged (object s, EventArgs a)
		{
			bool disassemblyCurrent = false;
			if (disassemblyDoc != null && DebuggingService.IsFeatureSupported (DebuggerFeatures.Disassembly)) {
				disassemblyView.Update ();
				if (IdeApp.Workbench.ActiveDocument == disassemblyDoc)
					disassemblyCurrent = true;
			}
			
			var frame = DebuggingService.CurrentFrame;
			if (frame == null)
				return;
			
			FilePath file = frame.SourceLocation.FileName;
			int line = frame.SourceLocation.Line;
			
			if (!file.IsNullOrEmpty && System.IO.File.Exists (file) && line != -1) {
				Document doc = IdeApp.Workbench.OpenDocument (file, line, 1, OpenDocumentOptions.Debugger);
				if (doc != null)
					return;
			}

			if (!DebuggingService.CurrentSessionSupportsFeature (DebuggerFeatures.Disassembly))
				return;

			if (disassemblyDoc == null)
				OnShowDisassembly (null, null);
			else
				disassemblyDoc.Select ();
		}
		
		void OnShowDisassembly (object s, EventArgs a)
		{
			if (disassemblyDoc == null) {
				disassemblyView = new DisassemblyView ();
				disassemblyDoc = IdeApp.Workbench.OpenDocument (disassemblyView, true);
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
			Backtrace bt = DebuggingService.CurrentCallStack;
			
			if (bt != null) {
				for (int n=0; n<bt.FrameCount; n++) {
					StackFrame sf = bt.GetFrame (n);
					if (!sf.IsExternalCode && sf.SourceLocation.Line != -1) {
						bool found = !string.IsNullOrEmpty (sf.SourceLocation.FileName)
							&& System.IO.File.Exists (sf.SourceLocation.FileName);
						if (found) {
							if (n != DebuggingService.CurrentFrameIndex)
								DebuggingService.CurrentFrameIndex = n;
							break;
						} else {
							LoggingService.LogWarning ("Debugger could not find file '{0}'", sf.SourceLocation.FileName);
						}
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
