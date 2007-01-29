// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Collections.Specialized;

using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.Projects.Parser
{
	public interface IParseInformation
	{
		ICompilationUnitBase ValidCompilationUnit {
			get;
		}
		ICompilationUnitBase DirtyCompilationUnit {
			get;
		}

		ICompilationUnitBase BestCompilationUnit {
			get;
		}

		ICompilationUnitBase MostRecentCompilationUnit {
			get;
		}
	}
	
	public interface IParserService
	{
		IParserDatabase CreateParserDatabase ();
		
		IParser GetParser (string fileName);
		IExpressionFinder GetExpressionFinder(string fileName);
	}
	
	public interface IParserDatabase
	{
		void Load (CombineEntry entry);
		void Unload (CombineEntry entry);
		bool IsLoaded (Project entry);
		
		// Returns the normalized assembly name to use to later reference this assembly
		string LoadAssembly (string assemblyName);
		void UnloadAssembly (string assemblyName);
		
		IProjectParserContext GetProjectParserContext (Project project);
		IFileParserContext GetFileParserContext (string file);
		IAssemblyParserContext GetAssemblyParserContext (string assemblyFile);

		IParseInformation UpdateFile (string fileName, string fileContent);
		IParseInformation UpdateFile (Project project, string fileName, string fileContent);
		
		bool TrackFileChanges { get; set; }
		bool IsParsing { get; }
		
		IProgressMonitorFactory ParseProgressMonitorFactory { get; set; }

		event CommentTasksChangedEventHandler CommentTasksChanged;
		event ParseInformationEventHandler ParseInformationChanged;
		event ClassInformationEventHandler ClassInformationChanged;
		event AssemblyInformationEventHandler AssemblyInformationChanged;

		event EventHandler ParseOperationStarted;
		event EventHandler ParseOperationFinished;
	}
	
	public interface IFileParserContext: IParserContext
	{
		IParseInformation ParseFile ();
	}

	public interface IProjectParserContext: IParserContext
	{
	}

	public interface IAssemblyParserContext: IParserContext
	{
	}

	public interface IParserContext
	{
		IParserDatabase ParserDatabase { get; }
		
		IParseInformation ParseFile (string fileName);
		IParseInformation ParseFile (string fileName, string fileContent);
		IParseInformation ParseFile (ITextFile file);
		
		// Makes sure that all parser information is up to date.
		void UpdateDatabase ();
		
		IExpressionFinder GetExpressionFinder(string fileName);
		
		IParseInformation GetParseInformation (string fileName);
		
		// Default Parser Layer dependent functions
		IClass    GetClass (string typeName);
		IClass    GetClass (string typeName, ReturnTypeList genericArguments);
		string[]  GetClassList (string subNameSpace, bool includeReferences);
		string[]  GetNamespaceList (string subNameSpace);
		LanguageItemCollection GetNamespaceContents (string subNameSpace, bool includeReferences);
		bool      NamespaceExists (string name);
		string    SearchNamespace (IUsing iusing, string partitialNamespaceName);
		IClass    SearchType (IUsing iusing, string partitialTypeName);
		IClass    SearchType (IUsing iusing, string partitialTypeName, ReturnTypeList genericArguments);
		
		IClass    GetClass (string typeName, bool deepSearchReferences, bool caseSensitive);
		IClass    GetClass (string typeName, ReturnTypeList genericArguments, bool deepSearchReferences, bool caseSensitive);
		string[]  GetClassList (string subNameSpace, bool includeReferences, bool caseSensitive);
		string[]  GetNamespaceList (string subNameSpace, bool includeReferences, bool caseSensitive);
		LanguageItemCollection GetNamespaceContents (string subNameSpace, bool includeReferences, bool caseSensitive);
		bool      NamespaceExists (string name, bool caseSensitive);
		string    SearchNamespace (IUsing iusing, string partitialNamespaceName, bool caseSensitive);
		IClass    SearchType (IUsing iusing, string partitialTypeName, bool caseSensitive);
		IClass    SearchType (IUsing iusing, string partitialTypeName, ReturnTypeList genericArguments, bool caseSensitive);
		IClass    SearchType (string name, IClass callingClass, ICompilationUnit unit);
		
		IEnumerable GetClassInheritanceTree (IClass cls);
		IEnumerable GetSubclassesTree (IClass cls, string[] namespaces);
		
		IClass[] GetFileContents (string fileName);
		IClass[] GetProjectContents ();
		
		// Functions for generated tasks handling 
		TagCollection GetFileSpecialComments (string fileName);
		
		////////////////////////////////////////////

		/// <summary>
		/// Resolves an expression.
		/// The caretLineNumber and caretColumn is 1 based.
		/// </summary>
		ResolveResult Resolve (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent);
		
		LanguageItemCollection IsAsResolve (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent);
		LanguageItemCollection CtrlSpace (int caretLine, int caretColumn, string fileName);
		ILanguageItem ResolveIdentifier (string id, int caretLineNumber, int caretColumn, string fileName, string fileContent);
		ILanguageItem GetEnclosingLanguageItem (int caretLineNumber, int caretColumn, ITextFile file);
	}
	
	public interface IProgressMonitorFactory
	{
		IProgressMonitor CreateProgressMonitor ();
	}
}
