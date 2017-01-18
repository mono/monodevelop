// 
// Result.cs
//  
// Author:
//       Michael Hutchinson <mhutchinson@novell.com>
// 
// Copyright (c) 2010 Novell, Inc.
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
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.AnalysisCore
{
	public class Result
	{
		public Result (TextSpan region, string message, bool underLine = true)
		{
			this.Region = region;
			this.Message = message;
			this.Underline = underLine;
		}
		
		public Result (TextSpan region, string message, DiagnosticSeverity level, IssueMarker inspectionMark, bool underline = true)
		{
			this.Region = region;
			this.Message = message;
			this.Level = level;
			this.InspectionMark = inspectionMark;
			this.Underline = underline;
		}
		 
		public void SetSeverity (DiagnosticSeverity level, IssueMarker inspectionMark)
		{
			this.Level = level;
			this.InspectionMark = inspectionMark;
		}

		public virtual bool HasOptionsDialog { get { return false; } }
		public virtual string OptionsTitle { get { return ""; } }
		public virtual void ShowResultOptionsDialog ()
		{
			throw new InvalidOperationException ();
		}
		
		public string Message { get; private set; }
		public DiagnosticSeverity Level { get; private set; }
		public IssueMarker InspectionMark { get; private set; }
		public TextSpan Region { get; private set; }
		
		public bool Underline { get; private set; }
	}
}
