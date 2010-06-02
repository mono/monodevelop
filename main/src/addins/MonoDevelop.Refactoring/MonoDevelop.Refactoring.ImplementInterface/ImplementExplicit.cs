// 
// ImplementExplicit.cs
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

using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using Mono.TextEditor;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring.ImplementInterface
{
	public class ImplementExplicit : RefactoringOperation
	{
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("I_mplement explicit");
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.ResolveResult == null)
				return false;
			
			IType type = options.Dom.GetType (options.ResolveResult.ResolvedType);
			if (type == null || type.ClassType != MonoDevelop.Projects.Dom.ClassType.Interface)
				return false;
			DocumentLocation location = options.GetTextEditorData ().Caret.Location;
			IType declaringType = options.Document.CompilationUnit.GetTypeAt (location.Line + 1, location.Column + 1);
			return declaringType != null && options.ResolveResult.ResolvedExpression.IsInInheritableTypeContext;
		}
		
		public override void Run (RefactoringOptions options)
		{
			DocumentLocation location = options.GetTextEditorData ().Caret.Location;
			IType interfaceType = options.Dom.GetType (options.ResolveResult.ResolvedType);
			IType declaringType = options.Document.CompilationUnit.GetTypeAt (location.Line + 1, location.Column + 1);
			
			var editor = options.GetTextEditorData ().Parent;
			
			InsertionCursorEditMode mode = new InsertionCursorEditMode (editor, HelperMethods.GetInsertionPoints (editor.Document, declaringType));
			mode.CurIndex = mode.InsertionPoints.Count - 1;
			mode.StartMode ();
			mode.Exited += delegate(object s, InsertionCursorEventArgs args) {
				if (args.Success) {
					CodeGenerator generator = CodeGenerator.CreateGenerator (options.GetTextEditorData ().Document.MimeType);
					args.InsertionPoint.Insert (editor, generator.CreateInterfaceImplementation (declaringType, interfaceType, true));
				}
			};
		}
	}
}
