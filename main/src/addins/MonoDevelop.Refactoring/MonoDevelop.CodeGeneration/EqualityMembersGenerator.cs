// 
// EqualityMembersGenerator.cs
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

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.PrettyPrinter;

using MonoDevelop.Core;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects.Dom;
using Gtk;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Core.Gui;
using System.Collections.Generic;
using MonoDevelop.Refactoring;
using System.Text;

namespace MonoDevelop.CodeGeneration
{
	
	public class EqualityMembersGenerator : ICodeGenerator
	{
		public string Icon {
			get {
				return "md-newmethod";
			}
		}
		
		public string Text {
			get {
				return GettextCatalog.GetString ("Equality members");
			}
		}
		
		public string GenerateDescription {
			get {
				return GettextCatalog.GetString ("Select members to include in equality.");
			}
		}
		
		public bool IsValid (CodeGenerationOptions options)
		{
			return new CreateEquality (options).IsValid ();
		}
		
		public IGenerateAction InitalizeSelection (CodeGenerationOptions options, Gtk.TreeView treeView)
		{
			CreateEquality createEventMethod = new CreateEquality (options);
			createEventMethod.Initialize (treeView);
			return createEventMethod;
		}
		
		class CreateEquality : IGenerateAction
		{
			CodeGenerationOptions options;
			TreeStore store = new TreeStore (typeof(bool), typeof(Gdk.Pixbuf), typeof(string), typeof(IMember));
			
			public CreateEquality (CodeGenerationOptions options)
			{
				this.options = options;
			}
			
			public void Initialize (Gtk.TreeView treeView)
			{
				TreeViewColumn column = new TreeViewColumn ();

				CellRendererToggle toggleRenderer = new CellRendererToggle ();
				toggleRenderer.Toggled += ToggleRendererToggled;
				column.PackStart (toggleRenderer, false);
				column.AddAttribute (toggleRenderer, "active", 0);

				CellRendererPixbuf pixbufRenderer = new CellRendererPixbuf ();
				column.PackStart (pixbufRenderer, false);
				column.AddAttribute (pixbufRenderer, "pixbuf", 1);

				CellRendererText textRenderer = new CellRendererText ();
				column.PackStart (textRenderer, true);
				column.AddAttribute (textRenderer, "text", 2);
				column.Expand = true;

				treeView.AppendColumn (column);
				
				foreach (IField field in options.EnclosingType.Fields) {
					store.AppendValues (false, ImageService.GetPixbuf (field.StockIcon, IconSize.Menu), field.Name, field);
				}

				foreach (IProperty property in options.EnclosingType.Properties) {
					if (property.GetRegion.Start.Line != property.GetRegion.End.Line || property.GetRegion.End.Column - property.GetRegion.Start.Column != 4)
						continue;
					store.AppendValues (false, ImageService.GetPixbuf (property.StockIcon, IconSize.Menu), property.Name, property);
				}
				treeView.Model = store;
			}
			
			public bool IsValid ()
			{
				if (options.EnclosingType == null || options.EnclosingMember != null)
					return false;
				List<IMember> list = options.EnclosingType.SearchMember ("GetHashCode", true);
				if (list != null && list.Count > 0)
					return false;
				
				if (options.EnclosingType.FieldCount > 0)
					return true;

				foreach (IProperty property in options.EnclosingType.Properties) {
					if (property.GetRegion.Start.Line != property.GetRegion.End.Line || property.GetRegion.End.Column - property.GetRegion.Start.Column != 4)
						continue;
					return true;
				}
				return false;
			}
			
