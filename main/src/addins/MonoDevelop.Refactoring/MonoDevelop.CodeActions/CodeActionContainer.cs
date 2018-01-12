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

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using System.Collections.Immutable;

namespace MonoDevelop.CodeActions
{
	class CodeActionContainer
	{
		public static readonly CodeActionContainer Empty = new CodeActionContainer(ImmutableArray<CodeFixCollection>.Empty, ImmutableArray<CodeRefactoring>.Empty);

		public bool IsEmpty {
			get {
				return CodeFixActions.Length + CodeRefactoringActions.Length == 0;
			}
		}

		ImmutableArray<CodeFixCollection> codeFixActions;
		public ImmutableArray<CodeFixCollection> CodeFixActions {
			get {
				return codeFixActions;
			}
			private set {
				codeFixActions = value;
			}
		}

		ImmutableArray<CodeRefactoring> codeRefactoringActions;
		public ImmutableArray<CodeRefactoring> CodeRefactoringActions {
			get {
				return codeRefactoringActions;
			}
			private set {
				codeRefactoringActions = value;
			}
		}

		internal CodeActionContainer (ImmutableArray<CodeFixCollection> codeDiagnosticActions, ImmutableArray<CodeRefactoring> codeRefactoringActions)
		{
			CodeFixActions = codeDiagnosticActions;
			CodeRefactoringActions = codeRefactoringActions;
		}
	}
}