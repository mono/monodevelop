// CodeTemplateCompletionData.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
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
//

using System;
using System.Text;
using MonoDevelop.Projects.Gui.Completion;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeTemplates;
using MonoDevelop.Ide.Gui.Content;

namespace MonoDevelop.CSharpBinding
{
	public class CodeTemplateCompletionData : CompletionData, IActionCompletionData
	{
		CodeTemplate template;
		TextEditor editor;
		int initialOffset;
		
		public CodeTemplateCompletionData (TextEditor editor, CodeTemplate template)
		{
			this.template = template;
			this.editor        = editor;
			this.initialOffset = editor.CursorPosition;
						
			this.DisplayText = template.Shortcut;
			this.CompletionText = template.Text;
			this.Icon = MonoDevelop.Core.Gui.Stock.Literal;
		}
		
		public void InsertCompletionText (ICompletionWidget widget, ICodeCompletionContext context)
		{
			editor.DeleteText (context.TriggerOffset, Math.Max (editor.CursorPosition - context.TriggerOffset, context.TriggerWordLength));
			editor.CursorPosition = context.TriggerOffset;
			template.InsertTemplate (editor);
		}
		
	}
}
