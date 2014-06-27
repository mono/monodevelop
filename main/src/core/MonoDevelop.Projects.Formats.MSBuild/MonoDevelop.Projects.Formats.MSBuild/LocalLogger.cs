// 
// LocalLogger.cs
//  
// Author:
//       Lluis Sanchez Gual <lluis@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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
using System.IO;
using System.Collections.Generic;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;

namespace MonoDevelop.Projects.Formats.MSBuild
{
	public class LocalLogger: Logger
	{
		IEventSource eventSource;
		readonly List<MSBuildTargetResult> results = new List<MSBuildTargetResult> ();
		readonly string projectFile;
		
		public LocalLogger (string projectFile)
		{
			this.projectFile = projectFile;
		}
		
		public List<MSBuildTargetResult> BuildResult {
			get { return results; }
		}
		
		public override void Initialize (IEventSource eventSource)
		{
			this.eventSource = eventSource;
			eventSource.WarningRaised += EventSourceWarningRaised;
			eventSource.ErrorRaised += EventSourceErrorRaised;
		}
		
		public override void Shutdown ()
		{
			eventSource.ErrorRaised -= EventSourceErrorRaised;
			eventSource.WarningRaised -= EventSourceWarningRaised;
		}

		void EventSourceWarningRaised (object sender, BuildWarningEventArgs e)
		{
			//NOTE: as of Mono 3.2.7, e.ProjectFile does not exist, so we use our projectFile variable instead
			results.Add (new MSBuildTargetResult (
				projectFile, true, e.Subcategory, e.Code, e.File,
				e.LineNumber, e.ColumnNumber, e.ColumnNumber, e.EndLineNumber,
				e.Message, e.HelpKeyword)
			);
		}

		void EventSourceErrorRaised (object sender, BuildErrorEventArgs e)
		{
			//NOTE: as of Mono 3.2.7, e.ProjectFile does not exist, so we use our projectFile variable instead
			results.Add (new MSBuildTargetResult (
				projectFile, false, e.Subcategory, e.Code, e.File,
				e.LineNumber, e.ColumnNumber, e.ColumnNumber, e.EndLineNumber,
				e.Message, e.HelpKeyword)
			);;
		}
	}
}
