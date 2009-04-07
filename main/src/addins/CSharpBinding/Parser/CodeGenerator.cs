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
using MonoDevelop.CSharpBinding;

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
		
		public override void AddNamespaceImport (RefactorerContext ctx, string fileName, string nsName)
		{
			IEditableTextFile file = ctx.GetFile (fileName);
			int pos = 0;
			ParsedDocument parsedDocument = parser.Parse (ctx.ParserContext, fileName, file.Text);
			StringBuilder text = new StringBuilder ();
			if (parsedDocument.CompilationUnit != null) {
				IUsing lastUsing = parsedDocument.CompilationUnit.Usings.Where (u => !u.IsFromNamespace).LastOrDefault ();
				if (lastUsing != null)
					pos = file.GetPositionFromLineColumn (lastUsing.Region.End.Line, lastUsing.Region.End.Column);
			}
			text.AppendLine ();
			text.Append ("using ");
			text.Append (nsName);
			text.Append (";");
			text.AppendLine ();
			file.InsertText (pos, text.ToString ());
			
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
				Ambience amb = new MonoDevelop.CSharpBinding.CSharpAmbience ();
				string tn = amb.GetString (privateImplementationType, OutputFlags.IncludeGenerics | OutputFlags.UseFullName | OutputFlags.UseIntrinsicTypeNames);
				privateImplementationType = new DomReturnType (tn);
			}
			return base.ImplementMember (ctx, cls, member, privateImplementationType);
		}
		
		public override void ImplementMembers (RefactorerContext ctx, IType cls, 
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
				yield return new KeyValuePair<IMember,IReturnType> (kvp.Key, new DomReturnType (tn));
			}
		}
		static void SetContext (IEnumerable<MemberReference> references, RefactorerContext ctx)
		{
			foreach (MemberReference r in references) {
				r.SetContext (ctx);
			}
		}
		public override IEnumerable<MemberReference> FindClassReferences (RefactorerContext ctx, string fileName, IType cls)
		{
			IEditableTextFile file = ctx.GetFile (fileName);
			NRefactoryResolver resolver = new NRefactoryResolver (ctx.ParserContext, cls.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, null, fileName);
			
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (resolver, file, cls);
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
		public override IEnumerable<MemberReference> FindMemberReferences (RefactorerContext ctx, string fileName, IType cls, IMember member)
		{
			ParsedDocument parsedDocument = parser.Parse (cls.SourceProjectDom, fileName);
			
			NRefactoryResolver resolver = new NRefactoryResolver (ctx.ParserContext, parsedDocument.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, null, fileName);
			resolver.SetupParsedCompilationUnit (parser.LastUnit);
			resolver.CallingMember = member;
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (resolver, ctx.GetFile (fileName), member);
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
		
		public override IEnumerable<MemberReference> FindParameterReferences (RefactorerContext ctx, string fileName, IParameter param)
		{
//			System.Console.WriteLine("Find parameter references !!!");
			//IMember member = param.DeclaringMember;
			NRefactoryResolver resolver = new NRefactoryResolver (ctx.ParserContext, param.DeclaringMember.DeclaringType.CompilationUnit, ICSharpCode.NRefactory.SupportedLanguage.CSharp, null, fileName);
			
			resolver.CallingMember = param.DeclaringMember;
			
			FindMemberAstVisitor visitor = new FindMemberAstVisitor (resolver, ctx.GetFile (fileName), param);
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
			string post = eolMarker + indent + "#endregion" + eolMarker;
			
			buffer.InsertText (pos, pre + indent + post);
			return pos + indent.Length + pre.Length;
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
				if (((CodeMemberEvent)member).Type != null) {
					result = result.Substring (0, result.Length - 1) + " {" + Environment.NewLine +
						"\tadd { /* TODO */ }" + Environment.NewLine +
						"\tremove { /* TODO */ }" + Environment.NewLine +
					"}";
				}
			}
			return result;
		}
	}
