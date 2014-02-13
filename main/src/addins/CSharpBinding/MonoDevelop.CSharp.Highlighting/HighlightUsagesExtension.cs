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
using System.Linq;
using MonoDevelop.Core;
using ICSharpCode.NRefactory.CSharp.Resolver;
using MonoDevelop.Ide.FindInFiles;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp;
using System.Threading.Tasks;
using System.Threading;
using Gtk;
using MonoDevelop.SourceEditor;

namespace MonoDevelop.CSharp.Highlighting
{
	class HighlightUsagesExtension : AbstractUsagesExtension
	{
		CSharpSyntaxMode syntaxMode;

		public override void Initialize ()
		{
			base.Initialize ();

			textEditorData.SelectionSurroundingProvider = new CSharpSelectionSurroundingProvider (Document);
			syntaxMode = new CSharpSyntaxMode (Document);
			textEditorData.Document.SyntaxMode = syntaxMode;
		}

		public override void Dispose ()
		{
			if (syntaxMode != null) {
				textEditorData.Document.SyntaxMode = null;
				syntaxMode.Dispose ();
				syntaxMode = null;
			}
			base.Dispose ();
		}

		protected override bool TryResolve (out object resolveResult)
		{
			ResolveResult result;
			AstNode node;
			resolveResult = null;
			if (!Document.TryResolveAt (Document.Editor.Caret.Location, out result, out node)) {
				return false;
			}
			if (node is PrimitiveType) {
				return false;
			}
			resolveResult = result;
			return true;
		}


		protected override IEnumerable<MemberReference> GetReferences (object resolveResult, CancellationToken token)
		{
			var result = (ResolveResult)resolveResult;

			var finder = new MonoDevelop.CSharp.Refactoring.CSharpReferenceFinder ();
			if (result is MemberResolveResult) {
				finder.SetSearchedMembers (new [] { ((MemberResolveResult)result).Member });
			} else if (result is TypeResolveResult) {
				finder.SetSearchedMembers (new [] { result.Type });
			} else if (result is MethodGroupResolveResult) { 
				finder.SetSearchedMembers (((MethodGroupResolveResult)result).Methods);
			} else if (result is NamespaceResolveResult) { 
				finder.SetSearchedMembers (new [] { ((NamespaceResolveResult)result).Namespace });
			} else if (result is LocalResolveResult) { 
				finder.SetSearchedMembers (new [] { ((LocalResolveResult)result).Variable });
			} else if (result is NamedArgumentResolveResult) { 
				finder.SetSearchedMembers (new [] { ((NamedArgumentResolveResult)result).Parameter });
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

