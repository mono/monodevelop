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
using MonoDevelop.Projects.Parser;
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
			if (!IdeApp.Services.DebuggingService.IsDebugging || IdeApp.Services.DebuggingService.IsRunning)
				return null;
				
			StackFrame frame = IdeApp.Services.DebuggingService.CurrentFrame;
			if (frame == null)
				return null;
			
			ExtendibleTextEditor ed = (ExtendibleTextEditor) editor;
			
			string fileName = ed.View.ContentName;
			if (fileName == null)
				fileName = ed.View.UntitledName;
			
			IParserContext ctx = ed.View.GetParserContext ();
			IExpressionFinder expressionFinder = null;
			if (fileName != null && ctx != null)
				expressionFinder = ctx.GetExpressionFinder (fileName);
			
			string expression = expressionFinder == null ? TextUtilities.GetExpressionBeforeOffset (ed.View, offset) : expressionFinder.FindFullExpression (editor.Document.Text, offset).Expression;
			if (expression == null)
				return null;
			expression = expression.Trim ();
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
		
		public Gtk.Window CreateTooltipWindow (TextEditor editor, object item)
		{
			return new DebugValueWindow ((ObjectValue) item);
		}
		
		public int GetRequiredWidth (TextEditor editor, Gtk.Window tipWindow)
		{
			return tipWindow.SizeRequest ().Width;
		}

		public bool IsInteractive (TextEditor editor, Gtk.Window tipWindow)
		{
			return true;
		}
		
		#endregion 
		
	}
}
