// 
// FindReferencesHandler.cs
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

using System;
using System.Linq;
using System.Threading;
using MonoDevelop.Components.Commands;
using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Refactoring;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.TypeSystem;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.Ide.Tasks;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MonoDevelop.CSharp.Refactoring
{
	class FindReferencesHandler
	{
		public void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null) {
				info.Enabled = false;
				return;
			}
			var pd = doc.ParsedDocument.GetAst<SemanticModel> ();
			info.Enabled = pd != null;
		}

		public void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;

			var info = RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor).Result;
			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (sym != null)
				RefactoringService.FindReferencesAsync (sym.GetDocumentationCommentId ());
		}
	}

	

	class FindAllReferencesHandler
	{

		public void Update (CommandInfo info)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || doc.ParsedDocument == null) {
				info.Enabled = false;
				return;
			}
			var pd = doc.ParsedDocument.GetAst<SemanticModel> ();
			info.Enabled = pd != null;
		}

		public void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			
			var info = RefactoringSymbolInfo.GetSymbolInfoAsync (doc, doc.Editor).Result;
			var sym = info.Symbol ?? info.DeclaredSymbol;
			if (sym != null)
				RefactoringService.FindAllReferencesAsync (sym.GetDocumentationCommentId ());
		}
	}

	class SearchResultComparer : IEqualityComparer<SearchResult>
	{
		public bool Equals (SearchResult x, SearchResult y)
		{
			return x.FileName == y.FileName &&
				        x.Offset == y.Offset &&
				        x.Length == y.Length;
		}

		public int GetHashCode (SearchResult obj)
		{
			int hash = 17;
			hash = hash * 23 + obj.Offset.GetHashCode ();
			hash = hash * 23 + obj.Length.GetHashCode ();
			hash = hash * 23 + (obj.FileName ?? "").GetHashCode ();
			return hash;
		}
	}
}
