// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Andrea Paatz" email="andrea@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Drawing;

using MonoDevelop.Services;
using MonoDevelop.Internal.Parser;
using CSharpBinding.Parser.SharpDevelopTree;
using ICSharpCode.SharpRefactory.Parser.AST;
using ICSharpCode.SharpRefactory.Parser;

namespace CSharpBinding.Parser
{
	class Resolver
	{
		IParserContext parserContext;
		ICompilationUnit cu;
		IClass callingClass;
		LookupTableVisitor lookupTableVisitor;
		
		public Resolver (IParserContext parserContext)
		{
			this.parserContext = parserContext;
		}
		
		public IParserContext ParserContext {
			get {
				return parserContext;
			}
		}
		
		public ICompilationUnit CompilationUnit {
			get {
				return cu;
			}
		}
		
		public IClass CallingClass {
			get {
				return callingClass;
			}
		}
		
		bool showStatic = false;
		
		bool inNew = false;
		
		public bool ShowStatic {
			get {
				return showStatic;
			}
			
			set {
				showStatic = value;
			}
		}
		
		int caretLine;
		int caretColumn;
		
		public IReturnType internalResolve (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent)
		{
			//Console.WriteLine("Start Resolving");
			if (expression == null) {
				return null;
			}
			expression = expression.TrimStart(null);
			if (expression == "") {
				return null;
			}
			this.caretLine     = caretLineNumber;
			this.caretColumn   = caretColumn;
			
			IParseInformation parseInfo = parserContext.GetParseInformation(fileName);
			ICSharpCode.SharpRefactory.Parser.AST.CompilationUnit fileCompilationUnit = parseInfo.MostRecentCompilationUnit.Tag as ICSharpCode.SharpRefactory.Parser.AST.CompilationUnit;
			if (fileCompilationUnit == null) {
//				ICSharpCode.SharpRefactory.Parser.Parser fileParser = new ICSharpCode.SharpRefactory.Parser.Parser();
//				fileParser.Parse(new Lexer(new StringReader(fileContent)));
				Console.WriteLine("Warning: no parse information!");
				return null;
			}
			/*
			//// try to find last expression in original string, it could be like " if (act!=null) act"
			//// in this case only "act" should be parsed as expression  
			!!is so!! don't change things that work
			Expression expr=null;	// tentative expression
			Lexer l=null;
			ICSharpCode.SharpRefactory.Parser.Parser p = new ICSharpCode.SharpRefactory.Parser.Parser();
			while (expression.Length > 0) {
				l = new Lexer(new StringReader(expression));
				expr = p.ParseExpression(l);
				if (l.LookAhead.val != "" && expression.LastIndexOf(l.LookAhead.val) >= 0) {
					if (expression.Substring(expression.LastIndexOf(l.LookAhead.val) + l.LookAhead.val.Length).Length > 0) 
						expression=expression.Substring(expression.LastIndexOf(l.LookAhead.val) + l.LookAhead.val.Length).Trim();
					else {
						expression=l.LookAhead.val.Trim();
						l=new Lexer(new StringReader(expression));
						expr=p.ParseExpression(l);
						break;
					}
				} else {
					if (l.Token.val!="" || expr!=null) break;
				}
			}
			//// here last subexpression should be fixed in expr
			if it should be changed in expressionfinder don't fix it here
			*/
			ICSharpCode.SharpRefactory.Parser.Parser p = new ICSharpCode.SharpRefactory.Parser.Parser();
			Lexer l = new Lexer(new StringReader(expression));
			Expression expr = p.ParseExpression(l);
			if (expr == null) {
				return null;
			}
			lookupTableVisitor = new LookupTableVisitor();
			lookupTableVisitor.Visit(fileCompilationUnit, null);
			
			TypeVisitor typeVisitor = new TypeVisitor(this);
			
			CSharpVisitor cSharpVisitor = new CSharpVisitor();
			cu = (ICompilationUnit)cSharpVisitor.Visit(fileCompilationUnit, null);
			if (cu != null) {
				callingClass = GetInnermostClass();
//				Console.WriteLine("CallingClass is " + callingClass == null ? "null" : callingClass.Name);
			}
			//Console.WriteLine("expression = " + expr.ToString());
			IReturnType type = expr.AcceptVisitor(typeVisitor, null) as IReturnType;
			//Console.WriteLine("type visited");
			if (type == null || type.PointerNestingLevel != 0) {
//				Console.WriteLine("Type == null || type.PointerNestingLevel != 0");
				if (type != null) {
					//Console.WriteLine("PointerNestingLevel is " + type.PointerNestingLevel);
				} else {
					//Console.WriteLine("Type == null");
				}
				//// when type is null might be file needs to be reparsed - some vars were lost
				fileCompilationUnit = parserContext.ParseFile (fileName, fileContent).MostRecentCompilationUnit.Tag 
					as ICSharpCode.SharpRefactory.Parser.AST.CompilationUnit;
				lookupTableVisitor.Visit(fileCompilationUnit,null);
				cu = (ICompilationUnit)cSharpVisitor.Visit(fileCompilationUnit, null);
				if (cu != null) {
					callingClass = GetInnermostClass();
				}
				type=expr.AcceptVisitor(typeVisitor,null) as IReturnType;
				if (type==null)	return null;
			}
			if (type.ArrayDimensions != null && type.ArrayDimensions.Length > 0) {
				type = new ReturnType("System.Array");
			}
			Console.WriteLine("Here: Type is " + type.FullyQualifiedName);
			return type;
		}

