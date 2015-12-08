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
using ICSharpCode.NRefactory6.CSharp.Features.GotoDefinition;
using Mono.Posix;
using MonoDevelop.Ide.TypeSystem;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Refactoring
{
	class GotoDeclarationHandler : CommandHandler
	{
		internal static MonoDevelop.Ide.FindInFiles.SearchResult GetJumpTypePartSearchResult (Microsoft.CodeAnalysis.ISymbol part, Microsoft.CodeAnalysis.Location location)
		{
			var provider = new MonoDevelop.Ide.FindInFiles.FileProvider (location.SourceTree.FilePath);
			var doc = TextEditorFactory.CreateNewDocument ();
			doc.Text = provider.ReadString ();
			int position = location.SourceSpan.Start;
			while (position + part.Name.Length < doc.Length) {
				if (doc.GetTextAt (position, part.Name.Length) == part.Name)
					break;
				position++;
			}
			return new MonoDevelop.Ide.FindInFiles.SearchResult (provider, position, part.Name.Length);
		}

		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			Run (doc);
		}

		public static void Run (MonoDevelop.Ide.Gui.Document doc)
		{
			GoToDefinitionService.TryGoToDefinition (doc.AnalysisDocument, doc.Editor.CaretOffset, default(CancellationToken));
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
