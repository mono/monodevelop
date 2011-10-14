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
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Ide.FindInFiles;
using System.Linq;
using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.Resolver;
using System.IO;
using MonoDevelop.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.CSharp.Refactoring
{
	using MonoDevelop.Projects;
	public class CSharpReferenceFinder : ReferenceFinder
	{
		ICSharpCode.NRefactory.CSharp.Resolver.FindReferences refFinder = new ICSharpCode.NRefactory.CSharp.Resolver.FindReferences ();
		List<object> searchedMembers;
		List<Tuple<IProjectContent, FilePath>> files = new List<Tuple<IProjectContent, FilePath>> ();
		List<Tuple<IProjectContent, FilePath, MonoDevelop.Ide.Gui.Document>> openDocuments = new List<Tuple<IProjectContent, FilePath, MonoDevelop.Ide.Gui.Document>> ();
		
		string memberName;
		
		public CSharpReferenceFinder ()
		{
			IncludeDocumentation = true;
		}
		
		public override void SetSearchedMembers (IEnumerable<object> searchedMembers)
		{
			this.searchedMembers = new List<object> (searchedMembers);
			var firstMember = searchedMembers.FirstOrDefault ();
			if (firstMember is INamedElement)
				memberName = ((INamedElement)firstMember).Name;
			if (firstMember is string)
				memberName = firstMember.ToString ();
			if (firstMember is IVariable)
				memberName = ((IVariable)firstMember).Name;
		}
		
		public override void SetPossibleFiles (IEnumerable<Tuple<IProjectContent, FilePath>> files)
		{
			foreach (var tuple in files) {
				var openDocument = IdeApp.Workbench.GetDocument (tuple.Item2);
				if (openDocument == null) {
					this.files.Add (tuple);
				} else {
					this.openDocuments.Add (Tuple.Create (tuple.Item1, tuple.Item2, openDocument));
				}
			}
		}
		
		MemberReference GetReference (ResolveResult result, AstNode node, string fileName, Mono.TextEditor.TextEditorData editor)
		{
			if (result == null || result.IsError)
				return null;
			object valid = null;
			if (result is MethodGroupResolveResult) {
				valid = ((MethodGroupResolveResult)result).Methods.FirstOrDefault (m => searchedMembers.Any (member => member is IMethod && ((IMethod)member).Region == m.Region));
			} else if (result is MemberResolveResult) {
				var foundMember = ((MemberResolveResult)result).Member;
				valid = searchedMembers.FirstOrDefault (member => member is IMember && ((IMember)member).Region == foundMember.Region);
			} else if (result is NamespaceResolveResult) {
				var ns = ((NamespaceResolveResult)result).NamespaceName;
				valid = searchedMembers.FirstOrDefault (n => n is string && n.ToString () == ns);
			} else if (result is LocalResolveResult) {
				var ns = ((LocalResolveResult)result).Variable;
				valid = searchedMembers.FirstOrDefault (n => n is IVariable && ((IVariable)n).Region == ns.Region);
			} else if (result is TypeResolveResult) {
				valid = searchedMembers.FirstOrDefault (n => n is IType && result.Type.Equals ((IType)n));
			}
			
			if (node is InvocationExpression)
				node = ((InvocationExpression)node).Target;
			
			if (node is MemberReferenceExpression)
				node = ((MemberReferenceExpression)node).MemberNameToken;
			if (node is MemberDeclaration && (searchedMembers.First () is IMember)) 
				node = ((MemberDeclaration)node).NameToken;
			
			if (node is TypeDeclaration && (searchedMembers.First () is IType)) 
				node = ((TypeDeclaration)node).NameToken;
			
			if (node is ParameterDeclaration && (searchedMembers.First () is IParameter)) 
				node = ((ParameterDeclaration)node).NameToken;
			
			var region = new DomRegion (fileName, node.StartLocation, node.EndLocation);
			
			return new MemberReference (valid as IEntity, region, editor.LocationToOffset (region.BeginLine, region.BeginColumn), memberName.Length);
		}
		
		public IEnumerable<MemberReference> FindInDocument (MonoDevelop.Ide.Gui.Document doc)
		{
			if (string.IsNullOrEmpty (memberName))
				return Enumerable.Empty<MemberReference> ();
			var editor = doc.Editor;
			var unit = doc.ParsedDocument.Annotation<CompilationUnit> ();
			var file = doc.ParsedDocument.Annotation<CSharpParsedFile> ();
			var ctx = doc.TypeResolveContext;
			var result = new List<MemberReference> ();
			
			foreach (var obj in searchedMembers) {
				if (obj is IEntity) {
					refFinder.FindReferencesInFile (refFinder.GetSearchScopes ((IEntity)obj), file, unit, ctx, (astNode, r) => result.Add (GetReference (r, astNode, editor.FileName, editor)));
				} else if (obj is IVariable) {
					refFinder.FindLocalReferences ((IVariable)obj, file, unit, ctx, (astNode, r) => result.Add (GetReference (r, astNode, editor.FileName, editor)));
				}
			}
			return result;
		}
		
		public override IEnumerable<MemberReference> FindReferences ()
		{
			var entity = searchedMembers.First () as IEntity;
			var scopes = refFinder.GetSearchScopes (entity);
			List<MemberReference> refs = new List<MemberReference> ();
			Project prj = null;
			ITypeResolveContext ctx = null;
			foreach (var opendoc in openDocuments) {
				refs.AddRange (FindInDocument (opendoc.Item3));
			}
			foreach (var file in files) {
				var editor = TextFileProvider.Instance.GetTextEditorData (file.Item2);
				
				var unit = new CSharpParser ().Parse (editor);
				if (unit == null)
					continue;
				
				var visitor = new TypeSystemConvertVisitor (file.Item1, file.Item2);
				var curPrj = file.Item1.Annotation<Project> ();
				if (prj != curPrj) {
					prj = curPrj;
					ctx = prj != null ? TypeSystemService.GetContext (prj) : null;
				}
				var curCtx = ctx ?? file.Item1;
				unit.AcceptVisitor (visitor, null);
				refFinder.FindReferencesInFile (scopes, visitor.ParsedFile, unit, curCtx, (astNode, result) => refs.Add (GetReference (result, astNode, file.Item2, editor)));
			}
			
			return refs;
		}
	}
}

