//
// FindMemberAstVisitor.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C)  2009  Novell, Inc (http://www.novell.com)
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
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;

using MonoDevelop.Projects.Dom;
using MonoDevelop.Projects.Dom.Output;
using MonoDevelop.Projects.Dom.Parser;
using MonoDevelop.Projects.Text;
using MonoDevelop.Projects.CodeGeneration;

using ICSharpCode.NRefactory;
using ICSharpCode.NRefactory.Parser;
using ICSharpCode.NRefactory.Ast;
using ICSharpCode.NRefactory.Visitors;
using MonoDevelop.Ide.FindInFiles;

namespace MonoDevelop.CSharpBinding
{
	public class FindMemberAstVisitor : AbstractAstVisitor
	{
		List<MemberReference> foundReferences = new List<MemberReference> ();
		public List<MemberReference> FoundReferences {
			get {
				return foundReferences;
			}
		}
		
		NRefactoryResolver resolver;
		IEditableTextFile file;
		
		IDomVisitable searchedMember;
		DomLocation   searchedMemberLocation;
		string        searchedMemberName;
		string        searchedMemberFile = null;
		
		Stack<TypeDeclaration> typeStack = new Stack<TypeDeclaration> ();
		
		public bool IncludeXmlDocumentation {
			get;
			set;
		}
		public FindMemberAstVisitor (NRefactoryResolver resolver, IEditableTextFile file, IDomVisitable searchedMember)
		{
			this.file = file;
			this.resolver = resolver;
			this.searchedMember = searchedMember;
			this.IncludeXmlDocumentation = false;
			if (searchedMember is IMethod) {
				IMethod method = (IMethod)searchedMember;
				this.searchedMemberName = method.IsConstructor ? method.DeclaringType.Name : method.Name;
				this.searchedMemberLocation = method.Location;
				if (method.DeclaringType != null && method.DeclaringType.CompilationUnit != null)
					this.searchedMemberFile = method.DeclaringType.CompilationUnit.FileName;
			} else if (searchedMember is IMember) {
				this.searchedMemberName = ((IMember)searchedMember).Name;
				this.searchedMemberLocation = ((IMember)searchedMember).Location;
				
				if (searchedMember is IType) {
					this.searchedMemberFile = ((IType)searchedMember).CompilationUnit.FileName;
				} else {
					if (((IMember)searchedMember).DeclaringType != null && ((IMember)searchedMember).DeclaringType.CompilationUnit != null)
						this.searchedMemberFile = ((IMember)searchedMember).DeclaringType.CompilationUnit.FileName;
				}
			} else if (searchedMember is IParameter) {
				this.searchedMemberName = ((IParameter)searchedMember).Name;
				this.searchedMemberLocation = ((IParameter)searchedMember).Location;
				if (((IParameter)searchedMember).DeclaringMember.DeclaringType.CompilationUnit != null)
					this.searchedMemberFile = ((IParameter)searchedMember).DeclaringMember.DeclaringType.CompilationUnit.FileName;
			} else if (searchedMember != null) {
				this.searchedMemberName = ((LocalVariable)searchedMember).Name;
				this.searchedMemberLocation = ((LocalVariable)searchedMember).Region.Start;
				if (((LocalVariable)searchedMember).CompilationUnit != null)
					this.searchedMemberFile = ((LocalVariable)searchedMember).CompilationUnit.FileName;
			}
		}
		
		static readonly Regex paramRegex    = new Regex ("\\<param\\s+name\\s*=\\s*\"(.*)\"", RegexOptions.Compiled);
		static readonly Regex paramRefRegex = new Regex ("\\<paramref\\s+name\\s*=\\s*\"(.*)\"", RegexOptions.Compiled);

		static readonly Regex seeRegex       = new Regex ("\\<see\\s+cref\\s*=\\s*\"(.*)\"", RegexOptions.Compiled);
		static readonly Regex seeAlsoRegRegex = new Regex ("\\<seealso\\s+cref\\s*=\\s*\"(.*)\"", RegexOptions.Compiled);

