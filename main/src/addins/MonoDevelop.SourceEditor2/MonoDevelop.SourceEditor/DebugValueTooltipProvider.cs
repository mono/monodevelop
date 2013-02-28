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
using MonoDevelop.Ide.Gui;
using MonoDevelop.Components;
using Mono.Debugging.Client;
using TextEditor = Mono.TextEditor.TextEditor;
using MonoDevelop.Ide.CodeCompletion;
using MonoDevelop.Debugger;

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;

namespace MonoDevelop.SourceEditor
{
	public class DebugValueTooltipProvider: TooltipProvider, IDisposable
	{
		Dictionary<string,ObjectValue> cachedValues = new Dictionary<string,ObjectValue> ();
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
			// Clear the cached values every time the current frame changes
			cachedValues.Clear ();

			if (tooltip != null)
				tooltip.Hide ();
		}

		void TargetProcessExited (object sender, EventArgs e)
		{
			if (tooltip != null)
				tooltip.Hide ();
		}

		#region ITooltipProvider implementation 
		
		public override TooltipItem GetItem (Mono.TextEditor.TextEditor editor, int offset)
		{
			if (offset >= editor.Document.TextLength)
				return null;
			
			if (!DebuggingService.IsDebugging || DebuggingService.IsRunning)
				return null;
				
			StackFrame frame = DebuggingService.CurrentFrame;
			if (frame == null)
				return null;
			
			var ed = (ExtensibleTextEditor)editor;
			
			string expression = null;
			int startOffset = 0, length = 0;
			if (ed.IsSomethingSelected && offset >= ed.SelectionRange.Offset && offset <= ed.SelectionRange.EndOffset) {
				expression = ed.SelectedText;
				startOffset = ed.SelectionRange.Offset;
				length = ed.SelectionRange.Length;
			} else {
				ICSharpCode.NRefactory.TypeSystem.DomRegion expressionRegion;
				ResolveResult res = ed.GetLanguageItem (offset, out expressionRegion);
				
				if (res == null || res.IsError || res.GetType () == typeof (ResolveResult))
					return null;
				
				//Console.WriteLine ("res is a {0}", res.GetType ().Name);
				
				if (expressionRegion.IsEmpty)
					return null;

				if (res is NamespaceResolveResult ||
				    res is ConversionResolveResult ||
				    res is ForEachResolveResult ||
				    res is TypeIsResolveResult ||
				    res is TypeOfResolveResult ||
				    res is ErrorResolveResult)
					return null;
				
				var start = new DocumentLocation (expressionRegion.BeginLine, expressionRegion.BeginColumn);
				var end   = new DocumentLocation (expressionRegion.EndLine, expressionRegion.EndColumn);
				
				startOffset = editor.Document.LocationToOffset (start);
				int endOffset = editor.Document.LocationToOffset (end);
				length = endOffset - startOffset;
				
				if (res is LocalResolveResult) {
					var lr = (LocalResolveResult) res;
					
					// Capture only the local variable portion of the expression...
					expression = lr.Variable.Name;
					length = expression.Length;
					
					// Calculate start offset based on the variable region because we don't want to include the type information.
					// Note: We might not actually need to do this anymore?
					if (lr.Variable.Region.BeginLine != start.Line || lr.Variable.Region.BeginColumn != start.Column) {
						start = new DocumentLocation (lr.Variable.Region.BeginLine, lr.Variable.Region.BeginColumn);
						startOffset = editor.Document.LocationToOffset (start);
					}
				} else if (res is InvocationResolveResult) {
					var ir = (InvocationResolveResult) res;
					
					if (ir.Member.Name != ".ctor")
						return null;
					
					expression = ir.Member.DeclaringType.FullName;
				} else if (res is MemberResolveResult) {
					var mr = (MemberResolveResult) res;
					
					if (mr.TargetResult == null) {
						// User is hovering over a member definition...
						
						if (mr.Member is IProperty) {
							// Visual Studio will evaluate Properties if you hover over their definitions...
							var prop = (IProperty) mr.Member;
							
							if (prop.CanGet) {
								if (prop.IsStatic)
									expression = prop.FullName;
								else
									expression = prop.Name;
							} else {
								return null;
							}
						} else if (mr.Member is IField) {
							var field = (IField) mr.Member;
							
							if (field.IsStatic)
								expression = field.FullName;
							else
								expression = field.Name;
						} else {
							return null;
						}
					}
					
					// If the TargetResult is not null, then treat it like any other ResolveResult.
				} else if (res is ConstantResolveResult) {
					// Fall through...
				} else if (res is ThisResolveResult) {
					// Fall through...
				} else if (res is TypeResolveResult) {
					// Fall through...
				} else {
					return null;
				}
				
				if (expression == null)
					expression = ed.GetTextBetween (start, end);
			}
			
			if (string.IsNullOrEmpty (expression))
				return null;
			
			ObjectValue val;
			if (!cachedValues.TryGetValue (expression, out val)) {
				val = frame.GetExpressionValue (expression, true);
				cachedValues [expression] = val;
			}
			
			if (val == null || val.IsUnknown || val.IsNotSupported)
				return null;
			
			val.Name = expression;
			
			return new TooltipItem (val, startOffset, length);
		}
		
		/*string GetExpressionBeforeOffset (TextEditor editor, int offset)
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
		}*/
		
		public static bool IsIdChar (char c)
		{
			return char.IsLetterOrDigit (c) || c == '_';
		}
			
		public override Gtk.Window ShowTooltipWindow (TextEditor editor, int offset, Gdk.ModifierType modifierState, int mouseX, int mouseY, TooltipItem item)
		{
			var location = editor.OffsetToLocation (item.ItemSegment.Offset);
			var point = editor.LocationToPoint (location);
			int lineHeight = (int) editor.LineHeight;
			int y = (int) point.Y;

			// find the top of the line that the mouse is hovering over
			while (y + lineHeight < mouseY)
				y += lineHeight;

			var caret = new Gdk.Rectangle (mouseX, y, 1, lineHeight);
			tooltip = new DebugValueWindow (editor, offset, DebuggingService.CurrentFrame, (ObjectValue) item.Item, null);
			tooltip.ShowPopup (editor, caret, PopupPosition.TopLeft);

			return tooltip;
		}

		public override bool IsInteractive (Mono.TextEditor.TextEditor editor, Gtk.Window tipWindow)
		{
			return DebuggingService.IsDebugging;
		}
		
		#endregion 
		
		#region IDisposable implementation
		public void Dispose ()
		{
			DebuggingService.CurrentFrameChanged -= CurrentFrameChanged;
			DebuggingService.DebugSessionStarted -= DebugSessionStarted;
		}
		#endregion
	}
}
