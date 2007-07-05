//
// CodeGenerator.cs
//
// Author:
//   Lluis Sanchez Gual
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
using System.IO;
using System.Drawing;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using Microsoft.CSharp;

using MonoDevelop.Ide.Gui.Content;

using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.CodeGeneration;

using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Parser.AST;

using CSharpBinding.Parser.SharpDevelopTree;

using ClassType = MonoDevelop.Projects.Parser.ClassType;

namespace CSharpBinding.Parser
{
	class CSharpRefactorer: BaseRefactorer
	{
		CSharpCodeProvider csharpProvider = new CSharpCodeProvider ();
		
		public override RefactorOperations SupportedOperations {
			get { return RefactorOperations.All; }
		}
	
		protected override ICodeGenerator GetGenerator ()
		{
			return csharpProvider.CreateGenerator ();
		}
		
		public override IClass RenameClass (RefactorerContext ctx, IClass cls, string newName)
		{
			IEditableTextFile file;
			int pos, begin, end;
			IClass []classes;
			Match match;
			Regex expr;
			string txt;
			
			if ((classes = cls.Parts) == null)
				return null;
			
			for (int i = 0; i < classes.Length; i++) {
				if ((file = ctx.GetFile (classes[i].Region.FileName)) == null)
					continue;
				
				begin = file.GetPositionFromLineColumn (cls.Region.BeginLine, cls.Region.BeginColumn);
				end = file.GetPositionFromLineColumn (cls.Region.EndLine, cls.Region.EndColumn);
				
				if (begin == -1 || end == -1)
					continue;
				
				txt = file.GetText (begin, end);
				
				switch (cls.ClassType) {
				case ClassType.Interface:
					expr = new Regex (@"\sinterface\s*(" + cls.Name + @")\s", RegexOptions.Multiline);
					break;
				case ClassType.Struct:
					expr = new Regex (@"\sstruct\s*(" + cls.Name + @")\s", RegexOptions.Multiline);
					break;
				case ClassType.Enum:
					expr = new Regex (@"\senum\s*(" + cls.Name + @")\s", RegexOptions.Multiline);
					break;
				default:
					expr = new Regex (@"\sclass\s*(" + cls.Name + @")\s", RegexOptions.Multiline);
					break;
				}
				
				match = expr.Match (" " + txt + " ");
				
				if (!match.Success)
					continue;
				
				pos = begin + match.Groups [1].Index - 1;
				file.DeleteText (pos, cls.Name.Length);
				file.InsertText (pos, newName);
			}
			
			file = ctx.GetFile (cls.Region.FileName);
			
			return GetGeneratedClass (ctx, file, cls);
		}
		
		//static CodeStatement ThrowNewNotImplementedException ()
		//{
		//	CodeExpression expr = new CodeSnippetExpression ("new NotImplementedException ()");
		//	return new CodeThrowExceptionStatement (expr);
		//}
		//
		//public override IMember AddMember (RefactorerContext ctx, IClass cls, CodeTypeMember member)
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
		
		protected override void EncapsulateFieldImpGetSet (RefactorerContext ctx, IClass cls, IField field, CodeMemberProperty prop)
		{
			if (prop.HasGet && prop.GetStatements.Count == 0)
				prop.GetStatements.Add (new CodeSnippetExpression ("return " + field.Name));
			
			if (prop.HasSet && prop.SetStatements.Count == 0)
				prop.SetStatements.Add (new CodeAssignStatement (new CodeVariableReferenceExpression (field.Name), new CodeVariableReferenceExpression ("value")));
		}
		
		public override MemberReferenceCollection FindClassReferences (RefactorerContext ctx, string fileName, IClass cls)
		{
			Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, cls, cls, refs);
			
			IEditableTextFile file = ctx.GetFile (fileName);
			visitor.Visit (ctx.ParserContext, file);
			return refs;
		}
		
		protected override int GetVariableNamePosition (IEditableTextFile file, LocalVariable var)
		{
			int begin = file.GetPositionFromLineColumn (var.Region.BeginLine, var.Region.BeginColumn);
			int end = file.GetPositionFromLineColumn (var.Region.EndLine, var.Region.EndColumn);
			
			if (begin == -1 || end == -1)
				return -1;
			
			string txt = file.GetText (begin, end);
			
			int i = txt.IndexOf ('=');
			if (i == -1)
				i = txt.Length;
			
			int pos = txt.LastIndexOf (var.Name, i);
			if (pos == -1)
				return -1;
			
			return begin + pos;
		}
		
