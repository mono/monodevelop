//
// CodeGenerator.cs
//
// Authors:
//   Lluis Sanchez Gual
//   Jeffrey Stedfast
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.CodeGeneration;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;

using CSharpBinding.Parser.SharpDevelopTree;

using ClassType = MonoDevelop.Projects.Dom.ClassType;

namespace CSharpBinding.Parser
{
	class CSharpRefactorer: BaseRefactorer
	{
		CSharpEnhancedCodeProvider csharpProvider = new CSharpEnhancedCodeProvider ();
		
		public override RefactorOperations SupportedOperations {
			get { return RefactorOperations.All; }
		}
		
		protected override CodeDomProvider GetCodeDomProvider ()
		{
			return csharpProvider;
		}
		
		public override string ConvertToLanguageTypeName (string netTypeName)
		{
			Console.WriteLine ("Convert : '{0}'", netTypeName);
			string result = CSharpAmbience.TypeConversionTable[netTypeName] as string;
			if (result != null)
				return result;
			return netTypeName;
		}
		
		public override IType RenameClass (RefactorerContext ctx, IType cls, string newName)
		{
			IEditableTextFile file;
			int pos, begin, end;

			Match match;
			Regex expr;
			string txt;
			foreach (IType pclass in cls.Parts) {
				if (pclass.BodyRegion.IsEmpty || (file = ctx.GetFile (pclass.CompilationUnit.FileName)) == null)
					continue;
				
				begin = file.GetPositionFromLineColumn (pclass.BodyRegion.Start.Line, pclass.BodyRegion.Start.Column);
				end = file.GetPositionFromLineColumn (pclass.BodyRegion.End.Line, pclass.BodyRegion.End.Column);
				
				if (begin == -1 || end == -1)
					continue;
				
				txt = file.GetText (begin, end);
				
				switch (cls.ClassType) {
				case ClassType.Interface:
					expr = new Regex (@"\sinterface\s*(" + cls.Name + @")(\s|:)", RegexOptions.Multiline);
					break;
				case ClassType.Struct:
					expr = new Regex (@"\sstruct\s*(" + cls.Name + @")(\s|:)", RegexOptions.Multiline);
					break;
				case ClassType.Enum:
					expr = new Regex (@"\senum\s*(" + cls.Name + @")(\s|:)", RegexOptions.Multiline);
					break;
				default:
					expr = new Regex (@"\sclass\s*(" + cls.Name + @")(\s|:)", RegexOptions.Multiline);
					break;
				}
				
				match = expr.Match (" " + txt + " ");
				
				if (!match.Success)
					continue;
				
				pos = begin + match.Groups [1].Index - 1;
				file.DeleteText (pos, cls.Name.Length);
				file.InsertText (pos, newName);
			}
			
			file = ctx.GetFile (cls.CompilationUnit.FileName);
			
			return GetGeneratedClass (ctx, file, cls);
		}
		
		//static CodeStatement ThrowNewNotImplementedException ()
		//{
		//	CodeExpression expr = new CodeSnippetExpression ("new NotImplementedException ()");
		//	return new CodeThrowExceptionStatement (expr);
		//}
		//
		//public override IMember AddMember (RefactorerContext ctx, IType cls, CodeTypeMember member)
		//{
		//	if (member is CodeMemberProperty) {
		//		CodeMemberProperty prop = (CodeMemberProperty) member;
		//		if (prop.HasGet && prop.GetStatements.Count == 0)
		//			prop.GetStatements.Add (ThrowNewNotImplementedException ());
		//		if (prop.HasSet && prop.SetStatements.Count == 0)
		//			prop.SetStatements.Add (ThrowNewNotImplementedException ());
		//	} else if (member is CodeMemberMethod) {
		//		CodeMemberMethod method = (CodeMemberMethod) member;
		//		if (method.Statements.Count == 0)
		//			method.Statements.Add (ThrowNewNotImplementedException ());
		//	}
		//	
		//	return base.AddMember (ctx, cls, member);
		//}
		
