// 
// ExtractMethod.cs
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
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Text;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

using MonoDevelop.Ide.Gui;
using MonoDevelop.Ide.Gui.Content;
using MonoDevelop.Ide.Gui.Dialogs;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core;
using MonoDevelop.Core.Gui;

namespace MonoDevelop.Refactoring.ExtractMethod
{
	public class ExtractMethodRefactoring : RefactoringOperation
	{
		public ExtractMethodRefactoring ()
		{
			Name = "Extract Method";
		}
		
		public override bool IsValid (RefactoringOptions options)
		{
			if (options.SelectedItem != null)
				return false;
			var buffer = options.Document.TextEditor;
			if (buffer.SelectionStartPosition - buffer.SelectionEndPosition != 0) {
				ParsedDocument doc = options.ParseDocument ();
				if (doc != null && doc.CompilationUnit != null) {
					int line, column;
					buffer.GetLineColumnFromPosition (buffer.CursorPosition, out line, out column);
					return doc.CompilationUnit.GetMemberAt (line, column) != null;
				}
			}
			return false;
		}
		
		public override string GetMenuDescription (RefactoringOptions options)
		{
			return GettextCatalog.GetString ("_Extract Method...");
		}
		
		public override void Run (RefactoringOptions options)
		{
			var buffer = options.Document.TextEditor;
			if (buffer.SelectionStartPosition - buffer.SelectionEndPosition == 0)
				return;

			ParsedDocument doc = options.ParseDocument ();
			if (doc == null || doc.CompilationUnit == null)
				return;

			int line, column;
			buffer.GetLineColumnFromPosition (buffer.CursorPosition, out line, out column);
			IMember member = doc.CompilationUnit.GetMemberAt (line, column);
			if (member == null)
				return;
			ExtractMethodParameters param = new ExtractMethodParameters () {
				Document = options.Document,
				DeclaringMember = member,
				Location = new DomLocation (line, column)
			};
			Analyze (options, param, true);
			ExtractMethodDialog dialog = new ExtractMethodDialog (options, this, param);
			dialog.Show ();
		}
		
		public class ExtractMethodParameters 
		{
			public Document Document {
				get;
				set;
			}
			
			public IMember DeclaringMember {
				get;
				set;
			}
				
			public string Name {
				get;
				set;
			}
			
			public bool GenerateComment {
				get;
				set;
			}
			
			public bool ReferencesMember {
				get;
				set;
			}
			
			public DomLocation Location {
				get;
				set;
			}
			
			public ICSharpCode.NRefactory.Ast.Modifiers Modifiers {
				get;
				set;
			}
			
			public HashSet<string> ChangedVariables {
				get;
				set;
			}
			
			/// <summary>
			/// The type of the expression, if the text is an expression, otherwise null.
			/// </summary>
			public IReturnType ExpressionType {
				get;
				set;
			}
			
			List<KeyValuePair<string, IReturnType>> parameters = new List<KeyValuePair<string, IReturnType>> ();
			public List<KeyValuePair<string, IReturnType>> Parameters {
				get {
					return parameters;
				}
			}
		}
		
		INode Analyze (RefactoringOptions options, ExtractMethodParameters param, bool fillParameter)
		{
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return null;

			string text = param.Document.TextEditor.GetText (options.Document.TextEditor.SelectionStartPosition, options.Document.TextEditor.SelectionEndPosition);

			INode result = provider.ParseText (text);

			VariableLookupVisitor visitor = new VariableLookupVisitor (resolver, param.Location);

			if (fillParameter) {
				if (result != null)
					result.AcceptVisitor (visitor, null);
				if (result is Expression) {
					ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (text), param.Location);
					if (resolveResult != null)
						param.ExpressionType = resolveResult.ResolvedType;
				}

				foreach (KeyValuePair<string, IReturnType> var in visitor.UnknownVariables)
					param.Parameters.Add (var);
				param.ReferencesMember = visitor.ReferencesMember;
				param.ChangedVariables = visitor.ChangedVariables;
			}
			
