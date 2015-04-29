// DebugValueTooltipProvider.cs
//
// Authors: Lluis Sanchez Gual <lluis@novell.com>
//          Jeffrey Stedfast <jeff@xamarin.com>
//
// Copyright (c) 2008 Novell, Inc (http://www.novell.com)
// Copyright (c) 2012 Xamarin Inc. (http://www.xamarin.com)
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
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Debugger;
using MonoDevelop.Components;
using Mono.Debugging.Client;
using TextEditor = Mono.TextEditor.TextEditor;

namespace MonoDevelop.SourceEditor
{
	public class DebugValueTooltipProvider: TooltipProvider, IDisposable
	{
		DebugValueWindow tooltip;
		
		public DebugValueTooltipProvider ()
		{
			DebuggingService.CurrentFrameChanged += CurrentFrameChanged;
			DebuggingService.DebugSessionStarted += DebugSessionStarted;
		}

		void DebugSessionStarted (object sender, EventArgs e)
		{
			DebuggingService.DebuggerSession.TargetExited += TargetProcessExited;
		}

		void CurrentFrameChanged (object sender, EventArgs e)
		{
			if (tooltip != null)
				tooltip.Hide ();
		}

		void TargetProcessExited (object sender, EventArgs e)
		{
			if (tooltip != null) {
				tooltip.Destroy ();
				tooltip = null;
			}
		}

		#region ITooltipProvider implementation

		public override TooltipItem GetItem (TextEditor editor, int offset)
		{
			if (offset >= editor.Document.TextLength)
				return null;

			if (!DebuggingService.IsDebugging || DebuggingService.IsRunning)
				return null;

			StackFrame frame = DebuggingService.CurrentFrame;
			if (frame == null)
				return null;

			var ed = (ExtensibleTextEditor) editor;
			string expression = null;
			int startOffset;

			if (ed.IsSomethingSelected && offset >= ed.SelectionRange.Offset && offset <= ed.SelectionRange.EndOffset) {
				startOffset = ed.SelectionRange.Offset;
				expression = ed.SelectedText;
			} else {
				var doc = IdeApp.Workbench.ActiveDocument;
				if (doc == null || doc.ParsedDocument == null)
					return null;

				var resolver = doc.GetContent<IDebuggerExpressionResolver> ();
				var data = editor.GetTextEditorData ();

				if (resolver != null) {
					expression = resolver.ResolveExpression (data, doc, offset, out startOffset);
				} else {
					int endOffset = data.FindCurrentWordEnd (offset);
					startOffset = data.FindCurrentWordStart (offset);

					expression = data.GetTextAt (startOffset, endOffset - startOffset);
				}
			}
			
			if (string.IsNullOrEmpty (expression))
				return null;

			var options = DebuggingService.DebuggerSession.EvaluationOptions.Clone ();
			options.AllowMethodEvaluation = true;
			options.AllowTargetInvoke = true;

			var val = frame.GetExpressionValue (expression, options);

			if (val == null || val.IsUnknown || val.IsNotSupported)
				return null;
			
			val.Name = expression;
			
			return new TooltipItem (val, startOffset, expression.Length);
		}
			
		public override Gtk.Window ShowTooltipWindow (TextEditor editor, int offset, Gdk.ModifierType modifierState, int mouseX, int mouseY, TooltipItem item)
		{
			var location = editor.OffsetToLocation (item.ItemSegment.Offset);
			var point = editor.LocationToPoint (location);
			int lineHeight = (int) editor.LineHeight;
			int y = point.Y;

			// find the top of the line that the mouse is hovering over
			while (y + lineHeight < mouseY)
				y += lineHeight;

			var caret = new Gdk.Rectangle (mouseX, y, 1, lineHeight);
			tooltip = new DebugValueWindow (editor, offset, DebuggingService.CurrentFrame, (ObjectValue) item.Item, null);
			tooltip.ShowPopup (editor, caret, PopupPosition.TopLeft);

			return tooltip;
		}

		public override bool IsInteractive (TextEditor editor, Gtk.Window tipWindow)
		{
			return DebuggingService.IsDebugging;
		}
		
		#endregion 
		
		#region IDisposable implementation
		public void Dispose ()
		{
			DebuggingService.CurrentFrameChanged -= CurrentFrameChanged;
			DebuggingService.DebugSessionStarted -= DebugSessionStarted;
			if (DebuggingService.DebuggerSession != null)
				DebuggingService.DebuggerSession.TargetExited -= TargetProcessExited;
		}
		#endregion
	}
}