		protected override void EncapsulateFieldImpGetSet (RefactorerContext ctx, IType cls, IField field, CodeMemberProperty prop)
		{
			if (prop.HasGet && prop.GetStatements.Count == 0)
				prop.GetStatements.Add (new CodeSnippetExpression ("return " + field.Name));
			
			if (prop.HasSet && prop.SetStatements.Count == 0)
				prop.SetStatements.Add (new CodeAssignStatement (new CodeVariableReferenceExpression (field.Name), new CodeVariableReferenceExpression ("value")));
		}
		
		public override IMember ImplementMember (RefactorerContext ctx, IType cls, IMember member, IReturnType privateImplementationType)
		{
			if (privateImplementationType != null) {
				// Workaround for bug in the code generator. Generic private implementation types are not generated correctly when they are generic.
				Ambience amb = new MonoDevelop.CSharpBinding.CSharpAmbience();
				string tn = amb.GetString (privateImplementationType, OutputFlags.IncludeGenerics | OutputFlags.UseFullName | OutputFlags.UseIntrinsicTypeNames);
				privateImplementationType = new DomReturnType (tn);
			}
			return base.ImplementMember (ctx, cls, member, privateImplementationType);
		}
		
		public override void ImplementMembers (RefactorerContext ctx, IType cls, 
		                                                      IEnumerable<KeyValuePair<IMember,IReturnType>> members,
		                                                      string foldingRegionName)
		{
			base.ImplementMembers (ctx, cls, FixGenericImpl (ctx, members), foldingRegionName);
		}
		
		// Workaround for bug in the code generator. Generic private implementation types are not generated correctly when they are generic.
		IEnumerable<KeyValuePair<IMember,IReturnType>> FixGenericImpl (RefactorerContext ctx, IEnumerable<KeyValuePair<IMember,IReturnType>> members)
		{
			Ambience amb = null;
			foreach (KeyValuePair<IMember,IReturnType> kvp in members) {
				if (kvp.Value == null) {
					yield return kvp;
					continue;
				}
								
				if (amb == null)
					amb = new MonoDevelop.CSharpBinding.CSharpAmbience ();
				string tn = amb.GetString (kvp.Value, OutputFlags.IncludeGenerics | OutputFlags.UseFullName | OutputFlags.UseIntrinsicTypeNames);
				yield return new KeyValuePair<IMember,IReturnType> (kvp.Key, new DomReturnType (tn));
			}
		}
		
		public override MemberReferenceCollection FindClassReferences (RefactorerContext ctx, string fileName, IType cls)
		{
//TODO:
			//Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			//MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, cls, cls, refs);
			
//			IEditableTextFile file = ctx.GetFile (fileName);
//			visitor.Visit (ctx.ParserContext, file);
			return refs;
		
		}
		
		protected override int GetVariableNamePosition (IEditableTextFile file, LocalVariable var)
		{
			int begin = file.GetPositionFromLineColumn (var.Location.Line, var.Location.Column);
			int end = file.GetPositionFromLineColumn (var.Location.Line, var.Location.Column + var.Name.Length);
			
			if (begin == -1 || end == -1)
				return -1;
			
			string txt = file.GetText (begin, end);
			
			int i = 0; /* = txt.IndexOf ('=');
			if (i == -1)
				i = txt.Length;*/
			
			int pos = -1;
			do {
				i = pos = txt.IndexOf (var.Name, i);
			} while ( (pos > 0 && !Char.IsLetter (file.GetCharAt (pos - 1))) &&
			          (pos + txt.Length + 1 < file.Length )&& !Char.IsLetterOrDigit (file.GetCharAt (pos + txt.Length + 1))
			         );
			if (pos == -1)
				return -1;
			
			return begin + pos;
		}
		
