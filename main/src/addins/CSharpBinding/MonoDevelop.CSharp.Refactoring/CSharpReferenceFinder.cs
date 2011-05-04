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
using System.Linq;

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
			HashSet<int > positions = new HashSet<int> ();
			var editor = TextFileProvider.Instance.GetTextEditorData (fileName);
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (editor.Document);
			visitor.IncludeXmlDocumentation = IncludeDocumentation;
			visitor.Init (searchedMembers);
			if (!visitor.FileContainsMemberName ()) {
				yield break;
			}
			var doc = ProjectDomService.ParseFile (dom, fileName, () => editor.Text);
			if (doc == null || doc.CompilationUnit == null)
				yield break;
			var resolver = new NRefactoryResolver (dom, doc.CompilationUnit, ICSharpCode.OldNRefactory.SupportedLanguage.CSharp, editor, fileName);

			visitor.ParseFile (resolver);
			visitor.RunVisitor (resolver);
			foreach (var reference in visitor.FoundReferences) {
				if (positions.Contains (reference.Position))
					continue;
				positions.Add (reference.Position);
				yield return reference;
			}
			visitor.ClearParsers ();
		}
	}
}