		public string MonodocResolver (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent) 
		{
			if (expression == null) {
				return null;
			}
			expression = expression.TrimStart (null);
			if (expression == "") {
				return null;
			}
			IReturnType retType = internalResolve (expression, caretLineNumber, caretColumn, fileName, fileContent);
			IClass retClass = parserContext.SearchType (retType.FullyQualifiedName, null, cu);
			if (retClass == null) {
				Console.WriteLine ("Retclass was null");
				return null;
			}
			
			Console.WriteLine (retClass.FullyQualifiedName);
			return "T:" + retClass.FullyQualifiedName;
		}
		
		public ResolveResult Resolve (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent) 
		{
			if (expression == null) {
				return null;
			}
			expression = expression.TrimStart(null);
			if (expression == "") {
				return null;
			}
			// disable the code completion for numbers like 3.47
			try {
				int.Parse(expression);
//				Console.WriteLine(expression);
				return null;
			} catch (Exception) {
			}
			if (expression.StartsWith("new ")) {
				inNew = true;
				expression = expression.Substring(4);
			} else {
				inNew = false;
			}
			if (expression.StartsWith("using ")) {
				// expression[expression.Length - 1] != '.'
				// the period that causes this Resove() is not part of the expression
				if (expression[expression.Length - 1] == '.') {
					return null;
				}
				int i;
				for (i = expression.Length - 1; i >= 0; --i) {
					if (!(Char.IsLetterOrDigit(expression[i]) || expression[i] == '_' || expression[i] == '.')) {
						break;
					}
				}
				// no Identifier before the period
				if (i == expression.Length - 1) {
					return null;
				}
				string t = expression.Substring(i + 1);
//				Console.WriteLine("in Using Statement");
				string[] namespaces = parserContext.GetNamespaceList (t);
				if (namespaces == null || namespaces.Length <= 0) {
					return null;
				}
				return new ResolveResult(namespaces);
			}
			
			//Console.WriteLine("Not in Using");
			IReturnType type = internalResolve (expression, caretLineNumber, caretColumn, fileName, fileContent);
			IClass returnClass = SearchType (type.FullyQualifiedName, cu);
			if (returnClass == null) {
				// Try if type is Namespace:
				string n = SearchNamespace(type.FullyQualifiedName, cu);
				if (n == null) {
					return null;
				}
				ArrayList content = parserContext.GetNamespaceContents (n,true);
				ArrayList classes = new ArrayList();
				for (int i = 0; i < content.Count; ++i) {
					if (content[i] is IClass) {
						classes.Add((IClass)content[i]);
					}
				}
				string[] namespaces = parserContext.GetNamespaceList (n, true, true);
				return new ResolveResult(namespaces, classes);
			}
			//Console.WriteLine("Returning Result!");
			if (returnClass.FullyQualifiedName == "System.Void")
				return null;
			return new ResolveResult(returnClass, ListMembers(new ArrayList(), returnClass));
		}
		
