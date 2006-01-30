// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;

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
		
		IParserContext GetProjectParserContext (Project project);
		IParserContext GetFileParserContext (string file);

		IParseInformation UpdateFile (Project project, string fileName, string fileContent);
		
		bool TrackFileChanges { get; set; }
		
		IProgressMonitorFactory ParseProgressMonitorFactory { get; set; }

		event ParseInformationEventHandler ParseInformationChanged;
		event ClassInformationEventHandler ClassInformationChanged;
	}
	
/*	public interface IFileParserContext: IParserContext
	{
		IParseInformation ParseFile ();
	}

	public interface IProjectParserContext: IParserContext
	{
		IParseInformation ParseFile (string fileName);
		IParseInformation ParseFile (string fileName, string fileContent);
	}
*/
	public interface IParserContext
	{
		IExpressionFinder GetExpressionFinder(string fileName);
		
		IParseInformation ParseFile (string fileName);
		IParseInformation ParseFile (string fileName, string fileContent);
		IParseInformation ParseFile (ITextFile file);
		
		IParseInformation GetParseInformation (string fileName);
		
		// Default Parser Layer dependent functions
		IClass    GetClass (string typeName);
		string[]  GetClassList (string subNameSpace, bool includeReferences);
		string[]  GetNamespaceList (string subNameSpace);
		LanguageItemCollection GetNamespaceContents (string subNameSpace, bool includeReferences);
		bool      NamespaceExists (string name);
		string    SearchNamespace (IUsing iusing, string partitialNamespaceName);
		IClass    SearchType (IUsing iusing, string partitialTypeName);
		
		IClass    GetClass (string typeName, bool deepSearchReferences, bool caseSensitive);
		string[]  GetClassList (string subNameSpace, bool includeReferences, bool caseSensitive);
		string[]  GetNamespaceList (string subNameSpace, bool includeReferences, bool caseSensitive);
		LanguageItemCollection GetNamespaceContents (string subNameSpace, bool includeReferences, bool caseSensitive);
		bool      NamespaceExists (string name, bool caseSensitive);
		string    SearchNamespace (IUsing iusing, string partitialNamespaceName, bool caseSensitive);
		IClass    SearchType (IUsing iusing, string partitialTypeName, bool caseSensitive);
		IClass    SearchType (string name, IClass callingClass, ICompilationUnit unit);
		
		IEnumerable GetClassInheritanceTree (IClass cls);
		
		IClass[] GetFileContents (string fileName);
		IClass[] GetProjectContents ();
		
		////////////////////////////////////////////

		/// <summary>
		/// Resolves an expression.
		/// The caretLineNumber and caretColumn is 1 based.
		/// </summary>
		ResolveResult Resolve (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent);
		
		string MonodocResolver (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent);
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
