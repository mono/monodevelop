using System;
using System.IO;
using System.CodeDom.Compiler;
using Gtk;

using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Core;
using MonoDevelop.Core;
using MonoDevelop.Projects;
using MonoDevelop.Core.Gui;

namespace PythonBinding
{
	public class PythonCompilerManager
	{
		public string GetCompiledOutputName (string fileName)
		{
			return Path.ChangeExtension (fileName, ".exe");
		}
		
		public string GetCompiledOutputName (IProject project)
		{
			PythonProject p = (PythonProject) project;
			PythonCompilerParameters compilerparameters = (PythonCompilerParameters) p.ActiveConfiguration;
			string exe  = Runtime.FileService.GetDirectoryNameWithSeparator (compilerparameters.OutputDirectory) + compilerparameters.OutputAssembly + ".exe";
			return exe;
		}
		
		public bool CanCompile (string fileName)
		{
			return Path.GetExtension (fileName).ToLower () == ".py";
		}
		
		BuildResult Compile (PythonCompilerParameters compilerparameters, string[] fileNames)
		{
			// just pretend we compiled
			// and leave it to the runtime for now
			return new BuildResult (new CompilerResults (new TempFileCollection ()), "");
		}

		public BuildResult CompileFile (string fileName, PythonCompilerParameters compilerparameters)
		{
			// just pretend we compiled
			// and leave it to the runtime for now
			return new BuildResult (new CompilerResults (new TempFileCollection ()), "");
		}
		
		public BuildResult CompileProject (IProject project)
		{
			// just pretend we compiled
			// and leave it to the runtime for now
			return new BuildResult (new CompilerResults (new TempFileCollection ()), "");
		}
		
		string GetCompilerName ()
		{
			return "IronPythonConsole";
		}
		
		BuildResult ParseOutput (TempFileCollection tf, StreamReader sr)
		{
			// just pretend we compiled
			// and leave it to the runtime for now
			return new BuildResult (new CompilerResults (new TempFileCollection ()), "");
		}
	}
}
