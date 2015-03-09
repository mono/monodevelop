//
// ValaCompiler.cs: abstract class that provides some basic implementation for ICompiler
//
// Authors:
//  Levi Bard <taktaktaktaktaktaktaktaktaktak@gmail.com> 
//
// Copyright (C) 2008 Levi Bard
// Based on CBinding by Marcos David Marin Amador <MarcosMarin@gmail.com>
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
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Diagnostics;

using Mono.Addins;

using MonoDevelop.Core;
 
using MonoDevelop.Core.Execution;
using MonoDevelop.Ide.Gui;
using MonoDevelop.Projects;
using MonoDevelop.Core.ProgressMonitoring;

namespace MonoDevelop.ValaBinding
{
	[Extension ("/ValaBinding/Compilers")]
	public class ValaCompiler : ICompiler
	{
		protected string compilerCommand;
				
		bool compilerFound;
		bool appsChecked;
		
		public ValaCompiler ()
		{
			compilerCommand = "valac";
		}
		
		public string Name {
			get{ return "valac"; }
		}
			
		public string CompilerCommand {
			get { return compilerCommand; }
		}

		/// <summary>
		/// Compile the project
		/// </summary>
		public BuildResult Compile(ValaProject project,
			ConfigurationSelector solutionConfiguration,
			IProgressMonitor monitor)
		{
            var projectFiles = project.Files;
            var packages = project.Packages;
            var projectConfiguration = (ValaProjectConfiguration)project.GetConfiguration(solutionConfiguration);

			if (!appsChecked)
            {
                // Check for compiler
				appsChecked = true;
				compilerFound = CheckApp(compilerCommand);
			}

			if (!compilerFound)
            {
                // No compiler!
				BuildResult cres = new BuildResult ();
				cres.AddError("Compiler not found: " + compilerCommand);
				return cres;
			}

			// Build compiler params string
			string compilerArgs = GetCompilerFlags(projectConfiguration) + " " +
                GeneratePkgCompilerArgs(packages, solutionConfiguration);
			
			// Build executable name
			string outputName = Path.Combine(
                FileService.RelativeToAbsolutePath(projectConfiguration.SourceDirectory, projectConfiguration.OutputDirectory),
                projectConfiguration.CompiledOutputName);
			
			monitor.BeginTask(GettextCatalog.GetString ("Compiling source"), 1);

            CompilerResults cr = new CompilerResults(new TempFileCollection());
            bool success = DoCompilation(projectFiles, compilerArgs, outputName, monitor, cr);

			GenerateDepfile(projectConfiguration, packages);

            if (success)
            {
                monitor.Step(1);
            }
			monitor.EndTask();

			return new BuildResult(cr, "");
		}
		
		string ICompiler.GetCompilerFlags(ValaProjectConfiguration configuration)
		{
			return ValaCompiler.GetCompilerFlags(configuration);
		}
		
