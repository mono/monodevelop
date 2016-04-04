//
// CodeActionContainer.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://xamarin.com)
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

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CodeActions;
using System;
using MonoDevelop.CodeIssues;
using System.Linq;
using Microsoft.CodeAnalysis;
using System.Collections;

namespace MonoDevelop.CodeActions
{
	class CodeActionContainer
	{
		public static readonly CodeActionContainer Empty = new CodeActionContainer();

		public bool IsEmpty {
			get {
				return CodeFixActions.Count + DiagnosticsAtCaret.Count + CodeRefactoringActions.Count == 0;
			}
		}

		IReadOnlyList<ValidCodeDiagnosticAction> codeFixActions;
		public IReadOnlyList<ValidCodeDiagnosticAction> CodeFixActions {
			get {
				return codeFixActions ?? new ValidCodeDiagnosticAction[0];
			}
			private set {
				codeFixActions = value;
			}
		}

		IReadOnlyList<ValidCodeAction> codeRefactoringActions;

		public IReadOnlyList<ValidCodeAction> CodeRefactoringActions {
			get {
				return codeRefactoringActions ?? new ValidCodeAction[0];
			}
			private set {
				codeRefactoringActions = value;
			}
		}

		public IEnumerable<ValidCodeAction> AllValidCodeActions {
			get {
				return CodeRefactoringActions.Concat (CodeFixActions);
			}
		}

		IReadOnlyList<Diagnostic> diagnosticsAtCaret;
		public IReadOnlyList<Diagnostic> DiagnosticsAtCaret {
			get {
				return diagnosticsAtCaret ?? new Diagnostic[0];
			}
			private set {
				diagnosticsAtCaret = value.Distinct (new DiagnosticComparer()).ToList ();
			}
		}

		class DiagnosticComparer : IEqualityComparer<Diagnostic>
		{
			bool IEqualityComparer<Diagnostic>.Equals (Diagnostic x, Diagnostic y)
			{
				if (x.Id != null && y.Id != null)
					return x.Id == y.Id;
				return x.Equals (y);
			}

			int IEqualityComparer<Diagnostic>.GetHashCode (Diagnostic obj)
			{
				return obj.Id != null ? obj.Id.GetHashCode () : obj.GetHashCode ();
			}
		}

		CodeActionContainer ()
		{
			CodeFixActions = new List<ValidCodeDiagnosticAction> ();
			CodeRefactoringActions = new List<ValidCodeAction> ();
			DiagnosticsAtCaret = new List<Diagnostic> ();
		}

		internal CodeActionContainer (List<ValidCodeDiagnosticAction> codeDiagnosticActions, List<ValidCodeAction> codeRefactoringActions, List<Diagnostic> diagnosticsAtCaret)
		{
			if (codeDiagnosticActions == null)
				throw new ArgumentNullException ("codeDiagnosticActions");
			if (codeRefactoringActions == null)
				throw new ArgumentNullException ("codeRefactoringActions");
			if (diagnosticsAtCaret == null)
				throw new ArgumentNullException ("diagnosticsAtCaret");
			CodeFixActions = codeDiagnosticActions;
			CodeRefactoringActions = codeRefactoringActions;
			DiagnosticsAtCaret = diagnosticsAtCaret;
		}
	}
}