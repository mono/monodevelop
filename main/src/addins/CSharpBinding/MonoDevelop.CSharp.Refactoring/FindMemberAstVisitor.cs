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

using ICSharpCode.OldNRefactory;
using ICSharpCode.OldNRefactory.Parser;
using ICSharpCode.OldNRefactory.Ast;
using ICSharpCode.OldNRefactory.Visitors;
using MonoDevelop.Ide.FindInFiles;
using MonoDevelop.CSharp.Resolver;
using MonoDevelop.Core;

namespace MonoDevelop.CSharp.Refactoring
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
		List<Tuple<MonoDevelop.Projects.Dom.INode, string, string, DomLocation>> searchedMembers = new List<Tuple<MonoDevelop.Projects.Dom.INode, string, string, DomLocation>> ();
		
		string fileName;
		Mono.TextEditor.Document text;
		Stack<TypeDeclaration> typeStack = new Stack<TypeDeclaration> ();

		public string SearchedMemberName {
			get { return searchedMembers.Count > 0 ? this.searchedMembers[0].Item2 : null; }
		}

		public bool IncludeXmlDocumentation {
			get;
			set;
		}

		public FindMemberAstVisitor (Mono.TextEditor.Document document, MonoDevelop.Projects.Dom.INode searchedMember = null)
		{
			fileName = document.FileName;
			this.text = document;
			if (searchedMember != null)
				Init (searchedMember);
		}
		
		public void Init (params MonoDevelop.Projects.Dom.INode[] searchedMembers)
		{
			Init ((IEnumerable<MonoDevelop.Projects.Dom.INode>)searchedMembers);
		}
		
		public void Init (IEnumerable<MonoDevelop.Projects.Dom.INode> members)
		{
			if (members == null)
				return;
			
			foreach (var member in members) {
				MonoDevelop.Projects.Dom.INode searchedMember = null;
				string SearchedMemberName = null;
				string searchedMemberFile = null;
				DomLocation searchedMemberLocation = DomLocation.Empty;
				
				if (member is IMember) {
					searchedMember = GetUnderlyingMember ((IMember)member);
				} else {
					searchedMember = member;
				}
				
				if (searchedMember is IMethod) {
					IMethod method = (IMethod)searchedMember;
					SearchedMemberName = method.IsConstructor ? method.DeclaringType.Name : method.Name;
					searchedMemberLocation = method.Location;
					if (method.DeclaringType != null && method.DeclaringType.CompilationUnit != null)
						searchedMemberFile = method.DeclaringType.CompilationUnit.FileName;
				} else if (searchedMember is IMember) {
					SearchedMemberName = ((IMember)searchedMember).Name;
					searchedMemberLocation = ((IMember)searchedMember).Location;
					
					if (searchedMember is IType) {
						var unit = ((IType)searchedMember).CompilationUnit;
						if (unit != null) {
							searchedMemberFile = unit.FileName;
						} else {
							LoggingService.LogWarning (members + " has no compilation unit.");
						}
					} else {
						if (((IMember)searchedMember).DeclaringType != null && ((IMember)searchedMember).DeclaringType.CompilationUnit != null)
							searchedMemberFile = ((IMember)searchedMember).DeclaringType.CompilationUnit.FileName;
					}
				} else if (searchedMember is IParameter) {
					SearchedMemberName = ((IParameter)searchedMember).Name;
					searchedMemberLocation = ((IParameter)searchedMember).Location;
					if (((IParameter)searchedMember).DeclaringMember.DeclaringType.CompilationUnit != null)
						searchedMemberFile = ((IParameter)searchedMember).DeclaringMember.DeclaringType.CompilationUnit.FileName;
				} else if (members != null) {
					SearchedMemberName = ((LocalVariable)searchedMember).Name;
					searchedMemberLocation = ((LocalVariable)searchedMember).Region.Start;
					if (((LocalVariable)searchedMember).CompilationUnit != null)
						searchedMemberFile = ((LocalVariable)searchedMember).CompilationUnit.FileName;
				}
				searchedMembers.Add (Tuple.Create (searchedMember, SearchedMemberName, searchedMemberFile, searchedMemberLocation));
			}
		}
		
		// if a member is a member of an instantiated class search for the 'real' uninstantiated member
		// ex. List<string> a; a.Count<-;  should search for List<T>.Count instead of List<string>.Count
		static IMember GetUnderlyingMember (IMember member)
		{
			if (member == null)
				return null;
			
			if (member.DeclaringType is InstantiatedType && member.ReturnType != null) {
				IType uninstantiatedType = ((InstantiatedType)member.DeclaringType).UninstantiatedType;
				foreach (IMember realMember in uninstantiatedType.SearchMember (member.Name, true)) {
					if (realMember.ReturnType == null)
						continue;
					if (realMember.MemberType == member.MemberType) {
						switch (member.MemberType) {
						case MemberType.Method:
							if (((IMethod)member).TypeParameters.Count != ((IMethod)realMember).TypeParameters.Count)
								continue;
							if (!DomMethod.ParameterListEquals (((IMethod)member).Parameters, ((IMethod)realMember).Parameters))
								continue;
							break;
						case MemberType.Property:
							if (!DomMethod.ParameterListEquals (((IProperty)member).Parameters, ((IProperty)realMember).Parameters))
								continue;
							break;
						}
						return realMember;
					}
				}
			}
			return member;
		}

		static readonly Regex paramRegex = new Regex ("\\<param\\s+name\\s*=\\s*[\"'](.*)[\"']", RegexOptions.Compiled);
		static readonly Regex paramRefRegex = new Regex ("\\<paramref\\s+name\\s*=\\s*[\"'](.*)[\"']", RegexOptions.Compiled);
		static readonly Regex seeRegex = new Regex ("\\<see\\s+cref\\s*=\\s*[\"'](.*)[\"']", RegexOptions.Compiled);
		static readonly Regex seeAlsoRegRegex = new Regex ("\\<seealso\\s+cref\\s*=\\s*[\"'](.*)[\"']", RegexOptions.Compiled);
		
		public bool FileContainsMemberName ()
		{
			return text.SearchForward (SearchedMemberName, 0).Any ();
		}
		
		List<ICSharpCode.OldNRefactory.IParser> parsers = new List<ICSharpCode.OldNRefactory.IParser> ();
		
		public void ClearParsers ()
		{
			parsers.ForEach (p => p.Dispose ());
			parsers.Clear ();
		}
		
		public void ParseFile (NRefactoryResolver resolver)
		{
			string parseText = text.Text;
			ClearParsers ();
			
			var parser = ICSharpCode.OldNRefactory.ParserFactory.CreateParser (ICSharpCode.OldNRefactory.SupportedLanguage.CSharp, new StringReader (parseText));
			parser.Lexer.EvaluateConditionalCompilation = true;
//			Console.WriteLine ("parse :" + text.FileName);
			parser.Parse ();
			resolver.SetupParsedCompilationUnit (parser.CompilationUnit);
			this.parsers.Add (parser);
			
			List<HashSet<string >> usedIdentifiers = GetUsedDefineCombinations (parser);
			for (int i = 0; i < usedIdentifiers.Count; i++) {
				parser.Lexer.ConditionalCompilationSymbols.Clear ();
				foreach (string define in usedIdentifiers[i])
					parser.Lexer.ConditionalCompilationSymbols.Add (define, true);
				parser = ICSharpCode.OldNRefactory.ParserFactory.CreateParser (ICSharpCode.OldNRefactory.SupportedLanguage.CSharp, new StringReader (parseText));
				parser.Parse ();
				this.parsers.Add (parser);
			}
		}
		
		public void RunVisitor (NRefactoryResolver resolver)
		{
			this.resolver = resolver;
			if (searchedMembers.Count == 0)
				return;
			if (parsers.Count == 0)
				ParseFile (resolver);
			
			foreach (var p in parsers)
				VisitCompilationUnit (p.CompilationUnit, null);
			var parser = parsers.First ();
			if (IncludeXmlDocumentation && searchedMembers.Count > 0) {
				var searchedMember = searchedMembers.First ().Item1;
				if (searchedMember is IParameter) {
					IParameter parameter = (IParameter)searchedMember;
					var docComments = from ICSharpCode.OldNRefactory.Comment cmt in 
						(from ISpecial s in parser.Lexer.SpecialTracker.CurrentSpecials 
						where s is ICSharpCode.OldNRefactory.Comment && s.StartPosition.Line <= parameter.DeclaringMember.Location.Line
						select s) 
						select cmt;
					
					ICSharpCode.OldNRefactory.Comment lastComment = null;
					foreach (ICSharpCode.OldNRefactory.Comment curComment in docComments.Reverse ()) {
						if (lastComment != null && Math.Abs (lastComment.StartPosition.Line - curComment.StartPosition.Line) > 1)
							break;
						// Concat doesn't work on MatchCollections
						foreach (var matchCol in new [] { paramRegex.Matches (curComment.CommentText), paramRefRegex.Matches (curComment.CommentText) }) {
							foreach (Match match in matchCol) {
								if (match.Groups [1].Value == SearchedMemberName) 
									AddUniqueReference (curComment.StartPosition.Line, curComment.StartPosition.Column + match.Groups [1].Index, SearchedMemberName);
							}
						}
						lastComment = curComment;
					}
				} else if (searchedMember is IMember) {
					IMember member = (IMember)searchedMember;
					var docComments = from ICSharpCode.OldNRefactory.Comment cmt in 
						(from ISpecial s in parser.Lexer.SpecialTracker.CurrentSpecials 
						where s is ICSharpCode.OldNRefactory.Comment
						select s) 
						select cmt;
					
					string fullName = member.FullName;
					
					foreach (ICSharpCode.OldNRefactory.Comment curComment in docComments) {
						// Concat doesn't work on MatchCollections
						foreach (var matchCol in new [] { seeRegex.Matches (curComment.CommentText), seeAlsoRegRegex.Matches (curComment.CommentText) }) {
							foreach (Match match in matchCol) {
								if (match.Groups [1].Value.StartsWith (fullName)) 
									AddUniqueReference (curComment.StartPosition.Line, curComment.StartPosition.Column + match.Groups [1].Index + fullName.Length - SearchedMemberName.Length, SearchedMemberName);
							}
						}
						
					}
				}
			}
		}

		public void RunVisitor (ICSharpCode.OldNRefactory.Ast.CompilationUnit compilationUnit)
		{
			if (searchedMembers.Count == 0)
				return;
			// search if the member name exists in the file (otherwise it doesn't make sense to search it)
			FindReplace findReplace = new FindReplace ();
			FilterOptions filterOptions = new FilterOptions {
				CaseSensitive = true,
				WholeWordsOnly = true
			};
			IEnumerable<SearchResult > result = findReplace.Search (new FileProvider (null), text.Text, SearchedMemberName, null, filterOptions);
			if (result == null || !result.Any ()) {
				return;
			}
			resolver.SetupParsedCompilationUnit (compilationUnit);
			VisitCompilationUnit (compilationUnit, null);
		}
		
		class ExpressionVisitor : ICSharpCode.OldNRefactory.Visitors.AbstractAstVisitor
		{
			HashSet<string> identifiers = new HashSet<string> ();

			public HashSet<string> Identifiers {
				get {
					return identifiers;
				}
			}
			
			public override object VisitIdentifierExpression(ICSharpCode.OldNRefactory.Ast.IdentifierExpression identifierExpression, object data)
			{
				identifiers.Add (identifierExpression.Identifier);
				return null;
			}
		}
		
		static IEnumerable<HashSet<T>> GetAllCombinations<T> (IEnumerable<T> input)
		{
			List<T > strings = new List<T> (input);
			List<HashSet<T >> result = new List<HashSet<T>> ();
			result.Add (new HashSet<T> ());
			for (int i = 0; i < strings.Count; i++) {
				int curCount = result.Count;
				for (int j = 0; j < curCount; j++) {
					HashSet<T > newSet = new HashSet<T> (result [j]);
					newSet.Add (strings [i]);
					result.Add (newSet);
				}
			}
			return result;
		}
		
		static List<HashSet<string>> GetUsedDefineCombinations (ICSharpCode.OldNRefactory.IParser parser)
		{
			List<HashSet<string >> result = new List<HashSet<string>> ();
			foreach (ISpecial special in parser.Lexer.SpecialTracker.CurrentSpecials) {
				PreprocessingDirective directive = special as PreprocessingDirective;
				if (directive == null || (directive.Cmd != "#if" && directive.Cmd != "#elif"))
					continue;
				
				ExpressionVisitor visitor = new ExpressionVisitor ();
				directive.Expression.AcceptVisitor (visitor, null);
				ICSharpCode.OldNRefactory.Parser.CSharp.ConditionalCompilation cond = new ICSharpCode.OldNRefactory.Parser.CSharp.ConditionalCompilation ();
				bool nothingDefined = cond.Evaluate (directive.Expression);
				foreach (var combination in GetAllCombinations (visitor.Identifiers)) {
					cond = new ICSharpCode.OldNRefactory.Parser.CSharp.ConditionalCompilation ();
					HashSet<string > defines = new HashSet<string> ();
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
			 next:
				;
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
			int pos = text.LocationToOffset (line, col);
			int spos = text.LocationToOffset (line, 1);
			int epos = text.LocationToOffset (line + 1, 1);
			if (epos == -1)
				epos = text.Length - 1;
			
			string txt;
			
			// FIXME: do we always need to do this? or just in my test cases so far? :)
			// use the base name and not the FullName
			name = GetNameWithoutPrefix (name);
			
			// FIXME: is there a better way to do this?
			// update @pos to point to the actual identifier and not the 
			// public/private/whatever modifier.
			int i;
			txt = text.GetTextBetween (pos, text.Length - 1);
			if (txt != null && (i = txt.IndexOf (name)) > 0)
				pos += i;
			
			if (spos != -1)
				txt = text.GetTextBetween (spos, epos - 1);
			else
				txt = null;
			
			return new MemberReference (null, fileName, pos, line, col, name, txt);
		}

		HashSet<MemberReference> unique = new HashSet<MemberReference> ();

		void AddUniqueReference (int line, int col, string name)
		{
			if (line < 1 || col < 1) {
				MonoDevelop.Core.LoggingService.LogWarning ("AddUniqueReference called with invalid position line: {0} col: {1} name: {2}.", line, col, name);
				System.Console.WriteLine ("Invalid uniqe reference stack trace:");
				System.Console.WriteLine ("-----");
				System.Console.WriteLine (Environment.StackTrace);
				System.Console.WriteLine ("-----");
				return;
			}
			
			MemberReference mref = CreateReference (line, col, name);
			
			if (unique.Add (mref)) 
				foundReferences.Add (mref);
		}

		bool IsSearchedNode (ICSharpCode.OldNRefactory.Ast.INode node)
		{
			if (node == null || node.StartLocation.IsEmpty)
				return false;
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				var searchedMemberFile = tuple.Item3;
				var searchedMemberLocation = tuple.Item4;
				
				if (searchedMember is CompoundType) {
					foreach (IType part in ((CompoundType)searchedMember).Parts) {
						if (fileName == part.CompilationUnit.FileName &&
						    node.StartLocation.Line == part.Location.Line 
//						&& node.StartLocation.Column == part.Location.Column
							)
							return true;
					}
				}
				
				return (string.IsNullOrEmpty (searchedMemberFile) || fileName == searchedMemberFile) && 
				       node.StartLocation.Line == searchedMemberLocation.Line 
//					&& node.StartLocation.Column == this.searchedMemberLocation.Column
					;
			}
			return false;
		}

		static bool IsIdentifierPart (char ch)
		{
			return Char.IsLetterOrDigit (ch) || ch == '_';
		}

		bool SearchText (string text, int startLine, int startColumn, out int line, out int column)
		{
			int position = this.text.LocationToOffset (startLine, startColumn);
			line = column = -1;
			if (position < 0)
				return false;
			
			while (position + SearchedMemberName.Length < this.text.Length) {
				bool isIdentifierStart = position <= 0 || !IsIdentifierPart (this.text.GetCharAt (position - 1));
				
				if (isIdentifierStart &&
				    (position + SearchedMemberName.Length >= this.text.Length || !IsIdentifierPart (this.text.GetCharAt (position + SearchedMemberName.Length))) &&
				    (this.text.GetTextAt (position, SearchedMemberName.Length) == SearchedMemberName)) { 
					var location = this.text.OffsetToLocation (position);
					line = location.Line;
					column = location.Column;
					return true;
				}
				position ++;
			}
			return false;
		}

		bool IsSearchTextAt (int startLine, int startColumn)
		{
			int position = this.text.LocationToOffset (startLine, startColumn);
			
			if ((position == 0 || !IsIdentifierPart (this.text.GetCharAt (position - 1))) && 
			    (position + SearchedMemberName.Length >= this.text.Length || !IsIdentifierPart (this.text.GetCharAt (position + SearchedMemberName.Length))) &&
			    this.text.GetTextAt (position, SearchedMemberName.Length) == SearchedMemberName) {
				return true;
			}
			return false;
		}

		void CheckNode (ICSharpCode.OldNRefactory.Ast.INode node)
		{
			if (IsSearchedNode (node)) {
				int line, column;
				if (SearchText (SearchedMemberName, node.StartLocation.Line, node.StartLocation.Column, out line, out column)) {
					AddUniqueReference (line, column, SearchedMemberName);
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
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				if (searchedMember is IType) {
					if (((IType)searchedMember).Parts.Any (t => t.CompilationUnit.FileName == fileName) &&
					    ((IType)searchedMember).FullName == CurrentTypeFullName &&
					    ((IType)searchedMember).TypeParameters.Count == typeStack.Peek ().Templates.Count &&
					    IsSearchTextAt (constructorDeclaration.StartLocation.Line, constructorDeclaration.StartLocation.Column))
						AddUniqueReference (constructorDeclaration.StartLocation.Line, constructorDeclaration.StartLocation.Column, this.SearchedMemberName);
				}
				if (searchedMember is IMethod) {
					IMethod method = (IMethod)searchedMember; 
					if (method.IsConstructor &&
						IsSearchTextAt (constructorDeclaration.StartLocation.Line, constructorDeclaration.StartLocation.Column))
						CheckNode (constructorDeclaration);
				}
			}
			
			return base.VisitConstructorDeclaration (constructorDeclaration, data);
		}

		public override object VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration, object data)
		{
			CheckNode (destructorDeclaration);
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				if (searchedMember is IType) {
					if (((IType)searchedMember).Parts.Any (t => t.CompilationUnit.FileName == fileName) &&
					    ((IType)searchedMember).FullName == CurrentTypeFullName && 
					    ((IType)searchedMember).TypeParameters.Count == typeStack.Peek ().Templates.Count && 
					    IsSearchTextAt (destructorDeclaration.StartLocation.Line, destructorDeclaration.StartLocation.Column + 1)) // need to skip the '~'
						AddUniqueReference (destructorDeclaration.StartLocation.Line, destructorDeclaration.StartLocation.Column + 1, this.SearchedMemberName);
				}
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
			return base.VisitIndexerDeclaration (indexerDeclaration, data);
		}

		public override object VisitLocalVariableDeclaration (LocalVariableDeclaration localVariableDeclaration, object data)
		{
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				if (searchedMember is LocalVariable) {
					LocalVariable searchedVariable = (LocalVariable)searchedMember;
					ICSharpCode.OldNRefactory.Ast.INode parent = localVariableDeclaration.Parent;
					while (parent != null && !(parent is MemberNode) && !(parent is ParametrizedNode)) {
						parent = parent.Parent;
					}
					
					if (parent != null &&
						localVariableDeclaration.StartLocation.Line == searchedVariable.Location.Line && 
						localVariableDeclaration.StartLocation.Column == searchedVariable.Location.Column && 
					    parent.StartLocation.Line == searchedVariable.DeclaringMember.Location.Line 
//					&& parent.StartLocation.Column == searchedVariable.DeclaringMember.Location.Column
						) {
						foreach (VariableDeclaration decl in localVariableDeclaration.Variables) {
							if (decl.Name == SearchedMemberName) 
								AddUniqueReference (decl.StartLocation.Y, decl.StartLocation.X, SearchedMemberName);
						}
					}
				}
			}
			return base.VisitLocalVariableDeclaration (localVariableDeclaration, data);
		}

		public override object VisitMethodDeclaration (MethodDeclaration methodDeclaration, object data)
		{
			CheckNode (methodDeclaration);
			return base.VisitMethodDeclaration (methodDeclaration, data);
		}

		public override object VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration, object data)
		{
			CheckNode (operatorDeclaration);
			return base.VisitOperatorDeclaration (operatorDeclaration, data);
		}

		public override object VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration, object data)
		{
			CheckNode (propertyDeclaration);
			return base.VisitPropertyDeclaration (propertyDeclaration, data);
		}

		public override object VisitTypeDeclaration (TypeDeclaration typeDeclaration, object data)
		{
			CheckNode (typeDeclaration);
			typeStack.Push (typeDeclaration);
			object result = base.VisitTypeDeclaration (typeDeclaration, data);
			typeStack.Pop ();
			return result; 
		}

		public override object VisitParameterDeclarationExpression (ParameterDeclarationExpression parameterDeclarationExpression, object data)
		{
			CheckNode (parameterDeclarationExpression);
			return base.VisitParameterDeclarationExpression (parameterDeclarationExpression, data);
		}

		public override object VisitVariableDeclaration (VariableDeclaration variableDeclaration, object data)
		{
			return base.VisitVariableDeclaration (variableDeclaration, data);
		}

		DomLocation ConvertLocation (Location loc)
		{
			return new DomLocation (loc.Line, loc.Column);
		}

		public override object VisitObjectCreateExpression (ICSharpCode.OldNRefactory.Ast.ObjectCreateExpression objectCreateExpression, object data)
		{
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;

				IMethod method = searchedMember as IMethod;
				if (method != null && method.IsConstructor) {
					ResolveResult resolveResult = resolver.ResolveExpression (objectCreateExpression, ConvertLocation (objectCreateExpression.StartLocation));
					if (resolveResult != null && resolveResult.ResolvedType != null) {
						IType resolvedType = resolver.Dom.GetType (resolveResult.ResolvedType);
						int pCount = objectCreateExpression.Parameters != null ? objectCreateExpression.Parameters.Count : 0;
						if (resolvedType != null && resolvedType.FullName == method.DeclaringType.FullName && pCount == method.Parameters.Count) {
							int line, column;
							if (SearchText (SearchedMemberName, objectCreateExpression.StartLocation.Line, objectCreateExpression.StartLocation.Column, out line, out column))
								AddUniqueReference (line, column, SearchedMemberName);
						}
					}
				}
				
				IProperty property = searchedMember as IProperty;
				if (property != null && objectCreateExpression.ObjectInitializer != null) {
					ResolveResult resolveResult = resolver.ResolveExpression (objectCreateExpression, ConvertLocation (objectCreateExpression.StartLocation));
					if (resolveResult != null && resolveResult.ResolvedType != null) {
						IType resolvedType = resolver.Dom.GetType (resolveResult.ResolvedType);
						if (resolvedType != null && resolvedType.FullName == property.DeclaringType.FullName) {
							foreach (Expression expr in objectCreateExpression.ObjectInitializer.CreateExpressions) {
								NamedArgumentExpression namedArgumentExpression = expr as NamedArgumentExpression;
								if (namedArgumentExpression == null)
									continue;
								if (namedArgumentExpression.Name == property.Name)
									AddUniqueReference (namedArgumentExpression.StartLocation.Line, namedArgumentExpression.StartLocation.Column, SearchedMemberName);
							}
						}
					}
				}
			}
			
			return base.VisitObjectCreateExpression (objectCreateExpression, data);
		}

		public override object VisitTryCatchStatement (TryCatchStatement tryCatchStatement, object data)
		{
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				if (searchedMember is LocalVariable) {
					foreach (CatchClause catchClause in tryCatchStatement.CatchClauses) {
						if (catchClause == null)
							continue;
						LocalVariable searchedVariable = (LocalVariable)searchedMember;
						
						if (catchClause.TypeReference != null && catchClause.VariableName == SearchedMemberName && searchedVariable.Region.Start == ConvertLocation (catchClause.StartLocation)) {
							int line, col;
							SearchText (SearchedMemberName, catchClause.StartLocation.Line, catchClause.StartLocation.Column, out line, out col);
							AddUniqueReference (line, col, SearchedMemberName);
						}
					}
				}
			}
			return base.VisitTryCatchStatement (tryCatchStatement, data);
		}

		public override object VisitForeachStatement (ForeachStatement foreachStatement, object data)
		{
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				var searchedMemberLocation = tuple.Item4;
				if (searchedMember is LocalVariable &&
					foreachStatement.VariableName == SearchedMemberName && 
					searchedMemberLocation.Line == foreachStatement.EmbeddedStatement.StartLocation.Line && 
					searchedMemberLocation.Column == foreachStatement.EmbeddedStatement.StartLocation.Column) {
					int line, col;
					SearchText (SearchedMemberName, foreachStatement.StartLocation.Line, foreachStatement.StartLocation.Column, out line, out col);
					AddUniqueReference (line, col, SearchedMemberName);
				}
			}
			
			return base.VisitForeachStatement (foreachStatement, data);
		}

		public override object VisitIdentifierExpression (IdentifierExpression idExp, object data)
		{
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				if (idExp.Identifier == SearchedMemberName) {
					int line = idExp.StartLocation.Y;
					int col = idExp.StartLocation.X;
					ResolveResult result = resolver.ResolveIdentifier (idExp.Identifier, ConvertLocation (idExp.StartLocation));
					
					if (searchedMember is IType) {
						IMember item = result != null && result is MemberResolveResult ? ((MemberResolveResult)result).ResolvedMember : null;
						if (item == null && result != null)
							item = resolver.Dom.GetType (result.ResolvedType);
						
						if (item != null && item is IType && ((IType)item).FullName == ((IType)searchedMember).FullName) {
							//	Debug ("adding IdentifierExpression class", idExp.Identifier, idExp);
							AddUniqueReference (line, col, idExp.Identifier);
						}
					} else if (searchedMember is LocalVariable && result is LocalVariableResolveResult) {
						LocalVariable avar = searchedMember as LocalVariable;
						LocalVariable var = ((LocalVariableResolveResult)result).LocalVariable;
	
						if (var != null && avar.DeclaringMember != null && var.DeclaringMember != null && avar.DeclaringMember.FullName == var.DeclaringMember.FullName) {
							//						Console.WriteLine (avar.Region.Start.Line + "---" +  var.Region.Start.Line);
							if (Math.Abs (avar.Region.Start.Line - var.Region.Start.Line) <= 1)
								AddUniqueReference (line, col, idExp.Identifier);
						}
					} else if (searchedMember is IParameter && result is ParameterResolveResult) {
						IParameter param = ((ParameterResolveResult)result).Parameter;
						if (param != null && ((IParameter)searchedMember).DeclaringMember.Location == param.DeclaringMember.Location)
							AddUniqueReference (line, col, idExp.Identifier);
					} else if (searchedMember is IMember && result is MemberResolveResult) {
						IMember m = GetUnderlyingMember (((MemberResolveResult)result).ResolvedMember);
						
						//Console.WriteLine (searchedMember +  "/" + item);
						if (m != null /*&& IsExpectedClass (m.DeclaringType)*/ && ((IMember)searchedMember).DeclaringType.FullName == m.DeclaringType.FullName &&
							((searchedMember is IField && m is IField) || (searchedMember is IMethod && m is IMethod) ||
							 (searchedMember is IProperty && m is IProperty) || (searchedMember is IEvent && m is IEvent))) {
							//Debug ("adding IdentifierExpression searchedMember", searchedMember.Name, idExp);
							AddUniqueReference (line, col, SearchedMemberName);
						}
					} 
				}
			}
			return base.VisitIdentifierExpression (idExp, data);
		}

		public override object VisitTypeReference (TypeReference typeReference, object data)
		{
			string type = typeReference.Type;
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				if (searchedMember is IType && this.SearchedMemberName == GetNameWithoutPrefix (type)) {
					ExpressionResult res = new ExpressionResult ("new " + typeReference.ToString () + "()");
					ResolveResult resolveResult = resolver.Resolve (res, ConvertLocation (typeReference.StartLocation));
					
					IReturnType cls = resolveResult != null ? resolveResult.ResolvedType : null;
					IType resolvedType = cls != null ? resolver.SearchType (cls) : null;
					if (resolvedType != null && resolvedType.FullName == ((IType)searchedMember).FullName) {
						int line, column;
						if (!SearchText (SearchedMemberName, typeReference.StartLocation.Line, typeReference.StartLocation.Column, out line, out column)) {
							line = typeReference.StartLocation.Line;
							column = typeReference.StartLocation.Column;
						}
						AddUniqueReference (line, column, typeReference.Type);
					}
				}
			}
			return base.VisitTypeReference (typeReference, data);
		}

		public override object VisitMemberReferenceExpression (MemberReferenceExpression fieldExp, object data)
		{
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
				var searchedMemberLocation = tuple.Item4;
				
				if (!(searchedMember is IParameter) && fieldExp.MemberName == SearchedMemberName) {
					ResolveResult resolveResult = resolver.ResolveExpression (fieldExp, ConvertLocation (fieldExp.StartLocation));
					MemberResolveResult mrr = resolveResult as MemberResolveResult;
					if (mrr != null) {
						IMember resolvedMember = GetUnderlyingMember (mrr.ResolvedMember);
						
						if (resolvedMember != null && resolvedMember.DeclaringType != null && searchedMember is IMember && ((IMember)searchedMember).DeclaringType != null && resolvedMember.Location == searchedMemberLocation && resolvedMember.DeclaringType.FullName == ((IMember)searchedMember).DeclaringType.FullName) {
							int line, column;
							if (SearchText (SearchedMemberName, fieldExp.StartLocation.Line, fieldExp.StartLocation.Column, out line, out column))
								AddUniqueReference (line, column, SearchedMemberName);
							return base.VisitMemberReferenceExpression (fieldExp, data);
						}
						return null;
					}
					IType cls = resolveResult != null ? resolver.Dom.GetType (resolveResult.ResolvedType) : null;
					if (cls != null) {
						int pos = text.LocationToOffset (fieldExp.StartLocation.Y, fieldExp.StartLocation.X);
						int endpos = text.LocationToOffset (fieldExp.EndLocation.Y, fieldExp.EndLocation.X);
						string txt = text.GetTextBetween (pos, endpos);
						if (txt == SearchedMemberName) {
							int line, column;
							if (!SearchText (SearchedMemberName, fieldExp.StartLocation.Line, fieldExp.StartLocation.Column, out line, out column)) {
								line = fieldExp.StartLocation.Line;
								column = fieldExp.StartLocation.Column;
							}
							AddUniqueReference (line, column, SearchedMemberName);
						}
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
			foreach (var tuple in searchedMembers) {
				var searchedMember = tuple.Item1;
			
				if (searchedMember is IMethod) {
					IMethod method = (IMethod)searchedMember;
					if (MightBeInvocation (invokeExp.TargetObject, method) && invokeExp.Arguments.Count == method.Parameters.Count) {
						ResolveResult resolveResult = resolver.ResolveExpression (invokeExp, ConvertLocation (invokeExp.StartLocation));
						IMethod resolvedMethod = null;
						if (resolveResult is MethodResolveResult) {
							MethodResolveResult mrr = (MethodResolveResult)resolveResult;
							resolvedMethod = mrr.MostLikelyMethod;
						} else if (resolveResult is MemberResolveResult) {
							resolvedMethod = GetUnderlyingMember (((MemberResolveResult)resolveResult).ResolvedMember) as IMethod;
						}
						if (resolvedMethod != null) {
							if (resolvedMethod.FullName == method.FullName && resolvedMethod.TypeParameters.Count == method.TypeParameters.Count) {
								int line, column;
								if (SearchText (SearchedMemberName, invokeExp.StartLocation.Line, invokeExp.StartLocation.Column, out line, out column))
									AddUniqueReference (line, column, SearchedMemberName);
							}
						}
					}
					invokeExp.Arguments.ForEach (o => o.AcceptVisitor (this, data));
					// for method searches the identifier expression of the invocation is NOT visited
					// we've already checked the method.
					return true;
				}
			}
			
			
			invokeExp.Arguments.ForEach (o => o.AcceptVisitor (this, data));
			return base.VisitInvocationExpression (invokeExp, data);
		}

		Stack<string> namespaceStack = new Stack<string> ();
		public override object VisitNamespaceDeclaration (ICSharpCode.OldNRefactory.Ast.NamespaceDeclaration namespaceDeclaration, object data)
		{
			namespaceStack.Push (namespaceDeclaration.Name);
			object result = base.VisitNamespaceDeclaration (namespaceDeclaration, data);
			namespaceStack.Pop ();
			return result;
		}
	}
}
