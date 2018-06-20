//
// CSharpFindReferenceUsagesProvider.cs
//
// Author:
//       jason <jaimison@microsoft.com>
//
// Copyright (c) 2018 Microsoft
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Projects;
using MonoDevelop.Refactoring;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Core;
using System.Threading.Tasks;

namespace MonoDevelop.CSharp.Refactoring
{
	class CSharpFindReferenceUsagesProvider : FindReferenceUsagesProvider
	{
		public override async Task FindReferences(ProjectReference projectReference, SearchProgressMonitor monitor)
		{
			await Task.Run (delegate {
				var currentProject = IdeApp.ProjectOperations.CurrentSelectedProject;
				if (currentProject == null)
					return;
				var analysisProject = TypeSystemService.GetCodeAnalysisProject (currentProject);
				if (analysisProject == null)
					return;

				monitor.BeginTask (GettextCatalog.GetString ("Analyzing project"), analysisProject.Documents.Count ());
				Parallel.ForEach (analysisProject.Documents, async document => {
					try {
						var model = await document.GetSemanticModelAsync (monitor.CancellationToken).ConfigureAwait (false);
						if (monitor.CancellationToken.IsCancellationRequested)
							return;

						var root = await model.SyntaxTree.GetRootAsync (monitor.CancellationToken).ConfigureAwait (false);
						if (monitor.CancellationToken.IsCancellationRequested)
							return;

						root.DescendantNodes (node => {
							if (monitor.CancellationToken.IsCancellationRequested)
								return false;

							if (node is ExpressionSyntax expr) {
								var info = model.GetSymbolInfo (expr);
								if (info.Symbol == null || info.Symbol.ContainingAssembly == null)
									return true;
								if (projectReference.Reference.IndexOf (',') >= 0) {
									if (!string.Equals (info.Symbol.ContainingAssembly.ToString (), projectReference.Reference, StringComparison.OrdinalIgnoreCase))
										return true;
								} else {
									if (!info.Symbol.ContainingAssembly.ToString ().StartsWith (projectReference.Reference, StringComparison.OrdinalIgnoreCase))
										return true;
								}
								monitor.ReportResult (new MemberReference (info.Symbol, document.FilePath, node.Span.Start, node.Span.Length));
								return false;
							}
							return true;
						}).Count ();
					} finally {
						monitor.Step ();
					}
				});
				monitor.EndTask ();
			});		
		}
	}
}