		ArrayList ListMembers(ArrayList members, IClass curType)
		{
//			Console.WriteLine("LIST MEMBERS!!!");
//			Console.WriteLine("showStatic = " + showStatic);
//			Console.WriteLine(curType.InnerClasses.Count + " classes");
//			Console.WriteLine(curType.Properties.Count + " properties");
//			Console.WriteLine(curType.Methods.Count + " methods");
//			Console.WriteLine(curType.Events.Count + " events");
//			Console.WriteLine(curType.Fields.Count + " fields");
			if (showStatic) {
				foreach (IClass c in curType.InnerClasses) {
					if (IsAccessible(curType, c)) {
						members.Add(c);
//						Console.WriteLine("Member added");
					}
				}
			}
			foreach (IProperty p in curType.Properties) {
				if (MustBeShowen(curType, p)) {
					members.Add(p);
//					Console.WriteLine("Member added");
				}
			}
//			Console.WriteLine("ADDING METHODS!!!");
			foreach (IMethod m in curType.Methods) {
//				Console.WriteLine("Method : " + m);
				if (MustBeShowen(curType, m)) {
					members.Add(m);
//					Console.WriteLine("Member added");
				}
			}
			foreach (IEvent e in curType.Events) {
				if (MustBeShowen(curType, e)) {
					members.Add(e);
//					Console.WriteLine("Member added");
				}
			}
			foreach (IField f in curType.Fields) {
				if (MustBeShowen(curType, f)) {
					members.Add(f);
//					Console.WriteLine("Member added");
				} else {
					//// enum fields must be shown here if present
					if (curType.ClassType == ClassType.Enum) {
						if (IsAccessible(curType,f)) members.Add(f);
//						Console.WriteLine("Member {0} added", f.FullyQualifiedName);
					}
				}
			}
//			Console.WriteLine("ClassType = " + curType.ClassType);
			if (curType.ClassType == ClassType.Interface && !showStatic) {
				foreach (string s in curType.BaseTypes) {
					IClass baseClass = parserContext.GetClass (s, true, true);
					if (baseClass != null && baseClass.ClassType == ClassType.Interface) {
						ListMembers(members, baseClass);
					}
				}
			} else {
				IClass baseClass = BaseClass(curType);
				if (baseClass != null) {
//					Console.WriteLine("Base Class = " + baseClass.FullyQualifiedName);
					ListMembers(members, baseClass);
				}
			}
//			Console.WriteLine("listing finished");
			return members;
		}
		
		public IClass BaseClass(IClass curClass)
		{
			foreach (string s in curClass.BaseTypes) {
				IClass baseClass = parserContext.GetClass (s, true, true);
				if (baseClass != null && baseClass.ClassType != ClassType.Interface) {
					return baseClass;
				}
			}
			return null;
		}
		
		bool InStatic()
		{
			IProperty property = GetProperty();
			if (property != null) {
				return property.IsStatic;
			}
			IMethod method = GetMethod();
			if (method != null) {
				return method.IsStatic;
			}
			return false;
		}
		
		bool IsAccessible(IClass c, IDecoration member)
		{
//			Console.WriteLine("member.Modifiers = " + member.Modifiers);
			if ((member.Modifiers & ModifierEnum.Internal) == ModifierEnum.Internal) {
				return true;
			}
			if ((member.Modifiers & ModifierEnum.Public) == ModifierEnum.Public) {
//				Console.WriteLine("IsAccessible");
				return true;
			}
			if ((member.Modifiers & ModifierEnum.Protected) == ModifierEnum.Protected && IsClassInInheritanceTree(c, callingClass)) {
//				Console.WriteLine("IsAccessible");
				return true;
			}
			return c.FullyQualifiedName == callingClass.FullyQualifiedName;
		}
		
