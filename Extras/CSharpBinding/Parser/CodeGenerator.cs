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
		
		public override void RenameField (IRefactorerContext ctx, IClass cls, IField field, string newName)
		{
			IEditableTextFile file = ctx.GetFile (cls.Region.FileName);
			if (file == null)
				return;

			int pos1 = file.GetPositionFromLineColumn (field.Region.BeginLine, field.Region.BeginColumn);
			int pos2 = file.GetPositionFromLineColumn (field.Region.EndLine, field.Region.EndColumn);
			string txt = file.GetText (pos1, pos2);
			Console.WriteLine ("TXT:[" + txt + "]");
			int i = txt.IndexOf ('=');
			if (i == -1) i = txt.Length;
			int p = txt.LastIndexOf (field.Name, i);
			if (p == -1)
				return;

			string newTxt = txt.Substring (0, p) + newName + txt.Substring (p + field.Name.Length);
			file.DeleteText (pos1, txt.Length);
			file.InsertText (pos1, newTxt);
		}
		
		public override void RenameFieldReferences (IRefactorerContext ctx, string fileName, IClass cls, IField field, string newName)
		{
			Resolver resolver = new Resolver (ctx.ParserContext);
			MemberRefactoryVisitor visitor = new MemberRefactoryVisitor (resolver, cls.FullyQualifiedName, field.Name, newName);
			Console.WriteLine ("Checking " + fileName);
			
			IEditableTextFile file = ctx.GetFile (fileName);
			visitor.Visit (ctx.ParserContext, file);
		}
	}
	
	class MemberRefactoryVisitor: AbstractASTVisitor
	{
		string className;
		string memberName;
		string newName;
		Resolver resolver;
		IEditableTextFile file;
		string fileContent;
		CompilationUnit fileCompilationUnit;
		
		public MemberRefactoryVisitor (Resolver resolver, string className, string memberName, string newName)
		{
			this.resolver = resolver;
			this.className = className;
			this.newName = newName;
			this.memberName = memberName;
		}
		
		public void Visit (IParserContext pctx, IEditableTextFile file)
		{
			this.file = file;
			this.fileContent = file.Text;
			
			IParseInformation pi = pctx.ParseFile (file);
			
			fileCompilationUnit = pi.MostRecentCompilationUnit.Tag as CompilationUnit;
			if (fileCompilationUnit != null)
				Visit (fileCompilationUnit, null);
			else
				Console.WriteLine ("No parse info");
		}
		
		public override object Visit (FieldReferenceExpression fieldExp, object data)
		{
			if (fieldExp.FieldName == memberName)
			{
				IClass cls = resolver.ResolveExpressionType (fileCompilationUnit, fieldExp.TargetObject, fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
				if (cls != null && cls.FullyQualifiedName == className) {
					Console.WriteLine ("Found field in " + file.Name + " " + fieldExp.StartLocation + " - " + fieldExp.EndLocation);
					int pos = file.GetPositionFromLineColumn (fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					string txt = file.GetText (pos, pos + memberName.Length);
					if (txt == memberName) {
						file.DeleteText (pos, memberName.Length);
						file.InsertText (pos, newName);
					}
				}
				
			}
			return base.Visit (fieldExp, data);
		}
		
		public override object Visit (IdentifierExpression idExp, object data)
		{
			if (idExp.Identifier == memberName)
			{
				ILanguageItem item = resolver.ResolveIdentifier (fileCompilationUnit, idExp.Identifier, idExp.StartLocation.Y, idExp.StartLocation.X);
				if (item is IField) {
					IField f = (IField) item;
					if (f.DeclaringType.FullyQualifiedName == className) {
						Console.WriteLine ("DC:" + f.DeclaringType);
						Console.WriteLine ("Found field in " + f.Name + " " + idExp.StartLocation + " - " + idExp.EndLocation);
						int pos = file.GetPositionFromLineColumn (idExp.StartLocation.Y, idExp.StartLocation.X);
						string txt = file.GetText (pos, pos + memberName.Length);
						if (txt == memberName) {
							file.DeleteText (pos, memberName.Length);
							file.InsertText (pos, newName);
						}
					}
				}
			}
			return base.Visit (idExp, data);
		}
	}
}