		public void RunVisitor ()
		{
			if (searchedMember == null)
				return;
			string text = file.Text;
			
			// search if the member name exists in the file (otherwise it doesn't make sense to search it)
			FindReplace findReplace = new FindReplace ();
			FilterOptions filterOptions = new FilterOptions {
				CaseSensitive = true,
				WholeWordsOnly = true
			};
			findReplace.CompilePattern (searchedMemberName, filterOptions);
			IEnumerable<SearchResult> result = findReplace.Search (new FileProvider (null), text, searchedMemberName, null, filterOptions);
			if (result == null || !result.Any ()) {
				return;
			}
			
			// It exists -> now looking with the parser
			ICSharpCode.NRefactory.IParser parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (ICSharpCode.NRefactory.SupportedLanguage.CSharp, new StringReader (text));
			parser.Lexer.EvaluateConditionalCompilation = true;
			parser.Parse ();
			resolver.SetupParsedCompilationUnit (parser.CompilationUnit);
			VisitCompilationUnit (parser.CompilationUnit, null);
			
			List<HashSet<string>> usedIdentifiers = GetUsedDefineCombinations (parser);
			for (int i = 0; i < usedIdentifiers.Count; i++) {
				parser.Lexer.ConditionalCompilationSymbols.Clear ();
				foreach (string define in usedIdentifiers[i])
					parser.Lexer.ConditionalCompilationSymbols.Add (define, true);
				parser = ICSharpCode.NRefactory.ParserFactory.CreateParser (ICSharpCode.NRefactory.SupportedLanguage.CSharp, new StringReader (file.Text));
				parser.Parse ();
				
				VisitCompilationUnit (parser.CompilationUnit, null);
			}
			if (IncludeXmlDocumentation) {
				if (searchedMember is IParameter) {
					IParameter parameter = (IParameter)searchedMember;
					var docComments = from ICSharpCode.NRefactory.Comment cmt in 
						(from ISpecial s in parser.Lexer.SpecialTracker.CurrentSpecials 
						where s is ICSharpCode.NRefactory.Comment && s.StartPosition.Line <= parameter.DeclaringMember.Location.Line
						select s) 
						select cmt;
					
					ICSharpCode.NRefactory.Comment lastComment = null;
					foreach (ICSharpCode.NRefactory.Comment curComment in docComments.Reverse ()) {
						if (lastComment != null && Math.Abs (lastComment.StartPosition.Line - curComment.StartPosition.Line) > 1)
							break;
						// Concat doesn't work on MatchCollections
						foreach (var matchCol in new [] { paramRegex.Matches (curComment.CommentText), paramRefRegex.Matches (curComment.CommentText) }) {
							foreach (Match match in matchCol) {
								if (match.Groups[1].Value == searchedMemberName) 
									AddUniqueReference (curComment.StartPosition.Line, curComment.StartPosition.Column + match.Groups[1].Index, searchedMemberName);
							}
						}
						lastComment = curComment;
					}
				} else if (searchedMember is IMember) {
					IMember member = (IMember)searchedMember;
						var docComments = from ICSharpCode.NRefactory.Comment cmt in 
						(from ISpecial s in parser.Lexer.SpecialTracker.CurrentSpecials 
						where s is ICSharpCode.NRefactory.Comment
						select s) 
						select cmt;
					
					string fullName = member.FullName;
					
					foreach (ICSharpCode.NRefactory.Comment curComment in docComments) {
						// Concat doesn't work on MatchCollections
						foreach (var matchCol in new [] { seeRegex.Matches (curComment.CommentText), seeAlsoRegRegex.Matches (curComment.CommentText) }) {
							foreach (Match match in matchCol) {
								if (match.Groups[1].Value.StartsWith (fullName)) 
									AddUniqueReference (curComment.StartPosition.Line, curComment.StartPosition.Column + match.Groups[1].Index + fullName.Length - searchedMemberName.Length, searchedMemberName);
							}
						}
						
					}
				}
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
		
		static string GetNameWithoutPrefix (string fullName)
		{
			int idx = fullName.LastIndexOf ('.');
			return idx < 0 ? fullName : fullName.Substring (idx + 1);
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
			
			return new MemberReference (null, file.Name, pos, line, col, name, txt);
		}
		
		HashSet<MemberReference> unique = new HashSet<MemberReference> ();
		void AddUniqueReference (int line, int col, string name)
		{
			if (line < 1 || col < 1) {
				MonoDevelop.Core.LoggingService.LogWarning ("AddUniqueReference called with invalid position line: {0} col: {1} name: {2}.", line, col, name);
				return;
			}
			
			MemberReference mref = CreateReference (line, col, name);
			
			if (unique.Add (mref)) 
				foundReferences.Add (mref);
		}
		
		bool IsSearchedNode (INode node)
		{
			if (node == null || node.StartLocation.IsEmpty)
				return false;
			return (string.IsNullOrEmpty (searchedMemberFile) || file.Name == searchedMemberFile) && 
			       node.StartLocation.Line == this.searchedMemberLocation.Line && 
			       node.StartLocation.Column == this.searchedMemberLocation.Column;
		}
		
		static bool IsIdentifierPart (char ch)
		{
			return Char.IsLetterOrDigit (ch) || ch == '_';
		}
		
		bool SearchText (string text, int startLine, int startColumn, out int line, out int column)
		{
			int position = file.GetPositionFromLineColumn (startLine, startColumn);
			line = column = -1;
			while (position + searchedMemberName.Length < file.Length) {
				if ((position == 0 || !IsIdentifierPart (file.GetCharAt (position - 1))) && 
				    (position + searchedMemberName.Length >= file.Length  || !IsIdentifierPart (file.GetCharAt (position + searchedMemberName.Length))) &&
				    file.GetText (position, position + searchedMemberName.Length) == searchedMemberName) {
					file.GetLineColumnFromPosition (position, out line, out column);
					return true;
				}
				position ++;
			}
			return false;
		}
		
		void CheckNode (INode node)
		{
			if (IsSearchedNode (node)) {
				int line, column;
				if (SearchText (searchedMemberName, node.StartLocation.Line, node.StartLocation.Column, out line, out column)) {
					AddUniqueReference (line, column, searchedMemberName);
				}
			}
		}
		string CurrentTypeFullName {
			get {
				if (typeStack.Count == 0)
					return null;
				string ns = namespaceStack.Count > 0 ? String.Join (".", namespaceStack.ToArray ()) + "." : "";
				return ns + typeStack.Peek ().Name;
			}
		}
		public override object VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration, object data)
		{
			CheckNode (constructorDeclaration);
			if (this.searchedMember is IType) {
				if (((IType)this.searchedMember).FullName == CurrentTypeFullName &&
				    ((IType)this.searchedMember).TypeParameters.Count == typeStack.Peek ().Templates.Count)
					AddUniqueReference (constructorDeclaration.StartLocation.Line, constructorDeclaration.StartLocation.Column, this.searchedMemberName);
			}
			if (this.searchedMember is IMethod) {
				IMethod method = (IMethod)this.searchedMember; 
				if (method.IsConstructor)
					CheckNode (constructorDeclaration);
				
			}
			
			return base.VisitConstructorDeclaration (constructorDeclaration, data);
		}
		
		public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			CheckNode (destructorDeclaration);
			if (this.searchedMember is IType) {
				if (((IType)this.searchedMember).FullName == CurrentTypeFullName && 
				    ((IType)this.searchedMember).TypeParameters.Count == typeStack.Peek ().Templates.Count)
					AddUniqueReference (destructorDeclaration.StartLocation.Line, destructorDeclaration.StartLocation.Column + 1, this.searchedMemberName);
			}
			
			return base.VisitDestructorDeclaration (destructorDeclaration, data);
		}
		
