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
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Drawing;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Linq;
using Microsoft.CSharp;
using MonoDevelop.Core;
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

using ClassType = MonoDevelop.Projects.Dom.ClassType;
using MonoDevelop.CSharp.Formatting;
using MonoDevelop.CSharp.Parser;
using MonoDevelop.CSharp.Dom;
using MonoDevelop.CSharp.Resolver;

namespace MonoDevelop.CSharp.Refactoring
{
	class CSharpRefactorer: BaseRefactorer
	{
		CSharpEnhancedCodeProvider csharpProvider = new CSharpEnhancedCodeProvider ();
		
		public override RefactorOperations SupportedOperations {
			get { return RefactorOperations.All; }
		}
		
		public override ValidationResult ValidateName (MonoDevelop.Projects.Dom.INode visitable, string name)
		{
			if (string.IsNullOrEmpty (name))
				return ValidationResult.CreateError (GettextCatalog.GetString ("Name must not be empty."));
			
			int token = ICSharpCode.NRefactory.Parser.CSharp.Keywords.GetToken (name);
			if (token >= ICSharpCode.NRefactory.Parser.CSharp.Tokens.Abstract)
				return ValidationResult.CreateError (GettextCatalog.GetString ("Name can't be a keyword."));
			
			char startChar = name[0];
			if (!Char.IsLetter (startChar) && startChar != '_')
				return ValidationResult.CreateError (GettextCatalog.GetString ("Name must start with a letter or '_'"));
			
			for (int i = 1; i < name.Length; i++) {
				char ch = name[i];
				if (!Char.IsLetterOrDigit (ch) && ch != '_')
					return ValidationResult.CreateError (GettextCatalog.GetString ("Name can only contain letters, digits and '_'"));
			}
			
			if (visitable is LocalVariable) {
				if (Char.IsUpper (startChar))
					return ValidationResult.CreateWarning (GettextCatalog.GetString ("Local variables shouldn't start with upper case"));
				return ValidationResult.Valid;
			}
			
			if (visitable is IParameter) {
				if (Char.IsUpper (startChar))
					return ValidationResult.CreateWarning (GettextCatalog.GetString ("Parameters shouldn't start with upper case"));
				return ValidationResult.Valid;
			}
			
			if (visitable is IField) {
				IField field = (IField)visitable;
				if (!field.IsConst && !field.IsStatic  && !field.IsReadonly && Char.IsUpper (startChar))
					return ValidationResult.CreateWarning (GettextCatalog.GetString ("Fields shouldn't start with upper case"));
				return ValidationResult.Valid;
			}
			
			if (visitable is IType) {
				IType type = (IType)visitable;
				if (type.ClassType == ClassType.Interface && startChar != 'I')
					return ValidationResult.CreateWarning (GettextCatalog.GetString ("Interfaces should always start with 'I'"));
				
				if (!Char.IsUpper (startChar))
					return ValidationResult.CreateWarning (GettextCatalog.GetString ("Types should always start with upper case"));
				return ValidationResult.Valid;
			}
			
			if (visitable is IMethod) {
				if (!Char.IsUpper (startChar))
					return ValidationResult.CreateWarning (GettextCatalog.GetString ("Methods should always start with upper case"));
				return ValidationResult.Valid;
			}
			
			if (visitable is IProperty) {
				IProperty prop = (IProperty)visitable;
				if (!prop.IsIndexer && !Char.IsUpper (startChar))
					return ValidationResult.CreateWarning (GettextCatalog.GetString ("Properties should always start with upper case"));
				return ValidationResult.Valid;
			}
			
			if (visitable is IEvent) {
				if (!Char.IsUpper (startChar))
					return ValidationResult.CreateWarning (GettextCatalog.GetString ("Events should always start with upper case"));
				return ValidationResult.Valid;
			}
			return ValidationResult.Valid;
		}
		
		protected override CodeDomProvider GetCodeDomProvider ()
		{
			return csharpProvider;
		}
		
