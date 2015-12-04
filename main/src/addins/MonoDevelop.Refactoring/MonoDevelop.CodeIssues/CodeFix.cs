//
// CodeFix.cs
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
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using MonoDevelop.Core;

namespace MonoDevelop.CodeIssues
{
	public class CodeFix
	{
		readonly static Type typeInfo;
		readonly static FieldInfo projectField;
		readonly static FieldInfo actionField;
		readonly static FieldInfo diagnosticsField;

		object codeFixInstance;

		static CodeFix ()
		{
			typeInfo = Type.GetType ("Microsoft.CodeAnalysis.CodeFixes.CodeFix, Microsoft.CodeAnalysis.Workspaces", true);
			if (typeInfo == null) 
				LoggingService.LogError ("CodeFix not found.");

			projectField = typeInfo.GetField ("Project", BindingFlags.NonPublic | BindingFlags.Instance);
			if (projectField == null) 
				LoggingService.LogError ("Project field not found.");

			actionField = typeInfo.GetField ("Action", BindingFlags.NonPublic | BindingFlags.Instance);
			if (actionField == null) 
				LoggingService.LogError ("Action field not found.");
			
			diagnosticsField = typeInfo.GetField ("Diagnostics", BindingFlags.NonPublic | BindingFlags.Instance);
			if (diagnosticsField == null) 
				LoggingService.LogError ("Diagnostics field not found.");
			

		}

		public Diagnostic PrimaryDiagnostic {
			get {
				return Diagnostics [0];
			}
		}

		public Project Project { get { return (Project)projectField.GetValue (codeFixInstance); } }
		public CodeAction Action { get { return (CodeAction)actionField.GetValue (codeFixInstance); } }
		public ImmutableArray<Diagnostic> Diagnostics { get { return (ImmutableArray<Diagnostic>)diagnosticsField.GetValue (codeFixInstance); } }

		public CodeFix (object codeFixInstance)
		{
			this.codeFixInstance = codeFixInstance;
		}

		public CodeFix (Project project, CodeAction action, Diagnostic diagnostic)
		{
			codeFixInstance = Activator.CreateInstance (typeInfo, project, action, diagnostic); 
		}

		public CodeFix (Project project, CodeAction action, ImmutableArray<Diagnostic> diagnostics)
		{
			codeFixInstance = Activator.CreateInstance (typeInfo, project, action, diagnostics); 
		}
	}
}