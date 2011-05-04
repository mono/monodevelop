// 
// ASPNetReferenceFinder.cs
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
using MonoDevelop.AspNet;
using MonoDevelop.CSharp.Completion;
using MonoDevelop.AspNet.Parser;
using MonoDevelop.AspNet.Gui;
using System.Linq;


namespace MonoDevelop.CSharp.Refactoring
{
	public class ASPNetReferenceFinder : ReferenceFinder
	{
		public ASPNetReferenceFinder ()
		{
			IncludeDocumentation = true;
		}
		
		IEnumerable<MemberReference> SearchMember (INode member, ProjectDom dom, FilePath fileName, Mono.TextEditor.TextEditorData editor, Mono.TextEditor.Document buildDocument, List<LocalDocumentInfo.OffsetInfo> offsetInfos, ParsedDocument parsedDocument)
		{
			var resolver = new NRefactoryResolver (dom, parsedDocument.CompilationUnit, ICSharpCode.OldNRefactory.SupportedLanguage.CSharp, editor, fileName);
			
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (buildDocument, member);
			visitor.IncludeXmlDocumentation = IncludeDocumentation;
			visitor.RunVisitor (resolver);
			
			foreach (var result in visitor.FoundReferences) {
				var offsetInfo = offsetInfos.FirstOrDefault (info => info.ToOffset <= result.Position && result.Position < info.ToOffset + info.Length);
				if (offsetInfo == null)
					continue;
				var offset = offsetInfo.FromOffset + result.Position - offsetInfo.ToOffset;
				var loc = editor.OffsetToLocation (offset);
				yield return new MemberReference (null, fileName, offset, loc.Line, loc.Column, result.Name, null);
			}
		}
		
		public override IEnumerable<MemberReference> FindReferences (ProjectDom dom, FilePath fileName, IEnumerable<INode> searchedMembers)
		{
			var editor = TextFileProvider.Instance.GetTextEditorData (fileName);
			AspNetAppProject project = dom.Project as AspNetAppProject;
			if (project == null)
				yield break;
			
			var unit = AspNetParserService.GetCompileUnit (project, fileName, true);
			if (unit == null)
				yield break;
			var refman = new DocumentReferenceManager (project);
			
			var parsedAspDocument = (AspNetParsedDocument)new AspNetParser ().Parse (dom, fileName, editor.Text);
			refman.Doc = parsedAspDocument;
			
			var usings = refman.GetUsings ();
			var documentInfo = new DocumentInfo (dom, unit, usings, refman.GetDoms ());
			
			var builder = new AspLanguageBuilder ();
			
			
			var buildDocument = new Mono.TextEditor.Document ();
			var offsetInfos = new List<LocalDocumentInfo.OffsetInfo> ();
			buildDocument.Text = builder.BuildDocumentString (documentInfo, editor, offsetInfos, true);
			var parsedDocument = AspLanguageBuilder.Parse (dom, fileName, buildDocument.Text);
			foreach (var member in searchedMembers) {
				foreach (var reference in SearchMember (member, dom, fileName, editor, buildDocument, offsetInfos, parsedDocument)) {
					yield return reference;
				}
			}
		}
	}
}

