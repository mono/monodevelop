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
			ExtractMethodParameters param = CreateParameters (options);
			if (param == null)
				return;
			ExtractMethodDialog dialog = new ExtractMethodDialog (options, this, param);
			dialog.TransientFor = IdeApp.Workbench.RootWindow;
			dialog.Show ();
		}
		
		public ExtractMethodParameters CreateParameters (RefactoringOptions options)
		{
			var buffer = options.Document.TextEditor;
			
			if (buffer.SelectionStartPosition - buffer.SelectionEndPosition == 0)
				return null;

			ParsedDocument doc = options.ParseDocument ();
			if (doc == null || doc.CompilationUnit == null)
				return null;

			int line, column;
			buffer.GetLineColumnFromPosition (buffer.CursorPosition, out line, out column);
			IMember member = doc.CompilationUnit.GetMemberAt (line, column);
			if (member == null)
				return null;
			
			ExtractMethodParameters param = new ExtractMethodParameters () {
				DeclaringMember = member,
				Location = new DomLocation (line, column)
			};
			Analyze (options, param, true);
			return param;
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
			
			public string Text {
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
			
			public Dictionary<string, VariableDescriptor> VariablesOutside {
				get;
				set;
			}
			public List<VariableDescriptor> OutsideVariableList {
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
			
			public List<VariableDescriptor> VariablesToDefine { get; set; }
			public List<VariableDescriptor> ChangedVariablesUsedOutside { get; set; }
			public List<VariableDescriptor> VariablesToGenerate { get; set; }
			
			public bool OneChangedVariable { get; set; }
		}
		
		ICSharpCode.NRefactory.Ast.INode Analyze (RefactoringOptions options, ExtractMethodParameters param, bool fillParameter)
		{
			IResolver resolver = options.GetResolver ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			if (resolver == null || provider == null)
				return null;

			string text = options.Document.TextEditor.GetText (options.Document.TextEditor.SelectionStartPosition, options.Document.TextEditor.SelectionEndPosition);
			param.Text = text;
			ICSharpCode.NRefactory.Ast.INode result = provider.ParseText (text);

			VariableLookupVisitor visitor = new VariableLookupVisitor (resolver, param.Location);

			if (fillParameter) {
				if (result != null)
					result.AcceptVisitor (visitor, null);
				if (result is Expression) {
					ResolveResult resolveResult = resolver.Resolve (new ExpressionResult (text), param.Location);
					if (resolveResult != null)
						param.ExpressionType = resolveResult.ResolvedType;
				}
				foreach (VariableDescriptor varDescr in visitor.VariableList.Where (v => !v.IsDefined && v.InitialValueUsed)) {
					param.Parameters.Add (varDescr);
				}
				param.Variables = new List<VariableDescriptor> (visitor.Variables.Values);
				foreach (VariableDescriptor varDescr in visitor.VariableList.Where (v => !v.IsDefined && param.Variables.Contains (v))) {
					if (param.Parameters.Contains (varDescr))
						continue;
					param.Parameters.Add (varDescr);
				}
				
				param.ReferencesMember = visitor.ReferencesMember;
				param.ChangedVariables = new HashSet<string> (visitor.Variables.Values.Where (v => v.GetsChanged).Select (v => v.Name));
				
				// analyze the variables outside of the selected text
				TextEditorData data = options.GetTextEditorData ();
				IMember member = param.DeclaringMember;
			
				int startOffset = data.Document.LocationToOffset (member.BodyRegion.Start.Line, member.BodyRegion.Start.Column);
				int endOffset = data.Document.LocationToOffset (member.BodyRegion.End.Line, member.BodyRegion.End.Column);
				text = data.Document.GetTextBetween (startOffset, data.SelectionRange.Offset) + data.Document.GetTextBetween (data.SelectionRange.EndOffset, endOffset);
				ICSharpCode.NRefactory.Ast.INode parsedNode = provider.ParseText (text);
				visitor = new VariableLookupVisitor (resolver, param.Location);
				if (parsedNode != null)
					parsedNode.AcceptVisitor (visitor, null);
				param.VariablesOutside = visitor.Variables;
				param.OutsideVariableList = visitor.VariableList;
				
				param.ChangedVariablesUsedOutside = new List<VariableDescriptor> (param.Variables.Where (v => v.GetsChanged && param.VariablesOutside.ContainsKey (v.Name)));
				param.OneChangedVariable = result is BlockStatement;
				if (param.OneChangedVariable) 
					param.OneChangedVariable = param.ChangedVariablesUsedOutside.Count == 1;
				
				param.VariablesToGenerate = new List<VariableDescriptor> (param.ChangedVariablesUsedOutside.Where (v => v.IsDefined));
				foreach (VariableDescriptor var in param.VariablesToGenerate) {
					param.Parameters.Add (var);
				}
				if (param.OneChangedVariable) {
					param.VariablesToDefine = new List<VariableDescriptor> (param.Parameters.Where (var => !var.InitialValueUsed));
					param.VariablesToDefine.ForEach (var => param.Parameters.Remove (var));
				} else {
					param.VariablesToDefine = new List<VariableDescriptor> ();
				}
			}
			
			return result;
		}
		
		public override List<Change> PerformChanges (RefactoringOptions options, object prop)
		{
			List<Change> result = new List<Change> ();
			ExtractMethodParameters param = (ExtractMethodParameters)prop;
			TextEditorData data = options.GetTextEditorData ();
			INRefactoryASTProvider provider = options.GetASTProvider ();
			IResolver resolver = options.GetResolver ();
			ICSharpCode.NRefactory.Ast.INode node = Analyze (options, param, false);
			
			if (param.VariablesToGenerate.Count > 0) {
				TextReplaceChange varGen = new TextReplaceChange ();
				varGen.Description = GettextCatalog.GetString ("Generate some temporary variables");
				varGen.FileName = options.Document.FileName;
				LineSegment line = data.Document.GetLine (Math.Max (0, data.Document.OffsetToLineNumber (data.SelectionRange.Offset) - 1));
				varGen.Offset = line.Offset + line.EditableLength;
				varGen.InsertedText = Environment.NewLine + options.GetWhitespaces (line.Offset);
				foreach (VariableDescriptor var in param.VariablesToGenerate) {
					TypeReference tr = options.ShortenTypeName (var.ReturnType).ConvertToTypeReference ();
					varGen.InsertedText += provider.OutputNode (options.Dom, new LocalVariableDeclaration (new VariableDeclaration (var.Name, null, tr)));
				}
				result.Add (varGen);
			}
			
			InvocationExpression invocation = new InvocationExpression (new IdentifierExpression (param.Name));
			foreach (VariableDescriptor var in param.Parameters) {
				if (!param.OneChangedVariable && param.ChangedVariables.Contains (var.Name)) {
					FieldDirection fieldDirection = FieldDirection.Ref;
					VariableDescriptor outsideVar = null;
					if (param.VariablesOutside.TryGetValue (var.Name, out outsideVar) && (var.GetsAssigned || param.VariablesToGenerate.Where (v => v.Name == var.Name).Any ())) {
						if (!outsideVar.GetsAssigned)
							fieldDirection = FieldDirection.Out;
					}
					invocation.Arguments.Add (new DirectionExpression (fieldDirection, new IdentifierExpression (var.Name)));
				} else {
					invocation.Arguments.Add (new IdentifierExpression (var.Name));
				}
			}
			
		//	string mimeType = DesktopService.GetMimeTypeForUri (options.Document.FileName);
			TypeReference returnType = new TypeReference ("System.Void", true);

			ICSharpCode.NRefactory.Ast.INode outputNode;
			if (param.OneChangedVariable) {
				string name = param.ChangedVariables.First ();
				returnType = options.ShortenTypeName (param.Variables.Find (v => v.Name == name).ReturnType).ConvertToTypeReference ();
				if (param.OutsideVariableList.Any (v => v.Name == name && !v.IsDefined)) {
					LocalVariableDeclaration varDecl = new LocalVariableDeclaration (returnType);
					varDecl.Variables.Add (new VariableDeclaration (name, invocation));
					outputNode = varDecl;
				} else {
					outputNode = new ExpressionStatement (new AssignmentExpression (new IdentifierExpression (name), ICSharpCode.NRefactory.Ast.AssignmentOperatorType.Assign, invocation));
				}
			} else {
				outputNode = node is BlockStatement ? (ICSharpCode.NRefactory.Ast.INode)new ExpressionStatement (invocation) : invocation;
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

			ExtractMethodAstTransformer transformer = new ExtractMethodAstTransformer (param.VariablesToGenerate);
			node.AcceptVisitor (transformer, null);
			if (!param.OneChangedVariable && node is Expression) {
				ResolveResult resolveResult = resolver.Resolve (new ExpressionResult ("(" + provider.OutputNode (options.Dom, node) + ")"), new DomLocation (options.Document.TextEditor.CursorLine, options.Document.TextEditor.CursorColumn));
				if (resolveResult.ResolvedType != null)
					returnType = options.ShortenTypeName (resolveResult.ResolvedType).ConvertToTypeReference ();
			}
			
			MethodDeclaration methodDecl = new MethodDeclaration ();
			methodDecl.Name = param.Name;
			methodDecl.Modifier = param.Modifiers;
			methodDecl.TypeReference = returnType;
			
			if (!param.ReferencesMember)
				methodDecl.Modifier |= ICSharpCode.NRefactory.Ast.Modifiers.Static;
			if (node is BlockStatement) {
				methodDecl.Body = new BlockStatement ();
				methodDecl.Body.AddChild (new EmptyStatement ());
				if (param.OneChangedVariable)
					methodDecl.Body.AddChild (new ReturnStatement (new IdentifierExpression (param.ChangedVariables.First ())));
			} else if (node is Expression) {
				methodDecl.Body = new BlockStatement ();
				methodDecl.Body.AddChild (new ReturnStatement (node as Expression));
			}

			foreach (VariableDescriptor var in param.VariablesToDefine) {
				BlockStatement block = methodDecl.Body;
				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (options.ShortenTypeName (var.ReturnType).ConvertToTypeReference());
				varDecl.Variables.Add (new VariableDeclaration (var.Name));
				block.Children.Insert (0, varDecl);
			}
			
			foreach (VariableDescriptor var in param.Parameters) {
				TypeReference typeReference = options.ShortenTypeName (var.ReturnType).ConvertToTypeReference ();
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, var.Name);
				if (!param.OneChangedVariable) {
					if (param.ChangedVariables.Contains (var.Name))
						pde.ParamModifier = ICSharpCode.NRefactory.Ast.ParameterModifiers.Ref;
					if (param.VariablesToGenerate.Where (v => v.Name == var.Name).Any ()) {
						pde.ParamModifier = ICSharpCode.NRefactory.Ast.ParameterModifiers.Out;
					}
					VariableDescriptor outsideVar = null;
					if (var.GetsAssigned && param.VariablesOutside.TryGetValue (var.Name, out outsideVar)) {
						if (!outsideVar.GetsAssigned)
							pde.ParamModifier = ICSharpCode.NRefactory.Ast.ParameterModifiers.Out;
					}
				}
				
				methodDecl.Parameters.Add (pde);
			}

	/*		foreach (VariableDescriptor var in variablesToOutput) {
				TypeReference typeReference = options.ShortenTypeName (var.ReturnType).ConvertToTypeReference ();
				ParameterDeclarationExpression pde = new ParameterDeclarationExpression (typeReference, var.Name);
				if (!param.OneChangedVariable && param.ChangedVariables.Contains (var.Name))
					pde.ParamModifier |= ICSharpCode.NRefactory.Ast.ParameterModifiers.Out;
				methodDecl.Parameters.Add (pde);
			}*/
			if (node is BlockStatement) {
				string indent = options.GetIndent (param.DeclaringMember);
				string text = provider.OutputNode (options.Dom, methodDecl, indent);
				int emptyStatementMarker = text.LastIndexOf (';');
				if (param.OneChangedVariable)
					emptyStatementMarker = text.LastIndexOf (';', emptyStatementMarker - 1);
				text = text.Substring (0, emptyStatementMarker) + param.Text + text.Substring (emptyStatementMarker + 1);
				insertNewMethod.InsertedText = Environment.NewLine + Environment.NewLine + text;
			} else {
				insertNewMethod.InsertedText = Environment.NewLine + Environment.NewLine + provider.OutputNode (options.Dom, methodDecl, options.GetIndent (param.DeclaringMember));
			}
			result.Add (insertNewMethod);

			return result;
		}
	}
}
