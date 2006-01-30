// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using Project_ = MonoDevelop.Projects.Project;

namespace MonoDevelop.Projects.Parser
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
		LanguageItemCollection members;
		LanguageItemCollection namespaces;
		
		public IClass Type {
			get {
				return type;
			}
		}
		
		public LanguageItemCollection Members {
			get {
				return members;
			}
		}
		
		public LanguageItemCollection Namespaces {
			get {
				return namespaces;
			}
		}
		
		public ResolveResult (string[] namespaces): this (namespaces,  new LanguageItemCollection())
		{
		}
		
		public ResolveResult (string[] namespaces, LanguageItemCollection classes) {
			this.namespaces = new LanguageItemCollection();
			foreach (string s in namespaces)
				this.namespaces.Add (new Namespace (s));
			members = classes;
		}
		
		public ResolveResult (LanguageItemCollection namespaces) {
			this.namespaces = namespaces;
			members = new LanguageItemCollection();
		}
		
		public ResolveResult(IClass type, LanguageItemCollection members) {
			this.type = type;
			this.members = members;
			namespaces = new LanguageItemCollection();
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
		
		IExpressionFinder CreateExpressionFinder (string fileName);

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

		LanguageItemCollection IsAsResolve (IParserContext parserContext, string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent);
		
		LanguageItemCollection CtrlSpace(IParserContext parserContext, int caretLine, int caretColumn, string fileName);
		
		ILanguageItem ResolveIdentifier (IParserContext parserContext, string id, int line, int col, string fileName, string fileContent);
	}
}
