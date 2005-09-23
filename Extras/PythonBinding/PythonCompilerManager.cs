using System;
using System.IO;
using System.CodeDom.Compiler;
using Gtk;

using MonoDevelop.Gui.Components;
using MonoDevelop.Services;
using MonoDevelop.Core.Services;
using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;

namespace PythonBinding
{
	public class PythonCompilerManager
	{
		FileUtilityService fileUtilityService = (FileUtilityService) ServiceManager.GetService (typeof (FileUtilityService));
		
		public string GetCompiledOutputName (string fileName)
		{
			return Path.ChangeExtension (fileName, ".exe");
		}
		
		public string GetCompiledOutputName (IProject project)
		{
			PythonProject p = (PythonProject) project;
			PythonCompilerParameters compilerparameters = (PythonCompilerParameters) p.ActiveConfiguration;
			string exe  = fileUtilityService.GetDirectoryNameWithSeparator (compilerparameters.OutputDirectory) + compilerparameters.OutputAssembly + ".exe";
			return exe;
		}
		
		public bool CanCompile (string fileName)
		{
			return Path.GetExtension (fileName).ToLower () == ".py";
		}
		
		ICompilerResult Compile (PythonCompilerParameters compilerparameters, string[] fileNames)
		{
			// just pretend we compiled
			// and leave it to the runtime for now
			return new DefaultCompilerResult (new CompilerResults (new TempFileCollection ()), "");
		}

		public ICompilerResult CompileFile (string fileName, PythonCompilerParameters compilerparameters)
		{
			// just pretend we compiled
			// and leave it to the runtime for now
			return new DefaultCompilerResult (new CompilerResults (new TempFileCollection ()), "");
		}
		
		public ICompilerResult CompileProject (IProject project)
		{
			// just pretend we compiled
			// and leave it to the runtime for now
			return new DefaultCompilerResult (new CompilerResults (new TempFileCollection ()), "");
		}
		
		string GetCompilerName ()
		{
			return "IronPythonConsole";
		}
		
		ICompilerResult ParseOutput (TempFileCollection tf, StreamReader sr)
		{
			// just pretend we compiled
			// and leave it to the runtime for now
			return new DefaultCompilerResult (new CompilerResults (new TempFileCollection ()), "");
		}
	}
}
