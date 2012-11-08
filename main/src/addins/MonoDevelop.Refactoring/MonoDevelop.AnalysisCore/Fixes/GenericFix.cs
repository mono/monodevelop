// 
// GenericFix.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.CodeIssues;
using MonoDevelop.Ide;

namespace MonoDevelop.AnalysisCore.Fixes
{
	public class InspectorResults : GenericResults
	{
		public CodeIssueProvider Inspector { get; private set; }

		public InspectorResults (CodeIssueProvider inspector, DomRegion region, string message, Severity level, IssueMarker mark, params GenericFix[] fixes)
			: base (region, message, level, mark, fixes)
		{
			this.Inspector = inspector;
		}

		public override bool HasOptionsDialog { get { return true; } }
		public override string OptionsTitle { get { return Inspector.Title; } }
		public override void ShowResultOptionsDialog ()
		{
			MessageService.RunCustomDialog (new CodeIssueOptionsDialog (Inspector), MessageService.RootWindow);
		}
		
	}

	public class GenericResults : FixableResult
	{
		public GenericResults (DomRegion region, string message, Severity level,
			IssueMarker mark, params GenericFix[] fixes)
			: base (region, message, level, mark)
		{
			this.Fixes = fixes;
		}
	}
	
	public class GenericFix : IAnalysisFix, IAnalysisFixAction
	{
		Action fix;
		string label;
		
		public GenericFix (string label, Action fix)
		{
			this.fix = fix;
			this.label = label;
		}

		
		#region IAnalysisFix implementation
		public string FixType {
			get {
				return "Generic";
			}
		}
		#endregion
		
		#region IAnalysisFixAction implementation
		public void Fix ()
		{
			fix ();
		}

		public string Label {
			get {
				return label;
			}
		}
		#endregion
	}
	
	public class GenericFixHandler : IFixHandler
	{
		#region IFixHandler implementation
		public IEnumerable<IAnalysisFixAction> GetFixes (MonoDevelop.Ide.Gui.Document doc, object fix)
		{
			yield return (GenericFix)fix;
		}
		#endregion
	}
}