		protected override int GetParameterNamePosition (IEditableTextFile file, IParameter param)
		{
			IMember member = param.DeclaringMember;
			int begin = file.GetPositionFromLineColumn (member.BodyRegion.Start.Line, member.BodyRegion.Start.Column);
			int end = file.GetPositionFromLineColumn (member.BodyRegion.End.Line, member.BodyRegion.End.Column);
			
			if (begin == -1 || end == -1)
				return -1;
			
			string txt = file.GetText (begin, end);
			int open, close, i, j;
			char obrace, cbrace;
			
			if (member is IProperty) { // indexer
				obrace = '[';
				cbrace = ']';
			} else {
				obrace = '(';
				cbrace = ')';
			}
			
			if ((open = txt.IndexOf (obrace)) == -1)
				return -1;
			
			if ((close = txt.LastIndexOf (cbrace)) == -1)
				return -1;
			
			open++;
			
			while (open < close) {
				if ((i = txt.IndexOf (param.Name, open)) == -1)
					return -1;
				
				if (!Char.IsWhiteSpace (txt[i - 1]))
					return -1;
				
				j = i + param.Name.Length;
				if (j == close || Char.IsWhiteSpace (txt[j]) || txt[j] == ',')
					return begin + i;
				
				if ((open = txt.IndexOf (',', i)) == -1)
					return -1;
				
				open++;
			}
			
			return -1;
		}
		
		bool IsMatchedField (string txt, string field, int index)
		{
			char c;
			if (index > 0) {
				c = txt[index - 1];			
				if (Char.IsLetterOrDigit (c) || c == '_')
					return false;
			}
			if (index + field.Length < txt.Length) {
				c = txt[index + field.Length];
				
				if (Char.IsLetterOrDigit (c) || c == '_')
					return false;
			}
			return true;
		}
		
		protected override int GetMemberNamePosition (IEditableTextFile file, IMember member)
		{
			int begin = file.GetPositionFromLineColumn (member.BodyRegion.Start.Line, member.BodyRegion.Start.Column);
			int end = file.GetPositionFromLineColumn (member.BodyRegion.End.Line, member.BodyRegion.End.Column);
			
			if (begin == -1 || end == -1)
				return -1;
			
			string txt = file.GetText (begin, end);
			string name = member.Name;
			int len = txt.Length;
			int pos = -1;
			if (member is IField) {
				// Fields are different because multiple fields can be declared
				// in the same region and might even reference each other
				// e.g. "public int fu, bar = 1, baz = bar;"
				do {
					if ((pos = txt.IndexOf (member.Name, pos + 1)) == -1)
						return -1;
				} while (!IsMatchedField (txt, member.Name, pos));
				
				return begin + pos;
			} else if (member is IMethod) {
				if ((len = txt.IndexOf ('(')) == -1)
					return -1;
				
				if (((IMethod) member).IsConstructor)
					name = member.DeclaringType.Name;
			} else if (member is IEvent) {
				// no variables to change
			} else if (member is IProperty) {
				if (((IProperty)member).IsIndexer && (len = txt.IndexOf ('[')) == -1)
					return -1;
			} else {
				return -1;
			}
			
			if ((pos = txt.LastIndexOf (name, len)) == -1)
				return -1;
			
			return begin + pos;
		}
		
