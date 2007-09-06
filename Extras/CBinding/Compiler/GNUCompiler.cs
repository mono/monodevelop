//
// GNUCompiler.cs: Provides most functionality to compile using a GNU compiler (gcc and g++)
//
// Authors:
//   Marcos David Marin Amador <MarcosMarin@gmail.com>
//
// Copyright (C) 2007 Marcos David Marin Amador
//
//
// This source code is licenced under The MIT License:
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;
using System.CodeDom.Compiler;

using Mono.Addins;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui;

namespace CBinding
{
	public abstract class GNUCompiler : CCompiler
	{
		public override ICompilerResult Compile (
			ProjectFileCollection projectFiles,
		    ProjectPackageCollection packages,
		    CProjectConfiguration configuration,
		    IProgressMonitor monitor)
		{
			CompilerResults cr = new CompilerResults (new TempFileCollection ());
			bool res = true;
			string args = GetCompilerFlags (configuration);
			
			string outputName = Path.Combine (configuration.OutputDirectory,
			                                  configuration.CompiledOutputName);
			
			// Precompile header files and place them in .prec/<config_name>/
			string precdir = Path.Combine (configuration.SourceDirectory, ".prec");
			if (!Directory.Exists (precdir))
				Directory.CreateDirectory (precdir);
			precdir = Path.Combine (precdir, configuration.Name);
			if (!Directory.Exists (precdir))
				Directory.CreateDirectory (precdir);
			
			PrecompileHeaders (projectFiles, configuration, args);
			
			foreach (ProjectFile f in projectFiles) {
				if (f.Subtype == Subtype.Directory) continue;
				
				if (f.BuildAction == BuildAction.Compile) {
					if (configuration.UseCcache || NeedsCompiling (f))
						res = DoCompilation (f, args, packages, monitor, cr, configuration.UseCcache);
				}
				else
					res = true;
				
				if (!res) break;
			}

			if (res) {
				switch (configuration.CompileTarget)
				{
				case CBinding.CompileTarget.Bin:
					MakeBin (
						projectFiles, packages, configuration, cr, monitor, outputName);
					break;
				case CBinding.CompileTarget.StaticLibrary:
					MakeStaticLibrary (
						projectFiles, monitor, outputName);
					break;
				case CBinding.CompileTarget.SharedLibrary:
					MakeSharedLibrary (
						projectFiles, packages, configuration, cr, monitor, outputName);
					break;
				}
			}
			
			return new DefaultCompilerResult (cr, "");
		}
		
		public override bool SupportsCcache {
			get { return true; }
		}
		
		public override string GetCompilerFlags (CProjectConfiguration configuration)
		{
			StringBuilder args = new StringBuilder ();
			string precdir = Path.Combine (configuration.SourceDirectory, ".prec");
			
			CCompilationParameters cp =
				(CCompilationParameters)configuration.CompilationParameters;
			
			if (configuration.DebugMode)
				args.Append ("-g ");
			
			if (configuration.CompileTarget == CBinding.CompileTarget.SharedLibrary)
				args.Append ("-fPIC ");
			
			switch (cp.WarningLevel)
			{
			case WarningLevel.None:
				args.Append ("-w ");
				break;
			case WarningLevel.Normal:
				// nothing
				break;
			case WarningLevel.All:
				args.Append ("-Wall ");
				break;
			}
			
			args.Append ("-O" + cp.OptimizationLevel + " ");
			
			if (cp.ExtraCompilerArguments != null && cp.ExtraCompilerArguments.Length > 0) {
				string extraCompilerArgs = cp.ExtraCompilerArguments.Replace ('\n', ' ');
				args.Append (extraCompilerArgs + " ");
			}
			
			if (cp.DefineSymbols != null && cp.DefineSymbols.Length > 0)
				args.Append (ProcessDefineSymbols (cp.DefineSymbols) + " ");
			
			if (configuration.Includes != null)
				foreach (string inc in configuration.Includes)
					args.Append ("-I" + inc + " ");
			
			args.Append ("-I" + precdir);
			
			return args.ToString ();
		}
		
		public override string GetDefineFlags (CProjectConfiguration configuration)
		{
			string defines = ((CCompilationParameters)configuration.CompilationParameters).DefineSymbols;
			return ProcessDefineSymbols (defines);
		}
		