/* Moved & Simplified to: FindMemberAstVisitors	
	class MemberRefactoryVisitor: AbstractAstVisitor
	{
		MemberReferenceCollection references;
		ICSharpCode.NRefactory.Ast.CompilationUnit fileCompilationUnit;
		IEditableTextFile file;
		RefactorerContext ctx;
		IType declaringType;
		IDomVisitable  member;
		NRefactoryResolver resolver;
		Hashtable unique;
		string memberName;
		Stack<TypeDeclaration> typeStack = new Stack<TypeDeclaration> ();
		
		public MemberRefactoryVisitor (RefactorerContext ctx, NRefactoryResolver resolver, IType declaringType, IDomVisitable member, MemberReferenceCollection references)
		{
			unique = new Hashtable ();
			
			this.ctx = ctx;
			this.resolver = resolver;
			this.declaringType = declaringType;
			this.references = references;
			this.member = member;
			// consider INameable interface ?
			if (member is IMember) {
				this.memberName = ((IMember)member).Name;
			} else if (member is IParameter) {
				this.memberName = ((IParameter)member).Name;
			} else {
				this.memberName = ((LocalVariable)member).Name;
			}
		}
		class ExpressionVisitor : ICSharpCode.NRefactory.Visitors.AbstractAstVisitor
		{
			HashSet<string> identifiers = new HashSet<string> ();
			public HashSet<string> Identifiers {
				get {
					return identifiers;
				}
			}
			
			public override object VisitIdentifierExpression(ICSharpCode.NRefactory.Ast.IdentifierExpression identifierExpression, object data)
			{
				identifiers.Add (identifierExpression.Identifier);
				return null;
			}
		}
		
		static IEnumerable<HashSet<T>> GetAllCombinations<T> (IEnumerable<T> input)
		{
			List<T> strings = new List<T> (input);
			List<HashSet<T>> result = new List<HashSet<T>> ();
			result.Add (new HashSet<T>());
			for (int i = 0; i < strings.Count; i++) {
				int curCount = result.Count;
				for (int j = 0; j < curCount; j++) {
					HashSet<T> newSet = new HashSet<T> (result[j]);
					newSet.Add (strings[i]);
					result.Add (newSet);
				}
			}
			return result;
		}
		
		static List<HashSet<string>> GetUsedDefineCombinations (ICSharpCode.NRefactory.IParser parser)
		{
			List<HashSet<string>> result = new List<HashSet<string>> ();
			foreach (ISpecial special in parser.Lexer.SpecialTracker.CurrentSpecials) {
				PreprocessingDirective directive = special as PreprocessingDirective;
				if (directive == null || (directive.Cmd != "#if" && directive.Cmd != "#elif"))
					continue;
				
				ExpressionVisitor visitor = new ExpressionVisitor ();
				directive.Expression.AcceptVisitor (visitor, null);
				ICSharpCode.NRefactory.Parser.CSharp.ConditionalCompilation cond = new ICSharpCode.NRefactory.Parser.CSharp.ConditionalCompilation ();
				bool nothingDefined = cond.Evaluate (directive.Expression);
				foreach (var combination in GetAllCombinations (visitor.Identifiers)) {
					cond = new ICSharpCode.NRefactory.Parser.CSharp.ConditionalCompilation ();
					HashSet<string> defines = new HashSet<string> ();
					foreach (string usedIdentifier in combination) {
						cond.Define (usedIdentifier);
						defines.Add (usedIdentifier);
						bool curDefineStatus = cond.Evaluate (directive.Expression);
						if (curDefineStatus != nothingDefined) {
							result.Add (defines);
							goto next;
						}
					}
				}
			 next: ;
			}
			return result ;
		}
		
		public void Visit (ProjectDom pctx, IEditableTextFile file)
		{
			ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (ICSharpCode.NRefactory.SupportedLanguage.CSharp, new StringReader (file.Text));
			parser.Lexer.EvaluateConditionalCompilation = true;
			parser.Parse ();
			Visit (pctx, file, parser.CompilationUnit);
			List<HashSet<string>> usedIdentifiers = GetUsedDefineCombinations (parser);
			
			for (int i = 0; i < usedIdentifiers.Count; i++) {
				parser.Lexer.ConditionalCompilationSymbols.Clear ();
				foreach (string define in usedIdentifiers[i])
					parser.Lexer.ConditionalCompilationSymbols.Add (define, true);
				parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (ICSharpCode.NRefactory.SupportedLanguage.CSharp, new StringReader (file.Text));
				parser.Parse ();
				Visit (pctx, file, parser.CompilationUnit);
			}
		}
		
		public void Visit (ProjectDom pctx, IEditableTextFile file, ICSharpCode.NRefactory.Ast.CompilationUnit unit)
		{
			this.file = file;
			fileCompilationUnit = unit;
			
			if (fileCompilationUnit != null)
				VisitCompilationUnit (fileCompilationUnit, null);
		}
		
		bool IsExpectedClass (IType type)
		{
			return IsExpectedClass (type, new Dictionary<string,string> ());
		}
		
		bool IsExpectedClass (IType type, Dictionary<string,string> checkedTypes)
		{
			if (type == null || checkedTypes.ContainsKey (type.FullName))
				return false;
			
			if (member is IType && type.FullName == ((IType)member).FullName)
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
			string type = fieldDeclaration.TypeReference.SystemType ?? fieldDeclaration.TypeReference.Type;
			if (member is IType && memberName == GetNameWithoutPrefix (type)) {
				int line = fieldDeclaration.StartLocation.Y;
				int col = fieldDeclaration.StartLocation.X;
				ResolveResult resolveResult = resolver.ResolveIdentifier (type, new DomLocation (line, col));
				
				IReturnType cls = resolveResult != null ? resolveResult.ResolvedType : null;
				
				if (cls != null && cls.FullName == ((IType)member).FullName) {
					//Debug ("adding FieldDeclaration", cls.FullName, fieldDeclaration);
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			if (member is IField) {
				if (typeStack.Peek ().Name  == ((IField)member).DeclaringType.FullName) {
					foreach (VariableDeclaration variable in fieldDeclaration.Fields) {
						if (variable.Name == memberName) {
							AddUniqueReference (variable.StartLocation.Y, variable.StartLocation.X, memberName);
						}
					}
				}
			}
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}
		
		public override object VisitTypeReference(TypeReference typeReference, object data)
		{
			//System.Console.WriteLine("visit type reference: " + typeReference);
			string type = typeReference.SystemType ?? typeReference.Type;
			
			if (member is IType && memberName == GetNameWithoutPrefix (type)) {
				int line = typeReference.StartLocation.Y;
				int col = typeReference.StartLocation.X;
				ResolveResult resolveResult = resolver.ResolveIdentifier (type, new DomLocation (line, col));
				IReturnType cls = resolveResult != null ? resolveResult.ResolvedType : null;
				if (cls == null || cls.FullName == ((IType)member).FullName) {
					//Debug ("adding CastExpression", cls.FullName, castExpression);
					AddUniqueReference (line, col, typeReference.Type);
				}
			}
			return base.VisitTypeReference (typeReference, data);
		}
		
		public override object VisitMemberReferenceExpression (MemberReferenceExpression fieldExp, object data)
		{
			if (!(member is IParameter) && fieldExp.MemberName == memberName) {
				ResolveResult resolveResult= resolver.ResolveExpression (fieldExp, new DomLocation (fieldExp.EndLocation.Y, fieldExp.EndLocation.X));
				IType cls = resolveResult != null ? resolver.Dom.GetType (resolveResult.ResolvedType) : null;
				if (cls != null && (IsExpectedClass (cls) || cls.Equals (member))) {
					int pos = file.GetPositionFromLineColumn (fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					int endpos = file.GetPositionFromLineColumn (fieldExp.EndLocation.Y, fieldExp.EndLocation.X);
					string txt = file.GetText (pos, endpos);
					if (txt == memberName) {
						//Debug ("adding FieldReferenceExpression", member.Name, fieldExp);
						AddUniqueReference (fieldExp.StartLocation.Y, fieldExp.StartLocation.X, memberName);
					}
				}
			}
			
			return base.VisitMemberReferenceExpression (fieldExp, data);
		}
		
		public override object VisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			if (member is IParameter) {
				if (parameterDeclarationExpression.ParameterName == memberName) 
					AddUniqueReference (parameterDeclarationExpression.StartLocation.Y, parameterDeclarationExpression.StartLocation.X, parameterDeclarationExpression.ParameterName);
			}
			return base.VisitParameterDeclarationExpression (parameterDeclarationExpression, data);
		}
		
		public override object VisitMethodDeclaration(MethodDeclaration methodDeclaration, object data) 
		{
			//System.Console.WriteLine("VisitMethodDeclaration " + methodDeclaration);
			// find override references.
			if (member is IMethod && methodDeclaration.Name == memberName) {
				MethodResolveResult mrr = resolver.ResolveIdentifier (memberName, new DomLocation (methodDeclaration.StartLocation.Y, methodDeclaration.StartLocation.X)) as MethodResolveResult;
				if (mrr != null) {
					IMethod m = mrr.MostLikelyMethod;
					if (IsExpectedClass (m.DeclaringType) && m.GenericParameters.Count == ((IMethod)member).GenericParameters.Count && m.Parameters.Count == ((IMethod)member).Parameters.Count) {
						AddUniqueReference (methodDeclaration.StartLocation.Y, methodDeclaration.StartLocation.X, memberName);
					}
				}
			}
			return base.VisitMethodDeclaration (methodDeclaration, data);
		}
		
		static bool MightBeInvocation (Expression expression, IMethod method)
		{
			if (expression is IdentifierExpression) 
				return ((IdentifierExpression)expression).Identifier == method.Name;
			if (expression is MemberReferenceExpression) 
				return ((MemberReferenceExpression)expression).MemberName == method.Name;
			return false;
		}
		
		public override object VisitInvocationExpression (InvocationExpression invokeExp, object data)
		{
			//Debug ("InvocationExpression", invokeExp.ToString (), invokeExp);
			if (member is IMethod) {
				IMethod method = (IMethod)member;
				if (MightBeInvocation (invokeExp.TargetObject, method) && invokeExp.Arguments.Count == method.Parameters.Count) {
					ResolveResult resolveResult = resolver.ResolveExpression (invokeExp.TargetObject, new DomLocation (invokeExp.StartLocation.Y, invokeExp.StartLocation.X));
					if (resolveResult is MethodResolveResult) {
						MethodResolveResult mrr = (MethodResolveResult)resolveResult;
						if (mrr.MostLikelyMethod.FullName == method.FullName && mrr.MostLikelyMethod.GenericParameters.Count == method.GenericParameters.Count)
							AddUniqueReference (invokeExp.StartLocation.Y, invokeExp.StartLocation.X, memberName);	
					}
				}
			}
			
			return base.VisitInvocationExpression (invokeExp, data);
		}
		
		public override object VisitLocalVariableDeclaration (LocalVariableDeclaration localVariableDeclaration, object data)
		{
			if (member is LocalVariable) {
				foreach (VariableDeclaration decl in localVariableDeclaration.Variables ) {
					if (decl.Name == memberName) 
						AddUniqueReference (decl.StartLocation.Y, decl.StartLocation.X, decl.Name);
				}
			}
			return base.VisitLocalVariableDeclaration (localVariableDeclaration, data);
		}

		public override object VisitIdentifierExpression (IdentifierExpression idExp, object data)
		{
			if (idExp.Identifier == memberName) {
				int line = idExp.StartLocation.Y;
				int col = idExp.StartLocation.X;
				
				ResolveResult result = resolver.ResolveIdentifier (idExp.Identifier, new DomLocation (line, col));
				
				if (member is IType) {
					IMember item = result != null ? ((MemberResolveResult)result).ResolvedMember : null;
					if (item == null || item is IType && ((IType) item).FullName == ((IType)member).FullName) {
						//Debug ("adding IdentifierExpression class", idExp.Identifier, idExp);
						AddUniqueReference (line, col, idExp.Identifier);
					}
				} else if (member is LocalVariable && result is LocalVariableResolveResult) {
					LocalVariable avar = member as LocalVariable;
					LocalVariable var = ((LocalVariableResolveResult)result).LocalVariable;
//					if (var != null && avar.Region.Contains (var.Region.Start)) {
						//Debug ("adding IdentifierExpression variable", idExp.Identifier, idExp);
					AddUniqueReference (line, col, idExp.Identifier);
//					}
				} else if (member is IParameter && result is ParameterResolveResult) {
					IParameter param = ((ParameterResolveResult)result).Parameter;
					
					// FIXME: might need to match more than this?
					if (param != null && IsExpectedMember (param.DeclaringMember)) {
						//Debug ("adding IdentifierExpression param", idExp.Identifier, idExp);
						AddUniqueReference (line, col, idExp.Identifier);
					}
				} else if (member is IMember && result is MemberResolveResult) {
					IMember item = ((MemberResolveResult)result).ResolvedMember;
					IMember m = item as IMember;
					if (m != null && IsExpectedClass (m.DeclaringType) &&
						((member is IField && item is IField) || (member is IMethod && item is IMethod) ||
						 (member is IProperty && item is IProperty) || (member is IEvent && item is IEvent))) {
						//Debug ("adding IdentifierExpression member", member.Name, idExp);
						AddUniqueReference (line, col, memberName);
					}
				} 
			}
			
			return base.VisitIdentifierExpression (idExp, data);
		}

		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			//Debug ("PropertyDeclaration", propertyDeclaration.Name, propertyDeclaration);
			// find override references.
			if (member is IProperty && (propertyDeclaration.Modifier & ICSharpCode.NRefactory.Ast.Modifiers.Override) == ICSharpCode.NRefactory.Ast.Modifiers.Override && propertyDeclaration.Name == memberName) {
				MemberResolveResult mrr = resolver.ResolveIdentifier (memberName, new DomLocation (propertyDeclaration.StartLocation.Y, propertyDeclaration.StartLocation.X)) as MemberResolveResult;
				IMember m = mrr.ResolvedMember;
				if (IsExpectedClass (m.DeclaringType)) {
					AddUniqueReference (propertyDeclaration.StartLocation.Y, propertyDeclaration.StartLocation.X, memberName);
				}
			}
			
			string type = propertyDeclaration.TypeReference.SystemType ?? propertyDeclaration.TypeReference.Type;
			if (member is IType && memberName == GetNameWithoutPrefix (type)) {
				int line = propertyDeclaration.StartLocation.Y;
				int col = propertyDeclaration.StartLocation.X;
				ResolveResult resolveResult = resolver.ResolveIdentifier (type, new DomLocation (line, col));
				IReturnType cls = resolveResult != null ? resolveResult.ResolvedType : null;
				
				if (cls != null && cls.FullName == ((IType) member).FullName) {
					//Debug ("adding PropertyDeclaration", cls.FullName, propertyDeclaration);
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			
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
			//System.Console.WriteLine("CastExpression" + castExpression.ToString ());
			string type = castExpression.CastTo.SystemType ?? castExpression.CastTo.Type;
			if (member is IType && memberName == GetNameWithoutPrefix (type)) {
				int line = castExpression.CastTo.StartLocation.Y;
				int col = castExpression.CastTo.StartLocation.X;
				ResolveResult resolveResult = resolver.ResolveIdentifier (type, new DomLocation (line, col));
				IReturnType cls = resolveResult != null ? resolveResult.ResolvedType : null;
				
				if (cls != null && cls.FullName == ((IType)member).FullName) {
					//Debug ("adding CastExpression", cls.FullName, castExpression);
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			
			return base.VisitCastExpression (castExpression, data);
		}
		
		public override object VisitObjectCreateExpression (ObjectCreateExpression objCreateExpression, object data)
		{
			//System.Console.WriteLine("ObjectCreateExpression:" + objCreateExpression);
			string type = objCreateExpression.CreateType.SystemType ?? objCreateExpression.CreateType.Type;
			int line = objCreateExpression.CreateType.StartLocation.Y;
			int col = objCreateExpression.CreateType.StartLocation.X;
			
			if ((member is IType || (member is IMethod && ((IMethod) member).IsConstructor)) && declaringType != null && declaringType.Name == GetNameWithoutPrefix (type)) {
				ResolveResult resolveResult = resolver.ResolveIdentifier (type, new DomLocation (line, col));
				IReturnType cls = resolveResult != null ? resolveResult.ResolvedType : null;
				
				if (cls != null && (member is IType && cls.FullName == ((IType)member).FullName) || declaringType.FullName == cls.FullName) {
					//Debug ("adding ObjectCreateExpression", cls.FullName, objCreateExpression);
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			
			return base.VisitObjectCreateExpression (objCreateExpression, data);
		}
		
		public override object VisitConstructorDeclaration (ICSharpCode.NRefactory.Ast.ConstructorDeclaration constructorDeclaration, object data)
		{
			if (member is IType) {
				if (constructorDeclaration.Name == memberName)
					AddUniqueReference (constructorDeclaration.StartLocation.Line, constructorDeclaration.StartLocation.Column, memberName);
			}
			return base.VisitConstructorDeclaration (constructorDeclaration, data);
		}

		public override object VisitVariableDeclaration (VariableDeclaration varDeclaration, object data)
		{
			//System.Console.WriteLine("VariableDeclaration:" + varDeclaration);
			string type = varDeclaration.TypeReference.SystemType ?? varDeclaration.TypeReference.Type;
			if (member is IType && memberName == GetNameWithoutPrefix (type)) {
				int line = varDeclaration.StartLocation.Y;
				int col = varDeclaration.StartLocation.X;
				ResolveResult resolveResult = resolver.ResolveIdentifier (type, new DomLocation (line, col));
				IReturnType cls = resolveResult != null ? resolveResult.ResolvedType : null;
				
				if (cls != null && cls.FullName == ((IType)member).FullName) {
					//Debug ("adding varDeclaration", cls.FullName, varDeclaration);
					line = varDeclaration.TypeReference.StartLocation.Y;
					col = varDeclaration.TypeReference.StartLocation.X;
					AddUniqueReference (line, col, cls.FullName);
				}
			}
			
			return base.VisitVariableDeclaration (varDeclaration, data);
		}
		
		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			//System.Console.WriteLine("VisitTypeDeclaration " + typeDeclaration);
			if (member is IType && typeDeclaration.BaseTypes != null) {
				string fname = ((IType)member).FullName;
				if (typeDeclaration.Name == memberName && ((IType)member).TypeParameters.Count == typeDeclaration.Templates.Count)
					AddUniqueReference (typeDeclaration.StartLocation.Line, typeDeclaration.StartLocation.Column, typeDeclaration.Name);
				
				foreach (TypeReference bc in typeDeclaration.BaseTypes) {
					ResolveResult resolveResult = resolver.ResolveIdentifier (bc.Type, new DomLocation (typeDeclaration.StartLocation.Y, typeDeclaration.StartLocation.X));
					IReturnType bclass = resolveResult != null ? resolveResult.ResolvedType : null;
					//System.Console.WriteLine(resolveResult + "/ bclass:" + bclass);
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
			typeStack.Push (typeDeclaration);
			object result =  base.VisitTypeDeclaration (typeDeclaration, data);
			typeStack.Pop ();
			return result; 
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
*/
}
