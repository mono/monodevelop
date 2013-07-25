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

using ICSharpCode.NRefactory.TypeSystem;
using ICSharpCode.NRefactory.Semantics;
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.CSharp.TypeSystem;
using ICSharpCode.NRefactory.CSharp.Resolver;

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

		static string GetIdentifierName (TextEditorData editor, Identifier id, out int startOffset)
		{
			startOffset = editor.LocationToOffset (id.StartLocation.Line, id.StartLocation.Column);

			return editor.GetTextBetween (id.StartLocation, id.EndLocation);
		}

		public static string ResolveExpression (TextEditorData editor, ResolveResult result, AstNode node, out int startOffset)
		{
			//Console.WriteLine ("result is a {0}", result.GetType ().Name);
			startOffset = -1;

			if (result is NamespaceResolveResult ||
			    result is ConversionResolveResult ||
			    result is ConstantResolveResult ||
			    result is ForEachResolveResult ||
			    result is TypeIsResolveResult ||
			    result is TypeOfResolveResult ||
			    result is ErrorResolveResult)
				return null;

			if (result.IsCompileTimeConstant)
				return null;

			startOffset = editor.LocationToOffset (node.StartLocation.Line, node.StartLocation.Column);

			if (result is InvocationResolveResult) {
				var ir = (InvocationResolveResult) result;
				if (ir.Member.Name == ".ctor") {
					// if the user is hovering over something like "new Abc (...)", we want to show them type information for Abc
					return ir.Member.DeclaringType.FullName;
				}

				// do not support general method invocation for tooltips because it could cause side-effects
				return null;
			} else if (result is LocalResolveResult) {
				if (node is ParameterDeclaration) {
					// user is hovering over a method parameter, but we don't want to include the parameter type
					var param = (ParameterDeclaration) node;

					return GetIdentifierName (editor, param.NameToken, out startOffset);
				}

				if (node is VariableInitializer) {
					// user is hovering over something like "int fubar = 5;", but we don't want the expression to include the " = 5"
					var variable = (VariableInitializer) node;

					return GetIdentifierName (editor, variable.NameToken, out startOffset);
				}
			} else if (result is MemberResolveResult) {
				var mr = (MemberResolveResult) result;

				if (node is PropertyDeclaration) {
					var prop = (PropertyDeclaration) node;
					var name = GetIdentifierName (editor, prop.NameToken, out startOffset);

					// if the property is static, then we want to return "Full.TypeName.Property"
					if (prop.Modifiers.HasFlag (Modifiers.Static))
						return mr.Member.DeclaringType.FullName + "." + name;

					// otherwise we want to return "this.Property" so that it won't conflict with anything else in the local scope
					return "this." + name;
				}

				if (node is FieldDeclaration) {
					var field = (FieldDeclaration) node;
					var name = GetIdentifierName (editor, field.NameToken, out startOffset);

					// if the field is static, then we want to return "Full.TypeName.Field"
					if (field.Modifiers.HasFlag (Modifiers.Static))
						return mr.Member.DeclaringType.FullName + "." + name;

					// otherwise we want to return "this.Field" so that it won't conflict with anything else in the local scope
					return "this." + name;
				}

				if (node is VariableInitializer) {
					// user is hovering over a field declaration that includes initialization
					var variable = (VariableInitializer) node;
					var name = GetIdentifierName (editor, variable.NameToken, out startOffset);

					// walk up the AST to find the FieldDeclaration so that we can determine if it is static or not
					var field = variable.GetParent<FieldDeclaration> ();

					// if the field is static, then we want to return "Full.TypeName.Field"
					if (field.Modifiers.HasFlag (Modifiers.Static))
						return mr.Member.DeclaringType.FullName + "." + name;

					// otherwise we want to return "this.Field" so that it won't conflict with anything else in the local scope
					return "this." + name;
				}

				if (node is NamedExpression) {
					// user is hovering over 'Property' in an expression like: var fubar = new Fubar () { Property = baz };
					var variable = node.GetParent<VariableInitializer> ();
					if (variable != null) {
						var variableName = GetIdentifierName (editor, variable.NameToken, out startOffset);
						var name = GetIdentifierName (editor, ((NamedExpression) node).NameToken, out startOffset);

						return variableName + "." + name;
					}
				}
			} else if (result is TypeResolveResult) {
				return ((TypeResolveResult) result).Type.FullName;
			}

			return editor.GetTextBetween (node.StartLocation, node.EndLocation);
		}

		static bool TryResolveAt (Document doc, DocumentLocation loc, out ResolveResult result, out AstNode node)
		{
			if (doc == null)
				throw new ArgumentNullException ("doc");

			result = null;
			node = null;

			var parsedDocument = doc.ParsedDocument;
			if (parsedDocument == null)
				return false;

			var unit = parsedDocument.GetAst<SyntaxTree> ();
			var parsedFile = parsedDocument.ParsedFile as CSharpUnresolvedFile;

			if (unit == null || parsedFile == null)
				return false;

			try {
				result = ResolveAtLocation.Resolve (new Lazy<ICompilation> (() => doc.Compilation), parsedFile, unit, loc, out node);
				if (result == null || node is Statement)
					return false;
			} catch {
				return false;
			}

			return true;
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

				ResolveResult result;
				AstNode node;

				var loc = editor.OffsetToLocation (offset);
				if (!TryResolveAt (doc, loc, out result, out node))
					return null;

				expression = ResolveExpression (editor.GetTextEditorData (), result, node, out startOffset);
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
		}
		#endregion
	}
}