		private bool NeedsCompiling (ProjectFile file)
		{
			string objectFile = Path.ChangeExtension (file.Name, ".o");
					
			if (!File.Exists (objectFile))
				return true;
			
			string[] dependedOnFiles = DependedOnFiles (file);
			
			if (dependedOnFiles == null) {
				return true;
			}
			
			DateTime lastObjectTime = File.GetLastWriteTime (objectFile);
			
			try {
				foreach (string depfile in dependedOnFiles) {
					if (File.GetLastWriteTime (depfile) > lastObjectTime) {
						return true;
					}
				}
			} catch (IOException e) {
				// This means the dependency file is telling us our source file
				// depends on a file that no longer exists, all this means is that 
				// the dependency file is outdated. We should just ignore this
				// since the dependency file will be automatically updated when
				// the source file is compiled.
				e.ToString (); // suppress warning.
			}
			
			return false;
		}
		
		/// <summary>
		/// Returns an array of depended on files or null if the
		/// file containing the depended on files (.d) does does not exist.
		/// </summary>
		private string[] DependedOnFiles (ProjectFile file)
		{
			List<string> dependencies = new List<string> ();
			string dependenciesFile = Path.ChangeExtension (file.Name, ".d");
			
			if (!File.Exists (dependenciesFile))
				return null;
			
			// It always depends on itself ;)
			dependencies.Add (file.Name);
			
			string temp;
			StringBuilder output = new StringBuilder ();
			
			using (StreamReader reader = new StreamReader (dependenciesFile)) {
				while ((temp = reader.ReadLine ()) != null) {
					output.Append (temp);
				}
			}
			
			string[] lines = output.ToString ().Split ('\\');
			
			for (int i = 0; i < lines.Length; i++) {
				string[] files = lines[i].Split (' ');
				// first line contains the rule (eg. file.o: dep1.c dep2.h ...) and we must skip it
				// and we skip the *.cpp or *.c etc. too
				for (int j = 0; j < files.Length; j++) {
					if (j == 0 || j == 1) continue;
					
					string depfile = files[j].Trim ();
					
					if (!string.IsNullOrEmpty (depfile))
						dependencies.Add (depfile);
				}
			}
			
			return dependencies.ToArray ();
		}
		
		private void PrecompileHeaders (ProjectFileCollection projectFiles,
		                                CProjectConfiguration configuration,
		                                string args)
		{
			foreach (ProjectFile file in projectFiles) {
				if (file.Subtype == Subtype.Code && CProject.IsHeaderFile (file.Name)) {
					string precomp = Path.Combine (configuration.SourceDirectory, ".prec");
					precomp = Path.Combine (precomp, configuration.Name);
					precomp = Path.Combine (precomp, Path.GetFileName (file.Name) + ".ghc");
					
					if (!File.Exists (precomp)) {
						DoPrecompileHeader (file, precomp, args);
						continue;
					}
					
					if (configuration.UseCcache || File.GetLastWriteTime (file.Name) > File.GetLastWriteTime (precomp)) {
						DoPrecompileHeader (file, precomp, args);
					}
				}
			}
		}
		
		private void DoPrecompileHeader (ProjectFile file, string output, string args)
		{
			string completeArgs = String.Format("{0} {1} -o {2}",
						                        file.Name,
						                        args,
						                        output);
			
			ProcessWrapper p = Runtime.ProcessService.StartProcess (compilerCommand, completeArgs, null, null);
			p.WaitForExit ();
			p.Close ();
		}
		
		private void MakeBin(ProjectFileCollection projectFiles,
		                     ProjectPackageCollection packages,
		                     CProjectConfiguration configuration,
		                     CompilerResults cr,
		                     IProgressMonitor monitor, string outputName)
		{			
			if (!NeedsUpdate (projectFiles, outputName)) return;
			
			string objectFiles = StringArrayToSingleString (ObjectFiles (projectFiles));
			string pkgargs = GeneratePkgLinkerArgs (packages);
			StringBuilder args = new StringBuilder ();
			CCompilationParameters cp =
				(CCompilationParameters)configuration.CompilationParameters;
			
			if (cp.ExtraLinkerArguments != null && cp.ExtraLinkerArguments.Length > 0) {
				string extraLinkerArgs = cp.ExtraLinkerArguments.Replace ('\n', ' ');
				args.Append (extraLinkerArgs + " ");
			}
			
			if (configuration.LibPaths != null)
				foreach (string libpath in configuration.LibPaths)
					args.Append ("-L" + libpath + " ");
			
			if (configuration.Libs != null)
				foreach (string lib in configuration.Libs)
					args.Append ("-l" + lib + " ");
			
			monitor.Log.WriteLine ("Generating binary...");
			
			string linker_args = string.Format ("-o {0} {1} {2} {3}",
			    outputName, objectFiles, args.ToString (), pkgargs);
			
			monitor.Log.WriteLine ("using: " + linkerCommand + " " + linker_args);
			
			ProcessWrapper p = Runtime.ProcessService.StartProcess (linkerCommand, linker_args, null, null);
			
			p.WaitForExit ();
			
			string line;
			StringWriter error = new StringWriter ();
			
			while ((line = p.StandardError.ReadLine ()) != null)
				error.WriteLine (line);
			
			monitor.Log.WriteLine (error.ToString ());
			
			ParseCompilerOutput (error.ToString (), cr);
			
			error.Close ();
			p.Close ();
			
			ParseLinkerOutput (error.ToString (), cr);
		}
		
