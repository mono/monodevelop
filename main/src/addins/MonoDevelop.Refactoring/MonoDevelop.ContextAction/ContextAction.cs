// 
// Result.cs
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
using MonoDevelop.Projects.Dom;
using MonoDevelop.Refactoring;

namespace MonoDevelop.ContextAction
{
	public enum ContextActionType 
	{
		Hidden,
		Error,
		Warning,
		Suggestion,
		Hint
	}
	
	public abstract class ContextAction
	{
		public ContextActionAddinNode Node {
			get;
			internal set;
		}
		
		public string Description {
			get;
			protected set;
		}
		
		public virtual string GetMenuText (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			return Node.Title;
		}
		
		public abstract void Run (MonoDevelop.Ide.Gui.Document document, DomLocation loc);
		public abstract bool IsValid (MonoDevelop.Ide.Gui.Document document, DomLocation loc);
		
		public static ICSharpCode.NRefactory.CSharp.AstType ShortenTypeName (MonoDevelop.Ide.Gui.Document doc, string fullyQualifiedTypeName)
		{
			return doc.ParsedDocument.CompilationUnit.ShortenTypeName (new DomReturnType (fullyQualifiedTypeName), doc.Editor.Caret.Line, doc.Editor.Caret.Column).ConvertToTypeReference ();
		}
		
		public static ICSharpCode.NRefactory.CSharp.AstType ShortenTypeName (MonoDevelop.Ide.Gui.Document doc, IReturnType fullyQualifiedTypeName)
		{
			return doc.ParsedDocument.CompilationUnit.ShortenTypeName (fullyQualifiedTypeName, doc.Editor.Caret.Line, doc.Editor.Caret.Column).ConvertToTypeReference ();
		}
		
	}
}
