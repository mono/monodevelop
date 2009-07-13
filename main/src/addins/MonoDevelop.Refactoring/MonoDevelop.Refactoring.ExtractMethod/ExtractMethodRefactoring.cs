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
using Mono.TextEditor;

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
				DeclaringMember = member,
				Location = new DomLocation (line, column)
			};
			Analyze (options, param, true);
			ExtractMethodDialog dialog = new ExtractMethodDialog (options, this, param);
			dialog.Show ();
		}
		
		public class ExtractMethodParameters 
		{
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
			
			public List<VariableDescriptor> Variables {
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
			
			List<VariableDescriptor> parameters = new List<VariableDescriptor> ();
			public List<VariableDescriptor> Parameters {
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

			string text = options.Document.TextEditor.GetText (options.Document.TextEditor.SelectionStartPosition, options.Document.TextEditor.SelectionEndPosition);

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

				foreach (VariableDescriptor varDescr in visitor.VariableList.Where (v => !v.IsDefined)) {
					param.Parameters.Add (varDescr);
				}
				param.ReferencesMember = visitor.ReferencesMember;
				param.Variables = new List<VariableDescriptor> (visitor.Variables.Values);
				param.ChangedVariables = new HashSet<string> (visitor.Variables.Values.Where (v => v.GetsChanged).Select (v => v.Name));
			}
			
			return result;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			ExtractMethodParameters param = (ExtractMethodParameters)prop;
			IMember member = param.DeclaringMember;
			TextEditorData data = options.GetTextEditorData ();
			int startOffset = data.Document.LocationToOffset (member.BodyRegion.Start.Line, member.BodyRegion.Start.Column);
			int endOffset = data.Document.LocationToOffset (member.BodyRegion.End.Line, member.BodyRegion.End.Column);

			string text = data.Document.GetTextBetween (startOffset, data.SelectionRange.Offset) + data.Document.GetTextBetween (data.SelectionRange.EndOffset, endOffset);
			INRefactoryASTProvider provider = options.GetASTProvider ();
			IResolver resolver = options.GetResolver ();
			INode parsedNode = provider.ParseText (text);
			VariableLookupVisitor visitor = new VariableLookupVisitor (resolver, param.Location);
			parsedNode.AcceptVisitor (visitor, null);


			INode node = Analyze (options, param, false);

			List<VariableDescriptor> changedVariablesUsedOutside = new List<VariableDescriptor> (from v in param.Variables
				where visitor.Variables.ContainsKey (v.Name) && v.GetsChanged
				select v);
			List<VariableDescriptor> variablesToGenerate = new List<VariableDescriptor> (changedVariablesUsedOutside.Where (v => !visitor.Variables[v.Name].IsDefined));
			if (variablesToGenerate.Count > 0) {
				TextReplaceChange varGen = new TextReplaceChange ();
				varGen.Description = GettextCatalog.GetString ("Generate some temporary variables");
				varGen.FileName = options.Document.FileName;
				LineSegment line = data.Document.GetLine (Math.Max (0, data.Document.OffsetToLineNumber (data.SelectionRange.Offset) - 1));
				varGen.Offset = line.Offset + line.EditableLength;
				varGen.InsertedText = Environment.NewLine + options.GetWhitespaces (line.Offset);
				foreach (VariableDescriptor var in variablesToGenerate) {
					TypeReference tr = new TypeReference (var.ReturnType.ToInvariantString ());
					tr.IsKeyword = true;
					varGen.InsertedText += provider.OutputNode (options.Dom, new LocalVariableDeclaration (new VariableDeclaration (var.Name, null, tr)));
				}
				result.Add (varGen);
			}

			bool oneChangedVariable = node is BlockStatement;
			if (oneChangedVariable) {
				oneChangedVariable = changedVariablesUsedOutside.Count == 1;
			}

			InvocationExpression invocation = new InvocationExpression (new IdentifierExpression (param.Name));
			foreach (VariableDescriptor var in param.Parameters) {
				if (!oneChangedVariable && param.ChangedVariables.Contains (var.Name)) {
					invocation.Arguments.Add (new DirectionExpression (FieldDirection.Ref, new IdentifierExpression (var.Name)));
				} else {
					invocation.Arguments.Add (new IdentifierExpression (var.Name));
				}
			}
			foreach (VariableDescriptor var in variablesToGenerate) {
				invocation.Arguments.Add (new DirectionExpression (FieldDirection.Out, new IdentifierExpression (var.Name)));
			}
			string mimeType = DesktopService.GetMimeTypeForUri (options.Document.FileName);
			TypeReference returnType = new TypeReference ("System.Void");

			INode outputNode;
			if (oneChangedVariable) {
				string name = param.ChangedVariables.First ();
				returnType = new TypeReference (param.Variables.Find (v => v.Name == name).ReturnType.ToInvariantString ());
				returnType.IsKeyword = true;
				if (visitor.VariableList.Any (v => v.Name == name && !v.IsDefined)) {
					LocalVariableDeclaration varDecl = new LocalVariableDeclaration (returnType);
					varDecl.Variables.Add (new VariableDeclaration (name, invocation));
					outputNode = varDecl;
				} else {
					outputNode = new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (name), ICSharpCode.NRefactory.Ast.AssignmentOperatorType.Assign, invocation));
				}
			} else {
				outputNode = node is BlockStatement ? (INode)new ExpressionStatement (invocation) : invocation;
			}

			TextReplaceChange replacement = new TextReplaceChange ();
			replacement.Description = string.Format (GettextCatalog.GetString ("Substitute selected statement(s) with call to {0}"), param.Name);
			replacement.FileName = options.Document.FileName;
			replacement.Offset = options.Document.TextEditor.SelectionStartPosition;
			replacement.RemovedChars = options.Document.TextEditor.SelectionEndPosition - options.Document.TextEditor.SelectionStartPosition;
			LineSegment line1 = data.Document.GetLineByOffset (options.Document.TextEditor.SelectionEndPosition);
			if (options.Document.TextEditor.SelectionEndPosition == line1.Offset) {
				if (line1.Offset > 0) {
					LineSegment line2 = data.Document.GetLineByOffset (line1.Offset - 1);
					replacement.RemovedChars -= line2.DelimiterLength;
				}
			}
			
			replacement.InsertedText = options.GetWhitespaces (options.Document.TextEditor.SelectionStartPosition) + provider.OutputNode (options.Dom, outputNode);
			
			result.Add (replacement);

			TextReplaceChange insertNewMethod = new TextReplaceChange ();
			insertNewMethod.FileName = options.Document.FileName;
			insertNewMethod.Description = string.Format (GettextCatalog.GetString ("Create new method {0} from selected statement(s)"), param.Name);
			insertNewMethod.Offset = options.Document.TextEditor.GetPositionFromLineColumn (param.DeclaringMember.BodyRegion.End.Line, param.DeclaringMember.BodyRegion.End.Column);

			ExtractMethodAstTransformer transformer = new ExtractMethodAstTransformer (variablesToGenerate);
			node.AcceptVisitor (transformer, null);
			
			MethodDeclaration methodDecl = new MethodDeclaration ();
			methodDecl.Name = param.Name;
			methodDecl.Modifier = param.Modifiers;
			if (!param.ReferencesMember)
				methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
			if (node is BlockStatement) {
				methodDecl.TypeReference = returnType;
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

			foreach (VariableDescriptor var in param.Parameters) {
				TypeReference typeReference = new TypeReference (var.ReturnType.ToInvariantString ());
				typeReference.IsKeyword = true;
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, var.Name);
				if (!oneChangedVariable && param.ChangedVariables.Contains (var.Name))
					pde.ParamModifier |= ICSharpCode.NRefactory.Ast.ParameterModifiers.Ref;
				methodDecl.Parameters.Add (pde);
			}

			foreach (VariableDescriptor var in variablesToGenerate) {
				TypeReference typeReference = new TypeReference (var.ReturnType.ToInvariantString ());
				typeReference.IsKeyword = true;
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, var.Name);
				if (!oneChangedVariable && param.ChangedVariables.Contains (var.Name))
					pde.ParamModifier |= ICSharpCode.NRefactory.Ast.ParameterModifiers.Out;
				methodDecl.Parameters.Add (pde);
			}
			
			insertNewMethod.InsertedText = Environment.NewLine + Environment.NewLine + provider.OutputNode (options.Dom, methodDecl, options.GetIndent (param.DeclaringMember));
			result.Add (insertNewMethod);

			return result;
		}
	}
}