		public override string ConvertToLanguageTypeName (string netTypeName)
		{
			return CSharpAmbience.NetToCSharpTypeName (netTypeName);
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
		
		public override DomLocation CompleteStatement (RefactorerContext ctx, string fileName, DomLocation caretLocation)
		{
			IEditableTextFile file = ctx.GetFile (fileName);
			int pos = file.GetPositionFromLineColumn (caretLocation.Line + 1, 1);
			
			StringBuilder line = new StringBuilder ();
			int lineNr = caretLocation.Line + 1, column = 1, maxColumn = 1, lastPos = pos;
			
			while (lineNr == caretLocation.Line + 1) {
				maxColumn = column;
				lastPos = pos;
				line.Append (file.GetCharAt (pos));
				pos++;
				file.GetLineColumnFromPosition (pos, out lineNr, out column);
			}
			string trimmedline = line.ToString ().Trim ();
			string indent      = line.ToString ().Substring (0, line.Length - line.ToString ().TrimStart (' ', '\t').Length);
			if (trimmedline.EndsWith (";") || trimmedline.EndsWith ("{"))
				return caretLocation;
			if (trimmedline.StartsWith ("if") || 
			    trimmedline.StartsWith ("while") ||
			    trimmedline.StartsWith ("switch") ||
			    trimmedline.StartsWith ("for") ||
			    trimmedline.StartsWith ("foreach")) {
				if (!trimmedline.EndsWith (")")) {
					file.InsertText (lastPos, " () {" + Environment.NewLine + indent + TextEditorProperties.IndentString + Environment.NewLine + indent + "}");
					caretLocation.Column = maxColumn + 1;
				} else {
					file.InsertText (lastPos, " {" + Environment.NewLine + indent + TextEditorProperties.IndentString + Environment.NewLine + indent + "}");
					caretLocation.Column = indent.Length + 1;
					caretLocation.Line++;
				}
			} else if (trimmedline.StartsWith ("do")) {
				file.InsertText (lastPos, " {" + Environment.NewLine + indent + TextEditorProperties.IndentString + Environment.NewLine + indent + "} while ();");
				caretLocation.Column = indent.Length + 1;
				caretLocation.Line++;
			} else {
				file.InsertText (lastPos, ";" + Environment.NewLine + indent);
				caretLocation.Column = indent.Length;
				caretLocation.Line++;
			}
			return caretLocation;
		}
		
		public override void AddGlobalNamespaceImport (RefactorerContext ctx, string fileName, string nsName)
		{
			IEditableTextFile file = ctx.GetFile (fileName);
			int pos = 0;
			ParsedDocument parsedDocument = parser.Parse (ctx.ParserContext, fileName, file.Text);
			StringBuilder text = new StringBuilder ();
			if (parsedDocument.CompilationUnit != null) {
				IUsing lastUsing = null;
				foreach (IUsing u in parsedDocument.CompilationUnit.Usings) {
					if (u.IsFromNamespace)
						break;
					lastUsing = u;
				}
				
				if (lastUsing != null)
					pos = file.GetPositionFromLineColumn (lastUsing.Region.End.Line, lastUsing.Region.End.Column);
			}
			
			if (pos != 0)
				text.AppendLine ();
			text.Append ("using ");
			text.Append (nsName);
			text.Append (";");
			if (pos == 0)
				text.AppendLine ();
			if (file is Mono.TextEditor.ITextEditorDataProvider) {
				Mono.TextEditor.TextEditorData data = ((Mono.TextEditor.ITextEditorDataProvider)file).GetTextEditorData ();
				int caretOffset = data.Caret.Offset;
				int insertedChars = data.Insert (pos, text.ToString ());
				if (pos < caretOffset) {
					data.Caret.Offset = caretOffset + insertedChars;
				}
			} else {
				file.InsertText (pos, text.ToString ());
			}
		}
		
		public override void AddLocalNamespaceImport (RefactorerContext ctx, string fileName, string nsName, DomLocation caretLocation)
		{
			IEditableTextFile file = ctx.GetFile (fileName);
			int pos = 0;
			ParsedDocument parsedDocument = parser.Parse (ctx.ParserContext, fileName, file.Text);
			StringBuilder text = new StringBuilder ();
			string indent = "";
			if (parsedDocument.CompilationUnit != null) {
				IUsing containingUsing = null;
				foreach (IUsing u in parsedDocument.CompilationUnit.Usings) {
					if (u.IsFromNamespace && u.Region.Contains (caretLocation)) {
						containingUsing = u;
					}
				}
				
				if (containingUsing != null) {
					indent = GetLineIndent (file, containingUsing.Region.Start.Line);
					
					IUsing lastUsing = null;
					foreach (IUsing u in parsedDocument.CompilationUnit.Usings) {
						if (u == containingUsing)
							continue;
						if (containingUsing.Region.Contains (u.Region)) {
							if (u.IsFromNamespace)
								break;
							lastUsing = u;
						}
					}
					
					if (lastUsing != null) {
						pos = file.GetPositionFromLineColumn (lastUsing.Region.End.Line, lastUsing.Region.End.Column);
					} else {
						pos = file.GetPositionFromLineColumn (containingUsing.ValidRegion.Start.Line, containingUsing.ValidRegion.Start.Column);
						// search line end
						while (pos < file.Length) {
							char ch = file.GetCharAt (pos);
							if (ch == '\n') {
								if (file.GetCharAt (pos + 1) == '\r')
									pos++;
								break;
							} else if (ch == '\r') {
								break;
							}
							pos++;
						}
					}
					
				} else {
					AddGlobalNamespaceImport (ctx, fileName, nsName);
					return;
				}
			}
			if (pos != 0)
				text.AppendLine ();
			text.Append (indent);
			text.Append ("\t");
			text.Append ("using ");
			text.Append (nsName);
			text.Append (";");
			if (pos == 0)
				text.AppendLine ();
			if (file is Mono.TextEditor.ITextEditorDataProvider) {
				Mono.TextEditor.TextEditorData data = ((Mono.TextEditor.ITextEditorDataProvider)file).GetTextEditorData ();
				int caretOffset = data.Caret.Offset;
				int insertedChars = data.Insert (pos, text.ToString ());
				if (pos < caretOffset) {
					data.Caret.Offset = caretOffset + insertedChars;
				}
			} else {
				file.InsertText (pos, text.ToString ());
			} 
		}
		//TODO
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
				Ambience amb = new CSharpAmbience ();
				string tn = amb.GetString (privateImplementationType, OutputFlags.IncludeGenerics | OutputFlags.UseFullName | OutputFlags.UseIntrinsicTypeNames);
				privateImplementationType = new DomReturnType (tn);
			}
			return base.ImplementMember (ctx, cls, member, privateImplementationType);
		}
		
