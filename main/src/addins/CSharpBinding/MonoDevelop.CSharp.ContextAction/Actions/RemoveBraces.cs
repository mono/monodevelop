// 
// RemoveBraces.cs
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
using MonoDevelop.Ide;

namespace MonoDevelop.CSharp.ContextAction
{
	public class RemoveBraces : CSharpContextAction
	{
		protected override string GetMenuText (CSharpContext context)
		{
			return GettextCatalog.GetString ("Remove braces");
		}
		
		protected override bool IsValid (CSharpContext context)
		{
			return GetBlockStatement (context) != null;
		}
		
		protected override void Run (CSharpContext context)
		{
			var block = GetBlockStatement (context);
			context.Document.Editor.Document.BeginAtomicUndo ();
			context.DoRemove (block.LBraceToken);
			context.DoRemove (block.RBraceToken);
			context.CommitChanges ();
			context.FormatText (ctx => ctx.Unit.GetNodeAt (block.Parent.StartLocation));
			context.Document.Editor.Document.EndAtomicUndo ();
		}
		
		BlockStatement GetBlockStatement (CSharpContext context)
		{
			var block = context.GetNode<BlockStatement> ();
			if (block == null || block.LBraceToken.IsNull || block.RBraceToken.IsNull)
				return null;
			if (block.Parent.Role == TypeDeclaration.MemberRole)
				return null;
			if (block.Statements.Count () != 1)
				return null;
			return block;
		}
	}
}

