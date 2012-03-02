//
// BuildEventHandler.cs
//
// Author:
//   Lluis Sanchez Gual
//
// Copyright (C) 2006 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;

namespace MonoDevelop.Projects
{
	public delegate void BuildEventHandler (object sender, BuildEventArgs args);
	
	public class BuildEventArgs: EventArgs
	{
		IProgressMonitor monitor;
		bool success;
		
		public BuildEventArgs (IProgressMonitor monitor, bool success)
		{
			this.monitor = monitor;
			this.success = success;
			this.WarningCount = -1;
			this.ErrorCount = -1;
			this.BuildCount = -1;
			this.FailedBuildCount = -1;
		}
		
		public IProgressMonitor ProgressMonitor {
			get { return monitor; }
		}
		
		public bool Success {
			get { return success; }
		}
		
		public int WarningCount {
			get; set;
		}
		
		public int ErrorCount {
			get; set;
		}
		
		public int BuildCount {
			get; set;
		}
		
		public int FailedBuildCount {
			get; set;
		}
		
		public SolutionItem SolutionItem {
			get; set;
		}
	}
}