		/// <summary>
		/// Generates compiler args for the current settings
		/// </summary>
		/// <param name="configuration">
		/// Project configuration
		/// <see cref="ValaProjectConfiguration"/>
		/// </param>
		/// <returns>
		/// A compiler-interpretable string
		/// <see cref="System.String"/>
		/// </returns>
        public static string GetCompilerFlags(ValaProjectConfiguration configuration)
        {
            List<string> args = new List<string>();

            ValaCompilationParameters cp =
                (ValaCompilationParameters)configuration.CompilationParameters;

            var outputDir = FileService.RelativeToAbsolutePath(configuration.SourceDirectory,
                        configuration.OutputDirectory);
            var outputNameWithoutExt = Path.GetFileNameWithoutExtension(configuration.Output);

            args.Add(string.Format("-d \"{0}\"", outputDir));

            if (configuration.DebugMode)
                args.Add("-g");

            switch (configuration.CompileTarget)
            {
                case ValaBinding.CompileTarget.Bin:
                    if (cp.EnableMultithreading)
                    {
                        args.Add("--thread");
                    }
                    break;
                case ValaBinding.CompileTarget.SharedLibrary:
                    if (Platform.IsWindows)
                    {
                        args.Add(string.Format("--Xcc=\"-shared\" --Xcc=-I\"{0}\" -H \"{1}.h\" --library \"{1}\"", outputDir, outputNameWithoutExt));
                    }
                    else
                    {
                        args.Add(string.Format("--Xcc=\"-shared\" --Xcc=\"-fPIC\" --Xcc=\"-I'{0}'\" -H \"{1}.h\" --library \"{1}\"", outputDir, outputNameWithoutExt));
                    }
                    break;
            }

            // Valac will get these sooner or later			
            //			switch (cp.WarningLevel)
            //			{
            //			case WarningLevel.None:
            //				args.Append ("-w ");
            //				break;
            //			case WarningLevel.Normal:
            //				// nothing
            //				break;
            //			case WarningLevel.All:
            //				args.Append ("-Wall ");
            //				break;
            //			}
            //			
            //			if (cp.WarningsAsErrors)
            //				args.Append ("-Werror ");
            //			
            if (0 < cp.OptimizationLevel)
            {
                args.Add("--Xcc=\"-O" + cp.OptimizationLevel + "\"");
            }

            // global extra compiler arguments
            string globalExtraCompilerArgs = PropertyService.Get("ValaBinding.ExtraCompilerOptions", "");
            if (!string.IsNullOrEmpty(globalExtraCompilerArgs))
            {
                args.Add(globalExtraCompilerArgs.Replace(Environment.NewLine, " "));
            }

            // extra compiler arguments specific to project
            if (cp.ExtraCompilerArguments != null && cp.ExtraCompilerArguments.Length > 0)
            {
                args.Add(cp.ExtraCompilerArguments.Replace(Environment.NewLine, " "));
            }

            if (cp.DefineSymbols != null && cp.DefineSymbols.Length > 0)
            {
                args.Add(ProcessDefineSymbols(cp.DefineSymbols));
            }

            if (configuration.Includes != null)
            {
                foreach (string inc in configuration.Includes)
                {
                    var includeDir = FileService.RelativeToAbsolutePath(configuration.SourceDirectory, inc);
                    args.Add("--vapidir \"" + includeDir + "\"");
                }
            }

            if (configuration.Libs != null)
                foreach (string lib in configuration.Libs)
                    args.Add("--pkg \"" + lib + "\"");

            return string.Join(" ", args.ToArray());
        }

        /// <summary>
        /// Generates compiler args for depended packages
        /// </summary>
        /// <param name="packages">
        /// The collection of packages for this project 
        /// <see cref="ProjectPackageCollection"/>
        /// </param>
        /// <returns>
        /// The string needed by the compiler to reference the necessary packages
        /// <see cref="System.String"/>
        /// </returns>
        public static string GeneratePkgCompilerArgs(ProjectPackageCollection packages,
            ConfigurationSelector solutionConfiguration)
        {
            if (packages == null || packages.Count < 1)
                return string.Empty;

            StringBuilder libs = new StringBuilder();

            foreach (ProjectPackage p in packages)
            {
                if (p.IsProject)
                {
                    var proj = p.GetProject();
                    var projectConfiguration = (ValaProjectConfiguration)proj.GetConfiguration(solutionConfiguration);
                    var outputDir = FileService.RelativeToAbsolutePath(projectConfiguration.SourceDirectory,
                        projectConfiguration.OutputDirectory);
                    var outputNameWithoutExt = Path.GetFileNameWithoutExtension(projectConfiguration.Output);
                    var vapifile = Path.Combine(outputDir, outputNameWithoutExt + ".vapi");
                    libs.AppendFormat(" --Xcc=-I\"{0}\" --Xcc=-L\"{0}\" --Xcc=-l\"{1}\" \"{2}\" ",
                        outputDir, outputNameWithoutExt, vapifile);
                }
                else
                {
                    libs.AppendFormat(" --pkg \"{0}\" ", p.Name);
                }
            }

            return libs.ToString();
        }
		
		/// <summary>
		/// Generates compilers flags for selected defines
		/// </summary>
		/// <param name="configuration">
		/// Project configuration
		/// <see cref="ValaProjectConfiguration"/>
		/// </param>
		/// <returns>
		/// A compiler-interpretable string
		/// <see cref="System.String"/>
		/// </returns>
		public string GetDefineFlags (ValaProjectConfiguration configuration)
		{
			string defines = ((ValaCompilationParameters)configuration.CompilationParameters).DefineSymbols;
			return ProcessDefineSymbols (defines);
		}
		
		/// <summary>
		/// Determines whether a file needs to be compiled
		/// </summary>
		/// <param name="file">
		/// The file in question
		/// <see cref="ProjectFile"/>
		/// </param>
		/// <returns>
		/// true if the file needs to be compiled
		/// <see cref="System.Boolean"/>
		/// </returns>
		private bool NeedsCompiling (ProjectFile file)
		{
			return true;
		}
		