			return result;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			ExtractMethodParameters param = (ExtractMethodParameters)prop;

			TextReplaceChange replacement = new TextReplaceChange ();
			replacement.Description = string.Format (GettextCatalog.GetString ("Substitute selected statement(s) with call to {0}"), param.Name);
			replacement.FileName = param.Document.FileName;
			replacement.Offset = param.Document.TextEditor.SelectionStartPosition;
			replacement.RemovedChars = param.Document.TextEditor.SelectionEndPosition - param.Document.TextEditor.SelectionStartPosition;

			INode node = Analyze (options, param, false);
			bool oneChangedVariable = param.ChangedVariables.Count == 1 && node is BlockStatement;
			InvocationExpression invocation = new InvocationExpression (new IdentifierExpression (param.Name));
			foreach (KeyValuePair<string, IReturnType> var in param.Parameters) {
				if (!oneChangedVariable && param.ChangedVariables.Contains (var.Key)) {
					invocation.Arguments.Add (new DirectionExpression (FieldDirection.Ref, new IdentifierExpression (var.Key)));
				} else {
					invocation.Arguments.Add (new IdentifierExpression (var.Key));
				}
			}
			string mimeType = DesktopService.GetMimeTypeForUri (param.Document.FileName);
			INRefactoryASTProvider provider = RefactoringService.GetASTProvider (mimeType);
			INode outputNode;
			if (oneChangedVariable) {
				outputNode = new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (param.ChangedVariables.First ()), ICSharpCode.NRefactory.Ast.AssignmentOperatorType.Assign, invocation));
			} else {
				outputNode = node is BlockStatement ? (INode)new ExpressionStatement (invocation) : invocation;
			}

			replacement.InsertedText = options.GetWhitespaces (param.Document.TextEditor.SelectionStartPosition) + provider.OutputNode (options.Dom, outputNode);
			result.Add (replacement);

			TextReplaceChange insertNewMethod = new TextReplaceChange ();
			insertNewMethod.FileName = param.Document.FileName;
			insertNewMethod.Description = string.Format (GettextCatalog.GetString ("Create new method {0} from selected statement(s)"), param.Name);
			insertNewMethod.Offset = param.Document.TextEditor.GetPositionFromLineColumn (param.DeclaringMember.BodyRegion.End.Line, param.DeclaringMember.BodyRegion.End.Column);

			MethodDeclaration methodDecl = new MethodDeclaration ();
			methodDecl.Name = param.Name;
			methodDecl.Modifier = param.Modifiers;
			if (!param.ReferencesMember)
				methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
			if (node is BlockStatement) {
				methodDecl.TypeReference = new TypeReference ("System.Void");
				methodDecl.TypeReference.IsKeyword = true;
				methodDecl.Body = (BlockStatement)node;
				if (oneChangedVariable)
					methodDecl.Body.AddChild (new ReturnStatement (new IdentifierExpression (param.ChangedVariables.First ())));
			} else if (node is Expression) {
				methodDecl.TypeReference = new TypeReference (param.ExpressionType != null ? param.ExpressionType.ToInvariantString () : "System.Void");
				methodDecl.TypeReference.IsKeyword = true;
				methodDecl.Body = new BlockStatement ();
				methodDecl.Body.AddChild (new ReturnStatement (node as Expression));
			}

			foreach (KeyValuePair<string, IReturnType> var in param.Parameters) {
				TypeReference typeReference = new TypeReference (var.Value.ToInvariantString ());
				typeReference.IsKeyword = true;
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, var.Key);
				if (!oneChangedVariable && param.ChangedVariables.Contains (var.Key))
					pde.ParamModifier |= ICSharpCode.NRefactory.Ast.ParameterModifiers.Ref;
				methodDecl.Parameters.Add (pde);
			}

			insertNewMethod.InsertedText = Environment.NewLine + Environment.NewLine + provider.OutputNode (options.Dom, methodDecl, options.GetIndent (param.DeclaringMember));
			result.Add (insertNewMethod);

			return result;
		}
	}
}
