// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike Krueger" email="mike@icsharpcode.net"/>
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
using Gtk;

using MonoDevelop.Internal.Project;
using MonoDevelop.Internal.Templates;
using MonoDevelop.Gui;
using MonoDevelop.Services;

namespace ILAsmBinding
{
	public class ILAsmLanguageBinding : ILanguageBinding
	{
		public const string LanguageName = "ILAsm";
		
		ILAsmCompilerManager  compilerManager  = new ILAsmCompilerManager();
		
		public ILAsmLanguageBinding ()
		{
			Runtime.ProjectService.DataContext.IncludeType (typeof(ILAsmCompilerParameters));
		}
		
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
			// Not supported
		}
		
		public ICloneable CreateCompilationParameters (XmlElement projectOptions)
		{
			return new ILAsmCompilerParameters();
		}
		
		public string CommentTag
		{
			get { return "//"; }
		}
	}
}