		private void MakeStaticLibrary (ProjectFileCollection projectFiles,
		                                IProgressMonitor monitor, string outputName)
		{
			if (!NeedsUpdate (projectFiles, outputName)) return;
			
			string objectFiles = StringArrayToSingleString (ObjectFiles (projectFiles));
			
			monitor.Log.WriteLine ("Generating static library...");
			monitor.Log.WriteLine ("using: ar rcs " + outputName + " " + objectFiles);
			
			Process p = Runtime.ProcessService.StartProcess (
				"ar", "rcs " + outputName + " " + objectFiles,
				null, null);
			p.WaitForExit ();
			p.Close ();
		}
		
		private void MakeSharedLibrary(ProjectFileCollection projectFiles,
		                               ProjectPackageCollection packages,
		                               CProjectConfiguration configuration,
		                               CompilerResults cr,
		                               IProgressMonitor monitor, string outputName)
		{
			if (!NeedsUpdate (projectFiles, outputName)) return;
			
			string objectFiles = StringArrayToSingleString (ObjectFiles (projectFiles));
			string pkgargs = GeneratePkgLinkerArgs (packages);
			StringBuilder args = new StringBuilder ();
			CCompilationParameters cp =
				(CCompilationParameters)configuration.CompilationParameters;
			
			if (cp.ExtraLinkerArguments != null && cp.ExtraLinkerArguments.Length > 0) {
				string extraLinkerArgs = cp.ExtraLinkerArguments.Replace ('\n', ' ');
				args.Append (extraLinkerArgs + " ");
			}
			
			if (configuration.LibPaths != null)
				foreach (string libpath in configuration.LibPaths)
					args.Append ("-L" + libpath + " ");
			
			if (configuration.Libs != null)
				foreach (string lib in configuration.Libs)
					args.Append ("-l" + lib + " ");
			
			monitor.Log.WriteLine ("Generating shared object...");
			
			string linker_args = string.Format ("-shared -o {0} {1} {2} {3}",
			    outputName, objectFiles, args.ToString (), pkgargs);
			
			monitor.Log.WriteLine ("using: " + linkerCommand + " " + linker_args);
			
			ProcessWrapper p = Runtime.ProcessService.StartProcess (linkerCommand, linker_args, null, null);

			p.WaitForExit ();
			
			string line;
			StringWriter error = new StringWriter ();
			
			while ((line = p.StandardError.ReadLine ()) != null)
				error.WriteLine (line);
			
			monitor.Log.WriteLine (error.ToString ());
			
			ParseCompilerOutput (error.ToString (), cr);
			
			error.Close ();
			p.Close ();
			
			ParseLinkerOutput (error.ToString (), cr);
		}
		
		private string ProcessDefineSymbols (string symbols)
		{
			StringBuilder processed = new StringBuilder (symbols);
			
			// Take care of multi adyacent spaces
			for (int i = 0; i < processed.Length; i++) {
				if (i + 1 < processed.Length &&
				    processed[i] == ' ' &&
				    processed[i + 1] == ' ') {
					processed.Remove (i--, 1);
				}
			}
			
			return processed.ToString ()
				            .Trim ()
				            .Replace (" ", " -D")
				            .Insert (0, "-D");
		}
		
