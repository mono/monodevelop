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
using MonoDevelop.Debugger;
using MonoDevelop.Components;
using Mono.Debugging.Client;
using TextEditor = Mono.TextEditor.TextEditor;

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

		static int IndexOfLastWhiteSpace (string text)
		{
			int index = text.Length - 1;

			while (index >= 0) {
				if (char.IsWhiteSpace (text[index]))
					break;
				index--;
			}

			return index;
		}

		static string GetLocalExpression (TextEditor editor, LocalResolveResult lr, DomRegion expressionRegion)
		{
			var start = new DocumentLocation (expressionRegion.BeginLine, expressionRegion.BeginColumn);
			var end   = new DocumentLocation (expressionRegion.EndLine, expressionRegion.EndColumn);
			var ed = (ExtensibleTextEditor) editor;

			// In a setter, the 'value' variable will have a begin line/column of 0,0 which is an undefined offset
			if (lr.Variable.Region.BeginLine != 0 && lr.Variable.Region.BeginColumn != 0) {
				// Use the start and end offsets of the variable region so that we get the "@" in variable names like "@class"
				start = new DocumentLocation (lr.Variable.Region.BeginLine, lr.Variable.Region.BeginColumn);
				end = new DocumentLocation (lr.Variable.Region.EndLine, lr.Variable.Region.EndColumn);
			}

			string expression = ed.GetTextBetween (start, end).Trim ();

			// Note: When the LocalResolveResult is a parameter, the Variable.Region includes the type
			if (lr.IsParameter) {
				int index = IndexOfLastWhiteSpace (expression);
				if (index != -1)
					expression = expression.Substring (index + 1);
			}

			return expression;
		}

		static string GetMemberExpression (TextEditor editor, MemberResolveResult mr, DomRegion expressionRegion)
		{
			var ed = (ExtensibleTextEditor) editor;
			string expression = null;
			string member = null;

			if (mr.Member != null) {
				if (mr.Member is IProperty) {
					// Visual Studio will evaluate Properties if you hover over their definitions...
					var prop = (IProperty) mr.Member;

					if (prop.CanGet) {
						if (prop.IsStatic)
							expression = prop.FullName;
						else
							member = prop.Name;
					} else {
						return null;
					}
				} else if (mr.Member is IField) {
					var field = (IField) mr.Member;

					if (field.IsStatic)
						expression = field.FullName;
					else
						member = field.Name;
				} else {
					return null;
				}
			} else {
				return null;
			}

			if (expression == null) {
				if (mr.TargetResult != null) {
					var targetRegion = mr.TargetResult.GetDefinitionRegion ();

					if (mr.TargetResult is LocalResolveResult) {
						expression = GetLocalExpression (editor, (LocalResolveResult) mr.TargetResult, targetRegion);
					} else if (mr.TargetResult is MemberResolveResult) {
						expression = GetMemberExpression (editor, (MemberResolveResult) mr.TargetResult, targetRegion);
					} else if (mr.TargetResult is InitializedObjectResolveResult) {
						return null;
					} else if (mr.TargetResult is ThisResolveResult) {
						return member;
					} else if (!targetRegion.IsEmpty) {
						var start = new DocumentLocation (targetRegion.BeginLine, targetRegion.BeginColumn);
						var end   = new DocumentLocation (targetRegion.EndLine, targetRegion.EndColumn);
						expression = ed.GetTextBetween (start, end).Trim ();
					}

					if (expression == null) {
						var start = new DocumentLocation (expressionRegion.BeginLine, expressionRegion.BeginColumn);
						var end   = new DocumentLocation (expressionRegion.EndLine, expressionRegion.EndColumn);
						return ed.GetTextBetween (start, end).Trim ();
					}
				}

				if (!string.IsNullOrEmpty (expression))
					expression += "." + member;
				else
					expression = member;
			}

			return expression;
		}
		
		public override TooltipItem GetItem (TextEditor editor, int offset)
		{
			if (offset >= editor.Document.TextLength)
				return null;
			
			if (!DebuggingService.IsDebugging || DebuggingService.IsRunning)
				return null;
				
			StackFrame frame = DebuggingService.CurrentFrame;
			if (frame == null)
				return null;
			
			var ed = (ExtensibleTextEditor)editor;
			int startOffset = 0, length = 0;
			DomRegion expressionRegion;
			string expression = null;
			ResolveResult res;

			if (ed.IsSomethingSelected && offset >= ed.SelectionRange.Offset && offset <= ed.SelectionRange.EndOffset) {
				expression = ed.SelectedText;
				startOffset = ed.SelectionRange.Offset;
				length = ed.SelectionRange.Length;
			} else if ((res = ed.GetLanguageItem (offset, out expressionRegion)) != null && !res.IsError && res.GetType () != typeof (ResolveResult)) {
				//Console.WriteLine ("res is a {0}", res.GetType ().Name);
				
				if (expressionRegion.IsEmpty)
					return null;

				if (res is NamespaceResolveResult ||
				    res is ConversionResolveResult ||
				    res is ConstantResolveResult ||
				    res is ForEachResolveResult ||
				    res is TypeIsResolveResult ||
				    res is TypeOfResolveResult ||
				    res is ErrorResolveResult)
					return null;

				if (res.IsCompileTimeConstant)
					return null;
				
				var start = new DocumentLocation (expressionRegion.BeginLine, expressionRegion.BeginColumn);
				var end   = new DocumentLocation (expressionRegion.EndLine, expressionRegion.EndColumn);
				
				startOffset = editor.Document.LocationToOffset (start);
				int endOffset = editor.Document.LocationToOffset (end);
				length = endOffset - startOffset;
				
				if (res is LocalResolveResult) {
					expression = GetLocalExpression (editor, (LocalResolveResult) res, expressionRegion);
					length = expression.Length;
				} else if (res is InvocationResolveResult) {
					var ir = (InvocationResolveResult) res;
					
					if (ir.Member.Name != ".ctor")
						return null;
					
					expression = ir.Member.DeclaringType.FullName;
				} else if (res is MemberResolveResult) {
					expression = GetMemberExpression (editor, (MemberResolveResult) res, expressionRegion);
				} else if (res is NamedArgumentResolveResult) {
					expression = ed.GetTextBetween (start, end);
				} else if (res is ThisResolveResult) {
					expression = ed.GetTextBetween (start, end);
				} else if (res is TypeResolveResult) {
					expression = ed.GetTextBetween (start, end);
				} else {
					return null;
				}
			} else {
				var data = editor.GetTextEditorData ();
				startOffset = data.FindCurrentWordStart (offset);
				int endOffset = data.FindCurrentWordEnd (offset);

				expression = ed.GetTextBetween (ed.OffsetToLocation (startOffset), ed.OffsetToLocation (endOffset));
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
		}
		#endregion
	}
}
