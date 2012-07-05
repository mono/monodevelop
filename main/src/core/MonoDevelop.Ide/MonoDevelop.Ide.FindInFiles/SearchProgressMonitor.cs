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
using System.Collections.Generic;
using MonoDevelop.Core;
using MonoDevelop.Ide.ProgressMonitoring;
using MonoDevelop.Ide.Gui;

namespace MonoDevelop.Ide.FindInFiles
{
	class SearchProgressMonitor : BaseProgressMonitor, ISearchProgressMonitor
	{
		SearchResultPad outputPad;
		readonly IProgressMonitor statusMonitor;

		public SearchProgressMonitor (Pad pad)
		{
			outputPad = (SearchResultPad) pad.Content;
			outputPad.AsyncOperation = AsyncOperation;
			outputPad.BeginProgress (pad.Title);
			statusMonitor = IdeApp.Workbench.ProgressMonitors.GetStatusProgressMonitor (GettextCatalog.GetString ("Searching..."), Stock.StatusSearch, false, true, false, pad);
		}
		
		[FreeDispatch]
		public bool AllowReuse {
			get { return outputPad.AllowReuse; }
		}
		
		[FreeDispatch]
		public void SetBasePath (string path)
		{
			outputPad.BasePath = path;
		}
		
		[AsyncDispatch]
		public void ReportResult (SearchResult result)
		{
			try {
				outputPad.ReportResult (result);
			} catch (Exception ex) {
				LoggingService.LogError ("Error adding search result for file {0}:{1} to result pad:\n{2}",
				                         result.FileName, result.Offset, ex.ToString ());
			}
		}
		
		[AsyncDispatch]
		public void ReportResults (IEnumerable<SearchResult> results)
		{
			try {
				outputPad.ReportResults (results);
			} catch (Exception ex) {
				LoggingService.LogError ("Error adding search results.", ex.ToString ());
			}
		}
		
		[AsyncDispatch]
		public void ReportStatus (string resultMessage)
		{
			outputPad.ReportStatus (resultMessage);
		}
		
		protected override void OnWriteLog (string text)
		{
			if (outputPad == null) throw GetDisposedException ();
			outputPad.WriteText (text);
		}
		
		protected override void OnCompleted ()
		{
			statusMonitor.Dispose ();
			
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
			
			outputPad = null;
		}

		static Exception GetDisposedException ()
		{
			return new InvalidOperationException ("Search progress monitor already disposed.");
		}

		public override void ReportError (string message, Exception ex)
		{
			base.ReportError (message, ex);
			statusMonitor.ReportError (message, ex);
		}
		
		public override void ReportSuccess (string message)
		{
			base.ReportSuccess (message);
			statusMonitor.ReportSuccess (message);
		}
		
		public override void ReportWarning (string message)
		{
			base.ReportWarning (message);
			statusMonitor.ReportWarning (message);
		}
		
		public override void Step (int work)
		{
			base.Step (work);
			statusMonitor.Step (work);
		}
		
		public override void BeginStepTask (string name, int totalWork, int stepSize)
		{
			base.BeginStepTask (name, totalWork, stepSize);
			statusMonitor.BeginStepTask (name, totalWork, stepSize);
		}
		
		public override void BeginTask (string name, int totalWork)
		{
			base.BeginTask (name, totalWork);
			statusMonitor.BeginTask (name, totalWork);
		}
		
		public override void EndTask ()
		{
			base.EndTask ();
			statusMonitor.EndTask ();
		}
	}
}