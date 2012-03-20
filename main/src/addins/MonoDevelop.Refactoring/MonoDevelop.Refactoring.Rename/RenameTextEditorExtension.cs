// 
// RenameTextEditorExtension.cs
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
using MonoDevelop.Ide.Commands;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Dom;

namespace MonoDevelop.Refactoring.Rename
{
	public class RenameTextEditorExtension : TextEditorExtension
	{
		[CommandUpdateHandler(EditCommands.Rename)]
		public void RenameCommand_Update(CommandInfo ci)
		{
			var doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null)
				return;

			var editor = doc.GetContent<ITextBuffer>();
			if (editor == null)
				return;

			var dom = doc.Dom;

			ResolveResult result;
			INode item;
			CurrentRefactoryOperationsHandler.GetItem(dom, doc, editor, out result, out item);

			var options = new RefactoringOptions()
			{
				Document = doc,
				Dom = dom,
				ResolveResult = result,
				SelectedItem = item is InstantiatedType ? ((InstantiatedType)item).UninstantiatedType : item
			};

			// If not a valid operation, allow command to be handled by others
			if (!new RenameRefactoring().IsValid(options))
				ci.Bypass = true;
		}

		[CommandHandler (EditCommands.Rename)]
		public void RenameCommand ()
		{
			new RenameHandler ().Start (base.Editor);
		}
	}
}

