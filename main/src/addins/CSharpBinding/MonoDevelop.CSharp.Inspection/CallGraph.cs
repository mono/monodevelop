// 
// CallGraph.cs
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
using System.Linq;
using System.Collections.Generic;
using ICSharpCode.NRefactory.CSharp;
using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Parser;
using Mono.TextEditor;
using MonoDevelop.Refactoring;

namespace MonoDevelop.CSharp.Inspection
{
	public class CallGraph
	{
		public List<AstType> PossibleTypeReferences {
			get;
			private set;
		}
		
		HashSet<string> usedUsings = new HashSet<string> ();
		public HashSet<string> UsedUsings {
			get {
				return usedUsings;
			}
		}
		
		public CallGraph ()
		{
		}
		
		public void Inspect (MonoDevelop.Ide.Gui.Document doc, IResolver resolver, ICSharpCode.NRefactory.CSharp.CompilationUnit unit)
		{
//			var findTypeReferencesVisitor = new MonoDevelop.Refactoring.RefactorImports.FindTypeReferencesVisitor (doc.Editor, resolver);
//			unit.AcceptVisitor (findTypeReferencesVisitor, null);
//			this.PossibleTypeReferences = findTypeReferencesVisitor.PossibleTypeReferences;
//			
//			foreach (var r in PossibleTypeReferences) {
//				if (r is PrimitiveType)
//					continue;
//				var loc = new DomLocation (r.StartLocation.Line, r.StartLocation.Column);
//				IType type = doc.Dom.SearchType (doc.CompilationUnit,
//					doc.CompilationUnit.GetTypeAt (loc), 
//					loc,
//					r.ConvertToReturnType ());
//				
//				if (type != null)
//					usedUsings.Add (type.Namespace);
//			}
		}
		
		
		public class VariableInfo 
		{
			public AstNode Decl {
				get;
				private set;
			}
			
			public bool IsUsed {
				get;
				set;
			}
			
			public bool IsAssigned {
				get;
				set;
			}
			
			public bool IsUsedOutsideOfConstructor {
				get;
				set;
			}
			
			public VariableInfo (AstNode decl)
			{
				this.Decl = decl;
				this.IsAssigned = decl is VariableInitializer ? !((VariableInitializer)decl).Initializer.IsNull : !((ParameterDeclaration)decl).DefaultExpression.IsNull;
			}
		}
		
		
		public class Context 
		{
			public Context Parent {
				get;
				set;
			}
			
			Dictionary<string, VariableInfo> fields      = new Dictionary<string, VariableInfo> ();
			Dictionary<string, VariableInfo> locals      = new Dictionary<string, VariableInfo> ();
			Dictionary<string, VariableInfo> parameters  = new Dictionary<string, VariableInfo> ();
			
			public VariableInfo GetInfo (string name)
			{
				VariableInfo info;
				
				if (locals.TryGetValue (name, out info))
					return info;
				if (parameters.TryGetValue (name, out info))
					return info;
				if (fields.TryGetValue (name, out info))
					return info;
				return Parent != null ? Parent.GetInfo (name) : null;
			}
			
			public Context ()
			{
				
			}
			
			public Context (Context parent)
			{
				this.Parent = parent;
			}
			
			public void AddField (string name, VariableInfo info)
			{
				fields.Add (name, info);
			}
			
			public void AddParameter (string name, VariableInfo info)
			{
				parameters.Add (name, info);
			}
			
			public void AddLocal (string name, VariableInfo info)
			{
				locals.Add (name, info);
			}
			
		}
		
		class CallgraphVisitor : DepthFirstAstVisitor<object, object>
		{
			Context curContext;
			
			
			/*
			public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
			{
				curContext = new Context ();
				base.VisitTypeDeclaration (typeDeclaration, data);
				curContext = curContext.Parent;
				return null;
			}
			
			public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
			{
				curContext = new Context (curContext);
				base.VisitMethodDeclaration (methodDeclaration, data);
				curContext = curContext.Parent;
				return null;
			}
			
			public override object VisitAccessor (Accessor accessor, object data)
			{
				curContext = new Context (curContext);
				base.VisitAccessor (accessor, data);
				curContext = curContext.Parent;
				return null;
			}
			
			public override object VisitIndexerExpression (IndexerExpression indexerExpression, object data)
			{
				curContext = new Context (curContext);
				base.VisitIndexerExpression (indexerExpression, data);
				curContext = curContext.Parent;
				return null;
			}
			
			
			public override object VisitParameterDeclaration (ParameterDeclaration parameterDeclaration, object data)
			{
				curContext.AddParameter (parameterDeclaration.Name, new VariableInfo (parameterDeclaration));
				return base.VisitParameterDeclaration (parameterDeclaration, data);
			}
			
			public override object VisitVariableDeclarationStatement (VariableDeclarationStatement variableDeclarationStatement, object data)
			{
				foreach (var varDecl in variableDeclarationStatement.Variables) {
					curContext.AddLocal (varDecl.Name, new VariableInfo (varDecl));
				}
				
				return base.VisitVariableDeclarationStatement (variableDeclarationStatement, data);
			}
			
			public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
			{
				foreach (var varDecl in fieldDeclaration.Variables) {
					curContext.AddField (varDecl.Name, new VariableInfo (varDecl));
				}
				
				return base.VisitFieldDeclaration (fieldDeclaration, data);
			}
			
			public override object VisitIdentifierExpression (IdentifierExpression identifierExpression, object data)
			{
				var v = curContext.GetInfo (identifierExpression.Identifier);
				if (v != null) {
					v.IsAssigned = 
				}
				return null;
			}
			
			public override object VisitBinaryOperatorExpression (BinaryOperatorExpression binaryOperatorExpression, object data)
			{
				VisitChildren (binaryOperatorExpression.Left, data);
				
				binaryOperatorExpression
				return base.VisitBinaryOperatorExpression (binaryOperatorExpression, data);
			}*/
			
		}
	}
}

