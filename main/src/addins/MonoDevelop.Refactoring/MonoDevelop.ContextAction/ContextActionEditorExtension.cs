// 
// QuickFixEditorExtension.cs
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
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Projects.Dom;
using Gtk;
using Mono.TextEditor;
using System.Collections.Generic;
using MonoDevelop.Refactoring;
using MonoDevelop.Components.Commands;
using MonoDevelop.AnalysisCore.Gui;
using MonoDevelop.SourceEditor;
using System.Linq;

namespace MonoDevelop.ContextAction
{
	public class ContextActionEditorExtension : TextEditorExtension 
	{
		ContextActionWidget widget;
		
		public void RemoveWidget ()
		{
			if (widget == null)
				return;
			TextEditor editor = Document.Editor.Parent;
			var container = editor.Parent as TextEditorContainer;
			if (container != null) {
				container.Remove (widget);
				container.QueueDraw ();
			}
			widget.Destroy ();
			widget = null;
		}
		
		public override void Dispose ()
		{
			document.DocumentParsed -= HandleDocumentDocumentParsed;
			RemoveWidget ();
			base.Dispose ();
		}
		
		public void CreateWidget (List<ContextAction> fixes, DomLocation loc)
		{
			if (!fixes.Any ())
				return;
			
			widget = new ContextActionWidget (this, Document, loc, fixes);
			var container = Document.Editor.Parent.Parent as TextEditorContainer;
			if (container == null) 
				return;
			container.AddTopLevelWidget (widget,
				2 + (int)Document.Editor.Parent.TextViewMargin.XOffset,
				-2 + (int)document.Editor.Parent.LineToY (document.Editor.Caret.Line));
			widget.Show ();
		}
		
		public override void CursorPositionChanged ()
		{
			RemoveWidget ();
			
			if (Document.ParsedDocument != null) {
				DomLocation loc = new DomLocation (Document.Editor.Caret.Line, Document.Editor.Caret.Column);
				RefactoringService.QueueQuickFixAnalysis (Document, loc, delegate(List<ContextAction> fixes) {
					Application.Invoke (delegate {
						RemoveWidget ();
						CreateWidget (fixes, loc);
					});
				});
			}
			base.CursorPositionChanged ();
		}
		
		public override void Initialize ()
		{
			base.Initialize ();
			document.DocumentParsed += HandleDocumentDocumentParsed;
			document.Editor.SelectionChanged += (sender, e) => CursorPositionChanged ();
		}
		
		void HandleDocumentDocumentParsed (object sender, EventArgs e)
		{
			CursorPositionChanged ();
		}
		
		[CommandHandler(MonoDevelop.Refactoring.RefactoryCommands.QuickFix)]
		void OnQuickFixCommand ()
		{
			if (widget == null)
				return;
			widget.PopupQuickFixMenu ();
		}

	}
}

