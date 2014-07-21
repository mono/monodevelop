// 
// RenameHandler.cs
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
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using MonoDevelop.Core;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor;


using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Refactoring.Rename
{
	class RenameHandler : CommandHandler
	{
		public void UpdateCommandInfo (CommandInfo ci)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			var analysisDocument = doc.AnalysisDocument;
			if (analysisDocument == null)
				return;
			var info = CurrentRefactoryOperationsHandler.GetSymbolInfoAsync (analysisDocument, doc.Editor.CaretOffset).Result;
			if (!CanRename (info.Symbol))
				ci.Bypass = true;
		}

		internal static bool CanRename (ISymbol symbol)
		{
			if (symbol == null)
				return false;
			switch (symbol.Kind) {
			case SymbolKind.Local:
			case SymbolKind.Parameter:
			case SymbolKind.NamedType:
			case SymbolKind.Namespace:
			case SymbolKind.Method:
			case SymbolKind.Field:
			case SymbolKind.Property:
			case SymbolKind.Event:
			case SymbolKind.Label:
			case SymbolKind.TypeParameter:
			case SymbolKind.RangeVariable:
				return true;
			}
			return false;
		}
		
		protected override void Run (object data)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null)
				return;
			Run (doc);
		}

		internal void Run (TextEditor editor, DocumentContext ctx)
		{
			var info = CurrentRefactoryOperationsHandler.GetSymbolInfoAsync (ctx.AnalysisDocument, editor.CaretOffset).Result;
			if (!CanRename (info.Symbol))
				return;
			new RenameRefactoring ().Rename (info.Symbol);
		}
	}
}