		/// <summary>
		/// Executes a build command
		/// </summary>
		/// <param name="command">
		/// The executable to be launched
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="args">
		/// The arguments to command
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="baseDirectory">
		/// The directory in which the command will be executed
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="monitor">
		/// The progress monitor to be used
		/// <see cref="IProgressMonitor"/>
		/// </param>
		/// <param name="errorOutput">
		/// Error output will be stored here
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// The exit code of the command
		/// <see cref="System.Int32"/>
		/// </returns>
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
		
		/// <summary>
		/// Transforms a whitespace-delimited string of 
		/// symbols into a set of compiler flags
		/// </summary>
		/// <param name="symbols">
		/// A whitespace-delimited string of symbols, 
		/// e.g., "DEBUG MONODEVELOP"
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.String"/>
		/// </returns>
		private static string ProcessDefineSymbols (string symbols)
		{
			return ((null == symbols) || (0 == symbols.Length))?
				string.Empty:
				"-D " + Regex.Replace (symbols, " +", " -D ");
		}
		
		/// <summary>
		/// Compiles the project
		/// </summary>
		private bool DoCompilation (ProjectFileCollection projectFiles, string args,
									string outputName,
									IProgressMonitor monitor,
									CompilerResults cr)
		{
			StringBuilder filelist = new StringBuilder ();
			foreach (ProjectFile f in projectFiles) { 
				if (f.Subtype != Subtype.Directory && f.BuildAction == BuildAction.Compile) {
					filelist.AppendFormat ("\"{0}\" ", f.FilePath);
				}
			}/// Build file list

			string compiler_args = string.Format ("{0} {1} -o \"{2}\"",
				args, filelist.ToString (), Path.GetFileName (outputName));
			
			string errorOutput = string.Empty;
			int exitCode = ExecuteCommand (compilerCommand, compiler_args, Path.GetDirectoryName (outputName), monitor, out errorOutput);
			
			ParseCompilerOutput(errorOutput, cr, projectFiles);

            if (exitCode != 0 && cr.Errors.Count == 0)
            {
                // error isn't recognized but cannot ignore it because exitCode != 0
                cr.Errors.Add(new CompilerError() { ErrorText = errorOutput });
            }

			return exitCode == 0;
		}
		
		/// <summary>
		/// Cleans up intermediate files
		/// </summary>
		/// <param name="projectFiles">
		/// The project's files
		/// <see cref="ProjectFileCollection"/>
		/// </param>
		/// <param name="configuration">
		/// Project configuration
		/// <see cref="ValaProjectConfiguration"/>
		/// </param>
		/// <param name="monitor">
		/// The progress monitor to be used
		/// <see cref="IProgressMonitor"/>
		/// </param>
        public void Clean(ProjectFileCollection projectFiles, ValaProjectConfiguration configuration, IProgressMonitor monitor)
        {
            /// Clean up intermediate files
            /// These should only be generated for libraries, but we'll check for them in all cases
            foreach (ProjectFile file in projectFiles)
            {
                if (file.BuildAction == BuildAction.Compile)
                {
                    string cFile = Path.Combine(
                        FileService.RelativeToAbsolutePath(configuration.SourceDirectory, configuration.OutputDirectory),
                        Path.GetFileNameWithoutExtension(file.Name) + ".c");
                    if (File.Exists(cFile)) { File.Delete(cFile); }

                    string hFile = Path.Combine(
                        FileService.RelativeToAbsolutePath(configuration.SourceDirectory, configuration.OutputDirectory),
                        Path.GetFileNameWithoutExtension(file.Name) + ".h");
                    if (File.Exists(hFile)) { File.Delete(hFile); }
                }
            }

            string vapiFile = Path.Combine(
                FileService.RelativeToAbsolutePath(configuration.SourceDirectory, configuration.OutputDirectory),
                configuration.Output + ".vapi");
            if (File.Exists(vapiFile)) { File.Delete(vapiFile); }
        }
		
		/// <summary>
		/// Determines whether the target needs to be updated
		/// </summary>
		/// <param name="projectFiles">
		/// The project's files
		/// <see cref="ProjectFileCollection"/>
		/// </param>
		/// <param name="target">
		/// The target
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// true if target needs to be updated
		/// <see cref="System.Boolean"/>
		/// </returns>
		private bool NeedsUpdate (ProjectFileCollection projectFiles,
								  string target)
		{
			return true;
		}
		
