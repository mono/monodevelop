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
using System.CodeDom;
using System.CodeDom.Compiler;
using Microsoft.CSharp;
using System.IO;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.CodeGeneration;
using ICSharpCode.SharpRefactory.Parser;
using ICSharpCode.SharpRefactory.Parser.AST;
using System.Drawing;
using System.Text.RegularExpressions;

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
			IEditableTextFile file = ctx.GetFile (cls.Region.FileName);
			if (file == null)
				return null;

			int pos1 = file.GetPositionFromLineColumn (cls.Region.BeginLine, cls.Region.BeginColumn);
			int pos2 = file.GetPositionFromLineColumn (cls.Region.EndLine, cls.Region.EndColumn);
			string txt = file.GetText (pos1, pos2);
			
			Regex targetExp = new Regex(@"\sclass\s*(" + cls.Name + @")\s", RegexOptions.Multiline);
			Match match = targetExp.Match (" " + txt + " ");
			if (!match.Success)
				return null;
			
			int pos = pos1 + match.Groups [1].Index - 1;
			file.DeleteText (pos, cls.Name.Length);
			file.InsertText (pos, newName);
			
			return GetGeneratedClass (ctx, file, cls);
		}
		
		public override MemberReferenceCollection FindClassReferences (RefactorerContext ctx, string fileName, IClass cls)
		{
			Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, cls.FullyQualifiedName, cls, refs);
			
			IEditableTextFile file = ctx.GetFile (fileName);
			visitor.Visit (ctx.ParserContext, file);
			return refs;
		}
		
		protected override int GetMemberNamePosition (IEditableTextFile file, IMember member)
		{
			int pos1 = file.GetPositionFromLineColumn (member.Region.BeginLine, member.Region.BeginColumn);
			int pos2 = file.GetPositionFromLineColumn (member.Region.EndLine, member.Region.EndColumn);
			string txt = file.GetText (pos1, pos2);
			
			if (member is IField)
			{
				int i = txt.IndexOf ('=');
				if (i == -1) i = txt.Length;
				int p = txt.LastIndexOf (member.Name, i);
				if (p == -1) return -1;
				return pos1 + p;
			}
			else if (member is IMethod)
			{
				int i = txt.IndexOf ('(');
				if (i == -1) return -1;
				int p = txt.LastIndexOf (member.Name, i);
				if (p == -1) return -1;
				return pos1 + p;
			}
			else if (member is IProperty)
			{
				int i = txt.IndexOf ('{');
				if (i == -1) return -1;
				int p = txt.LastIndexOf (member.Name, i);
				if (p == -1) return -1;
				return pos1 + p;
			}
			else if (member is IEvent)
			{
				int i = txt.IndexOf ('{');
				if (i == -1) i = txt.Length;
				int p = txt.LastIndexOf (member.Name, i);
				if (p == -1) return -1;
				return pos1 + p;
			}
			
			return -1;
		}
		
		public override MemberReferenceCollection FindMemberReferences (RefactorerContext ctx, string fileName, IClass cls, IMember member)
		{
			Resolver resolver = new Resolver (ctx.ParserContext);
			MemberReferenceCollection refs = new MemberReferenceCollection ();
			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (ctx, resolver, cls.FullyQualifiedName, member, refs);
			
			IEditableTextFile file = ctx.GetFile (fileName);
			visitor.Visit (ctx.ParserContext, file);
			return refs;
		}
	}
	
	class MemberRefactoryVisitor: AbstractASTVisitor
	{
		string className;
		ILanguageItem member;
		Resolver resolver;
		IEditableTextFile file;
		CompilationUnit fileCompilationUnit;
		MemberReferenceCollection references;
		RefactorerContext ctx;
		
		public MemberRefactoryVisitor (RefactorerContext ctx, Resolver resolver, string className, ILanguageItem member, MemberReferenceCollection references)
		{
			this.ctx = ctx;
			this.resolver = resolver;
			this.className = className;
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
		
		public override object Visit (FieldReferenceExpression fieldExp, object data)
		{
			if (member is IField && fieldExp.FieldName == member.Name)
			{
				IClass cls = resolver.ResolveExpressionType (fileCompilationUnit, fieldExp.TargetObject, fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
				if (cls != null && cls.FullyQualifiedName == className) {
					int pos = file.GetPositionFromLineColumn (fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					string txt = file.GetText (pos, pos + member.Name.Length);
					if (txt == member.Name)
						references.Add (new MemberReference (ctx, file.Name, pos, member.Name));
				}
				
			}
			return base.Visit (fieldExp, data);
		}
		
		public override object Visit (InvocationExpression invokeExp, object data)
		{
			if (member is IMethod && invokeExp.TargetObject is FieldReferenceExpression) {
				FieldReferenceExpression fieldExp = (FieldReferenceExpression) invokeExp.TargetObject;
				IClass cls = resolver.ResolveExpressionType (fileCompilationUnit, fieldExp.TargetObject, fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
				if (cls != null && cls.FullyQualifiedName == className) {
					int pos = file.GetPositionFromLineColumn (fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					string txt = file.GetText (pos, pos + member.Name.Length);
					if (txt == member.Name)
						references.Add (new MemberReference (ctx, file.Name, pos, member.Name));
				}
			}
			return base.Visit (invokeExp, data);
		}
		
		public override object Visit (IdentifierExpression idExp, object data)
		{
			if (idExp.Identifier == member.Name)
			{
				ILanguageItem item = resolver.ResolveIdentifier (fileCompilationUnit, idExp.Identifier, idExp.StartLocation.Y, idExp.StartLocation.X);
				if (member is IMember) {
					IMember m = item as IMember;
					if (m != null && m.DeclaringType.FullyQualifiedName == className &&
						((member is IField && item is IField) || (member is IMethod && item is IMethod) ||
						 (member is IProperty && item is IProperty) || (member is IEvent && item is IEvent)))
					{
						int pos = file.GetPositionFromLineColumn (idExp.StartLocation.Y, idExp.StartLocation.X);
						references.Add (new MemberReference (ctx, file.Name, pos, member.Name));
					}
				} else if (member is IClass && item is IClass && (((IClass)member).FullyQualifiedName ==  ((IClass)item).FullyQualifiedName)) {
					int pos = file.GetPositionFromLineColumn (idExp.StartLocation.Y, idExp.StartLocation.X);
					references.Add (new MemberReference (ctx, file.Name, pos, member.Name));
				}
				
			}
			return base.Visit (idExp, data);
		}
	}
}
