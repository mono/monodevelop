//
// FilteredProgressMonitor.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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

namespace MonoDevelop.Core.ProgressMonitoring
{
	// This class can be used to filter some of the progress information that
	// the operation being monitorized provides.
	public class FilteredProgressMonitor: AggregatedProgressMonitor
	{
		public FilteredProgressMonitor (IProgressMonitor targetMonitor)
			: this (targetMonitor, MonitorAction.WriteLog | MonitorAction.ReportError | MonitorAction.ReportWarning | MonitorAction.ReportSuccess | MonitorAction.Cancel | MonitorAction.SlaveCancel)
		{
		}
		
		public FilteredProgressMonitor (IProgressMonitor targetMonitor, MonitorAction actionMask)
		{
			AddSlaveMonitor (targetMonitor, actionMask);
		}
	}
}