		/// <summary>
		/// Parses a compiler output string into CompilerResults
		/// </summary>
		/// <param name="errorString">
		/// The string output by the compiler
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="cr">
		/// The CompilerResults into which to parse errorString
		/// <see cref="CompilerResults"/>
		/// </param>
        protected void ParseCompilerOutput(string errorString, CompilerResults cr, ProjectFileCollection projectFiles)
        {
            TextReader reader = new StringReader(errorString);
            string next;

            while ((next = reader.ReadLine()) != null)
            {
                CompilerError error = CreateErrorFromErrorString(next, projectFiles);
                // System.Console.WriteLine ("Creating error from string \"{0}\"", next);
                if (error != null)
                {
                    cr.Errors.Insert(0, error);
                    // System.Console.WriteLine ("Adding error");
                }
            }

            reader.Close();
        }
		
		/// Error regex for valac
		/// Sample output: "/home/user/project/src/blah.vala:23.5-23.5: error: syntax error, unexpected }, expecting ;"
		private static Regex errorRegex = new Regex (
			@"^\s*(?<file>.*):(?<line>\d*)\.(?<column>\d*)-\d*\.\d*: (?<level>[^:]*): (?<message>.*)",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		private static Regex gccRegex = new Regex (
		    @"^\s*(?<file>.*\.c):(?<line>\d*):((?<column>\d*):)?\s*(?<level>[^:]*):\s(?<message>.*)",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		/// Error regex for gnu linker - this could still be pertinent for vala
		private static Regex linkerRegex = new Regex (
			@"^\s*(?<file>[^:]*):(?<line>\d*):\s*(?<message>[^:]*)",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		/// <summary>
		/// Creates a compiler error from an output string
		/// </summary>
		/// <param name="errorString">
		/// The error string to be parsed
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A newly created CompilerError
		/// <see cref="CompilerError"/>
		/// </returns>
		private CompilerError CreateErrorFromErrorString (string errorString, ProjectFileCollection projectFiles)
		{
			Match errorMatch = null;
			foreach (Regex regex in new Regex[]{errorRegex, gccRegex})
				if ((errorMatch = regex.Match (errorString)).Success)
					break;

            if (!errorMatch.Success)
            {
                return null;
            }

            CompilerError error = new CompilerError();
			foreach (ProjectFile pf in projectFiles) {
				if (Path.GetFileName (pf.Name) == errorMatch.Groups["file"].Value) {
					error.FileName = pf.FilePath;
					break;
				}
			}// check for fully pathed file
			if (string.Empty == error.FileName) {
				error.FileName = errorMatch.Groups["file"].Value;
			}// fallback to exact match
			error.Line = int.Parse (errorMatch.Groups["line"].Value);
			if (errorMatch.Groups["column"].Success)
				error.Column = int.Parse (errorMatch.Groups["column"].Value);
			error.IsWarning = !errorMatch.Groups["level"].Value.Equals(GettextCatalog.GetString ("error"), StringComparison.Ordinal) &&
                !errorMatch.Groups["level"].Value.StartsWith("fatal error");
			error.ErrorText = errorMatch.Groups["message"].Value;
			
			return error;
		}
		
		/// <summary>
		/// Parses linker output into compiler results
		/// </summary>
		/// <param name="errorString">
		/// The linker output to be parsed
		/// <see cref="System.String"/>
		/// </param>
		/// <param name="cr">
		/// Results will be stored here
		/// <see cref="CompilerResults"/>
		/// </param>
		protected void ParseLinkerOutput (string errorString, CompilerResults cr)
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
		
		/// <summary>
		/// Creates a linker error from an output string
		/// </summary>
		/// <param name="errorString">
		/// The error string to be parsed
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// A newly created LinkerError
		/// <see cref="LinkerError"/>
		/// </returns>
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
		
		/// <summary>
		/// Check to see if we have a given app
		/// </summary>
		/// <param name="app">
		/// The app to check
		/// <see cref="System.String"/>
		/// </param>
		/// <returns>
		/// true if the app is found
		/// <see cref="System.Boolean"/>
		/// </returns>
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

        public void GenerateDepfile(ValaProjectConfiguration configuration, ProjectPackageCollection packages)
        {
            try
            {
                if (configuration.CompileTarget != CompileTarget.SharedLibrary) { return; }

                using (StreamWriter writer = new StreamWriter(Path.Combine(
                    FileService.RelativeToAbsolutePath(configuration.SourceDirectory, configuration.OutputDirectory),
                    Path.ChangeExtension(configuration.Output, ".deps"))))
                {
                    foreach (ProjectPackage package in packages)
                    {
                        writer.WriteLine(package.Name);
                    }
                }
            }
            catch { /* Don't care */ }
        }
	}
}