		protected override int GetParameterNamePosition (IEditableTextFile file, IParameter param)
		{
			IMember member = param.DeclaringMember;
			int begin = file.GetPositionFromLineColumn (member.Region.BeginLine, member.Region.BeginColumn);
			int end = file.GetPositionFromLineColumn (member.Region.EndLine, member.Region.EndColumn);
			
			if (begin == -1 || end == -1)
				return -1;
			
			string txt = file.GetText (begin, end);
			int open, close, i, j;
			char obrace, cbrace;
			
			if (member is IIndexer) {
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
			char c = txt[index - 1];
			
			if (Char.IsLetterOrDigit (c) || c == '_')
				return false;
			
			c = txt[index + field.Length];
			
			if (Char.IsLetterOrDigit (c) || c == '_')
				return false;
			
			return true;
		}
		
		protected override int GetMemberNamePosition (IEditableTextFile file, IMember member)
		{
			int begin = file.GetPositionFromLineColumn (member.Region.BeginLine, member.Region.BeginColumn);
			int end = file.GetPositionFromLineColumn (member.Region.EndLine, member.Region.EndColumn);
			
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
			} else if (member is IProperty) {
				// no variables to change
			} else if (member is IEvent) {
				// no variables to change
			} else if (member is IIndexer) {
				if ((len = txt.IndexOf ('[')) == -1)
					return -1;
			} else {
				return -1;
			}
			
			if ((pos = txt.LastIndexOf (name, len)) == -1)
				return -1;
			
			return begin + pos;
		}
		
		protected override IRegion GetMemberBounds (IEditableTextFile file, IMember member)
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
			
			IClass klass = member.DeclaringType;
			IField field = (IField) member;
			int lineBegin, lineEnd;
			int colBegin, colEnd;
			int pos, i;
			
			// find the offset of the field
			for (i = 0; i < klass.Fields.Count; i++) {
				if (klass.Fields[i].Name == field.Name)
					break;
			}
			
			if (i > 0 && klass.Fields[i - 1].Region.CompareTo (field.Region) == 0) {
				// Field has other fields declared before it in the same statement
				pos = GetMemberNamePosition (file, member);
				
				// seek backward for declaration separator
				while (file.Text[pos] != ',')
					pos--;
				
				// eat up unneeded lwsp
				while (Char.IsWhiteSpace (file.Text[pos]))
					pos--;
				
				file.GetLineColumnFromPosition (pos, out lineBegin, out colBegin);
				
				if (i < klass.Fields.Count && klass.Fields[i + 1].Region.CompareTo (field.Region) == 0) {
					// Field also has other fields declared after it in the same statement
					pos = GetMemberNamePosition (file, klass.Fields[i + 1]);
					
					// seek backward for declaration separator
					while (file.Text[pos] != ',')
						pos--;
					
					// eat up unneeded lwsp
					while (Char.IsWhiteSpace (file.Text[pos]))
						pos--;
					
					file.GetLineColumnFromPosition (pos, out lineEnd, out colEnd);
				} else {
					// No fields after this...
					colEnd = field.Region.EndColumn - 1;  // don't include the ';'
					lineEnd = field.Region.EndLine;
				}
			} else if (i < (klass.Fields.Count - 1) && klass.Fields[i + 1].Region.CompareTo (field.Region) == 0) {
				// Field has other fields declared after it in the same statement
				pos = GetMemberNamePosition (file, member);
				file.GetLineColumnFromPosition (pos, out lineBegin, out colBegin);
				pos = GetMemberNamePosition (file, klass.Fields[i + 1]);
				file.GetLineColumnFromPosition (pos, out lineEnd, out colEnd);
			} else {
				// Field is declared in a statement by itself
				
				// fall back to default implementation
				return base.GetMemberBounds (file, member);
			}
			
