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

namespace MonoDevelop.CodeActions
{
	class CodeActionContainer
	{
		public static readonly CodeActionContainer Empty = new CodeActionContainer();

		public bool IsEmpty {
			get {
				return CodeDiagnosticActions.Count + CodeRefactoringActions.Count == 0;
			}
		}

		public IReadOnlyList<Tuple<CodeFixDescriptor, CodeAction>> CodeDiagnosticActions {
			get;
			private set;
		}

		public IReadOnlyList<Tuple<CodeRefactoringDescriptor, CodeAction>> CodeRefactoringActions {
			get;
			private set;
		}

		CodeActionContainer ()
		{
			CodeDiagnosticActions = new List<Tuple<CodeFixDescriptor, CodeAction>> ();
			CodeRefactoringActions = new List<Tuple<CodeRefactoringDescriptor, CodeAction>> ();
		}

		internal CodeActionContainer (List<Tuple<CodeFixDescriptor, CodeAction>> codeDiagnosticActions, List<Tuple<CodeRefactoringDescriptor, CodeAction>> codeRefactoringActions)
		{
			if (codeDiagnosticActions == null)
				throw new ArgumentNullException ("codeDiagnosticActions");
			if (codeRefactoringActions == null)
				throw new ArgumentNullException ("codeRefactoringActions");
			CodeDiagnosticActions = codeDiagnosticActions;
			CodeRefactoringActions = codeRefactoringActions;
		}
	}
}