		bool MustBeShowen(IClass c, IDecoration member)
		{
//			Console.WriteLine("member:" + member.Modifiers);
			if ((!showStatic &&  ((member.Modifiers & ModifierEnum.Static) == ModifierEnum.Static)) ||
			    ( showStatic && !((member.Modifiers & ModifierEnum.Static) == ModifierEnum.Static))) {
				//// enum type fields are not shown here - there is no info in member about enum field
				return false;
			}
//			Console.WriteLine("Testing Accessibility");
			return IsAccessible(c, member);
		}
		
		public ArrayList SearchMethod(IReturnType type, string memberName)
		{
			if (type == null || type.PointerNestingLevel != 0) {
				return new ArrayList();
			}
			IClass curType;
			if (type.ArrayDimensions != null && type.ArrayDimensions.Length > 0) {
				curType = SearchType("System.Array", null);
			} else {
				curType = SearchType(type.FullyQualifiedName, null);
				if (curType == null) {
					return new ArrayList();
				}
			}
			return SearchMethod(new ArrayList(), curType, memberName);
		}
		
		ArrayList SearchMethod(ArrayList methods, IClass curType, string memberName)
		{
			foreach (IMethod m in curType.Methods) {
				if (m.Name == memberName &&
				    MustBeShowen(curType, m) &&
				    !((m.Modifiers & ModifierEnum.Override) == ModifierEnum.Override)) {
					methods.Add(m);
				}
			}
			IClass baseClass = BaseClass(curType);
			if (baseClass != null) {
				return SearchMethod(methods, baseClass, memberName);
			}
			showStatic = false;
			return methods;
		}
		
		public ArrayList SearchIndexer(IReturnType type)
		{
			IClass curType = SearchType(type.FullyQualifiedName, null);
			if (curType != null) {
				return SearchIndexer(new ArrayList(), curType);
			}
			return new ArrayList();
		}
		
		public ArrayList SearchIndexer(ArrayList indexer, IClass curType)
		{
			foreach (IIndexer i in curType.Indexer) {
				if (MustBeShowen(curType, i) && !((i.Modifiers & ModifierEnum.Override) == ModifierEnum.Override)) {
					indexer.Add(i);
				}
			}
			IClass baseClass = BaseClass(curType);
			if (baseClass != null) {
				return SearchIndexer(indexer, baseClass);
			}
			showStatic = false;
			return indexer;
		}
		
		// no methods or indexer
		public IReturnType SearchMember(IReturnType type, string memberName)
		{
			if (type == null || memberName == null || memberName == "") {
				return null;
			}
//			Console.WriteLine("searching member {0} in {1}", memberName, type.Name);
			IClass curType = SearchType(type.FullyQualifiedName, cu);
			if (curType == null) {
//				Console.WriteLine("Type not found in SearchMember");
				return null;
			}
			if (type.PointerNestingLevel != 0) {
				return null;
			}
			if (type.ArrayDimensions != null && type.ArrayDimensions.Length > 0) {
				curType = SearchType("System.Array", null);
			}
			if (curType.ClassType == ClassType.Enum) {
				foreach (IField f in curType.Fields) {
					if (f.Name == memberName && MustBeShowen(curType, f)) {
						showStatic = false;
						return type; // enum members have the type of the enum
					}
				}
			}
			if (showStatic) {
//				Console.WriteLine("showStatic == true");
				foreach (IClass c in curType.InnerClasses) {
					if (c.Name == memberName && IsAccessible(curType, c)) {
						return new ReturnType(c.FullyQualifiedName);
					}
				}
			}
//			Console.WriteLine("#Properties " + curType.Properties.Count);
			foreach (IProperty p in curType.Properties) {
//				Console.WriteLine("checke Property " + p.Name);
//				Console.WriteLine("member name " + memberName);
				if (p.Name == memberName && MustBeShowen(curType, p)) {
//					Console.WriteLine("Property found " + p.Name);
					showStatic = false;
					return p.ReturnType;
				}
			}
			foreach (IField f in curType.Fields) {
//				Console.WriteLine("checke Feld " + f.Name);
//				Console.WriteLine("member name " + memberName);
				if (f.Name == memberName && MustBeShowen(curType, f)) {
//					Console.WriteLine("Field found " + f.Name);
					showStatic = false;
					return f.ReturnType;
				}
			}
			foreach (IEvent e in curType.Events) {
				if (e.Name == memberName && MustBeShowen(curType, e)) {
					showStatic = false;
					return e.ReturnType;
				}
			}
			foreach (string baseType in curType.BaseTypes) {
				IClass c = parserContext.GetClass (baseType, true, true);
				if (c != null) {
					IReturnType erg = SearchMember(new ReturnType(c.FullyQualifiedName), memberName);
					if (erg != null) {
						return erg;
					}
				}
			}
			return null;
		}
		
