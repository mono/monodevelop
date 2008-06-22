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
		bool appsChecked;
		bool compilerFound;
		bool linkerFound;
		
		public override BuildResult Compile (
			ProjectFileCollection projectFiles,
		    ProjectPackageCollection packages,
		    CProjectConfiguration configuration,
		    IProgressMonitor monitor)
		{
			if (!appsChecked) {
				appsChecked = true;
				compilerFound = CheckApp (compilerCommand);
				linkerFound = CheckApp (linkerCommand);
			}
				
			if (!compilerFound) {
				BuildResult cres = new BuildResult ();
				cres.AddError ("Compiler not found: " + compilerCommand);
				return cres;
			}
			
			if (!linkerFound) {
				BuildResult cres = new BuildResult ();
				cres.AddError ("Linker not found: " + linkerCommand);
				return cres;
			}
			
			CompilerResults cr = new CompilerResults (new TempFileCollection ());
			bool success = true;
			string compilerArgs = GetCompilerFlags (configuration) + " " + GeneratePkgCompilerArgs (packages);
			
			string outputName = Path.Combine (configuration.OutputDirectory,
			                                  configuration.CompiledOutputName);
			
			// Precompile header files and place them in .prec/<config_name>/
			if (configuration.PrecompileHeaders) {
				string precDir = Path.Combine (configuration.SourceDirectory, ".prec");
				string precConfigDir = Path.Combine (precDir, configuration.Id);
				if (!Directory.Exists (precDir))
					Directory.CreateDirectory (precDir);
				if (!Directory.Exists (precConfigDir))
					Directory.CreateDirectory (precConfigDir);
				
				if (!PrecompileHeaders (projectFiles, configuration, compilerArgs, monitor, cr))
					success = false;
			} else {
				//old headers could interfere with the build
				CleanPrecompiledHeaders (configuration);
			}
			
			//compile source to object files
			monitor.BeginTask (GettextCatalog.GetString ("Compiling source to object files"), 1);
			foreach (ProjectFile f in projectFiles) {
				if (!success) break;
				if (f.Subtype == Subtype.Directory || f.BuildAction != BuildAction.Compile || CProject.IsHeaderFile (f.FilePath))
					continue;
				
				if (configuration.UseCcache || NeedsCompiling (f))
					success = DoCompilation (f, compilerArgs, monitor, cr, configuration.UseCcache);
			}
			if (success)
				monitor.Step (1);
			monitor.EndTask ();

			if (success) {
				switch (configuration.CompileTarget)
				{
				case CBinding.CompileTarget.Bin:
					MakeBin (projectFiles, packages, configuration, cr, monitor, outputName);
					break;
				case CBinding.CompileTarget.StaticLibrary:
					MakeStaticLibrary (projectFiles, monitor, outputName);
					break;
				case CBinding.CompileTarget.SharedLibrary:
					MakeSharedLibrary (projectFiles, packages, configuration, cr, monitor, outputName);
					break;
				}
			}
			
			return new BuildResult (cr, "");
		}
		
		public override bool SupportsCcache {
			get { return true; }
		}
		
		public override bool SupportsPrecompiledHeaders {
			get { return true; }
		}
		
		public override string GetCompilerFlags (CProjectConfiguration configuration)
		{
			StringBuilder args = new StringBuilder ();
			
			if (configuration.DebugMode)
				args.Append ("-g ");
			
			if (configuration.CompileTarget == CBinding.CompileTarget.SharedLibrary)
				args.Append ("-fPIC ");
			
			switch (configuration.WarningLevel)
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
			
			if (configuration.WarningsAsErrors)
				args.Append ("-Werror ");
			
			args.Append ("-O" + configuration.OptimizationLevel + " ");
			
			if (configuration.ExtraCompilerArguments != null && configuration.ExtraCompilerArguments.Length > 0) {
				string extraCompilerArgs = ExpandBacktickedParameters(configuration.ExtraCompilerArguments.Replace ('\n', ' '));
				args.Append (extraCompilerArgs + " ");
			}
			
			if (configuration.DefineSymbols != null && configuration.DefineSymbols.Length > 0)
				args.Append (ProcessDefineSymbols (configuration.DefineSymbols) + " ");
			
			if (configuration.Includes != null)
				foreach (string inc in configuration.Includes)
					args.Append ("-I\"" + StringParserService.Parse (inc) + "\" ");
			
			if (configuration.PrecompileHeaders) {
				string precdir = Path.Combine (configuration.SourceDirectory, ".prec");
				precdir = Path.Combine (precdir, configuration.Id);
				args.Append ("-I\"" + precdir + "\"");
			}
			
			return args.ToString ();
		}
		
		public override string GetDefineFlags (CProjectConfiguration configuration)
		{
			return ProcessDefineSymbols (configuration.DefineSymbols);
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
		
		private bool PrecompileHeaders (ProjectFileCollection projectFiles,
		                                CProjectConfiguration configuration,
		                                string args,
		                                IProgressMonitor monitor,
		                                CompilerResults cr)
		{
			monitor.BeginTask (GettextCatalog.GetString ("Precompiling headers"), 1);
			bool success = true;
			
			foreach (ProjectFile file in projectFiles) {
				if (file.Subtype == Subtype.Code && CProject.IsHeaderFile (file.Name)) {
					string precomp = Path.Combine (configuration.SourceDirectory, ".prec");
					precomp = Path.Combine (precomp, configuration.Id);
					precomp = Path.Combine (precomp, Path.GetFileName (file.Name) + ".ghc");
					if (file.BuildAction == BuildAction.Compile) {
						if (!File.Exists (precomp) || configuration.UseCcache || File.GetLastWriteTime (file.Name) > File.GetLastWriteTime (precomp)) {
							if (DoPrecompileHeader (file, precomp, args, monitor, cr) == false) {
								success = false;
								break;
							}
						}
					} else {
						//remove old files or they'll interfere with the build
						if (File.Exists (precomp))
							File.Delete (precomp);
					}
				}
				
			}
			if (success)
				monitor.Step (1);
			monitor.EndTask ();
			return success;
		}
		
		private bool DoPrecompileHeader (ProjectFile file, string output, string args, IProgressMonitor monitor, CompilerResults cr)
		{
			string completeArgs = String.Format ("\"{0}\" {1} -o {2}", file.Name, args, output);
			string errorOutput;
			int exitCode = ExecuteCommand (compilerCommand, completeArgs, Path.GetDirectoryName (output), monitor, out errorOutput);
			ParseCompilerOutput (errorOutput, cr);
			return (exitCode == 0);
		}
		
		private void MakeBin (ProjectFileCollection projectFiles,
		                     ProjectPackageCollection packages,
		                     CProjectConfiguration configuration,
		                     CompilerResults cr,
		                     IProgressMonitor monitor, string outputName)
		{			
			if (!NeedsUpdate (projectFiles, outputName)) return;
			
			string objectFiles = string.Join (" ", ObjectFiles (projectFiles, true));
			string pkgargs = GeneratePkgLinkerArgs (packages);
			StringBuilder args = new StringBuilder ();
			
			if (configuration.ExtraLinkerArguments != null && configuration.ExtraLinkerArguments.Length > 0) {
				string extraLinkerArgs = ExpandBacktickedParameters(configuration.ExtraLinkerArguments.Replace ('\n', ' '));
				args.Append (extraLinkerArgs + " ");
			}
			
			if (configuration.LibPaths != null)
				foreach (string libpath in configuration.LibPaths)
					args.Append ("-L\"" + StringParserService.Parse (libpath) + "\" ");
			
			if (configuration.Libs != null)
				foreach (string lib in configuration.Libs)
					args.Append ("-l\"" + lib + "\" ");
			
			string linker_args = string.Format ("-o \"{0}\" {1} {2} {3}",
			    outputName, objectFiles, args.ToString (), pkgargs);
			
			monitor.BeginTask (GettextCatalog.GetString ("Generating binary \"{0}\" from object files", Path.GetFileName (outputName)), 1);
			
			string errorOutput;
			int exitCode = ExecuteCommand (linkerCommand, linker_args, Path.GetDirectoryName (outputName), monitor, out errorOutput);
			if (exitCode == 0)
				monitor.Step (1);
			monitor.EndTask ();
			
			ParseCompilerOutput (errorOutput, cr);
			ParseLinkerOutput (errorOutput, cr);
		}
		
		private void MakeStaticLibrary (ProjectFileCollection projectFiles,
		                                IProgressMonitor monitor, string outputName)
		{
			if (!NeedsUpdate (projectFiles, outputName)) return;
			
			string objectFiles = string.Join (" ", ObjectFiles (projectFiles, true));
			string args = string.Format ("rcs \"{0}\" {1}", outputName, objectFiles);
			
			monitor.BeginTask (GettextCatalog.GetString ("Generating static library {0} from object files", Path.GetFileName (outputName)), 1);
			
			string errorOutput;
			int exitCode = ExecuteCommand ("ar", args, Path.GetDirectoryName (outputName), monitor, out errorOutput);
			if (exitCode == 0)
				monitor.Step (1);
			monitor.EndTask ();
		}
		
		private void MakeSharedLibrary(ProjectFileCollection projectFiles,
		                               ProjectPackageCollection packages,
		                               CProjectConfiguration configuration,
		                               CompilerResults cr,
		                               IProgressMonitor monitor, string outputName)
		{
			if (!NeedsUpdate (projectFiles, outputName)) return;
			
			string objectFiles = string.Join (" ", ObjectFiles (projectFiles, true));
			string pkgargs = GeneratePkgLinkerArgs (packages);
			StringBuilder args = new StringBuilder ();
			
			if (configuration.ExtraLinkerArguments != null && configuration.ExtraLinkerArguments.Length > 0) {
				string extraLinkerArgs = ExpandBacktickedParameters(configuration.ExtraLinkerArguments.Replace ('\n', ' '));
				args.Append (extraLinkerArgs + " ");
			}
			
			if (configuration.LibPaths != null)
				foreach (string libpath in configuration.LibPaths)
					args.Append ("-L\"" + StringParserService.Parse (libpath) + "\" ");
			
			if (configuration.Libs != null)
				foreach (string lib in configuration.Libs)
					args.Append ("-l\"" + lib + "\" ");
			
			string linker_args = string.Format ("-shared -o \"{0}\" {1} {2} {3}",
			    outputName, objectFiles, args.ToString (), pkgargs);
			
			monitor.BeginTask (GettextCatalog.GetString ("Generating shared object \"{0}\" from object files", Path.GetFileName (outputName)), 1);
			
			string errorOutput;
			int exitCode = ExecuteCommand (linkerCommand , linker_args, Path.GetDirectoryName (outputName), monitor, out errorOutput);
			if (exitCode == 0)
				monitor.Step (1);
			monitor.EndTask ();
			
			ParseCompilerOutput (errorOutput, cr);
			ParseLinkerOutput (errorOutput, cr);
		}
		
		int ExecuteCommand (string command, string args, string baseDirectory, IProgressMonitor monitor, out string errorOutput)
		{
			errorOutput = string.Empty;
			int exitCode = -1;
			
			StringWriter swError = new StringWriter ();
			LogTextWriter chainedError = new LogTextWriter ();
			chainedError.ChainWriter (monitor.Log);
			chainedError.ChainWriter (swError);
			
			monitor.Log.WriteLine ("{0} {1}", command, args);
			
			AggregatedOperationMonitor operationMonitor = new AggregatedOperationMonitor (monitor);
			
			try {
				ProcessWrapper p = Runtime.ProcessService.StartProcess (command, args, baseDirectory, monitor.Log, chainedError, null);
				operationMonitor.AddOperation (p); //handles cancellation
				
				p.WaitForOutput ();
				errorOutput = swError.ToString ();
				exitCode = p.ExitCode;
				p.Dispose ();
				
				if (monitor.IsCancelRequested) {
					monitor.Log.WriteLine (GettextCatalog.GetString ("Build cancelled"));
					monitor.ReportError (GettextCatalog.GetString ("Build cancelled"), null);
					if (exitCode == 0)
						exitCode = -1;
				}
			} finally {
				chainedError.Close ();
				swError.Close ();
				operationMonitor.Dispose ();
			}
			
			return exitCode;
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
		                            IProgressMonitor monitor,
		                            CompilerResults cr,
		                            bool use_ccache)
		{			
			string outputName = Path.ChangeExtension (file.Name, ".o");
			
			string compiler_args = string.Format ("{0} -MMD \"{1}\" {2} -c -o \"{3}\"",
			    (use_ccache ? compilerCommand : string.Empty), file.Name, args, outputName);
			
			string errorOutput;
			int exitCode = ExecuteCommand ((use_ccache ? "ccache" : compilerCommand), compiler_args, Path.GetDirectoryName (outputName), monitor, out errorOutput);
			
			ParseCompilerOutput (errorOutput, cr);
			return exitCode == 0;
		}
		
		/// <summary>
		/// Gets the files that get compiled into object code.
		/// </summary>
		/// <param name="projectFiles">
		/// A <see cref="ProjectFileCollection"/>
		/// The project's files, extracts from here the files that get compiled into object code.
		/// </param>
		/// <param name="withQuotes">
		/// A <see cref="System.Boolean"/>
		/// If true, it will surround each object file with quotes 
		/// so that gcc has no problem with paths that contain spaces.
		/// </param>
		/// <returns>
		/// An array of strings, each string is the name of a file
		/// that will get compiled into object code. The file name
		/// will already have the .o extension.
		/// </returns>
		private string[] ObjectFiles (ProjectFileCollection projectFiles, bool withQuotes)
		{
			List<string> objectFiles = new List<string> ();
			
			foreach (ProjectFile f in projectFiles) {
				if (f.BuildAction == BuildAction.Compile) {
					if (!withQuotes)
						objectFiles.Add (Path.ChangeExtension (f.Name, ".o"));
					else
						objectFiles.Add ("\"" + Path.ChangeExtension (f.Name, ".o") + "\"");
				}
			}
			
			return objectFiles.ToArray ();
		}
		
		public override void Clean (ProjectFileCollection projectFiles, CProjectConfiguration configuration, IProgressMonitor monitor)
		{
			//clean up object files
			foreach (ProjectFile file in projectFiles) {
				if (file.BuildAction == BuildAction.Compile) {
					string oFile = Path.ChangeExtension (file.Name, ".o");
					if (File.Exists (oFile))
						File.Delete (oFile);
					string dFile = Path.ChangeExtension (file.Name, ".d");
					if (File.Exists (dFile))
						File.Delete (dFile);
				}
			}
			
			CleanPrecompiledHeaders (configuration);
		}
		
		void CleanPrecompiledHeaders (CProjectConfiguration configuration)
		{
			if (string.IsNullOrEmpty (configuration.SourceDirectory))
			    return;
			
			string precDir = Path.Combine (configuration.SourceDirectory, ".prec");			

			if (Directory.Exists (precDir))
				Directory.Delete (precDir, true);
		}
		
		private bool NeedsUpdate (ProjectFileCollection projectFiles,
		                          string target)
		{
			if (!File.Exists (target))
				return true;
			
			foreach (string obj in ObjectFiles (projectFiles, false))
				if (File.GetLastWriteTime (obj) > File.GetLastWriteTime (target))
					return true;
			
			return false;
		}
		
		protected override void ParseCompilerOutput (string errorString, CompilerResults cr)
		{
			TextReader reader = new StringReader (errorString);
			string next;
				
			while ((next = reader.ReadLine ()) != null) {
				CompilerError error = CreateErrorFromErrorString (next, reader);
				if (error != null)
					cr.Errors.Insert (0, error);
			}
			
			reader.Close ();
		}
		
		private static Regex withColRegex = new Regex (
		    @"^\s*(?<file>.*):(?<line>\d*):(?<column>\d*):\s*(?<level>.*)\s*:\s(?<message>.*)",
		    RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private static Regex noColRegex = new Regex (
		    @"^\s*(?<file>.*):(?<line>\d*):\s*(?<level>.*)\s*:\s(?<message>.*)",
		    RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		private static Regex linkerRegex = new Regex (
		    @"^\s*(?<file>[^:]*):(?<line>\d*):\s*(?<message>[^:]*)",
		    RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		
		private CompilerError CreateErrorFromErrorString (string errorString, TextReader reader)
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
				
				// Multi-line error message? attempt to parse it into a single error.
				if (error.ErrorText.StartsWith ("(") && !error.ErrorText.EndsWith (")"))
				{
					string error_continued = reader.ReadLine ();
					
					// Unexpected result?  just return the error.
					if (error_continued == null)
						return error;
					
					// Get the rest of the error message.
					CompilerError error_fragment = CreateErrorFromErrorString (error_continued, reader);
					if (error_fragment != null)
					{
						error.ErrorText += " " + error_fragment.ErrorText;
					}
				}
				
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
					cr.Errors.Insert (0, error);
			}
			
			reader.Close ();
		}
		
		private CompilerError CreateLinkerErrorFromErrorString (string errorString)
		{
			CompilerError error = new CompilerError ();
			
			Match linkerMatch = linkerRegex.Match (errorString);
			
			if (linkerMatch.Success)
			{
				error.FileName = linkerMatch.Groups["file"].Value;
				error.Line = int.Parse (linkerMatch.Groups["line"].Value);
				error.ErrorText = linkerMatch.Groups["message"].Value;
				
				return error;
			}
			
			return null;
		}

		// expands backticked portions of the parameter-list using "sh" and "echo"
		public string ExpandBacktickedParameters(string tmp)
		{
			string parameters = "-c \"echo -n " + tmp + "\"";
			Process p = new Process();
			
			p.StartInfo.FileName = "sh";
			p.StartInfo.Arguments = parameters;
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.RedirectStandardOutput = true;
			p.Start();
			p.WaitForExit();

			return p.StandardOutput.ReadToEnd();
		}
		
		bool CheckApp (string app)
		{
			try {
				ProcessWrapper p = Runtime.ProcessService.StartProcess (app, "--version", null, null);
				p.WaitForOutput ();
				return true;
			} catch {
				return false;
			}
		}
	}
}
