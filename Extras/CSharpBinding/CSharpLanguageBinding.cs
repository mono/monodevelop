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

using MonoDevelop.Projects;
using MonoDevelop.Core;

namespace CSharpBinding
{
	public class CSharpLanguageBinding : ILanguageBinding
	{
		public const string LanguageName = "C#";
		
		CSharpBindingCompilerManager   compilerManager  = new CSharpBindingCompilerManager();
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public bool CanCompile(string fileName)
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
	}
}
