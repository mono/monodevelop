// OutputProgressMonitor.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
//   Mike Krüger <mike@icsharpcode.net>
//
// Copyright (c) 2007 Novell, Inc (http://www.novell.com)
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
using System.Collections;
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui;
using MonoDevelop.Ide.Gui.Pads;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui.ProgressMonitoring;

using Gtk;
using Pango;

namespace MonoDevelop.Ide.Gui
{	
	internal class OutputProgressMonitor : NullProgressMonitor, IConsole
	{
		DefaultMonitorPad outputPad;
		event EventHandler stopRequested;
		
		LogTextWriter logger = new LogTextWriter ();
		LogTextWriter internalLogger = new LogTextWriter ();
		
		public OutputProgressMonitor (DefaultMonitorPad pad, string title, string icon)
		{
			pad.AsyncOperation = this.AsyncOperation;
			outputPad = pad;
			outputPad.BeginProgress (title);
			logger.TextWritten += outputPad.WriteText;
			internalLogger.TextWritten += outputPad.WriteConsoleLogText;
		}
		
		public override void BeginTask (string name, int totalWork)
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.BeginTask (name, totalWork);
			base.BeginTask (name, totalWork);
		}
		
		public override void EndTask ()
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.EndTask ();
			base.EndTask ();
		}
		
		protected override void OnCompleted ()
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.WriteText ("\n");
			
			foreach (string msg in Messages)
				outputPad.WriteText (msg + "\n");
			
			foreach (string msg in Warnings)
				outputPad.WriteText (msg + "\n");
			
			foreach (ProgressError msg in Errors)
				outputPad.WriteError (msg.Message + "\n");
			
			outputPad.EndProgress ();
			base.OnCompleted ();
			
			outputPad = null;
		}
		
		Exception GetDisposedException ()
		{
			return new InvalidOperationException ("Output progress monitor already disposed.");
		}
		
		protected override void OnCancelRequested ()
		{
			base.OnCancelRequested ();
			if (stopRequested != null)
				stopRequested (this, null);
		}
		
		public override TextWriter Log {
			get { return logger; }
		}
		
		TextWriter IConsole.Log {
			get { return internalLogger; }
		}
		
		TextReader IConsole.In {
			get { return new StringReader (""); }
		}
		
		TextWriter IConsole.Out {
			get { return logger; }
		}
		
		TextWriter IConsole.Error {
			get { return logger; }
		} 
		
		bool IConsole.CloseOnDispose {
			get { return false; }
		}
		
		event EventHandler IConsole.CancelRequested {
			add { stopRequested += value; }
			remove { stopRequested -= value; }
		}
	}
}
