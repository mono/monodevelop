// 
// GotoDeclarationHandler.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Threading.Tasks;
using MonoDevelop.Ide.Editor;
using System.Threading;
using System;
using Microsoft.CodeAnalysis.CSharp;

namespace MonoDevelop.Refactoring
{
	public enum RefactoryCommands
	{
		CurrentRefactoryOperations,
		GotoDeclaration, // in 'referenced' in IdeViMode.cs as string
		FindReferences,
		FindAllReferences,
		FindDerivedClasses,
		DeclareLocal,
		RemoveUnusedImports,
		SortImports,
		RemoveSortImports,
		CreateMethod,
		IntroduceConstant,
		IntegrateTemporaryVariable,
		ImportSymbol,
		QuickFix,
		Resolve
	}

	class RefactoringSymbolInfo
	{
		public readonly static RefactoringSymbolInfo Empty = new RefactoringSymbolInfo(new SymbolInfo());

		SymbolInfo symbolInfo;

		public ISymbol Symbol {
			get {
				return symbolInfo.Symbol;
			}
		}

		public ImmutableArray<ISymbol> CandidateSymbols {
			get {
				return symbolInfo.CandidateSymbols;
			}
		}

		public ISymbol DeclaredSymbol {
			get;
			internal set;
		}

		public RefactoringSymbolInfo (SymbolInfo symbolInfo)
		{
			this.symbolInfo = symbolInfo;
		}

		public static async Task<RefactoringSymbolInfo> GetSymbolInfoAsync (DocumentContext document, int offset, CancellationToken cancellationToken = default(CancellationToken))
		{
			if (document == null)
				throw new ArgumentNullException ("document");
			if (document.ParsedDocument == null)
				return RefactoringSymbolInfo.Empty;
			var unit = document.ParsedDocument.GetAst<SemanticModel> ();
			if (unit != null) {
				var root = await unit.SyntaxTree.GetRootAsync (cancellationToken).ConfigureAwait (false);
				try {
					var token = root.FindToken (offset);
					var symbol = unit.GetSymbolInfo (token.Parent);
					return new RefactoringSymbolInfo (symbol) {
						DeclaredSymbol = token.IsKind (SyntaxKind.IdentifierToken) ? unit.GetDeclaredSymbol (token.Parent) : null
					};
				} catch (Exception) {
					return RefactoringSymbolInfo.Empty;
				}
			}
			return RefactoringSymbolInfo.Empty;
		}
	}


	class GotoDeclarationHandler : CommandHandler
	{
		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			JumpToDeclaration (doc, RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor.CaretOffset).Result);
		}
		
		public static void JumpToDeclaration (MonoDevelop.Ide.Gui.Document doc, RefactoringSymbolInfo info)
		{
			if (info.Symbol != null)
				IdeApp.ProjectOperations.JumpToDeclaration (info.Symbol, doc.Project);
			if (info.CandidateSymbols.Length > 0)
				IdeApp.ProjectOperations.JumpToDeclaration (info.CandidateSymbols[0], doc.Project);
		}
	}
}