	/*	public override void ImplementMembers (RefactorerContext ctx, IType cls, 
		                                                      IEnumerable<KeyValuePair<IMember,IReturnType>> members,
		                                                      string foldingRegionName)
		{
			base.ImplementMembers (ctx, cls, FixGenericImpl (ctx, cls, members), foldingRegionName);
		}
		static Ambience amb = new MonoDevelop.CSharpBinding.CSharpAmbience ();
		// Workaround for bug in the code generator. Generic private implementation types are not generated correctly when they are generic.
		IEnumerable<KeyValuePair<IMember,IReturnType>> FixGenericImpl (RefactorerContext ctx, IType cls, IEnumerable<KeyValuePair<IMember,IReturnType>> members)
		{
			foreach (KeyValuePair<IMember,IReturnType> kvp in members) {
				if (kvp.Value == null) {
					yield return kvp;
					continue;
				}
				
				string tn = amb.GetString (kvp.Value, OutputFlags.IncludeGenerics | OutputFlags.UseFullName | OutputFlags.UseIntrinsicTypeNames);
				Console.WriteLine ("tn :" + tn);
				yield return new KeyValuePair<IMember,IReturnType> (kvp.Key, new DomReturnType (tn));
			}
		}*/
		static void SetContext (IEnumerable<MemberReference> references, RefactorerContext ctx)
		{
			foreach (MemberReference r in references) {
				r.SetContext (ctx);
			}
		}
		public override IEnumerable<MemberReference> FindClassReferences (RefactorerContext ctx, string fileName, IType cls, bool includeXmlComment)
		{
			IEditableTextFile file = ctx.GetFile (fileName);
			NRefactoryResolver resolver = new NRefactoryResolver (ctx.ParserContext, cls.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, null, fileName);
			
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (resolver, file, cls);
			visitor.IncludeXmlDocumentation = includeXmlComment;
			visitor.RunVisitor ();
			SetContext (visitor.FoundReferences, ctx);
			return visitor.FoundReferences;
		}
		