		public override object VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration, object data)
		{
			CheckNode (delegateDeclaration);
			return base.VisitDelegateDeclaration (delegateDeclaration, data);
		}
		
		public override object VisitEventDeclaration (EventDeclaration eventDeclaration, object data)
		{
			CheckNode (eventDeclaration);
			return base.VisitEventDeclaration (eventDeclaration, data);
		}
		
		public override object VisitFieldDeclaration (FieldDeclaration fieldDeclaration, object data)
		{
			CheckNode (fieldDeclaration);
			return base.VisitFieldDeclaration (fieldDeclaration, data);
		}
		
		public override object VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration, object data)
		{
			CheckNode (indexerDeclaration);
			return base.VisitIndexerDeclaration(indexerDeclaration, data);
		}
		
		public override object VisitLocalVariableDeclaration (LocalVariableDeclaration localVariableDeclaration, object data)
		{
			if (searchedMember is LocalVariable) {
				LocalVariable searchedVariable = (LocalVariable)searchedMember;
				INode parent = localVariableDeclaration.Parent;
				while (parent != null && !(parent is MemberNode) && !(parent is ParametrizedNode)) {
					parent = parent.Parent;
				}
				if (parent != null &&
				    parent.StartLocation.Line == searchedVariable.DeclaringMember.Location.Line && 
				    parent.StartLocation.Column == searchedVariable.DeclaringMember.Location.Column &&
				    searchedVariable.Region.Start.Line  == localVariableDeclaration.StartLocation.Line - 1 && 
				    searchedVariable.Region.Start.Column  == localVariableDeclaration.StartLocation.Column - 1) {
					foreach (VariableDeclaration decl in localVariableDeclaration.Variables) {
						if (decl.Name == searchedMemberName) 
							AddUniqueReference (decl.StartLocation.Y, decl.StartLocation.X, searchedMemberName);
					}
				}
			}
			return base.VisitLocalVariableDeclaration(localVariableDeclaration, data);
		}
		
		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			CheckNode (methodDeclaration);
			return base.VisitMethodDeclaration(methodDeclaration, data);
		}
		
		public override object VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, object data)
		{
			CheckNode (operatorDeclaration);
			return base.VisitOperatorDeclaration(operatorDeclaration, data);
		}
		
		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			CheckNode (propertyDeclaration);
			return base.VisitPropertyDeclaration(propertyDeclaration, data);
		}
		
		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			CheckNode (typeDeclaration);
			typeStack.Push (typeDeclaration);
			object result =  base.VisitTypeDeclaration (typeDeclaration, data);
			typeStack.Pop ();
			return result; 
		}
		
		public override object VisitParameterDeclarationExpression(ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			CheckNode (parameterDeclarationExpression);
			return base.VisitParameterDeclarationExpression(parameterDeclarationExpression, data);
		}
				
		public override object VisitVariableDeclaration (VariableDeclaration variableDeclaration, object data)
		{
			return base.VisitVariableDeclaration(variableDeclaration, data);
		}
		
		public override object VisitObjectCreateExpression (ICSharpCode.NRefactory.Ast.ObjectCreateExpression objectCreateExpression, object data)
		{
			IMethod method = searchedMember as IMethod;
			if (method != null && method.IsConstructor) {
				ResolveResult resolveResult = resolver.ResolveExpression (objectCreateExpression, new DomLocation (objectCreateExpression.StartLocation.Y - 1, objectCreateExpression.StartLocation.X - 1));
				if (resolveResult != null && resolveResult.ResolvedType != null) {
					IType resolvedType = resolver.Dom.GetType (resolveResult.ResolvedType);
					int pCount = objectCreateExpression.Parameters != null ? objectCreateExpression.Parameters.Count : 0;
					if (resolvedType != null && resolvedType.FullName == method.DeclaringType.FullName && pCount == method.Parameters.Count) {
						int line, column;
						if (SearchText (searchedMemberName, objectCreateExpression.StartLocation.Line, objectCreateExpression.StartLocation.Column, out line, out column))
							AddUniqueReference (line, column, searchedMemberName);
					}
				}
			}
			return base.VisitObjectCreateExpression (objectCreateExpression, data);
		}
		
		public override object VisitTryCatchStatement (TryCatchStatement tryCatchStatement, object data)
		{
			if (searchedMember is LocalVariable) {
				foreach (CatchClause catchClause in tryCatchStatement.CatchClauses) {
					if (catchClause == null)
						continue;
					LocalVariable searchedVariable = (LocalVariable)searchedMember;
					
					if (catchClause.TypeReference != null && catchClause.VariableName == searchedMemberName && searchedVariable.Region.Start.Line == catchClause.StartLocation.Line - 1 && searchedVariable.Region.Start.Column == catchClause.StartLocation.Column - 1) {
						int line, col;
						SearchText (searchedMemberName, catchClause.StartLocation.Line, catchClause.StartLocation.Column, out line, out col);
						AddUniqueReference (line, col, searchedMemberName);
					}
				}
			}
			return base.VisitTryCatchStatement (tryCatchStatement, data);
		}
		
		public override object VisitForeachStatement(ForeachStatement foreachStatement, object data)
		{
			if (searchedMember is LocalVariable && foreachStatement.VariableName == searchedMemberName && searchedMemberLocation.Line == foreachStatement.StartLocation.Line - 1 && searchedMemberLocation.Column == foreachStatement.StartLocation.Column - 1) {
				int line, col;
				SearchText (searchedMemberName, foreachStatement.StartLocation.Line, foreachStatement.StartLocation.Column, out line, out col);
				AddUniqueReference (line, col, searchedMemberName);
			}
			
			return base.VisitForeachStatement (foreachStatement, data);
		}
		
		public override object VisitIdentifierExpression (IdentifierExpression idExp, object data)
		{
			if (idExp.Identifier == searchedMemberName) {
				int line = idExp.StartLocation.Y;
				int col = idExp.StartLocation.X;
				ResolveResult result = resolver.ResolveIdentifier (idExp.Identifier, new DomLocation (line - 1, col - 1));
				//Console.WriteLine ("result:" + result);
				if (searchedMember is IType) {
					IMember item = result != null ? ((MemberResolveResult)result).ResolvedMember : null;
					if (item != null && item is IType && ((IType)item).FullName == ((IType)searchedMember).FullName) {
					//	Debug ("adding IdentifierExpression class", idExp.Identifier, idExp);
						Console.WriteLine ("result:" + result);
					
						AddUniqueReference (line, col, idExp.Identifier);
					}
				} else if (searchedMember is LocalVariable && result is LocalVariableResolveResult) {
					LocalVariable avar = searchedMember as LocalVariable;
					LocalVariable var = ((LocalVariableResolveResult)result).LocalVariable;

					if (var != null && avar.DeclaringMember.FullName == var.DeclaringMember.FullName) {
//						Console.WriteLine (avar.Region.Start.Line + "---" +  var.Region.Start.Line);
						if (Math.Abs (avar.Region.Start.Line - var.Region.Start.Line) <= 1)
							AddUniqueReference (line, col, idExp.Identifier);
					}
				} else if (searchedMember is IParameter && result is ParameterResolveResult) {
					IParameter param = ((ParameterResolveResult)result).Parameter;
					if (param != null && ((IParameter)searchedMember).DeclaringMember.Location == param.DeclaringMember.Location)
						AddUniqueReference (line, col, idExp.Identifier);
				} else if (searchedMember is IMember && result is MemberResolveResult) {
					IMember item = ((MemberResolveResult)result).ResolvedMember;
					IMember m = item as IMember;
					//Console.WriteLine (searchedMember +  "/" + item);
					if (m != null /*&& IsExpectedClass (m.DeclaringType)*/ && ((IMember)searchedMember).DeclaringType.FullName == item.DeclaringType.FullName &&
						((searchedMember is IField && item is IField) || (searchedMember is IMethod && item is IMethod) ||
						 (searchedMember is IProperty && item is IProperty) || (searchedMember is IEvent && item is IEvent))) {
						//Debug ("adding IdentifierExpression searchedMember", searchedMember.Name, idExp);
						AddUniqueReference (line, col, searchedMemberName);
					}
				} 
			}
			
			return base.VisitIdentifierExpression (idExp, data);
		}
		
		public override object VisitTypeReference(TypeReference typeReference, object data)
		{
			string type = typeReference.SystemType ?? typeReference.Type;
			if (searchedMember is IType && this.searchedMemberName == GetNameWithoutPrefix (type)) {
				int line = typeReference.StartLocation.Y;
				int col  = typeReference.StartLocation.X;
				ExpressionResult res = new ExpressionResult ("new " + typeReference.ToString () + "()");
				ResolveResult resolveResult = resolver.Resolve (res, new DomLocation (line - 1, col - 1));
				
				IReturnType cls = resolveResult != null ? resolveResult.ResolvedType : null;
				IType resolvedType = cls != null ? resolver.Dom.SearchType (new SearchTypeRequest (resolver.Unit, cls, resolver.CallingType)) : null;
				if (resolvedType != null && resolvedType.FullName == ((IType)searchedMember).FullName) 
					AddUniqueReference (line, col, typeReference.Type);
			}
			return base.VisitTypeReference (typeReference, data);
		}

		public override object VisitMemberReferenceExpression (MemberReferenceExpression fieldExp, object data)
		{
			if (!(searchedMember is IParameter) && fieldExp.MemberName == searchedMemberName) {
				ResolveResult resolveResult= resolver.ResolveExpression (fieldExp, new DomLocation (fieldExp.EndLocation.Y - 1, fieldExp.EndLocation.X - 1));
				MemberResolveResult mrr = resolveResult as MemberResolveResult;
				if (mrr != null) {
					if (mrr.ResolvedMember != null && mrr.ResolvedMember.Location == searchedMemberLocation && mrr.ResolvedMember.DeclaringType.FullName == ((IMember)searchedMember).DeclaringType.FullName) {
						int line, column;
						if (SearchText (searchedMemberName, fieldExp.StartLocation.Line, fieldExp.StartLocation.Column, out line, out column))
							AddUniqueReference (line, column, searchedMemberName);
						return base.VisitMemberReferenceExpression (fieldExp, data);
					}
					return null;
				}
				IType cls = resolveResult != null ? resolver.Dom.GetType (resolveResult.ResolvedType) : null;
				if (cls != null) {
					int pos = file.GetPositionFromLineColumn (fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
					int endpos = file.GetPositionFromLineColumn (fieldExp.EndLocation.Y, fieldExp.EndLocation.X);
					string txt = file.GetText (pos, endpos);
					if (txt == searchedMemberName) {
						AddUniqueReference (fieldExp.StartLocation.Y, fieldExp.StartLocation.X, searchedMemberName);
					}
				}
			}
			return base.VisitMemberReferenceExpression (fieldExp, data);
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
			if (searchedMember is IMethod) {
				IMethod method = (IMethod)searchedMember;
				if (MightBeInvocation (invokeExp.TargetObject, method) && invokeExp.Arguments.Count == method.Parameters.Count) {
					ResolveResult resolveResult = resolver.ResolveExpression (invokeExp, new DomLocation (invokeExp.StartLocation.Y - 1, invokeExp.StartLocation.X - 1));
					IMethod resolvedMethod = null;
					if (resolveResult is MethodResolveResult) {
						MethodResolveResult mrr = (MethodResolveResult)resolveResult;
						resolvedMethod = mrr.MostLikelyMethod;
					} else if (resolveResult is MemberResolveResult) {
						resolvedMethod = ((MemberResolveResult)resolveResult).ResolvedMember as IMethod;
					}
					if (resolvedMethod != null) {
						if (resolvedMethod.FullName == method.FullName && resolvedMethod.TypeParameters.Count == method.TypeParameters.Count) {
							int line, column;
							if (SearchText (searchedMemberName, invokeExp.StartLocation.Line, invokeExp.StartLocation.Column, out line, out column))
								AddUniqueReference (line, column, searchedMemberName);
						}
					}
				}
				return true;
			}
			invokeExp.Arguments.ForEach (o => o.AcceptVisitor(this, data));
			return base.VisitInvocationExpression (invokeExp, data);
		}
		
		Stack<string> namespaceStack = new Stack<string> ();
		public override object VisitNamespaceDeclaration (ICSharpCode.NRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
		{
			namespaceStack.Push (namespaceDeclaration.Name);
			object result =  base.VisitNamespaceDeclaration (namespaceDeclaration, data);
			namespaceStack.Pop ();
			return result;
		}

				
	}
}
