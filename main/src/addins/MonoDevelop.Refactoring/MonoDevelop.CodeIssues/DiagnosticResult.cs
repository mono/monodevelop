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
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis;
using RefactoringEssentials;
using System.Linq;
using System.Globalization;

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

		public DiagnosticResult (Diagnostic diagnostic) : base (diagnostic.Location.SourceSpan, diagnostic.GetMessage ())
		{
			if (diagnostic == null)
				throw new ArgumentNullException (nameof (diagnostic));
			this.diagnostic = diagnostic;

			SetSeverity (diagnostic.Severity, GetIssueMarker ()); 
		}

		static bool DescriptorHasTag (DiagnosticDescriptor desc, string tag)
		{
			return desc.CustomTags.Any (c => CultureInfo.InvariantCulture.CompareInfo.Compare (c, tag) == 0);
		}

		IssueMarker GetIssueMarker ()
		{
			if (DescriptorHasTag (diagnostic.Descriptor, WellKnownDiagnosticTags.Unnecessary))
				return IssueMarker.GrayOut;
			if (diagnostic.Descriptor.Category == DiagnosticAnalyzerCategories.RedundanciesInCode || diagnostic.Descriptor.Category == DiagnosticAnalyzerCategories.RedundanciesInDeclarations)
				return IssueMarker.GrayOut;
			if (diagnostic.Severity == DiagnosticSeverity.Info)
				return IssueMarker.DottedLine;
			return IssueMarker.WavedLine;
		}
	}
}

