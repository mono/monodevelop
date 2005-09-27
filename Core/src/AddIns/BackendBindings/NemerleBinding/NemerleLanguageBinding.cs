using System;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Reflection;
using System.Resources;
using System.Xml;

using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace NemerleBinding
{
	/// <summary>
	/// This class describes the main functionalaty of a language binding
	/// </summary>
	public class NemerleLanguageBinding : ILanguageBinding
	{
		public const string LanguageName = "Nemerle";
		
		NemerleBindingCompilerServices   compilerServices  = new NemerleBindingCompilerServices();
		
		public NemerleLanguageBinding ()
		{
			Services.ProjectService.DataContext.IncludeType (typeof(NemerleParameters));
		}
		
		public string Language {
			get { return LanguageName; }
		}
		
		public bool CanCompile(string fileName)
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
	}
}
