// 
// UseExplicitType.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2011 Novell, Inc (http://www.novell.com)
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.ContextAction
{
	public class UseExplicitType : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			return GettextCatalog.GetString ("Use explicit type");
		}

		VariableDeclarationStatement GetVariableDeclarationStatement (CSharpContext context)
		{
			var result = context.GetNode<VariableDeclarationStatement> ();
			if (result != null && result.Variables.Count == 1 && !result.Variables.First ().Initializer.IsNull && result.Type.Contains (context.Location.Line, context.Location.Column) && result.Type.IsMatch (new SimpleType ("var"))) {
				var resolver = context.Resolver;
				var resolveResult = resolver.Resolve (result.Variables.First ().Initializer.ToString (), context.Location);
				if (resolveResult == null || resolveResult.ResolvedType == null || string.IsNullOrEmpty (resolveResult.ResolvedType.FullName))
					return null;
				return result;
				
			}
			return null;
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			return GetVariableDeclarationStatement (context) != null;
		}
		
		protected override void Run (CSharpContext context)
		{
			var varDecl = GetVariableDeclarationStatement (context);
			var resolver = context.Resolver;
			var resolveResult = resolver.Resolve (varDecl.Variables.First ().Initializer.ToString (), context.Location);
			
			int offset = context.Document.Editor.LocationToOffset (varDecl.Type.StartLocation.Line, varDecl.Type.StartLocation.Column);
			int endOffset = context.Document.Editor.LocationToOffset (varDecl.Type.EndLocation.Line, varDecl.Type.EndLocation.Column);
			string text = context.OutputNode (ShortenTypeName (context.Document, resolveResult.ResolvedType), 0).Trim ();
			context.DoReplace (offset, endOffset - offset, text);
			context.CommitChanges ();
			context.Document.Editor.Caret.Offset = offset + text.Length;
		}
	}
}