		protected override DomRegion GetMemberBounds (IEditableTextFile file, IMember member)
		{
			if (!(member is IField))
				return base.GetMemberBounds (file, member);
			
			// The idea here is that it is common to declare multiple fields in the same
			// statement, like so:
			//
			// public int fu, bar, baz;
			//
			// If @member is "bar", then we want to return the region containing:
			//
			// ", bar"
			//
			// so that when our caller uses this region to delete the text declaring @member,
			// it won't also delete the text declaring the other fields in this same statement.
			
			IType klass = member.DeclaringType;
			IField field = (IField) member;
			IField kfield = null, lastField = null, nextField = null;
			int lineBegin, lineEnd;
			int colBegin, colEnd;
			int pos;
			
			// find the offset of the field
			foreach (IField f in klass.Fields) {
				if (kfield != null) {
					nextField = f;
					break;
				}
				if (f.Name == field.Name) {
					kfield = f;
					continue;
				}
				lastField = f;
			}
			
			if (kfield != null && lastField.Location.CompareTo (field.Location) == 0) {
				// Field has other fields declared before it in the same statement
				pos = GetMemberNamePosition (file, member);
				
				// seek backward for declaration separator
				while (file.Text[pos] != ',')
					pos--;
				
				// eat up unneeded lwsp
				while (Char.IsWhiteSpace (file.Text[pos]))
					pos--;
				
				file.GetLineColumnFromPosition (pos, out lineBegin, out colBegin);
				
				if (nextField != null  && nextField.Location.CompareTo (field.Location) == 0) {
					// Field also has other fields declared after it in the same statement
					pos = GetMemberNamePosition (file, nextField);
					
					// seek backward for declaration separator
					while (file.Text[pos] != ',')
						pos--;
					
					// eat up unneeded lwsp
					while (Char.IsWhiteSpace (file.Text[pos]))
						pos--;
					
					file.GetLineColumnFromPosition (pos, out lineEnd, out colEnd);
				} else {
					// No fields after this...
					colEnd = field.BodyRegion.End.Column - 1;  // don't include the ';'
					lineEnd = field.BodyRegion.End.Line;
				}
			} else if (nextField != null  && nextField.Location.CompareTo (field.Location) == 0) {
				// Field has other fields declared after it in the same statement
				pos = GetMemberNamePosition (file, member);
				file.GetLineColumnFromPosition (pos, out lineBegin, out colBegin);
				pos = GetMemberNamePosition (file, nextField);
				file.GetLineColumnFromPosition (pos, out lineEnd, out colEnd);
			} else {
				// Field is declared in a statement by itself
				
				// fall back to default implementation
				return base.GetMemberBounds (file, member);
			}
			
			return new DomRegion (lineBegin, colBegin, lineEnd, colEnd);
		}
		
		public override MemberReferenceCollection FindMemberReferences (RefactorerContext ctx, string fileName, IType cls, IMember member)
		{
//TODO:
			//Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			//MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, cls, member, refs);
			
			//IEditableTextFile file = ctx.GetFile (fileName);
			//visitor.Visit (ctx.ParserContext, file);
			return refs;
		}
		
		public override MemberReferenceCollection FindVariableReferences (RefactorerContext ctx, string fileName, LocalVariable var)
		{
//TODO:
			//Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
//			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, null, var, refs);
			
//			IEditableTextFile file = ctx.GetFile (fileName);
//			visitor.Visit (ctx.ParserContext, file);
			return refs;
		}
		
		public override MemberReferenceCollection FindParameterReferences (RefactorerContext ctx, string fileName, IParameter param)
		{
//TODO:
//			IMember member = param.DeclaringMember;
//			Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
//			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, member.DeclaringType, param, refs);
			
//			IEditableTextFile file = ctx.GetFile (fileName);
//			visitor.Visit (ctx.ParserContext, file);
			
			return refs;
		}
		
		public override int AddFoldingRegion (RefactorerContext ctx, IType cls, string regionName)
		{
			IEditableTextFile buffer = ctx.GetFile (cls.CompilationUnit.FileName);
			int pos = GetNewMethodPosition (buffer, cls);
			
			int line, col;
			buffer.GetLineColumnFromPosition (pos, out line, out col);
			string indent = GetLineIndent (buffer, line);
			
			string pre = "#region " + regionName + " \n" + indent + "\n" + indent;
			string post = "\n" + indent + "\n" + indent + "#endregion \n" + indent;
			buffer.InsertText (pos, pre + post);
			return pos + pre.Length;
		}
		
		protected override CodeGeneratorOptions GetOptions (bool isMethod)
		{
			CodeGeneratorOptions ops = new CodeGeneratorOptions ();
			if (TextEditorProperties.ConvertTabsToSpaces)
				ops.IndentString = new String (' ', TextEditorProperties.TabIndent);
			else
				ops.IndentString = "\t";
			
			if (isMethod)
				ops.BracingStyle = "C";
			
			return ops;
		}
	}
	
	class MemberRefactoryVisitor: AbstractAstVisitor {
		MemberReferenceCollection references;
		MonoDevelop.Projects.Dom.ICompilationUnit fileCompilationUnit;
		IEditableTextFile file;
		RefactorerContext ctx;
		IType declaringType;
		IMember member;
		Resolver resolver;
		Hashtable unique;
		