		bool IsInside(Point between, Point start, Point end)
		{
			if (between.Y < start.Y || between.Y > end.Y) {
//				Console.WriteLine("Y = {0} not between {1} and {2}", between.Y, start.Y, end.Y);
				return false;
			}
			if (between.Y > start.Y) {
				if (between.Y < end.Y) {
					return true;
				}
				// between.Y == end.Y
//				Console.WriteLine("between.Y = {0} == end.Y = {1}", between.Y, end.Y);
//				Console.WriteLine("returning {0}:, between.X = {1} <= end.X = {2}", between.X <= end.X, between.X, end.X);
				return between.X <= end.X;
			}
			// between.Y == start.Y
//			Console.WriteLine("between.Y = {0} == start.Y = {1}", between.Y, start.Y);
			if (between.X < start.X) {
				return false;
			}
			// start is OK and between.Y <= end.Y
			return between.Y < end.Y || between.X <= end.X;
		}
		
		ReturnType SearchVariable(string name)
		{
//			Console.WriteLine("Searching Variable");
//			
//			Console.WriteLine("LookUpTable has {0} entries", lookupTableVisitor.variables.Count);
//			Console.WriteLine("Listing Variables:");
			IDictionaryEnumerator enumerator = lookupTableVisitor.variables.GetEnumerator();
			while (enumerator.MoveNext()) {
				Console.WriteLine(enumerator.Key);
			}
//			Console.WriteLine("end listing");
			ArrayList variables = (ArrayList)lookupTableVisitor.variables[name];
			if (variables == null || variables.Count <= 0) {
//				Console.WriteLine(name + " not in LookUpTable");
				return null;
			}
			
			ReturnType found = null;
			foreach (LocalLookupVariable v in variables) {
//				Console.WriteLine("Position: ({0}/{1})", v.StartPos, v.EndPos);
				if (IsInside(new Point(caretColumn, caretLine), v.StartPos, v.EndPos)) {
					found = new ReturnType(v.TypeRef);
//					Console.WriteLine("Variable found");
					break;
				}
			}
			if (found == null) {
//				Console.WriteLine("No Variable found");
				return null;
			}
			return found;
		}
		
