// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;

using MonoDevelop.Services;
using MonoDevelop.Internal.Project;
using Project_ = MonoDevelop.Internal.Project.Project;

namespace MonoDevelop.Internal.Parser
{
//	[Flags]
//	public enum ShowMembers {
//		Public     = 1,
//		Protected  = 2,
//		Private    = 4,
//		Static     = 8
//	}
	
	public class ResolveResult
	{
		IClass type;
		ArrayList members;
		StringCollection namespaces;
		
		public IClass Type {
			get {
				return type;
			}
		}
		
		public ArrayList Members {
			get {
				return members;
			}
		}
		
		public StringCollection Namespaces {
			get {
				return namespaces;
			}
		}
		
		public ResolveResult(string[] namespaces) {
			this.namespaces = new StringCollection();
			this.namespaces.AddRange(namespaces);
			members = new ArrayList();
		}
		
		public ResolveResult(string[] namespaces, ArrayList classes) {
			this.namespaces = new StringCollection();
			this.namespaces.AddRange(namespaces);
			members = classes;
		}
		
		public ResolveResult(StringCollection namespaces) {
			this.namespaces = namespaces;
			members = new ArrayList();
		}
		
		public ResolveResult(IClass type, ArrayList members) {
			this.type = type;
			this.members = members;
			namespaces = new StringCollection();
		}
//		object[]    resolveContents;
//		ShowMembers showMembers;
//		
//		public bool ShowPublic {
//			get {
//				return (showMembers & ShowMembers.Public) == ShowMembers.Public;
//			}
//		}
//
//		public bool ShowProtected {
//			get {
//				return (showMembers & ShowMembers.Protected) == ShowMembers.Protected;
//			}
//		}
//		
//		public bool ShowPrivate {
//			get {
//				return (showMembers & ShowMembers.Private) == ShowMembers.Private;
//			}
//		}
//
//		public bool ShowStatic {
//			get {
//				return (showMembers & ShowMembers.Static) == ShowMembers.Static;
//			}
//		}
//		
//		public object[] ResolveContents {
//			get {
//				return resolveContents;
//			}
//		}
//		
//		public ShowMembers ShowMembers {
//			get {
//				return showMembers;
//			}
//		}
//		
//		public ResolveResult(object[] resolveContents, ShowMembers showMembers)
//		{
//			this.resolveContents = resolveContents;
//			this.showMembers     = showMembers;
//		}
	}
	
	public interface IParser {
		
		string[] LexerTags {
			get;
			set;
		}
		
		IExpressionFinder ExpressionFinder {
			get;
		}

		bool CanParse (string fileName);
		bool CanParse (Project_ project);
		
		ICompilationUnitBase Parse(string fileName);
		ICompilationUnitBase Parse(string fileName, string fileContent);
		
		/// <summary>
		/// Resolves an expression.
		/// The caretLineNumber and caretColumn is 1 based.
		/// </summary>
		ResolveResult Resolve(IParserContext parserContext, 
		                      string expression, 
		                      int caretLineNumber, 
		                      int caretColumn, 
		                      string fileName,
		                      string fileContent);

		string MonodocResolver (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent);

		ArrayList IsAsResolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent);
		
		ArrayList CtrlSpace(IParserContext parserContext, int caretLine, int caretColumn, string fileName);
	}
}
