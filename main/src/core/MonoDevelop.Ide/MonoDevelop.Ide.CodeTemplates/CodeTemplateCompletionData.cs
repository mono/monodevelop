// 
// CodeTemplateCompletionData.cs
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

using System;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Core;
using MonoDevelop.Ide.Editor.Extension;
using MonoDevelop.Ide.Editor;

namespace MonoDevelop.Ide.CodeTemplates
{
	public interface ICodeTemplateHandler
	{
		void InsertTemplate (CodeTemplate template, TextEditor editor, DocumentContext context);
	}
	
	class CodeTemplateCompletionData : CompletionData
	{
		readonly TextEditorExtension doc;
		readonly CodeTemplate template;
		
		public CodeTemplateCompletionData (TextEditorExtension doc, CodeTemplate template)
		{
			this.doc      = doc;
			this.template = template;
			this.CompletionText = template.Shortcut;
			this.Icon        = template.Icon;
			this.DisplayText = template.Shortcut;
			this.Description = template.Shortcut + Environment.NewLine + GettextCatalog.GetString (template.Description);
		}
		
		public override void InsertCompletionText (CompletionListWindow window, ref KeyActions ka, KeyDescriptor descriptor)
		{
			template.Insert (doc.Editor, doc.DocumentContext);
		}
	}
}
