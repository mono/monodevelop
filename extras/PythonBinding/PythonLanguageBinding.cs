//  PythonLanguageBinding.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Mike Krüger <mkrueger@novell.com>
// 
//  This program is free software; you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation; either version 2 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU General Public License for more details.
//  
//  You should have received a copy of the GNU General Public License
//  along with this program; if not, write to the Free Software
//  Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA 02111-1307 USA

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

using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;

namespace PythonBinding
{
	public class PythonLanguageBinding : IDotNetLanguageBinding
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
		
		public bool IsSourceCodeFile (string fileName)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.CanCompile(fileName);
		}
		
		public BuildResult CompileFile(string fileName)
		{
			Debug.Assert(compilerManager != null);
			PythonCompilerParameters param = new PythonCompilerParameters();
			param.OutputAssembly = Path.ChangeExtension(fileName, ".exe");
			return compilerManager.CompileFile(fileName, param);
		}
		
		public BuildResult CompileProject(IProject project)
		{
			Debug.Assert(compilerManager != null);
			return compilerManager.CompileProject(project);
		}
		
		public BuildResult RecompileProject(IProject project)
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