			public void GenerateCode ()
			{
				TreeIter iter;
				if (!store.GetIterFirst (out iter))
					return;

				List<IMember> includedMembers = new List<IMember> ();
				do {
					bool include = (bool)store.GetValue (iter, 0);
					if (include)
						includedMembers.Add ((IMember)store.GetValue (iter, 3));
				} while (store.IterNext (ref iter));

				INRefactoryASTProvider astProvider = options.GetASTProvider ();
				if (astProvider == null)
					return;

				StringBuilder output = new StringBuilder ();

				// Genereate Equals
				MethodDeclaration methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "Equals";

				methodDeclaration.TypeReference = DomReturnType.Bool.ConvertToTypeReference ();
				methodDeclaration.Modifier = ICSharpCode.NRefactory.Ast.Modifiers.Public | ICSharpCode.NRefactory.Ast.Modifiers.Override;
				methodDeclaration.Body = new BlockStatement ();
				methodDeclaration.Parameters.Add (new ParameterDeclarationExpression (DomReturnType.Object.ConvertToTypeReference (), "obj"));
				IdentifierExpression paramId = new IdentifierExpression ("obj");
				IfElseStatement ifStatement = new IfElseStatement (null);
				ifStatement.Condition = new BinaryOperatorExpression (paramId, BinaryOperatorType.Equality, new PrimitiveExpression (null));
				ifStatement.TrueStatement.Add (new ReturnStatement (new PrimitiveExpression (false)));
				methodDeclaration.Body.AddChild (ifStatement);

				ifStatement = new IfElseStatement (null);
				List<Expression> arguments = new List<Expression> ();
				arguments.Add (new ThisReferenceExpression ());
				arguments.Add (paramId);
				ifStatement.Condition = new InvocationExpression (new IdentifierExpression ("ReferenceEquals"), arguments);
				ifStatement.TrueStatement.Add (new ReturnStatement (new PrimitiveExpression (true)));
				methodDeclaration.Body.AddChild (ifStatement);

				ifStatement = new IfElseStatement (null);
				ifStatement.Condition = new BinaryOperatorExpression (new InvocationExpression (new MemberReferenceExpression (paramId, "GetType")), BinaryOperatorType.InEquality, new TypeOfExpression (new TypeReference (options.EnclosingType.Name)));
				ifStatement.TrueStatement.Add (new ReturnStatement (new PrimitiveExpression (false)));
				methodDeclaration.Body.AddChild (ifStatement);

				LocalVariableDeclaration varDecl = new LocalVariableDeclaration (new DomReturnType (options.EnclosingType).ConvertToTypeReference ());
				varDecl.Variables.Add (new VariableDeclaration ("other", new CastExpression (varDecl.TypeReference, paramId, CastType.Cast)));
				methodDeclaration.Body.AddChild (varDecl);

				IdentifierExpression otherId = new IdentifierExpression ("other");
				Expression binOp = null;
				foreach (IMember member in includedMembers) {
					Expression right = new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.Equality, new MemberReferenceExpression (otherId, member.Name));
					if (binOp == null) {
						binOp = right;
					} else {
						binOp = new BinaryOperatorExpression (binOp, BinaryOperatorType.LogicalAnd, right);
					}
				}

				methodDeclaration.Body.AddChild (new ReturnStatement (binOp));

				output.Append (astProvider.OutputNode (options.Dom, methodDeclaration, RefactoringOptions.GetIndent (options.Document, options.EnclosingType) + "\t"));

				methodDeclaration = new MethodDeclaration ();
				methodDeclaration.Name = "GetHashCode";
				
				methodDeclaration.TypeReference = DomReturnType.Int32.ConvertToTypeReference ();
				methodDeclaration.Modifier = ICSharpCode.NRefactory.Ast.Modifiers.Public | ICSharpCode.NRefactory.Ast.Modifiers.Override;
				methodDeclaration.Body = new BlockStatement ();
				
				binOp = null;
				foreach (IMember member in includedMembers) {
					Expression right;
					right = new InvocationExpression (new MemberReferenceExpression (new IdentifierExpression (member.Name), "GetHashCode"));
					
					IType type = options.Dom.SearchType (new SearchTypeRequest (options.Document.ParsedDocument.CompilationUnit, member.ReturnType, options.EnclosingType));
					if (type != null && type.ClassType != MonoDevelop.Projects.Dom.ClassType.Struct)
						right = new ParenthesizedExpression (new ConditionalExpression (new BinaryOperatorExpression (new IdentifierExpression (member.Name), BinaryOperatorType.InEquality, new PrimitiveExpression (null)), right, new PrimitiveExpression (0)));
					
					if (binOp == null) {
						binOp = right;
					} else {
						binOp = new BinaryOperatorExpression (binOp, BinaryOperatorType.ExclusiveOr, right);
					}
				}
				BlockStatement uncheckedBlock = new BlockStatement ();
				uncheckedBlock.AddChild (new ReturnStatement (binOp));
				
				methodDeclaration.Body.AddChild (new UncheckedStatement (uncheckedBlock));
				output.AppendLine ();
				output.Append (astProvider.OutputNode (options.Dom, methodDeclaration, RefactoringOptions.GetIndent (options.Document, options.EnclosingType) + "\t"));
				options.Document.TextEditor.InsertText (options.Document.TextEditor.GetPositionFromLineColumn (options.Document.TextEditor.CursorLine, 1), output.ToString ());
			}
			
			void ToggleRendererToggled (object o, ToggledArgs args)
			{
				Gtk.TreeIter iter;
				if (store.GetIterFromString (out iter, args.Path)) {
					bool active = (bool)store.GetValue (iter, 0);
					store.SetValue (iter, 0, !active);
				}
			}
		}
	}
}