		public MemberRefactoryVisitor (RefactorerContext ctx, Resolver resolver, IType declaringType, IMember member, MemberReferenceCollection references)
		{
			unique = new Hashtable ();
			
			this.ctx = ctx;
			this.resolver = resolver;
			this.declaringType = declaringType;
			this.references = references;
			this.member = member;
		}
		
		public void Visit (ProjectDom pctx, IEditableTextFile file)
		{
			this.file = file;
			// TODO
			fileCompilationUnit = null; //pctx.ParseFile (file);
			
		//	if (fileCompilationUnit != null)
		//		VisitCompilationUnit (fileCompilationUnit, null);
		}
		
		bool IsExpectedClass (IType type)
		{
			return IsExpectedClass (type, new Dictionary<string,string> ());
		}
		
		bool IsExpectedClass (IType type, Dictionary<string,string> checkedTypes)
		{
			if (checkedTypes.ContainsKey (type.FullName))
				return false;
			
			if (type.FullName == declaringType.FullName)
				return true;
			
			checkedTypes [type.FullName] = type.FullName;
			
			if (type.BaseTypes != null) {
				foreach (IReturnType bc in type.BaseTypes) {
					IType bcls = ctx.ParserContext.GetType (bc);
					if (bcls != null && IsExpectedClass (bcls, checkedTypes))
						return true;
				}
			}
			return false;
		}
		
		bool IsExpectedMember (IMember member)
		{
			IMember actual = ((IParameter) this.member).DeclaringMember;
			
			if (member.Name == actual.Name)
				return true;
			
			return false;
		}
		
		public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			//Debug ("FieldDeclaration", fieldDeclaration.ToString (), fieldDeclaration);
//TODO:
/*			string type = ReturnType.GetSystemType (fieldDeclaration.TypeReference.Type);
			if (member is IType && member.Name == GetNameWithoutPrefix (type)) {
				int line = fieldDeclaration.StartLocation.Y;
				int col = fieldDeclaration.StartLocation.X;
				IType cls = resolver.ResolveIdentifier (fileCompilationUnit, type, line, col) as IType;
				
				if (cls != null && cls.FullName == ((IType) member).FullName) {
					//Debug ("adding FieldDeclaration", cls.FullName, fieldDeclaration);
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			*/
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}
		
		public override object VisitTypeReference(TypeReference typeReference, object data)
		{
//TODO:
			/*string type = ReturnType.GetSystemType (typeReference.Type);
			if (member is IType && member.Name == GetNameWithoutPrefix (type)) {
				int line = typeReference.StartLocation.Y;
				int col = typeReference.StartLocation.X;
				IType cls = resolver.ResolveIdentifier (fileCompilationUnit, type, line, col) as IType;
				
				if (cls != null && cls.FullName == declaringType.FullName) {
					//Debug ("adding CastExpression", cls.FullName, castExpression);
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			*/
			return base.VisitTypeReference (typeReference, data);
		}
		
		public override object VisitMemberReferenceExpression (MemberReferenceExpression fieldExp, object data)
		{
			//Debug ("FieldReferenceExpression", fieldExp.FieldName, fieldExp);
//TODO:
/*			if (!(member is IParameter) && fieldExp.FieldName == member.Name) {
				IType cls = resolver.ResolveExpressionType (fileCompilationUnit, fieldExp.TargetObject, fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
				if (cls != null && IsExpectedClass (cls)) {
					int pos = file.GetPositionFromLineColumn (fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					int endpos = file.GetPositionFromLineColumn (fieldExp.EndLocation.Y, fieldExp.EndLocation.X);
					string txt = file.GetText (pos, endpos);
					if (txt == member.Name) {
						//Debug ("adding FieldReferenceExpression", member.Name, fieldExp);
						AddUniqueReference (fieldExp.StartLocation.Y, fieldExp.StartLocation.X, member.Name);
					}
				}
			}*/
			
			return base.VisitMemberReferenceExpression (fieldExp, data);
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data) 
		{
			// find override references.
//TODO:
/*			if (member is IMethod && (methodDeclaration.Modifier & ICSharpCode.NRefactory.Ast.Modifiers.Override) == ICSharpCode.NRefactory.Ast.Modifiers.Override && methodDeclaration.Name == member.Name) {
				IMember m = resolver.ResolveIdentifier (fileCompilationUnit, member.Name, methodDeclaration.StartLocation.Y, methodDeclaration.StartLocation.X) as IMember;
				if (IsExpectedClass (m.DeclaringType)) {
					AddUniqueReference (methodDeclaration.StartLocation.Y, methodDeclaration.StartLocation.X, member.Name);
				}
			}*/
			return base.VisitMethodDeclaration (methodDeclaration, data);
		}
		
