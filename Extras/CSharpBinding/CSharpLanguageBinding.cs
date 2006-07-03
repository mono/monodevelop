// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krüger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.CodeDom.Compiler;
using System.Threading;
using Microsoft.CSharp;

using MonoDevelop.Projects;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;
using MonoDevelop.Core;

using CSharpBinding.Parser;

namespace CSharpBinding
{
	public class CSharpLanguageBinding : IDotNetLanguageBinding
	{
		public const string LanguageName = "C#";
		
		CSharpBindingCompilerManager   compilerManager  = new CSharpBindingCompilerManager();
		CSharpCodeProvider provider;
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.CanCompile(fileName);
		}
		
		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.Compile (projectFiles, references, configuration, monitor);
		}
		
		public void GenerateMakefile (Project project, Combine parentCombine)
		{
			compilerManager.GenerateMakefile (project, parentCombine);
		}
		
		public ICloneable CreateCompilationParameters (XmlElement projectOptions)
		{
			return new CSharpCompilerParameters();
		}
		
		public string CommentTag
		{
			get { return "//"; }
		}
		
		public CodeDomProvider GetCodeDomProvider ()
		{
			if (provider == null)
				provider = new CSharpEnhancedCodeProvider ();
			return provider;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".cs";
		}
		
		TParser parser = new TParser ();
		CSharpRefactorer refactorer = new CSharpRefactorer ();
			
		public IParser Parser {
			get { return parser; }
		}
		
		public IRefactorer Refactorer {
			get { return refactorer; }
		}
		
		public ClrVersion[] GetSupportedClrVersions ()
		{
			return new ClrVersion[] { ClrVersion.Net_1_1, ClrVersion.Net_2_0 };
		}
	}
}
