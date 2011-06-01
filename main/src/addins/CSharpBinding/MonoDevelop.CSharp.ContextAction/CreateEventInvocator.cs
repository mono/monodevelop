// 
// CreateEventInvocator.cs
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
using ICSharpCode.NRefactory.CSharp;
using ICSharpCode.NRefactory.PatternMatching;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Core;
using System.Collections.Generic;
using Mono.TextEditor;
using System.Linq;
using MonoDevelop.Ide;
using Mono.TextEditor.PopupWindow;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.ContextAction
{
	public class CreateEventInvocator : CSharpContextAction
	{
		public CreateEventInvocator ()
		{
			Description = GettextCatalog.GetString ("Creates a standard OnXXX event method.");
		}
		
		public override string GetMenuText (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			return GettextCatalog.GetString ("Create event invocator");
		}

		EventDeclaration GetEventDeclaration (ParsedDocument doc, DomLocation loc)
		{
			var unit = doc.LanguageAST as ICSharpCode.NRefactory.CSharp.CompilationUnit;
			if (unit == null)
				return null;
			
			return unit.GetNodeAt<EventDeclaration> (loc.Line, loc.Column);
		}
		
		public override bool IsValid (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var eventDeclaration = GetEventDeclaration (document.ParsedDocument, loc);
			if (eventDeclaration == null)
				return false;
			var member = document.CompilationUnit.GetMemberAt (loc) as IEvent;
			if (member == null)
				return false;
			return !member.DeclaringType.Methods.Any (m => m.Name == "On" + member.Name);
		}
		
		public override void Run (MonoDevelop.Ide.Gui.Document document, DomLocation loc)
		{
			var eventDeclaration = GetEventDeclaration (document.ParsedDocument, loc);
			var member = document.CompilationUnit.GetMemberAt (loc) as IEvent;
			if (eventDeclaration == null || member == null)
				return;
			
			MethodDeclaration methodDeclaration = new MethodDeclaration ();
			methodDeclaration.Name = "On" + member.Name;
			methodDeclaration.ReturnType = eventDeclaration.ReturnType.Clone ();
			methodDeclaration.Modifiers = ICSharpCode.NRefactory.CSharp.Modifiers.Protected | ICSharpCode.NRefactory.CSharp.Modifiers.Virtual;
			methodDeclaration.Body = new BlockStatement ();

			IType type = document.Dom.SearchType (document.ParsedDocument.CompilationUnit, member.DeclaringType, member.Location, member.ReturnType);
			IMethod invokeMethod = type.Methods.Where (m => m.Name == "Invoke").FirstOrDefault ();
					
			if (invokeMethod == null)
				return;
			
			bool hasSenderParam = false;
			IEnumerable<IParameter> pars = invokeMethod.Parameters;
			if (invokeMethod.Parameters.Any ()) {
				var first = invokeMethod.Parameters [0];
				if (first.Name == "sender" && first.ReturnType.FullName == "System.Object") {
					hasSenderParam = true;
					pars = invokeMethod.Parameters.Skip (1);
				}
			}
			
			foreach (var par in pars) {
				var typeName = ShortenTypeName (document, par.ReturnType);
				var decl = new ParameterDeclaration (typeName, par.Name);
				methodDeclaration.Parameters.Add (decl);
			}
			
			const string handlerName = "handler";
					
			var handlerVariable = new VariableDeclarationStatement (ShortenTypeName (document, member.ReturnType),
						handlerName,
						new MemberReferenceExpression (new ThisReferenceExpression (), member.Name));
			methodDeclaration.Body.Statements.Add (handlerVariable);
					
			IfElseStatement ifStatement = new IfElseStatement ();
			ifStatement.Condition = new BinaryOperatorExpression (new IdentifierExpression (handlerName), BinaryOperatorType.InEquality, new PrimitiveExpression (null));
			List<Expression> arguments = new List<Expression> ();
			if (hasSenderParam)
				arguments.Add (new ThisReferenceExpression ());
			foreach (var par in pars)
				arguments.Add (new IdentifierExpression (par.Name));
			
			ifStatement.TrueStatement = new ExpressionStatement (new InvocationExpression (new IdentifierExpression (handlerName), arguments));
			methodDeclaration.Body.Statements.Add (ifStatement);
			
			var editor = document.Editor.Parent;
			InsertionCursorEditMode mode = new InsertionCursorEditMode (editor, CodeGenerationService.GetInsertionPoints (document, member.DeclaringType));
			ModeHelpWindow helpWindow = new ModeHelpWindow ();
			helpWindow.TransientFor = IdeApp.Workbench.RootWindow;
			helpWindow.TitleText = GettextCatalog.GetString ("<b>Create event invocator -- Targeting</b>");
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Key</b>"), GettextCatalog.GetString ("<b>Behavior</b>")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Up</b>"), GettextCatalog.GetString ("Move to <b>previous</b> target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Down</b>"), GettextCatalog.GetString ("Move to <b>next</b> target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Enter</b>"), GettextCatalog.GetString ("<b>Declare event invocator implementation</b> at target point.")));
			helpWindow.Items.Add (new KeyValuePair<string, string> (GettextCatalog.GetString ("<b>Esc</b>"), GettextCatalog.GetString ("<b>Cancel</b> this quick fix.")));
			mode.HelpWindow = helpWindow;
			mode.CurIndex = mode.InsertionPoints.Count - 1;
			mode.StartMode ();
			mode.Exited += delegate(object s, InsertionCursorEventArgs iCArgs) {
				if (iCArgs.Success) {
					iCArgs.InsertionPoint.Insert (document.Editor, OutputNode (document, methodDeclaration, ""));
				}
			};
		}
		
	}
}

