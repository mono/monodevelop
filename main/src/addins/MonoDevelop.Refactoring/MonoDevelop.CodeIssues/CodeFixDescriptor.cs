//
// CodeFixDescriptor.cs
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
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;

namespace MonoDevelop.CodeIssues
{
	class CodeFixDescriptor
	{
		readonly Type codeFixProviderType;
		readonly ExportCodeFixProviderAttribute attribute;
		ICodeFixProvider instance;

		public string Name {
			get {
				return attribute.Name;
			}
		}

		public string Language {
			get {
				return attribute.Language;
			}
		}

		internal CodeFixDescriptor (Type codeFixProviderType, ExportCodeFixProviderAttribute attribute)
		{
			if (codeFixProviderType == null)
				throw new ArgumentNullException ("codeFixProviderType");
			if (attribute == null)
				throw new ArgumentNullException ("attribute");
			this.codeFixProviderType = codeFixProviderType;
			this.attribute = attribute;
		}
		
		public ICodeFixProvider GetCodeFixProvider ()
		{
			if (instance == null)
				instance = (ICodeFixProvider)Activator.CreateInstance(codeFixProviderType);

			return instance;
		}

		public CodeDiagnosticDescriptor GetCodeDiagnosticDescriptor (string language)
		{
			var fixableIds = GetCodeFixProvider ().GetFixableDiagnosticIds ().ToList ();

			foreach (var descriptor in CodeDiagnosticService.GetCodeIssues (language)) {
				if (descriptor.GetProvider ().SupportedDiagnostics.Any (diagnostic => fixableIds.Contains (diagnostic.Id)))
					return descriptor;

			}
			return null;

		}
	}
}
