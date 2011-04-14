// 
// RefactoringOptions.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
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

using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui;
 
using System.Text;
using MonoDevelop.Projects.Text;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring
{
	public class RefactoringOptions
	{
		public ProjectDom Dom {
			get;
			set;
		}
		
		public Document Document {
			get;
			set;
		}
		
		public MonoDevelop.Projects.Dom.INode SelectedItem {
			get;
			set;
		}
		
		public ResolveResult ResolveResult {
			get;
			set;
		}
		
		// file provider for unit test purposes.
		public ITextFileProvider TestFileProvider {
			get;
			set;
		}
		
		public string MimeType {
			get {
				return DesktopService.GetMimeTypeForUri (Document.FileName);
			}
		}
		
		public Mono.TextEditor.TextEditorData GetTextEditorData ()
		{
			return Document.Editor;
		}
		
		public INRefactoryASTProvider GetASTProvider ()
		{
			return RefactoringService.GetASTProvider (MimeType);
		}
		
		public IResolver GetResolver ()
		{
			MonoDevelop.Projects.Dom.Parser.IParser domParser = GetParser ();
			if (domParser == null)
				return null;
			return domParser.CreateResolver (Dom, Document, Document.FileName);
		}
		
		public MonoDevelop.Projects.Dom.Parser.IParser GetParser ()
		{
			return ProjectDomService.GetParser (Document.FileName);
		}
		
		public AstNode ParseMember (IMember member)
		{
			if (member == null || member.BodyRegion.IsEmpty)
				return null;
			INRefactoryASTProvider provider = GetASTProvider ();
			if (provider == null) 
				return null;
			
			int start = Document.Editor.Document.LocationToOffset (member.BodyRegion.Start.Line, member.BodyRegion.Start.Column);
			int end = Document.Editor.Document.LocationToOffset (member.BodyRegion.End.Line, member.BodyRegion.End.Column);
			string memberBody = Document.Editor.GetTextBetween (start, end);
			return provider.ParseText (memberBody);
		}
		
		public static string GetWhitespaces (Document document, int insertionOffset)
		{
			StringBuilder result = new StringBuilder ();
			for (int i = insertionOffset; i < document.Editor.Length; i++) {
				char ch = document.Editor.GetCharAt (i);
				if (ch == ' ' || ch == '\t') {
					result.Append (ch);
				} else {
					break;
				}
			}
			return result.ToString ();
		}
		
		public static string GetIndent (Document document, IMember member)
		{
			return GetWhitespaces (document, document.Editor.Document.LocationToOffset (member.Location.Line, 1));
		}
		public string GetWhitespaces (int insertionOffset)
		{
			return GetWhitespaces (Document, insertionOffset);
		}
		
		public string GetIndent (IMember member)
		{
			return GetIndent (Document, member);
		}
		
		public IReturnType ShortenTypeName (IReturnType fullyQualifiedTypeName)
		{
			return Document.ParsedDocument.CompilationUnit.ShortenTypeName (fullyQualifiedTypeName, Document.Editor.Caret.Line, Document.Editor.Caret.Column);
		}
		
		public ParsedDocument ParseDocument ()
		{
			return ProjectDomService.Parse (Dom.Project, Document.FileName, Document.Editor.Text);
		}
	}
}