		public override object VisitInvocationExpression (InvocationExpression invokeExp, object data)
		{
			//Debug ("InvocationExpression", invokeExp.ToString (), invokeExp);
//TODO:
/*			if (member is IMethod && invokeExp.TargetObject is MemberReferenceExpression) {
				MemberReferenceExpression fieldExp = (MemberReferenceExpression) invokeExp.TargetObject;
				if (fieldExp.FieldName == member.Name) {
					IType cls = resolver.ResolveExpressionType (fileCompilationUnit, fieldExp.TargetObject, fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					if (cls != null && IsExpectedClass (cls)) {
						//Debug ("adding InvocationExpression", member.Name, invokeExp);
						AddUniqueReference (fieldExp.StartLocation.Y, fieldExp.StartLocation.X, member.Name);
					}
				}
			}*/
			
			return base.VisitInvocationExpression (invokeExp, data);
		}
		
		public override object VisitIdentifierExpression (IdentifierExpression idExp, object data)
		{
			//Debug ("IdentifierExpression", idExp.Identifier, idExp);
//TODO:
/*			if (idExp.Identifier == member.Name) {
				int line = idExp.StartLocation.Y;
				int col = idExp.StartLocation.X;
				
				IMember item = resolver.ResolveIdentifier (fileCompilationUnit, idExp.Identifier, line, col);
				if (member is IMember) {
					IMember m = item as IMember;
					if (m != null && IsExpectedClass (m.DeclaringType) &&
						((member is IField && item is IField) || (member is IMethod && item is IMethod) ||
						 (member is IProperty && item is IProperty) || (member is IEvent && item is IEvent))) {
						//Debug ("adding IdentifierExpression member", member.Name, idExp);
						AddUniqueReference (line, col, member.Name);
					}
				} else if (member is IType) {
					if (item is IType && ((IType) item).FullName == declaringType.FullName) {
						//Debug ("adding IdentifierExpression class", idExp.Identifier, idExp);
						AddUniqueReference (line, col, idExp.Identifier);
					}
				} else if (member is LocalVariable) {
					LocalVariable avar = member as LocalVariable;
					LocalVariable var = item as LocalVariable;
					
					if (var != null && avar.Region.IsInside (var.Region.Start.Line, var.Region.Start.Column)) {
						//Debug ("adding IdentifierExpression variable", idExp.Identifier, idExp);
						AddUniqueReference (line, col, idExp.Identifier);
					}
				} else if (member is IParameter) {
					IParameter param = item as IParameter;
					
					// FIXME: might need to match more than this?
					if (param != null && IsExpectedMember (param.DeclaringMember)) {
						//Debug ("adding IdentifierExpression param", idExp.Identifier, idExp);
						AddUniqueReference (line, col, idExp.Identifier);
					}
				}
			}*/
			
			return base.VisitIdentifierExpression (idExp, data);
		}

		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			//Debug ("PropertyDeclaration", propertyDeclaration.Name, propertyDeclaration);
			// find override references.
//TODO:
/*
			if (member is IProperty && (propertyDeclaration.Modifier & Modifiers.Override) == Modifiers.Override && propertyDeclaration.Name == member.Name) {
				IMember m = resolver.ResolveIdentifier (fileCompilationUnit, member.Name, propertyDeclaration.StartLocation.Y, propertyDeclaration.StartLocation.X) as IMember;
				if (IsExpectedClass (m.DeclaringType)) {
					AddUniqueReference (propertyDeclaration.StartLocation.Y, propertyDeclaration.StartLocation.X, member.Name);
				}
			}
			
			string type = ReturnType.GetSystemType (propertyDeclaration.TypeReference.Type);
			if (member is IType && member.Name == GetNameWithoutPrefix (type)) {
				int line = propertyDeclaration.StartLocation.Y;
				int col = propertyDeclaration.StartLocation.X;
				IType cls = resolver.ResolveIdentifier (fileCompilationUnit, type, line, col) as IType;
				
				if (cls != null && cls.FullName == ((IType) member).FullName) {
					//Debug ("adding PropertyDeclaration", cls.FullName, propertyDeclaration);
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			*/
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}
		
		bool ClassNamesMatch (string fqName, string name)
		{
			int dot;
			
			do {
				if (name == fqName)
					return true;
				
				if ((dot = fqName.IndexOf ('.')) == -1)
					break;
				
				fqName = fqName.Substring (dot + 1);
			} while (true);
			
			return false;
		}
				
		public override object VisitCastExpression (CastExpression castExpression, object data)
		{
			//Debug ("CastExpression", castExpression.ToString (), castExpression);
//TODO:
/*			string type = ReturnType.GetSystemType (castExpression.CastTo.Type);
			if (member is IType && member.Name == GetNameWithoutPrefix (type)) {
				int line = castExpression.CastTo.StartLocation.Y;
				int col = castExpression.CastTo.StartLocation.X;
				IType cls = resolver.ResolveIdentifier (fileCompilationUnit, type, line, col) as IType;
				
				if (cls != null && cls.FullName == declaringType.FullName) {
					//Debug ("adding CastExpression", cls.FullName, castExpression);
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			*/
			return base.VisitCastExpression (castExpression, data);
		}
		
		public override object VisitObjectCreateExpression (ObjectCreateExpression objCreateExpression, object data)
		{
			//Debug ("ObjectCreateExpression", objCreateExpression.ToString (), objCreateExpression);
//TODO:
/*
			string type = ReturnType.GetSystemType (objCreateExpression.CreateType.Type);
			int line = objCreateExpression.CreateType.StartLocation.Y;
			int col = objCreateExpression.CreateType.StartLocation.X;
			
			if ((member is IType || (member is IMethod && ((IMethod) member).IsConstructor)) 
			    && declaringType.Name == GetNameWithoutPrefix (type)) {
				IType cls = resolver.ResolveIdentifier (fileCompilationUnit, type, line, col) as IType;
				
				if (cls != null && cls.FullName == declaringType.FullName) {
					//Debug ("adding ObjectCreateExpression", cls.FullName, objCreateExpression);
					AddUniqueReference (line, col, cls.FullName);
				}
			}*/
			
			return base.VisitObjectCreateExpression (objCreateExpression, data);
		}
		
		public override object VisitVariableDeclaration (VariableDeclaration varDeclaration, object data)
		{
			//Debug ("VariableDeclaration", varDeclaration.ToString (), varDeclaration);
//TODO:
/*
			string type = ReturnType.GetSystemType (varDeclaration.TypeReference.Type);
			if (member is IType && member.Name == GetNameWithoutPrefix (type)) {
				int line = varDeclaration.StartLocation.Y;
				int col = varDeclaration.StartLocation.X;
				IType cls = resolver.ResolveIdentifier (fileCompilationUnit, type, line, col) as IType;
				
				if (cls != null && cls.FullName == declaringType.FullName) {
					//Debug ("adding varDeclaration", cls.FullName, varDeclaration);
					line = varDeclaration.TypeReference.StartLocation.Y;
					col = varDeclaration.TypeReference.StartLocation.X;
					AddUniqueReference (line, col, cls.FullName);
				}
			}*/
			
			return base.VisitVariableDeclaration (varDeclaration, data);
		}
		
		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			//Debug ("TypeDeclaration", typeDeclaration.Name, typeDeclaration);
