// 
// CodeAnalysisRunner.cs
//  
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
// 
// Copyright (c) 2012 Xamarin Inc. (http://xamarin.com)
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
//#define PROFILE
using System;
using MonoDevelop.AnalysisCore;
using ICSharpCode.NRefactory.Refactoring;
using ICSharpCode.NRefactory6.CSharp;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;

namespace MonoDevelop.CodeIssues
{
	class DiagnosticResult : Result
	{
		readonly Diagnostic diagnostic;

		public Diagnostic Diagnostic {
			get {
				return diagnostic;
			}
		}

		public DiagnosticResult (Diagnostic diagnostic) : base (GetSpan (diagnostic), diagnostic.GetMessage ())
		{
			if (diagnostic == null)
				throw new ArgumentNullException ("diagnostic");
			this.diagnostic = diagnostic;

			SetSeverity (ConvertSeverity (diagnostic.Severity), GetIssueMarker ()); 
		}

		static TextSpan GetSpan (Diagnostic diagnostic)
		{
			int start = diagnostic.Location.SourceSpan.Start;
			int end = diagnostic.Location.SourceSpan.End;

			foreach (var loc in diagnostic.AdditionalLocations) {
				start = Math.Min (start, loc.SourceSpan.Start);
				end = Math.Max (start, loc.SourceSpan.End);
			}

			return TextSpan.FromBounds (start, end);
		}

		IssueMarker GetIssueMarker ()
		{
			if (diagnostic.Category == IssueCategories.RedundanciesInCode || diagnostic.Category == IssueCategories.RedundanciesInDeclarations)
				return IssueMarker.GrayOut;
			if (diagnostic.Severity == DiagnosticSeverity.Info)
				return IssueMarker.DottedLine;
			return IssueMarker.WavedLine;
		}

		static Severity ConvertSeverity (DiagnosticSeverity severity)
		{
			switch (severity) {
			case DiagnosticSeverity.Hidden:
				return Severity.None;
			case DiagnosticSeverity.Info:
				return Severity.Hint;
			case DiagnosticSeverity.Warning:
				return Severity.Warning;
			case DiagnosticSeverity.Error:
				return Severity.Error;
			default:
				throw new ArgumentOutOfRangeException ("severity", severity, "not supported");
			}
		}
	}
}