		/// <remarks>
		/// does the dynamic lookup for the typeName
		/// </remarks>
		public IReturnType DynamicLookup(string typeName)
		{
//			Console.WriteLine("starting dynamic lookup");
//			Console.WriteLine("name == " + typeName);
			
			// try if it exists a variable named typeName
			ReturnType variable = SearchVariable(typeName);
			if (variable != null) {
				showStatic = false;
				return variable;
			}
//			Console.WriteLine("No Variable found");
			
			if (callingClass == null) {
				return null;
			}
			//// somehow search in callingClass fields is not returning anything, so I am searching here once again
			foreach (IField f in callingClass.Fields) {
				if (f.Name == typeName) {
//					Console.WriteLine("Field found " + f.Name);
					return f.ReturnType;
				}
			}
			//// end of mod for search in Fields
		
			// try if typeName is a method parameter
			IReturnType p = SearchMethodParameter(typeName);
			if (p != null) {
//				Console.WriteLine("MethodParameter Found");
				showStatic = false;
				return p;
			}
//			Console.WriteLine("No Parameter found");
			
			// check if typeName == value in set method of a property
			if (typeName == "value") {
				p = SearchProperty();
				if (p != null) {
					showStatic = false;
					return p;
				}
			}
//			Console.WriteLine("No Property found");
			
			// try if there exists a nonstatic member named typeName
			showStatic = false;
			IReturnType t = SearchMember(callingClass == null ? null : new ReturnType(callingClass.FullyQualifiedName), typeName);
			if (t != null) {
				return t;
			}
//			Console.WriteLine("No nonstatic member found");
			
			// try if there exists a static member named typeName
			showStatic = true;
			t = SearchMember(callingClass == null ? null : new ReturnType(callingClass.FullyQualifiedName), typeName);
			if (t != null) {
				showStatic = false;
				return t;
			}
//			Console.WriteLine("No static member found");
			
			// try if there exists a static member in outer classes named typeName
			foreach (IClass c in GetOuterClasses()) {
				t = SearchMember(callingClass == null ? null : new ReturnType(c.FullyQualifiedName), typeName);
				if (t != null) {
					showStatic = false;
					return t;
				}
			}
//			Console.WriteLine("No static member in outer classes found");
//			Console.WriteLine("DynamicLookUp resultless");
			return null;
		}
		
		IProperty GetProperty()
		{
			foreach (IProperty property in callingClass.Properties) {
				if (property.BodyRegion != null && property.BodyRegion.IsInside(caretLine, caretColumn)) {
					return property;
				}
			}
			return null;
		}
		
		IMethod GetMethod()
		{
			foreach (IMethod method in callingClass.Methods) {
				if (method.BodyRegion != null && method.BodyRegion.IsInside(caretLine, caretColumn)) {
					return method;
				}
			}
			return null;
		}
		
		IReturnType SearchProperty()
		{
			IProperty property = GetProperty();
			if (property == null) {
				return null;
			}
			if (property.SetterRegion != null && property.SetterRegion.IsInside(caretLine, caretColumn)) {
				return property.ReturnType;
			}
			return null;
		}
		
		IReturnType SearchMethodParameter(string parameter)
		{
			IMethod method = GetMethod();
			if (method == null) {
				Console.WriteLine("Method not found");
				return null;
			}
			foreach (IParameter p in method.Parameters) {
				if (p.Name == parameter) {
					Console.WriteLine("Parameter found");
					return p.ReturnType;
				}
			}
			return null;
		}
		
		/// <remarks>
		/// use the usings to find the correct name of a namespace
		/// </remarks>
		public string SearchNamespace(string name, ICompilationUnit unit)
		{
			if (parserContext.NamespaceExists (name)) {
				return name;
			}
			if (unit == null) {
				return null;
			}
			foreach (IUsing u in unit.Usings) {
				if (u != null && (u.Region == null || u.Region.IsInside(caretLine, caretColumn))) {
					string nameSpace = parserContext.SearchNamespace (u, name);
					if (nameSpace != null) {
						return nameSpace;
					}
				}
			}
			return null;
		}
		
		/// <remarks>
		/// use the usings and the name of the namespace to find a class
		/// </remarks>
		public IClass SearchType(string name, ICompilationUnit unit)
		{
//			Console.WriteLine("Searching Type " + name);
			if (name == null || name == String.Empty) {
//				Console.WriteLine("No Name!");
				return null;
			}
			IClass c;
			c = parserContext.GetClass (name);
			if (c != null) {
//				Console.WriteLine("Found!");
				return c;
			}
			Console.WriteLine("No FullName");
			if (unit != null) {
				Console.WriteLine(unit.Usings.Count + " Usings");
				foreach (IUsing u in unit.Usings) {
					if (u != null && (u.Region == null || u.Region.IsInside(caretLine, caretColumn))) {
//						Console.WriteLine("In UsingRegion");
						c = parserContext.SearchType (u, name);
						if (c != null) {
//							Console.WriteLine("SearchType Successfull!!!");
							return c;
						}
					}
				}
			}
			if (callingClass == null) {
				return null;
			}
			string fullname = callingClass.FullyQualifiedName;
			string[] namespaces = fullname.Split(new char[] {'.'});
			string curnamespace = "";
			int i = 0;
			
			do {
				curnamespace += namespaces[i] + '.';
				c = parserContext.GetClass (curnamespace + name);
				if (c != null) {
					return c;
				}
				i++;
			}
			while (i < namespaces.Length);
			
			return null;
		}
		
