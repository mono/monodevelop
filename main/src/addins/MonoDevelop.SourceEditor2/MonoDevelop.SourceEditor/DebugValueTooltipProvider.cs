// DebugValueTooltipProvider.cs
//
// Author:
//   Lluis Sanchez Gual <lluis@novell.com>
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
//

using System;
using System.Collections.Generic;
using Mono.TextEditor;
using MonoDevelop.Ide.Gui;
using Mono.Debugging.Client;
using TextEditor = Mono.TextEditor.TextEditor;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Gui.Completion;

namespace MonoDevelop.SourceEditor
{
	public class DebugValueTooltipProvider: ITooltipProvider
	{
		Dictionary<string,ObjectValue> cachedValues = new Dictionary<string,ObjectValue> ();
		
		public DebugValueTooltipProvider()
		{
			IdeApp.Services.DebuggingService.CurrentFrameChanged += delegate {
				// Clear the cached values every time the current frame changes
				cachedValues.Clear ();
			};
		}

		#region ITooltipProvider implementation 
		
		public object GetItem (TextEditor editor, int offset)
		{
			if (offset >= editor.Document.Length)
				return null;
			
			if (!IdeApp.Services.DebuggingService.IsDebugging || IdeApp.Services.DebuggingService.IsRunning)
				return null;
				
			StackFrame frame = IdeApp.Services.DebuggingService.CurrentFrame;
			if (frame == null)
				return null;
			
			ExtensibleTextEditor ed = (ExtensibleTextEditor) editor;
			
			string fileName = ed.View.ContentName;
			if (fileName == null)
				fileName = ed.View.UntitledName;
			
			string expression;
			if (ed.IsSomethingSelected && offset >= ed.SelectionRange.Offset && offset <= ed.SelectionRange.EndOffset) {
				expression = ed.SelectedText;
			}
			else {
				IExpressionFinder expressionFinder = ProjectDomService.GetExpressionFinder (fileName);
				expression = expressionFinder == null ? GetExpressionBeforeOffset (ed, offset) : expressionFinder.FindFullExpression (editor.Document.Text, offset).Expression;
				if (expression == null)
					return null;
				expression = expression.Trim ();
			}
			
			if (expression.Length == 0)
				return null;
			
			ObjectValue val;
			if (!cachedValues.TryGetValue (expression, out val)) {
				val = frame.GetExpressionValue (expression, false);
				cachedValues [expression] = val;
			}
			if (val != null && val.IsUnknown)
				return null;
			else
				return val;
		}
		
		string GetExpressionBeforeOffset (TextEditor editor, int offset)
		{
			int start = offset;
			while (start > 0 && IsIdChar (editor.Document.GetCharAt (start)))
				start--;
			while (offset < editor.Document.Length && IsIdChar (editor.Document.GetCharAt (offset)))
				offset++;
			start++;
			if (offset - start > 0 && start < editor.Document.Length)
				return editor.Document.GetTextAt (start, offset - start);
			else
				return string.Empty;
		}
		
		public static bool IsIdChar (char c)
		{
			return char.IsLetterOrDigit (c) || c == '_';
		}
			
		public Gtk.Window CreateTooltipWindow (TextEditor editor, object item)
		{
			return new DebugValueWindow (editor, IdeApp.Services.DebuggingService.CurrentFrame, (ObjectValue) item);
		}
		
		public void GetRequiredPosition (TextEditor editor, Gtk.Window tipWindow, out int requiredWidth, out double xalign)
		{
			xalign = 0.1;
			requiredWidth = tipWindow.SizeRequest ().Width;
		}

		public bool IsInteractive (TextEditor editor, Gtk.Window tipWindow)
		{
			return true;
		}
		
		#endregion 
		
	}
}
