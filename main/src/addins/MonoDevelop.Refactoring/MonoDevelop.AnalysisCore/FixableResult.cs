// 
// FixableResult.cs
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
using System.Collections.Generic;
using MonoDevelop.SourceEditor;
using MonoDevelop.SourceEditor.QuickTasks;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using Mono.TextEditor;
using ICSharpCode.NRefactory.Refactoring;

namespace MonoDevelop.AnalysisCore
{
	public class FixableResult : Result
	{
		public FixableResult (DomRegion region, string message, Severity level,
			IssueMarker mark, params IAnalysisFix[] fixes)
			: base (region, message, level, mark)
		{
			this.Fixes = fixes;
		}
		
		public IAnalysisFix[] Fixes { get; protected set; }
	}
	
	//FIXME: should this really use MonoDevelop.Ide.Gui.Document? Fixes could be more generic.
	public interface IAnalysisFix
	{
		string FixType { get; }
	}
	
	public interface IFixHandler
	{
		IEnumerable<IAnalysisFixAction> GetFixes (MonoDevelop.Ide.Gui.Document doc, object fix);
	}
	
	public interface IAnalysisFixAction
	{
		string Label { get; }
		bool SupportsBatchFix { get; }
		DocumentRegion DocumentRegion { get; }
		void Fix ();
		void BatchFix ();
	}
}

