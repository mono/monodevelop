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
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.FindInFiles;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp;
using System.Threading;
using MonoDevelop.SourceEditor;

namespace MonoDevelop.CSharp.Highlighting
{
	public class HighlightUsagesExtension : AbstractUsagesExtension<ResolveResult>
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

		protected override bool TryResolve (out ResolveResult resolveResult)
		{
			AstNode node;
			resolveResult = null;
			if (!Document.TryResolveAt (Document.Editor.Caret.Location, out resolveResult, out node)) {
				return false;
			}
			if (node is PrimitiveType) {
				return false;
			}
			return true;
		}


		protected override IEnumerable<MemberReference> GetReferences (ResolveResult resolveResult, CancellationToken token)
		{
			var finder = new MonoDevelop.CSharp.Refactoring.CSharpReferenceFinder ();
			if (resolveResult is MemberResolveResult) {
				finder.SetSearchedMembers (new [] { ((MemberResolveResult)resolveResult).Member });
			} else if (resolveResult is TypeResolveResult) {
				finder.SetSearchedMembers (new [] { resolveResult.Type });
			} else if (resolveResult is MethodGroupResolveResult) { 
				finder.SetSearchedMembers (((MethodGroupResolveResult)resolveResult).Methods);
			} else if (resolveResult is NamespaceResolveResult) { 
				finder.SetSearchedMembers (new [] { ((NamespaceResolveResult)resolveResult).Namespace });
			} else if (resolveResult is LocalResolveResult) { 
				finder.SetSearchedMembers (new [] { ((LocalResolveResult)resolveResult).Variable });
			} else if (resolveResult is NamedArgumentResolveResult) { 
				finder.SetSearchedMembers (new [] { ((NamedArgumentResolveResult)resolveResult).Parameter });
			} else {
				return EmptyList;
			}

			try {
				return new List<MemberReference> (finder.FindInDocument (Document, token));
			} catch (Exception e) {
				LoggingService.LogError ("Error in highlight usages extension.", e);
			}
			return EmptyList;
		}
	}
}

