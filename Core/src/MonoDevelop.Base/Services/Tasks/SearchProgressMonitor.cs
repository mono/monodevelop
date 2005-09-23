//
// SearchProgressMonitor.cs
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
using System.CodeDom.Compiler;
using System.IO;
using System.Diagnostics;
using MonoDevelop.Services;

using MonoDevelop.Core.Properties;
using MonoDevelop.Core.Services;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Pads;

using Gtk;
using Pango;

namespace MonoDevelop.Services
{
	public class SearchProgressMonitor : BaseProgressMonitor, ISearchProgressMonitor
	{
		SearchResultPad outputPad;
		event EventHandler stopRequested;
		
		public SearchProgressMonitor (SearchResultPad pad, string title)
		{
			pad.AsyncOperation = this.AsyncOperation;
			outputPad = pad;
			outputPad.BeginProgress (title);
		}
		
		[FreeDispatch]
		public bool AllowReuse {
			get { return outputPad.AllowReuse; }
		}
		
		[FreeDispatch]
		public void SetBasePath (string path)
		{
			outputPad.SetBasePath (path);
		}
		
		[AsyncDispatch]
		public void ReportResult (string fileName, int line, int column, string text)
		{
			outputPad.AddResult (fileName, line, column, text);
		}
		
		protected override void OnWriteLog (string text)
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.WriteText (text);
		}
		
		protected override void OnCompleted ()
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.WriteText ("\n");
			
			foreach (string msg in SuccessMessages)
				outputPad.WriteText (msg + "\n");
			
			foreach (string msg in Warnings)
				outputPad.WriteText (msg + "\n");
			
			foreach (string msg in Errors)
				outputPad.WriteText (msg + "\n");
			
			outputPad.EndProgress ();
			base.OnCompleted ();
			
			Runtime.TaskService.ReleasePad (outputPad);
			outputPad = null;
		}
		
		Exception GetDisposedException ()
		{
			return new InvalidOperationException ("Search progress monitor already disposed.");
		}
		
		protected override void OnCancelRequested ()
		{
			base.OnCancelRequested ();
			if (stopRequested != null)
				stopRequested (this, null);
		}
	}
}
