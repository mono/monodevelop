// 
// CodeGenerationOptions.cs
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

using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;

namespace MonoDevelop.CodeGeneration
{
	public class CodeGenerationOptions
	{
		public ProjectDom Dom {
			get;
			set;
		}
		
		public Document Document {
			get;
			set;
		}
		
		public IType EnclosingType {
			get;
			set;
		}
		
		public IMember EnclosingMember {
			get;
			set;
		}
		
		public string MimeType {
			get {
				return DesktopService.GetMimeTypeForUri (Document.FileName);
			}
		}
		
		public INRefactoryASTProvider GetASTProvider ()
		{
			return RefactoringService.GetASTProvider (MimeType);
		}
		
		public MonoDevelop.Projects.Dom.Parser.IParser GetParser ()
		{
			return ProjectDomService.GetParser (Document.FileName);
		}
		
		public ICSharpCode.NRefactory.CSharp.AstType ShortenTypeName (ICSharpCode.NRefactory.CSharp.AstType typeReference)
		{
			return Document.CompilationUnit.ShortenTypeName (typeReference.ConvertToReturnType (), Document.Editor.Caret.Line, Document.Editor.Caret.Column).ConvertToTypeReference ();
		}
		
		public IResolver GetResolver ()
		{
			MonoDevelop.Projects.Dom.Parser.IParser domParser = GetParser ();
			if (domParser == null)
				return null;
			return domParser.CreateResolver (Dom, Document, Document.FileName);
		}
		
		public static CodeGenerationOptions CreateCodeGenerationOptions (Document document)
		{
			var options = new CodeGenerationOptions () {
				Dom = document.Dom,
				Document = document,
			};
			if (document.ParsedDocument != null && document.ParsedDocument.CompilationUnit != null) {
				options.EnclosingType = document.ParsedDocument.CompilationUnit.GetTypeAt (document.Editor.Caret.Line, document.Editor.Caret.Column);
				options.EnclosingMember = document.ParsedDocument.CompilationUnit.GetMemberAt (document.Editor.Caret.Line, document.Editor.Caret.Column);
			}
			return options;
		}
		
	}
}
