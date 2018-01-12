//
// CodeIssueDescriptor.cs
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
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Diagnostics;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.CodeIssues
{
	class CodeDiagnosticDescriptor
	{
		readonly Type diagnosticAnalyzerType;

		DiagnosticAnalyzer instance;

		/// <summary>
		/// Gets the identifier string.
		/// </summary>
		internal string IdString {
			get {
				return diagnosticAnalyzerType.FullName;
			}
		}

		public Type DiagnosticAnalyzerType {
			get {
				return diagnosticAnalyzerType;
			}
		}

		/// <summary>
		/// Gets the languages for this issue.
		/// </summary>
		public string[] Languages { get; private set; }

		public DiagnosticSeverity? DiagnosticSeverity {
			get {
				DiagnosticSeverity? result = null;

				foreach (var diagnostic in GetProvider ().SupportedDiagnostics) {
					if (!result.HasValue)
						result = GetSeverity(diagnostic);
					if (result != GetSeverity(diagnostic))
						return null;
				}
				return result;
			}

			set {
				if (!value.HasValue)
					return;
				foreach (var diagnostic in GetProvider ().SupportedDiagnostics) {
					SetSeverity (diagnostic, value.Value);
				}
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this code action is enabled by the user.
		/// </summary>
		/// <value><c>true</c> if this code action is enabled; otherwise, <c>false</c>.</value>
		public bool IsEnabled {
			get {
				foreach (var diagnostic in GetProvider ().SupportedDiagnostics) {
					if (GetIsEnabled (diagnostic))
						return true;
				}
				return false;
			}
			set {
				foreach (var diagnostic in GetProvider ().SupportedDiagnostics) {
					SetIsEnabled (diagnostic, value);
				}
			}
		}

		internal DiagnosticSeverity GetSeverity (DiagnosticDescriptor diagnostic)
		{
			return PropertyService.Get ("CodeIssues." + Languages + "." + IdString + "." + diagnostic.Id + ".severity", diagnostic.DefaultSeverity);
		}

		internal DiagnosticSeverity GetSeverity (string diagnosticId, DiagnosticSeverity defaultSeverity)
		{
			return PropertyService.Get ("CodeIssues." + Languages + "." + IdString + "." + diagnosticId + ".severity", defaultSeverity);
		}

		internal void SetSeverity (DiagnosticDescriptor diagnostic, DiagnosticSeverity severity)
		{
			PropertyService.Set ("CodeIssues." + Languages + "." + IdString + "." + diagnostic.Id + ".severity", severity);
		}

		internal bool GetIsEnabled (DiagnosticDescriptor diagnostic)
		{
			return PropertyService.Get ("CodeIssues." + Languages + "." + IdString + "." + diagnostic.Id + ".enabled", true);
		}

		internal void SetIsEnabled (DiagnosticDescriptor diagnostic, bool value)
		{
			PropertyService.Set ("CodeIssues." + Languages + "." + IdString + "." + diagnostic.Id + ".enabled", value);
		}

		internal CodeDiagnosticDescriptor (string[] languages, Type codeIssueType)
		{
			if (languages == null)
				throw new ArgumentNullException (nameof (languages));
			if (codeIssueType == null)
				throw new ArgumentNullException (nameof (codeIssueType));
			Languages = languages;
			diagnosticAnalyzerType = codeIssueType;
		}

		/// <summary>
		/// Gets the roslyn code action provider.
		/// </summary>
		public DiagnosticAnalyzer GetProvider ()
		{
			if (instance == null)
				instance = (DiagnosticAnalyzer)Activator.CreateInstance(diagnosticAnalyzerType);

			return instance;
		}

		public override string ToString ()
		{
			return $"[CodeIssueDescriptor: IdString={IdString}, Language={Languages}]";
		}

		internal static async Task RunAction (DocumentContext context, CodeAction action, CancellationToken cancellationToken)
		{
			var operations = await action.GetOperationsAsync (cancellationToken).ConfigureAwait (false);

			foreach (var op in operations) {
				if (op == null)
					continue;
				try {
					op.Apply (context.RoslynWorkspace, cancellationToken);
				} catch (Exception e) {
					LoggingService.LogError ("Error while applying operation : " + op, e);
				}
			}

			foreach (var nested in action.NestedCodeActions) {
				await RunAction (context, nested, cancellationToken).ConfigureAwait (false);
			}
		}
	}
}