		/// <remarks>
		/// Returns true, if class possibleBaseClass is in the inheritance tree from c
		/// </remarks>
		bool IsClassInInheritanceTree(IClass possibleBaseClass, IClass c)
		{
			if (possibleBaseClass == null || c == null) {
				return false;
			}
			if (possibleBaseClass.FullyQualifiedName == c.FullyQualifiedName) {
				return true;
			}
			foreach (string baseClass in c.BaseTypes) {
				IClass bc = parserContext.GetClass (baseClass, true, true);
				if (IsClassInInheritanceTree(possibleBaseClass, bc)) {
					return true;
				}
			}
			return false;
		}
		
		/// <remarks>
		/// Returns the innerst class in which the carret currently is, returns null
		/// if the carret is outside any class boundaries.
		/// </remarks>
		IClass GetInnermostClass()
		{
			if (cu != null) {
				foreach (IClass c in cu.Classes) {
					if (c != null && c.Region != null && c.Region.IsInside(caretLine, caretColumn)) {
						return GetInnermostClass(c);
					}
				}
			}
			return null;
		}
		
		IClass GetInnermostClass(IClass curClass)
		{
			if (curClass == null) {
				return null;
			}
			if (curClass.InnerClasses == null) {
				return GetResolvedClass (curClass);
			}
			foreach (IClass c in curClass.InnerClasses) {
				if (c != null && c.Region != null && c.Region.IsInside(caretLine, caretColumn)) {
					return GetInnermostClass(c);
				}
			}
			return GetResolvedClass (curClass);
		}
		
		/// <remarks>
		/// Returns all (nestet) classes in which the carret currently is exept
		/// the innermost class, returns an empty collection if the carret is in 
		/// no class or only in the innermost class.
		/// the most outer class is the last in the collection.
		/// </remarks>
		ClassCollection GetOuterClasses()
		{
			ClassCollection classes = new ClassCollection();
			if (cu != null) {
				foreach (IClass c in cu.Classes) {
					if (c != null && c.Region != null && c.Region.IsInside(caretLine, caretColumn)) {
						if (c != GetInnermostClass()) {
							GetOuterClasses(classes, c);
							classes.Add(GetResolvedClass (c));
						}
						break;
					}
				}
			}
			
			return classes;
		}
		
		void GetOuterClasses(ClassCollection classes, IClass curClass)
		{
			if (curClass != null) {
				foreach (IClass c in curClass.InnerClasses) {
					if (c != null && c.Region != null && c.Region.IsInside(caretLine, caretColumn)) {
						if (c != GetInnermostClass()) {
							GetOuterClasses(classes, c);
							classes.Add(GetResolvedClass (c));
						}
						break;
					}
				}
			}
		}
		
		public IClass GetResolvedClass (IClass cls)
		{
			// Returns an IClass in which all type names have been properly resolved
			return parserContext.GetClass (cls.FullyQualifiedName);
		}

