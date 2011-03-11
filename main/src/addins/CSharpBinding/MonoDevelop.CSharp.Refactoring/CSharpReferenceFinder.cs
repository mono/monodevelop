// 
// CSharpReferenceFinder.cs
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
using System.Collections.Generic;

using MonoDevelop.Core;
using MonoDevelop.Ide;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Ide.FindInFiles;

namespace MonoDevelop.CSharp.Refactoring
{
	public class CSharpReferenceFinder : ReferenceFinder
	{
		public CSharpReferenceFinder ()
		{
			IncludeDocumentation = true;
		}

		public override IEnumerable<MemberReference> FindReferences (ProjectDom dom, FilePath fileName, IEnumerable<INode> searchedMembers)
		{
			foreach (var member in searchedMembers) {
				foreach (var reference in FindReferences (dom, fileName, member)) {
					yield return reference;
				}
			}
		}
		
		IEnumerable<MemberReference> FindReferences (ProjectDom dom, FilePath fileName, INode member)
		{
			var editor = TextFileProvider.Instance.GetTextEditorData (fileName);
			var doc    = ProjectDomService.GetParsedDocument (dom, fileName);
			
			if (doc == null || doc.CompilationUnit == null)
				return null;
			var resolver = new NRefactoryResolver (dom, doc.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, editor, fileName);
			
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (editor.Document, resolver, member);
			visitor.IncludeXmlDocumentation = IncludeDocumentation;
			visitor.RunVisitor ();
			return visitor.FoundReferences;
		}
	}
}

