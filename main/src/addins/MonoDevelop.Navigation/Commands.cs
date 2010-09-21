// 
// Commands.cs
//  
// Author:
//       Nikhil Sarda <diff.operator@gmail.com>
// 
// Copyright (c) 2010 Nikhil Sarda
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
using System.Linq;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Dom;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Ide;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using MonoDevelop.Refactoring;

namespace MonoDevelop.Navigation
{
	public enum NavigationCommands
	{
		ShowNavigationOptions,
		Other
	}
	
	public class NavigationCommandHandler : CommandHandler
	{
		CodeCompletionContext completionContext;
		Document doc;
		
		protected override void Update (CommandArrayInfo info)
		{
			doc = IdeApp.Workbench.ActiveDocument;
			if (doc == null || doc.FileName == FilePath.Null || IdeApp.ProjectOperations.CurrentSelectedSolution == null)
				return;

			ITextBuffer editor = doc.GetContent<ITextBuffer> ();
			if (editor == null)
				return;

			var completionWidget = doc.GetContent<ICompletionWidget> ();
			if (completionWidget == null)
				return;
			completionContext = completionWidget.CreateCodeCompletionContext (doc.Editor.Caret.Offset);
			
			int line, column;
			editor.GetLineColumnFromPosition (editor.CursorPosition, out line, out column);
			ProjectDom ctx = doc.Dom;
			ResolveResult resolveResult;
			INode item;
			CurrentRefactoryOperationsHandler.GetItem (ctx, doc, editor, out resolveResult, out item);
			
			if (item is DomType) {
				info.Add (new CommandInfo ("Show type heirarchy", true, false) {
						}, item);
			} else if (item is DomMethod) {
				info.Add (new CommandInfo ("Show call heirarchy", true, false) {
						}, item);
			}
		}
		
		protected override void Run (object dataItem)
		{
			if (dataItem == null)
				return;

			if (dataItem is DomType) {
				ClassNavigationWindow.ShowClassNavigationWnd (dataItem as IType, completionContext);
			} else if (dataItem is DomMethod) {
				var methodDomRegion = ((DomMethod)dataItem).BodyRegion;
				//TODO
				throw new NotImplementedException();
			}
		}
	}
}