		public ArrayList IsAsResolve (string expression, int caretLine, int caretColumn, string fileName, string fileContent)
		{
			ArrayList result = new ArrayList ();
			this.caretLine = caretLine;
			this.caretColumn = caretColumn;
			
			IParseInformation parseInfo = parserContext.GetParseInformation (fileName);
			ICSharpCode.SharpRefactory.Parser.AST.CompilationUnit fcu = parseInfo.MostRecentCompilationUnit.Tag as ICSharpCode.SharpRefactory.Parser.AST.CompilationUnit;
			if (fcu == null)
				return null;
			ICSharpCode.SharpRefactory.Parser.Parser p = new ICSharpCode.SharpRefactory.Parser.Parser ();
			Lexer l = new Lexer (new StringReader (expression));
			Expression expr = p.ParseExpression(l);
			if (expr == null)
				return null;

			lookupTableVisitor = new LookupTableVisitor ();
			lookupTableVisitor.Visit (fcu, null);

			TypeVisitor typeVisitor = new TypeVisitor (this);

			CSharpVisitor csharpVisitor = new CSharpVisitor ();
			cu = (ICompilationUnit)csharpVisitor.Visit (fcu, null);
			if (cu != null) {
				callingClass = GetInnermostClass ();
			}

			IReturnType type = expr.AcceptVisitor (typeVisitor, null) as IReturnType;
			if (type == null || type.PointerNestingLevel != 0) {
				fcu = parserContext.ParseFile (fileName, fileContent).MostRecentCompilationUnit.Tag as ICSharpCode.SharpRefactory.Parser.AST.CompilationUnit;
				lookupTableVisitor.Visit (fcu, null);
				cu = (ICompilationUnit)csharpVisitor.Visit (fcu, null);

				if (cu != null) {
					callingClass = GetInnermostClass ();
				}
				type = expr.AcceptVisitor (typeVisitor, null) as IReturnType;
				if (type == null)
					return null;
			}
			if (type.ArrayDimensions != null && type.ArrayDimensions.Length > 0)
				type = new ReturnType ("System.Array");

			IClass returnClass = SearchType (type.FullyQualifiedName, cu);
//			IClass returnClass = parserContext.SearchType (type.FullyQualifiedName, null, cu);
			if (returnClass == null)
				return null;

			foreach (IClass iclass in parserContext.GetClassInheritanceTree (returnClass)) {
				if (!result.Contains (iclass))
					result.Add (iclass);
			}
			return result;
		}
		
		public ArrayList CtrlSpace (int caretLine, int caretColumn, string fileName)
		{
			ArrayList result = new ArrayList(TypeReference.PrimitiveTypes);
			this.caretLine = caretLine;
			this.caretColumn = caretColumn;
			IParseInformation parseInfo = parserContext.GetParseInformation (fileName);
			ICSharpCode.SharpRefactory.Parser.AST.CompilationUnit fileCompilationUnit = parseInfo.MostRecentCompilationUnit.Tag as ICSharpCode.SharpRefactory.Parser.AST.CompilationUnit;
			if (fileCompilationUnit == null) {
				Console.WriteLine("!Warning: no parseinformation!");
				return null;
			}
			lookupTableVisitor = new LookupTableVisitor();
			lookupTableVisitor.Visit(fileCompilationUnit, null);
			CSharpVisitor cSharpVisitor = new CSharpVisitor();
			cu = (ICompilationUnit)cSharpVisitor.Visit(fileCompilationUnit, null);
			if (cu != null) {
				callingClass = GetInnermostClass();
				Console.WriteLine("CallingClass is " + (callingClass == null ? "null" : callingClass.Name));
			}
			foreach (string name in lookupTableVisitor.variables.Keys) {
				ArrayList variables = (ArrayList)lookupTableVisitor.variables[name];
				if (variables != null && variables.Count > 0) {
					foreach (LocalLookupVariable v in variables) {
						if (IsInside(new Point(caretColumn, caretLine), v.StartPos, v.EndPos)) {
							result.Add(new Parameter (name, new ReturnType (v.TypeRef.SystemType)));
							break;
						}
					}
				}
			}
			if (callingClass != null) {
				ListMembers(result, callingClass);
			}
			string n = "";
			result.AddRange(parserContext.GetNamespaceContents (n, true));
			foreach (IUsing u in cu.Usings) {
				if (u != null && (u.Region == null || u.Region.IsInside(caretLine, caretColumn))) {
					foreach (string name in u.Usings) {
						result.AddRange(parserContext.GetNamespaceContents (name, true));
					}
					foreach (string alias in u.Aliases.Keys) {
						result.Add(alias);
					}
				}
			}
			return result;
		}
	}
}
