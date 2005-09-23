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

namespace PythonBinding
{
	public class PythonLanguageBinding : ILanguageBinding
	{
		public const string LanguageName = "Python";
		
		PythonExecutionManager executionManager = new PythonExecutionManager();
		PythonCompilerManager  compilerManager  = new PythonCompilerManager();
		
		public string Language {
			get {
				return LanguageName;
			}
		}
		
		public void Execute(string filename)
		{
			Execute (filename, false);
		}
	
		public void Execute(string filename, bool debug)
		{
			Debug.Assert(executionManager != null);
			executionManager.Execute(filename, debug);
		}
		
		public void Execute (IProject project)
		{
			Execute (project, false);
		}

		public void DebugProject (IProject project)
		{
			Execute (project, true);
		}

		public void Execute(IProject project, bool debug)
		{
			Debug.Assert(executionManager != null);
			executionManager.Execute(project, debug);
		}
		
		public string GetCompiledOutputName(string fileName)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.GetCompiledOutputName(fileName);
		}
		
		public string GetCompiledOutputName(IProject project)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.GetCompiledOutputName(project);
		}
		
		public bool CanCompile(string fileName)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.CanCompile(fileName);
		}
		
		public ICompilerResult CompileFile(string fileName)
		{
			Debug.Assert(compilerManager != null);
			PythonCompilerParameters param = new PythonCompilerParameters();
			param.OutputAssembly = Path.ChangeExtension(fileName, ".exe");
			return compilerManager.CompileFile(fileName, param);
		}
		
		public ICompilerResult CompileProject(IProject project)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.CompileProject(project);
		}
		
		public ICompilerResult RecompileProject(IProject project)
		{
			return CompileProject(project);
		}
		
		public IProject CreateProject(ProjectCreateInformation info, XmlElement projectOptions)
		{
			return new PythonProject(info, projectOptions);
		}

		public void GenerateMakefile (IProject project, Combine parentCombine)
		{
			throw new NotImplementedException ();
		}
		
		public string CommentTag
		{
			get { return "#"; }
		}
	}
}
