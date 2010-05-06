// 
// FindDerivedClassesHandler.cs
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
using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components.Commands;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Refactoring;
using MonoDevelop.Ide;

namespace MonoDevelop.Refactoring
{
	public class FindDerivedClassesHandler : CommandHandler
	{
		protected override void Run (object data)
		{
			Document doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || IdeApp.ProjectOperations.CurrentSelectedSolution == null)
				return;
			ITextBuffer editor = doc.GetContent<ITextBuffer> ();
			if (editor == null)
				return;
			int line, column;
			editor.GetLineColumnFromPosition (editor.CursorPosition, out line, out column);
			ProjectDom ctx = doc.Dom;
			
			ResolveResult resolveResult;
			INode item;
			CurrentRefactoryOperationsHandler.GetItem (ctx, doc, editor, out resolveResult, out item);
			
			IMember eitem = resolveResult != null ? (resolveResult.CallingMember ?? resolveResult.CallingType) : null;
			string itemName = null;
			if (item is IMember)
				itemName = ((IMember)item).Name;
			if (item != null && eitem != null && (eitem.Equals (item) || (eitem.Name == itemName && !(eitem is IProperty) && !(eitem is IMethod)))) {
				item = eitem;
				eitem = null;
			}
			IType eclass = null;
			if (item is IType) {
				if (((IType)item).ClassType == ClassType.Interface)
					eclass = CurrentRefactoryOperationsHandler.FindEnclosingClass (ctx, editor.Name, line, column); else
					eclass = (IType)item;
				if (eitem is IMethod && ((IMethod)eitem).IsConstructor && eitem.DeclaringType.Equals (item)) {
					item = eitem;
					eitem = null;
				}
			}
			Refactorer refactorer = new Refactorer (ctx, doc.CompilationUnit, eclass, item, null);
			refactorer.FindDerivedClasses ();
		}
	}
}