		protected override int GetVariableNamePosition (IEditableTextFile file, LocalVariable var)
		{
			int begin = file.GetPositionFromLineColumn (var.Region.Start.Line, var.Region.Start.Column);
			int end = file.GetPositionFromLineColumn (var.Region.Start.Line, var.Region.End.Column);
			
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
		static NRefactoryParser parser = new NRefactoryParser ();
		public override IEnumerable<MemberReference> FindMemberReferences (RefactorerContext ctx, string fileName, IType cls, IMember member, bool includeXmlComment)
		{
			ParsedDocument parsedDocument = parser.Parse (cls.SourceProjectDom, fileName, ctx.GetFile (fileName).Text);
			
			NRefactoryResolver resolver = new NRefactoryResolver (ctx.ParserContext, parsedDocument.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, null, fileName);
			resolver.SetupParsedCompilationUnit (parser.LastUnit);
			resolver.CallingMember = member;
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (resolver, ctx.GetFile (fileName), member);
			visitor.IncludeXmlDocumentation = includeXmlComment;
			visitor.RunVisitor ();
			SetContext (visitor.FoundReferences, ctx);
			return visitor.FoundReferences;
		}

		public override IEnumerable<MemberReference> FindVariableReferences (RefactorerContext ctx, string fileName, LocalVariable var)
		{
			//System.Console.WriteLine("Find variable references !!!");
//			ParsedDocument parsedDocument = ProjectDomService.ParseFile (fileName);
			NRefactoryResolver resolver = new NRefactoryResolver (ctx.ParserContext, var.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, null, fileName);
			resolver.CallingMember = var.DeclaringMember;
			
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (resolver, ctx.GetFile (fileName), var);
			visitor.RunVisitor ();
			SetContext (visitor.FoundReferences, ctx);
			return visitor.FoundReferences;
		}
		
		public override IEnumerable<MemberReference> FindParameterReferences (RefactorerContext ctx, string fileName, IParameter param, bool includeXmlComment)
		{
			NRefactoryResolver resolver = new NRefactoryResolver (ctx.ParserContext, param.DeclaringMember.DeclaringType.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, null, fileName);
			
			resolver.CallingMember = param.DeclaringMember;
			
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (resolver, ctx.GetFile (fileName), param);
			visitor.IncludeXmlDocumentation = includeXmlComment;
			visitor.RunVisitor ();
			SetContext (visitor.FoundReferences, ctx);
			return visitor.FoundReferences;
		}
		
		public override int AddFoldingRegion (RefactorerContext ctx, IType cls, string regionName)
		{
			IEditableTextFile buffer = ctx.GetFile (cls.CompilationUnit.FileName);
			int pos = GetNewMethodPosition (buffer, cls);
			string eolMarker = Environment.NewLine;
			if (cls.SourceProject != null) {
				TextStylePolicy policy = cls.SourceProject.Policies.Get<TextStylePolicy> ();
				if (policy != null)
					eolMarker = policy.GetEolMarker ();
			}
			
			int line, col;
			buffer.GetLineColumnFromPosition (pos, out line, out col);
			
			string indent = buffer.GetText (buffer.GetPositionFromLineColumn (line, 1), pos);
			
			string pre = "#region " + regionName + eolMarker;
			string post = indent + "#endregion" + eolMarker;
			
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
		
		protected override string GenerateCodeFromMember (CodeTypeMember member)
		{
			string result = base.GenerateCodeFromMember (member);
			// workaround for Bug 434240 - Cannot explicitly implement events
			if (member is CodeMemberEvent) {
				CodeTypeReference codeTypeReference = ((CodeMemberEvent)member).PrivateImplementationType;
				if (codeTypeReference != null) {
					result = result.TrimEnd (' ', '\t', '\n', '\r');
					result = result.Substring (0, result.Length - 1) + " {" + Environment.NewLine +
						"\tadd { /* TODO */ }" + Environment.NewLine +
						"\tremove { /* TODO */ }" + Environment.NewLine +
					"}\n\n";
				}
			}
			return result;
		}
	}
}
