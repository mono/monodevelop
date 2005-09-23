// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.Diagnostics;
using System.IO;

using MonoDevelop.Core.AddIns;

using MonoDevelop.Internal.Parser;
using MonoDevelop.Internal.Project;

using MonoDevelop.Gui;

namespace MonoDevelop.Services
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
		
		IParser GetParser(string fileName);
		IExpressionFinder GetExpressionFinder(string fileName);
	}
	
	public interface IParserDatabase
	{
		void Load (CombineEntry entry);
		void Unload (CombineEntry entry);
		
		IParserContext GetProjectParserContext (Project project);
		IParserContext GetFileParserContext (string file);

		void UpdateFile (Project project, string fileName, string fileContent);
		
		bool TrackFileChanges { get; set; }
		
		IProgressMonitorFactory ParseProgressMonitorFactory { get; set; }

		event ParseInformationEventHandler ParseInformationChanged;
		event ClassInformationEventHandler ClassInformationChanged;
	}

	public interface IParserContext
	{
		IExpressionFinder GetExpressionFinder(string fileName);
		
		IParseInformation ParseFile (string fileName);
		IParseInformation ParseFile (string fileName, string fileContent);
		
		IParseInformation GetParseInformation (string fileName);
		
		// Default Parser Layer dependent functions
		IClass    GetClass (string typeName);
		string[]  GetClassList (string subNameSpace, bool includeReferences);
		string[]  GetNamespaceList (string subNameSpace);
		ArrayList GetNamespaceContents (string subNameSpace, bool includeReferences);
		bool      NamespaceExists (string name);
		string    SearchNamespace (IUsing iusing, string partitialNamespaceName);
		IClass    SearchType (IUsing iusing, string partitialTypeName);
		
		IClass    GetClass (string typeName, bool deepSearchReferences, bool caseSensitive);
		string[]  GetClassList (string subNameSpace, bool includeReferences, bool caseSensitive);
		string[]  GetNamespaceList (string subNameSpace, bool includeReferences, bool caseSensitive);
		ArrayList GetNamespaceContents (string subNameSpace, bool includeReferences, bool caseSensitive);
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
		ArrayList IsAsResolve (string expression, int caretLineNumber, int caretColumn, string fileName, string fileContent);
		ArrayList CtrlSpace (int caretLine, int caretColumn, string fileName);
	}
	
	public interface IProgressMonitorFactory
	{
		IProgressMonitor CreateProgressMonitor ();
	}
}
