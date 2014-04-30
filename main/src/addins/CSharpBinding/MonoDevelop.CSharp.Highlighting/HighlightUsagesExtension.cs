// 
// HighlightUsagesExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Core;
using MonoDevelop.Ide.FindInFiles;
using System.Threading;
using MonoDevelop.SourceEditor;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Microsoft.CodeAnalysis.FindSymbols;
using MonoDevelop.Ide.TypeSystem;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace MonoDevelop.CSharp.Highlighting
{
	class UsageData
	{
		public RefactoringSymbolInfo SymbolInfo;
		public Document Document;

		public ISymbol Symbol {
			get { return SymbolInfo.Symbol ?? SymbolInfo.DeclaredSymbol; }
		}
	}
	
	class HighlightUsagesExtension : AbstractUsagesExtension<UsageData>
	{
		CSharpSyntaxMode syntaxMode;

		public override void Initialize ()
		{
			base.Initialize ();

			TextEditorData.SelectionSurroundingProvider = new CSharpSelectionSurroundingProvider (Document);
			syntaxMode = new CSharpSyntaxMode (Document);
			TextEditorData.Document.SyntaxMode = syntaxMode;
		}

		public override void Dispose ()
		{
			if (syntaxMode != null) {
				TextEditorData.Document.SyntaxMode = null;
				syntaxMode.Dispose ();
				syntaxMode = null;
			}
			base.Dispose ();
		}

		protected async override Task<UsageData> ResolveAsync (CancellationToken token)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null) {
				return new UsageData ();
			}

			var document = doc.AnalysisDocument;
			var symbolInfo = await CurrentRefactoryOperationsHandler.GetSymbolInfoAsync (document, doc.Editor.Caret.Offset, token);
			return new UsageData {
				Document = document,
				SymbolInfo = symbolInfo
			};
		}

		protected override IEnumerable<MemberReference> GetReferences (UsageData resolveResult, CancellationToken token)
		{
			if (resolveResult.Symbol == null)
				yield break;
			var doc = resolveResult.Document;
			var documents = ImmutableHashSet.Create (doc); 
			var symbol = resolveResult.Symbol;
			foreach (var loc in symbol.Locations) {
				if (loc.FilePath == doc.FilePath)
					yield return new MemberReference (symbol, doc.FilePath, loc.SourceSpan.Start, loc.SourceSpan.Length) {
						ReferenceUsageType = ReferenceUsageType.Declariton	
					};
			}
			foreach (var mref in SymbolFinder.FindReferencesAsync (symbol, RoslynTypeSystemService.Workspace.CurrentSolution, documents, token).Result) {
				foreach (var loc in mref.Locations) {
					yield return new MemberReference (symbol, doc.FilePath, loc.Location.SourceSpan.Start, loc.Location.SourceSpan.Length) {
						ReferenceUsageType = ReferenceUsageType.Read	
					};
				}
			}
		}
	}
}

