//
// ValidCodeDiagnosticAction.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://xamarin.com)
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
using Microsoft.CodeAnalysis.CodeActions;
using MonoDevelop.Core.Text;
using MonoDevelop.CodeActions;
using Microsoft.CodeAnalysis;
using MonoDevelop.CodeIssues;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;

namespace MonoDevelop.CodeActions
{
	/// <summary>
	/// Represents a code action that's valid at a specific segment that was created as a action for a specific code diagnostic.
	/// </summary>
	class ValidCodeDiagnosticAction : ValidCodeAction
	{
		ImmutableArray<Diagnostic> validDiagnostics;

		public CodeDiagnosticFixDescriptor Diagnostic {
			get;
			private set;
		}

		public ImmutableArray<Diagnostic> ValidDiagnostics {
			get {
				return validDiagnostics;
			}
		}

		public ValidCodeDiagnosticAction (CodeDiagnosticFixDescriptor diagnostic, CodeAction codeAction, ImmutableArray<Diagnostic> validDiagnostics, TextSpan validSegment) : base (codeAction, validSegment)
		{
			this.Diagnostic = diagnostic;
			this.validDiagnostics = validDiagnostics;
		}
	}
}