//TODO:
/*
			if (member is IType && typeDeclaration.BaseTypes != null) {
				string fname = declaringType.FullName;
				
				foreach (TypeReference bc in typeDeclaration.BaseTypes) {
					IType bclass = resolver.ResolveIdentifier (fileCompilationUnit, bc.Type, typeDeclaration.StartLocation.Y, typeDeclaration.StartLocation.X) as IType;
					if (bclass == null || bclass.FullName != fname)
						continue;
					
					// Note: typeDeclaration.StartLocation marks the location of the subtype,
					// we want the location of the parent class' reference in the declaration
					int begin = file.GetPositionFromLineColumn (typeDeclaration.StartLocation.Y, typeDeclaration.StartLocation.X);
					int end = file.GetPositionFromLineColumn (typeDeclaration.EndLocation.Y, typeDeclaration.EndLocation.X);
					string txt = file.GetText (begin, end);
					int offset, wstart, wend, brace;
					
					if ((brace = txt.IndexOf ('{')) == -1)
						continue;
					
					txt = txt.Substring (0, brace);
					
					if ((offset = txt.IndexOf (typeDeclaration.Name)) == -1)
						continue;
					
					offset += typeDeclaration.Name.Length;
					
					if ((offset = txt.IndexOf (':', offset)) == -1)
						continue;
					
					offset++;
					
					bool found = false;
					
					do {
						if ((wstart = txt.IndexOf (bclass.Name, offset)) == -1)
							break;
						
						wend = wstart + bclass.Name.Length;
						if (wend < txt.Length && (txt[wend] == ',' || txt[wend] == '{' || Char.IsWhiteSpace (txt[wend]))) {
							while (wstart > offset && !Char.IsWhiteSpace (txt[wstart - 1]) && txt[wstart - 1] != ',')
								wstart--;
							
							if ((found = ClassNamesMatch (fname, txt.Substring (wstart, wend - wstart))))
								break;
						}
						
						offset = wend;
					} while (true);
					
					if (found) {
						int line, column;
						
						file.GetLineColumnFromPosition (begin + offset, out line, out column);
						//Debug ("adding TypeDeclaration", typeDeclaration.Name, typeDeclaration);
						AddUniqueReference (line, column, bc.Type);
					}
				}
			}
			*/
			return base.VisitTypeDeclaration (typeDeclaration, data);
		}
		
		MemberReference CreateReference (int line, int col, string name)
		{
			int pos = file.GetPositionFromLineColumn (line, col);
			int spos = file.GetPositionFromLineColumn (line, 1);
			int epos = file.GetPositionFromLineColumn (line + 1, 1);
			if (epos == -1) epos = file.Length - 1;
			
			string txt;
			
			// FIXME: do we always need to do this? or just in my test cases so far? :)
			// use the base name and not the FullName
			name = GetNameWithoutPrefix (name);
			
			// FIXME: is there a better way to do this?
			// update @pos to point to the actual identifier and not the 
			// public/private/whatever modifier.
			int i;
			txt = file.GetText (pos, file.Length - 1);
			if (txt != null && (i = txt.IndexOf (name)) > 0)
				pos += i;
			
			if (spos != -1)
				txt = file.GetText (spos, epos - 1);
			else
				txt = null;
			
			return new MemberReference (ctx, file.Name, pos, line, col, name, txt);
		}
		
		void AddUniqueReference (int line, int col, string name)
		{
			if (line < 1 || col < 1) {
				MonoDevelop.Core.LoggingService.LogWarning ("AddUniqueReference called with invalid position line: {0} col: {1} name: {2}.", line, col, name);
				return;
			}
			
			MemberReference mref = CreateReference (line, col, name);
			
			if (unique.ContainsKey (mref))
				return;
			
			unique.Add (mref, true);
			
			references.Add (mref);
		}
		
		string GetNameWithoutPrefix (string fullName)
		{
			int i = fullName.LastIndexOf ('.');
			if (i == -1)
				return fullName;
			else
				return fullName.Substring (i+1);
		}
		
	}
}
