//
// ISuppressionFixProvider.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace MonoDevelop.CodeIssues
{
	interface ISuppressionFixProvider
	{
		/// <summary>
		/// Returns true if the given diagnostic can be suppressed or unsuppressed.
		/// </summary>
		bool CanBeSuppressedOrUnsuppressed(Diagnostic diagnostic);

		/// <summary>
		/// Gets one or more add suppression or remove suppression fixes for the specified diagnostics represented as a list of <see cref="CodeAction"/>'s.
		/// </summary>
		/// <returns>A list of zero or more potential <see cref="CodeFix"/>'es. It is also safe to return null if there are none.</returns>
		Task<IEnumerable<CodeFix>> GetSuppressionsAsync(Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken);

		/// <summary>
		/// Gets one or more add suppression or remove suppression fixes for the specified no-location diagnostics represented as a list of <see cref="CodeAction"/>'s.
		/// </summary>
		/// <returns>A list of zero or more potential <see cref="CodeFix"/>'es. It is also safe to return null if there are none.</returns>
		Task<IEnumerable<CodeFix>> GetSuppressionsAsync(Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken);

		/// <summary>
		/// Gets an optional <see cref="FixAllProvider"/> that can fix all/multiple occurrences of diagnostics fixed by this fix provider.
		/// Return null if the provider doesn't support fix all/multiple occurrences.
		/// Otherwise, you can return any of the well known fix all providers from <see cref="WellKnownFixAllProviders"/> or implement your own fix all provider.
		/// </summary>
		FixAllProvider GetFixAllProvider();
	}
}

