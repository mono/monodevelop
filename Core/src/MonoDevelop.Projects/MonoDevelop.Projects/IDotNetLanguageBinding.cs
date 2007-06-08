//// <file>
////     <copyright see="prj:///doc/copyright.txt"/>
////     <license see="prj:///doc/license.txt"/>
////     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
////     <version value="$version"/>
//// </file>
//
//using System;
//using System.CodeDom.Compiler;
//using System.Collections;
//using System.Xml;
//
//using MonoDevelop.Projects;
//using MonoDevelop.Core;
//using MonoDevelop.Projects.Parser;
//using MonoDevelop.Projects.CodeGeneration;
//
//namespace MonoDevelop.Projects
//{
//	/// <summary>
//	/// The <code>IDotNetLanguageBinding</code> interface is the base interface
//	/// of all language bindings avaiable.
//	/// </summary>
//	public interface IDotNetLanguageBinding: ILanguageBinding
//	{
//		ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor);
//		
//		ICloneable CreateCompilationParameters (XmlElement projectOptions);
//
//		// Optional. Return null if not supported.
//		CodeDomProvider GetCodeDomProvider ();
//		
//		ClrVersion[] GetSupportedClrVersions ();
//	}
//	
//}
//