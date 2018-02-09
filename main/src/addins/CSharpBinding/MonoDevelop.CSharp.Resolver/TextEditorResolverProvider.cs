//
// TextEditorResolverProvider.cs
//
// Author:
//       David Karlaš <david.karlas@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc (http://www.xamarin.com)
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
using MonoDevelop.Ide.Gui.Content;
using Microsoft.CodeAnalysis;
using MonoDevelop.Ide.Editor;
using System.Linq;

namespace MonoDevelop.CSharp.Resolver
{
	class TextEditorResolverProvider : ITextEditorResolverProvider
	{
		#region ITextEditorResolverProvider implementation

		public ISymbol GetLanguageItem (MonoDevelop.Ide.Gui.Document document, int offset, out DocumentRegion expressionRegion)
		{
			expressionRegion = DocumentRegion.Empty;
			var parsedDocument = document.ParsedDocument;
			if (parsedDocument == null)
				return null;
			var model = parsedDocument.GetAst<SemanticModel> ();
			if (model == null)
				return null;
			foreach (var symbol in model.LookupSymbols (offset)) {
				var firstDeclaration = symbol.DeclaringSyntaxReferences.FirstOrDefault ();
				if (firstDeclaration != null) {
					expressionRegion = new DocumentRegion (
						document.Editor.OffsetToLocation (firstDeclaration.Span.Start),
						document.Editor.OffsetToLocation (firstDeclaration.Span.End));
				}
				return symbol;
			}
			return null;
		}

		public ISymbol GetLanguageItem (MonoDevelop.Ide.Gui.Document document, int offset, string identifier)
		{
			if (document.ParsedDocument == null)
				return null;
			var model = document.ParsedDocument.GetAst<SemanticModel> ();
			if (model == null)
				return null;

			int index = identifier.LastIndexOf ("`", System.StringComparison.Ordinal);
			int arity = 0;
			if (index != -1) {
				try {
					arity = int.Parse (identifier.Substring (index + 1));
				} catch {
					return null;
				}
				identifier = identifier.Remove (index);
			}

			foreach (var symbol in model.LookupSymbols (offset, name: identifier)) {
				var typeSymbol = symbol as INamedTypeSymbol;
				if (typeSymbol != null && (arity == 0 || arity == typeSymbol.Arity)) {
					return symbol;
				}
				var namespaceSymbol = symbol as INamespaceSymbol;
				if (namespaceSymbol != null) {
					return namespaceSymbol;
				}
			}
			return null;
		}

		#endregion
	}
}
