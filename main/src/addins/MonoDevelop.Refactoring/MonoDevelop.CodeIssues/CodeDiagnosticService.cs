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
using System.Linq;
using MonoDevelop.AnalysisCore;
using System.Collections.Generic;
using MonoDevelop.Ide.Gui;
using System.Threading;
using MonoDevelop.CodeIssues;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using ICSharpCode.NRefactory6.CSharp.Refactoring;
using System.Diagnostics;
using MonoDevelop.Core;
using System.Threading.Tasks;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Editor;
using Mono.Addins;

namespace MonoDevelop.CodeIssues
{
	static class CodeDiagnosticService
	{
		readonly static List<CodeDiagnosticProvider> providers = new List<CodeDiagnosticProvider> ();

		static CodeDiagnosticService ()
		{	
			providers.Add (new BuiltInCodeDiagnosticProvider ()); 
			AddinManager.AddExtensionNodeHandler ("/MonoDevelop/Refactoring/CodeDiagnosticProvider", delegate(object sender, ExtensionNodeEventArgs args) {
				var node = (TypeExtensionNode)args.ExtensionNode;
				switch (args.Change) {
				case ExtensionChange.Add:
					providers.Add ((CodeDiagnosticProvider)node.CreateInstance ());
					break;
				}
			});
		}

		public async static Task<IEnumerable<CodeDiagnosticDescriptor>> GetCodeIssuesAsync (DocumentContext documentContext, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var result = new List<CodeDiagnosticDescriptor> ();
			foreach (var provider in providers) {
				result.AddRange (await provider.GetCodeIssuesAsync (documentContext, language, cancellationToken).ConfigureAwait (false));
			}

			return result;
		}

		public async static Task<IEnumerable<CodeFixDescriptor>> GetCodeFixDescriptorAsync (DocumentContext documentContext, string language, CancellationToken cancellationToken = default (CancellationToken))
		{
			var result = new List<CodeFixDescriptor> ();
			foreach (var provider in providers) {
				result.AddRange (await provider.GetCodeFixDescriptorAsync (documentContext, language, cancellationToken).ConfigureAwait (false));
			}
			return result;
		}
	}
}