		/// <summary>
		/// Compiles a single source file into object code
		/// and creates a file with it's dependencies.
		/// </summary>
		private bool DoCompilation (ProjectFile file, string args,
		                            ProjectPackageCollection packages,
		                            IProgressMonitor monitor,
		                            CompilerResults cr,
		                            bool use_ccache)
		{			
			string outputName = Path.ChangeExtension (file.Name, ".o");
			string pkgargs = GeneratePkgCompilerArgs (packages);
			
			string compiler_args = string.Format ("{0} -MMD {1} {2} -c -o {3} {4}",
			    (use_ccache ? compilerCommand : string.Empty), file.Name, args, outputName, pkgargs);
			
			monitor.Log.WriteLine ("using: " + compilerCommand + " " + compiler_args);
			
			ProcessWrapper p = Runtime.ProcessService.StartProcess (
			    (use_ccache ? "ccache" : compilerCommand), compiler_args, null, null);

			p.WaitForExit ();
			
			string line;
			
			StringWriter error = new StringWriter ();
			
			while ((line = p.StandardError.ReadLine ()) != null)
				error.WriteLine (line);
			
			monitor.Log.WriteLine (error.ToString ());
			
			ParseCompilerOutput (error.ToString (), cr);
			
			error.Close ();
			
			bool result = p.ExitCode == 0;
			p.Close ();
			
			return result;
		}
		
		private string[] ObjectFiles (ProjectFileCollection projectFiles)
		{
			List<string> objectFiles = new List<string> ();
			
			foreach (ProjectFile f in projectFiles) {
				if (f.BuildAction == BuildAction.Compile) {
					objectFiles.Add (Path.ChangeExtension (f.Name, ".o"));
				}
			}
			
			return objectFiles.ToArray ();
		}
		
		private string StringArrayToSingleString (string[] array)
		{
			StringBuilder str = new StringBuilder ();
			
			foreach (string s in array) {
				str.Append (s + " ");
			}
			
			return str.ToString ();
		}
		
		private bool NeedsUpdate (ProjectFileCollection projectFiles,
		                          string target)
		{
			if (!File.Exists (target))
				return true;
			
			foreach (string obj in ObjectFiles (projectFiles))
				if (File.GetLastWriteTime (obj) > File.GetLastWriteTime (target))
					return true;
			
			return false;
		}
		
		protected override void ParseCompilerOutput (string errorString, CompilerResults cr)
		{
			TextReader reader = new StringReader (errorString);
			string next;
			
			while ((next = reader.ReadLine ()) != null) {
				CompilerError error = CreateErrorFromErrorString (next);
				if (error != null)
					cr.Errors.Add (error);
			}
			
			reader.Close ();
		}
		
		private static Regex withColRegex = new Regex (
		    @"^\s*(?<file>.*):(?<line>\d*):(?<column>\d*):\s*(?<level>.*)\s*:\s(?<message>.*)",
		    RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private static Regex noColRegex = new Regex (
		    @"^\s*(?<file>.*):(?<line>\d*):\s*(?<level>.*)\s*:\s(?<message>.*)",
		    RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		
		private CompilerError CreateErrorFromErrorString (string errorString)
		{
			CompilerError error = new CompilerError ();
			
			Match withColMatch = withColRegex.Match (errorString);
			
			if (withColMatch.Success)
			{
				error.FileName = withColMatch.Groups["file"].Value;
				error.Line = int.Parse (withColMatch.Groups["line"].Value);
				error.Column = int.Parse (withColMatch.Groups["column"].Value);
				error.IsWarning = withColMatch.Groups["level"].Value.Equals ("warning");
				error.ErrorText = withColMatch.Groups["message"].Value;
				
				return error;
			}
			
			Match noColMatch = noColRegex.Match (errorString);
			
			if (noColMatch.Success)
			{
				error.FileName = noColMatch.Groups["file"].Value;
				error.Line = int.Parse (noColMatch.Groups["line"].Value);
				error.IsWarning = noColMatch.Groups["level"].Value.Equals ("warning");
				error.ErrorText = noColMatch.Groups["message"].Value;
				
				return error;
			}
			
			return null;
		}
		
		protected override void ParseLinkerOutput (string errorString, CompilerResults cr)
		{
			TextReader reader = new StringReader (errorString);
			string next;
			
			while ((next = reader.ReadLine ()) != null) {
				CompilerError error = CreateLinkerErrorFromErrorString (next);
				if (error != null)
					cr.Errors.Add (error);
			}
			
			reader.Close ();
		}
		
		// FIXME: needs to be improved UPDATE: or does it...?
		private CompilerError CreateLinkerErrorFromErrorString (string errorString)
		{
			CompilerError error = new CompilerError ();
			
			error.ErrorText = errorString;
			
			return error;
		}
	}
}
