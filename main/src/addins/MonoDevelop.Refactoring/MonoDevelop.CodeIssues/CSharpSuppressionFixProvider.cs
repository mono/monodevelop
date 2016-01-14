//
// CSharpSuppressionFixProvider.cs
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;
using MonoDevelop.Core;

namespace MonoDevelop.CodeIssues
{
	class CSharpSuppressionFixProvider : ISuppressionFixProvider
	{
		static readonly Type typeInfo;
		static readonly MethodInfo canBeSuppressedOrUnsuppressedMethod;
		static readonly MethodInfo getFixAllProviderMethod;
		static readonly MethodInfo getSuppressionsAsync1Method;
		static readonly MethodInfo getSuppressionsAsync2Method;

		object instance;

		public static ISuppressionFixProvider Instance;

		static CSharpSuppressionFixProvider ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CSharp.CodeFixes.Suppression.CSharpSuppressionCodeFixProvider, Microsoft.CodeAnalysis.CSharp.Features", true);

			if (typeInfo == null) 
				LoggingService.LogError ("CSharpSuppressionCodeFixProvider not found.");

			canBeSuppressedOrUnsuppressedMethod = typeInfo.GetMethod ("CanBeSuppressedOrUnsuppressed");
			if (canBeSuppressedOrUnsuppressedMethod == null)
				LoggingService.LogError ("CanBeSuppressedOrUnsuppressed not found.");
			getFixAllProviderMethod = typeInfo.GetMethod ("GetFixAllProvider");
			if (getFixAllProviderMethod == null)
				LoggingService.LogError ("GetFixAllProvider not found.");
				              
			getSuppressionsAsync1Method = typeInfo.GetMethod ("GetSuppressionsAsync", new [] { typeof(Project), typeof(IEnumerable<Diagnostic>), typeof(CancellationToken) });
			if (getSuppressionsAsync1Method == null)
				LoggingService.LogError ("GetSuppressionsAsync1 not found.");
			getSuppressionsAsync2Method = typeInfo.GetMethod ("GetSuppressionsAsync", new [] { typeof(Document), typeof(TextSpan), typeof(IEnumerable<Diagnostic>), typeof(CancellationToken) });
			if (getSuppressionsAsync2Method == null)
				LoggingService.LogError ("GetSuppressionsAsync2 not found.");
			Instance = new CSharpSuppressionFixProvider ();
		}

		public CSharpSuppressionFixProvider ()
		{
			instance = Activator.CreateInstance (typeInfo, true);
		}

		public bool CanBeSuppressedOrUnsuppressed (Diagnostic diagnostic)
		{
			return (bool)canBeSuppressedOrUnsuppressedMethod.Invoke (instance, new [] { diagnostic } );
		}

		public FixAllProvider GetFixAllProvider ()
		{
			return (FixAllProvider)canBeSuppressedOrUnsuppressedMethod.Invoke (instance, null);
		}

		public Task<IEnumerable<CodeFix>> GetSuppressionsAsync (Project project, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var o2 = getSuppressionsAsync1Method.Invoke (instance, new object[] { project, diagnostics, cancellationToken } );
			var task = (Task)o2;

			var propertyInfo = task.GetType ().GetProperty ("Result");
			var result = (IEnumerable)propertyInfo.GetValue (task);

			List<CodeFix> wrappedResult = new List<CodeFix> ();
			foreach (var o in result)
				wrappedResult.Add (new CodeFix (o));
			return Task.FromResult((IEnumerable<CodeFix>)wrappedResult);
		}

		public Task<IEnumerable<CodeFix>> GetSuppressionsAsync (Document document, TextSpan span, IEnumerable<Diagnostic> diagnostics, CancellationToken cancellationToken)
		{
			var o2 = getSuppressionsAsync2Method.Invoke (instance, new object [] { document, span, diagnostics, cancellationToken });
			var task = (Task)o2;


			var propertyInfo = task.GetType ().GetProperty ("Result");
			var result = (IEnumerable)propertyInfo.GetValue (task);

			List<CodeFix> wrappedResult = new List<CodeFix> ();
			foreach (var o in result)
				wrappedResult.Add (new CodeFix (o));
			return Task.FromResult((IEnumerable<CodeFix>)wrappedResult);
		}
	}
}

