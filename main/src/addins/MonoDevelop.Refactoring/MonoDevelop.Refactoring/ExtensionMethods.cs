//
// ExtensionMethods.cs
//
// Author:
//       Mike Krüger <mkrueger@xamarin.com>
//
// Copyright (c) 2013 Xamarin Inc. (http://xamarin.com)
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
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.Gui;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;

namespace MonoDevelop.Refactoring
{
    public static class ExtensionMethods
    {
		/// <summary>
		/// Returns a full C# syntax tree resolver which is shared between semantic highlighting, source analysis and refactoring.
		/// For code analysis tasks this should be used instead of generating an own resolver. Only exception is if a local resolving is done using a 
		/// resolve navigator.
		/// </summary>
		public static CSharpAstResolver GetSharedResolver (this Document document)
		{
			var parsedDocument = document.ParsedDocument;
			if (parsedDocument == null)
				return null;
			
			var unit       = parsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;
			if (unit == null || parsedFile == null)
				return null;
			
			var currentResolver = document.Annotation<CSharpAstResolver> ();
			
			if (currentResolver != null) {
				if (currentResolver.UnresolvedFile == parsedFile)
					return currentResolver;
				document.RemoveAnnotations<CSharpAstResolver> ();
			}

			var result = new CSharpAstResolver (document.Compilation, unit, parsedFile);
			document.AddAnnotation (result);
			return result;
		}	
    }
}