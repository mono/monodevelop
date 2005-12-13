using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.CodeDom.Compiler;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Projects.Parser;
using MonoDevelop.Projects.CodeGeneration;

namespace NemerleBinding
{
	/// <summary>
	/// This class describes the main functionalaty of a language binding
	/// </summary>
	public class NemerleLanguageBinding : IDotNetLanguageBinding
	{
		public const string LanguageName = "Nemerle";
		
		NemerleBindingCompilerServices   compilerServices  = new NemerleBindingCompilerServices();
		
		public string Language {
			get { return LanguageName; }
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
			return compilerServices.CanCompile(fileName);
		}
		
		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			return compilerServices.Compile (projectFiles, references, configuration, monitor);
		}
		
		public void GenerateMakefile (Project project, Combine parentCombine)
		{
			compilerServices.GenerateMakefile(project, parentCombine);
		}
		
		public ICloneable CreateCompilationParameters (XmlElement projectOptions)
		{
			return new NemerleParameters ();
		}
		
		// http://nemerle.org/csharp-diff.html
		public string CommentTag
		{
			get { return "//"; }
		}
		
		public CodeDomProvider GetCodeDomProvider ()
		{
			return null;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".n";
		}
		
		public IParser Parser {
			get { return null; }
		}
		
		public IRefactorer Refactorer {
			get { return null; }
		}
	}
}
