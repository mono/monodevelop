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

using NemerleBinding.Parser;
using Nemerle.Compiler;

namespace NemerleBinding
{
	/// <summary>
	/// This class describes the main functionalaty of a language binding
	/// </summary>
	public class NemerleLanguageBinding : IDotNetLanguageBinding
	{
		public const string LanguageName = "Nemerle";
		NemerleCodeProvider provider = new NemerleCodeProvider();
		
		NemerleBindingCompilerServices   compilerServices  = new NemerleBindingCompilerServices();
		
		public string Language {
			get { return LanguageName; }
		}
		
		public bool IsSourceCodeFile (string fileName)
		{
			return compilerServices.CanCompile(fileName);
		}
		
		public BuildResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			return compilerServices.Compile (projectFiles, references, configuration, monitor);
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
			return provider;
		}
		
		public string GetFileName (string baseName)
		{
			return baseName + ".n";
		}

        TParser parser = new TParser();
        NemerleRefactorer refactorer = new NemerleRefactorer();
		
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