			return new DefaultRegion (lineBegin, colBegin, lineEnd, colEnd);
		}
		
		public override MemberReferenceCollection FindMemberReferences (RefactorerContext ctx, string fileName, IClass cls, IMember member)
		{
			Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, cls, member, refs);
			
			IEditableTextFile file = ctx.GetFile (fileName);
			visitor.Visit (ctx.ParserContext, file);
			return refs;
		}
		
		public override MemberReferenceCollection FindVariableReferences (RefactorerContext ctx, string fileName, LocalVariable var)
		{
			Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, null, var, refs);
			
			IEditableTextFile file = ctx.GetFile (fileName);
			visitor.Visit (ctx.ParserContext, file);
			return refs;
		}
		
		public override MemberReferenceCollection FindParameterReferences (RefactorerContext ctx, string fileName, IParameter param)
		{
			IMember member = param.DeclaringMember;
			Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, member.DeclaringType, param, refs);
			
			IEditableTextFile file = ctx.GetFile (fileName);
			visitor.Visit (ctx.ParserContext, file);
			
			return refs;
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
	
	class MemberRefactoryVisitor: AbstractAstVisitor
	{
		IClass declaringType;
		ILanguageItem member;
		Resolver resolver;
		IEditableTextFile file;
		CompilationUnit fileCompilationUnit;
		MemberReferenceCollection references;
		RefactorerContext ctx;
		
		public MemberRefactoryVisitor (RefactorerContext ctx, Resolver resolver, IClass declaringType, ILanguageItem member, MemberReferenceCollection references)
		{
			this.ctx = ctx;
			this.resolver = resolver;
			this.declaringType = declaringType;
			this.references = references;
			this.member = member;
		}
		
		public void Visit (IParserContext pctx, IEditableTextFile file)
		{
			this.file = file;
			
			IParseInformation pi = pctx.ParseFile (file);
			
			fileCompilationUnit = pi.MostRecentCompilationUnit.Tag as CompilationUnit;
			if (fileCompilationUnit != null)
				Visit (fileCompilationUnit, null);
		}
		
		bool IsExpectedClass (IClass type)
		{
			if (type.FullyQualifiedName == declaringType.FullyQualifiedName)
				return true;
			
			if (type.BaseTypes != null) {
				foreach (IReturnType bc in type.BaseTypes) {
					IClass bcls = ctx.ParserContext.GetClass (bc.FullyQualifiedName, bc.GenericArguments, true, true);
					if (bcls != null && IsExpectedClass (bcls))
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
		
		//void Debug (string what, string name, AbstractNode node)
		//{
		//	Console.WriteLine ("{0} reference for {1} @ ({2}, {3})", what, name,
		//	                   node.StartLocation.Y, node.StartLocation.X);
		//}
		
		public override object Visit(FieldDeclaration fieldDeclaration, object data)
		{
			if (member is IClass && member.Name == GetNameWithoutPrefix (ReturnType.GetSystemType (fieldDeclaration.TypeReference.Type))) {
				IClass cls = resolver.ResolveIdentifier (fileCompilationUnit, ReturnType.GetSystemType (fieldDeclaration.TypeReference.Type), fieldDeclaration.StartLocation.Y, fieldDeclaration.StartLocation.X) as IClass;
				if (cls != null && cls.FullyQualifiedName == ((IClass)member).FullyQualifiedName) {
					//Debug ("adding FieldDeclaration", cls.FullyQualifiedName, fieldDeclaration);
					references.Add (CreateReference (fieldDeclaration.StartLocation.Y, fieldDeclaration.StartLocation.X, cls.FullyQualifiedName));
				}
			}
			return base.Visit (fieldDeclaration, data);
		}
		
		public override object Visit (FieldReferenceExpression fieldExp, object data)
		{
			//Debug ("FieldReferenceExpression", fieldExp.FieldName, fieldExp);
			if (fieldExp.FieldName == member.Name) {
				IClass cls = resolver.ResolveExpressionType (fileCompilationUnit, fieldExp.TargetObject, fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
				if (cls != null && IsExpectedClass (cls)) {
					int pos = file.GetPositionFromLineColumn (fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					string txt = file.GetText (pos, pos + member.Name.Length);
					if (txt == member.Name) {
						//Debug ("adding FieldReferenceExpression", member.Name, fieldExp);
						references.Add (CreateReference (fieldExp.StartLocation.Y, fieldExp.StartLocation.X, member.Name));
					}
				}
			}
			
			return base.Visit (fieldExp, data);
		}
		
		public override object Visit (InvocationExpression invokeExp, object data)
		{
			//Debug ("InvocationExpression", invokeExp.ToString (), invokeExp);
			if (member is IMethod && invokeExp.TargetObject is FieldReferenceExpression) {
				FieldReferenceExpression fieldExp = (FieldReferenceExpression) invokeExp.TargetObject;
				if (fieldExp.FieldName == member.Name) {
					IClass cls = resolver.ResolveExpressionType (fileCompilationUnit, fieldExp.TargetObject, fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					if (cls != null && IsExpectedClass (cls)) {
						//Debug ("adding InvocationExpression", member.Name, invokeExp);
						references.Add (CreateReference (fieldExp.StartLocation.Y, fieldExp.StartLocation.X, member.Name));
					}
				}
			}
			return base.Visit (invokeExp, data);
		}
		
		public override object Visit (IdentifierExpression idExp, object data)
		{
			//Debug ("IdentifierExpression", idExp.Identifier, idExp);
			if (idExp.Identifier == member.Name) {
				Point p = idExp.StartLocation;
				ILanguageItem item = resolver.ResolveIdentifier (fileCompilationUnit, idExp.Identifier, p.Y, p.X);
				if (member is IMember) {
					IMember m = item as IMember;
					if (m != null && IsExpectedClass (m.DeclaringType) &&
						((member is IField && item is IField) || (member is IMethod && item is IMethod) ||
						 (member is IProperty && item is IProperty) || (member is IEvent && item is IEvent))) {
						//Debug ("adding IdentifierExpression member", member.Name, idExp);
						references.Add (CreateReference (idExp.StartLocation.Y, idExp.StartLocation.X, member.Name));
					}
				} else if (member is IClass) {
					if (item is IClass && ((IClass) item).FullyQualifiedName == declaringType.FullyQualifiedName) {
						//Debug ("adding IdentifierExpression class", idExp.Identifier, idExp);
						references.Add (CreateReference (idExp.StartLocation.Y, idExp.StartLocation.X, idExp.Identifier));
					}
				} else if (member is LocalVariable) {
					LocalVariable avar = member as LocalVariable;
					LocalVariable var = item as LocalVariable;
					
					if (var != null && var.Region.IsInside (avar.Region.BeginLine, avar.Region.EndColumn)) {
						//Debug ("adding IdentifierExpression variable", idExp.Identifier, idExp);
						references.Add (CreateReference (idExp.StartLocation.Y, idExp.StartLocation.X, idExp.Identifier));
					}
				} else if (member is IParameter) {
					IParameter param = item as IParameter;
					
					// FIXME: might need to match more than this?
					if (param != null && IsExpectedMember (param.DeclaringMember)) {
						//Debug ("adding IdentifierExpression param", idExp.Identifier, idExp);
						references.Add (CreateReference (idExp.StartLocation.Y, idExp.StartLocation.X, idExp.Identifier));
					}
				}
			}
			return base.Visit (idExp, data);
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
		
		public override object Visit(TypeDeclaration typeDeclaration, object data)
		{
			if (member is IClass && typeDeclaration.BaseTypes != null) {
				string fname = declaringType.FullyQualifiedName;
				
				foreach (TypeReference bc in typeDeclaration.BaseTypes) {
					IClass bclass = resolver.ResolveIdentifier (fileCompilationUnit, bc.Type, typeDeclaration.StartLocation.Y, typeDeclaration.StartLocation.X) as IClass;
					if (bclass == null || bclass.FullyQualifiedName != fname)
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
						references.Add (CreateReference (line, column, bc.Type));
					}
				}
			}
			return base.Visit (typeDeclaration, data);
		}
		
		MemberReference CreateReference (int lin, int col, string name)
		{
			int pos = file.GetPositionFromLineColumn (lin, col);
			int spos = file.GetPositionFromLineColumn (lin, 1);
			int epos = file.GetPositionFromLineColumn (lin + 1, 1);
			if (epos == -1) epos = file.Length - 1;
			
			string txt;
			
			// FIXME: do we always need to do this? or just in my test cases so far? :)
			// use the base name and not the FullyQualifiedName
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
			
			return new MemberReference (ctx, file.Name, pos, lin, col, name, txt);
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
