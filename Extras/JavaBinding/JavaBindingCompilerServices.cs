//  JavaBindingCompilerServices.cs
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
using System.Diagnostics;
using System.IO;
using System.CodeDom.Compiler;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Projects;

namespace JavaBinding
{
	public class JavaBindingCompilerServices
	{
		string ikvmPath;
		
		public bool CanCompile (string fileName)
		{
			return Path.GetExtension(fileName) == ".java";
		}
		
		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			ikvmPath = JavaLanguageBinding.Properties.IkvmPath;
			if (ikvmPath != "" && !File.Exists (Path.Combine (ikvmPath, "ikvmc.exe"))) {
				ikvmPath = Path.Combine (ikvmPath, "bin");
				if (!File.Exists (Path.Combine (ikvmPath, "ikvmc.exe")))
					ikvmPath = "";
			}
			
			if (ikvmPath == "") {
				monitor.Log.WriteLine ("The Java addin has not been properly configured.");
				monitor.Log.WriteLine ("Please set the location of IKVM in the Java configuration section of MonoDevelop preferences.");
				CompilerResults cre = new CompilerResults (new TempFileCollection ());
				CompilerError err = new CompilerError ();
				err.ErrorText = "The Java addin has not been properly configured.";
				cre.Errors.Add (err);
				return new DefaultCompilerResult (cre, "");
			}

			JavaCompilerParameters compilerparameters = (JavaCompilerParameters) configuration.CompilationParameters;
			if (compilerparameters == null)
				compilerparameters = new JavaCompilerParameters ();
			
			string outdir = configuration.OutputDirectory;
			string options = "";

			string compiler = compilerparameters.CompilerPath;
			
			if (configuration.DebugMode) 
				options += " -g ";
			else
				options += " -g:none ";
			
			if (compilerparameters.Optimize)
				options += " -O ";
			
			if (compilerparameters.Deprecation)
				options += " -deprecation ";
			
			if (compilerparameters.GenWarnings)
				options += " -nowarn ";
			
			options += " -encoding utf8 ";
			
			string files  = "";
			
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
						case BuildAction.Compile:
							files = files + " \"" + finfo.Name + "\"";
						break;
					}
				}
			}

			string classpath = compilerparameters.ClassPath;
			string refClasspath = GenerateReferenceStubs (monitor, configuration, compilerparameters, references);
			if (refClasspath.Length > 0) {
				if (classpath.Length > 0) classpath += ":";
				classpath += refClasspath;
			}
			
			string args = "";
			
			if (compilerparameters.Compiler == JavaCompiler.Gcj)
				args = "-C ";
			
			//FIXME re-enable options
			//FIXME re-enable compilerPath
			if (classpath == "") {
				args += files + " -d " + outdir;			
			} else {
				args += " -classpath " + classpath + files + " -d " + outdir;
			}
			args = options + " " + args;
			//Console.WriteLine (args);

			CompilerResults cr = new CompilerResults (new TempFileCollection ());
			StringWriter output = new StringWriter ();
			StringWriter error = new StringWriter ();
			
			bool res = DoCompilation (monitor, compiler, args, configuration, compilerparameters, output, error);
			ParseJavaOutput (compilerparameters.Compiler, error.ToString(), cr);
			
			if (res) {
				output = new StringWriter ();
				error = new StringWriter ();
				CompileToAssembly (monitor, configuration, compilerparameters, references, output, error);
				ParseIkvmOutput (compilerparameters.Compiler, error.ToString(), cr);
			}
			
			return new DefaultCompilerResult (cr, "");
		}

		private string GenerateReferenceStubs (IProgressMonitor monitor, DotNetProjectConfiguration configuration, JavaCompilerParameters compilerparameters, ProjectReferenceCollection references)
		{
			monitor.Log.WriteLine ("Generating reference stubs ...");
			
			// Create stubs for referenced assemblies
			string ikvmstub = Path.Combine (ikvmPath, "ikvmstub.exe");
			
			string classpath = "";
			
			if (references != null) {
				foreach (ProjectReference lib in references) {
					foreach (string asm in lib.GetReferencedFileNames ()) {
						ProcessWrapper p = Runtime.ProcessService.StartProcess ("/bin/sh", "-c \"mono " + ikvmstub + " " + asm + "\"", configuration.OutputDirectory, null);
						p.WaitForExit ();
						
						if (classpath.Length > 0) classpath += ":";
						string name = Path.GetFileNameWithoutExtension (Path.GetFileName (asm));
						classpath += Path.Combine (configuration.OutputDirectory, name + ".jar");
					}
				}
			}
			return classpath;
		}
		
		private bool DoCompilation (IProgressMonitor monitor, string compiler, string args, DotNetProjectConfiguration configuration, JavaCompilerParameters compilerparameters, TextWriter output, TextWriter error)
		{
			LogTextWriter chainedError = new LogTextWriter ();
			chainedError.ChainWriter (monitor.Log);
			chainedError.ChainWriter (error);
			
			LogTextWriter chainedOutput = new LogTextWriter ();
			chainedOutput.ChainWriter (monitor.Log);
			chainedOutput.ChainWriter (output);
			
			monitor.Log.WriteLine ("Compiling Java source code ...");

			Process p = Runtime.ProcessService.StartProcess (compiler, args, null, chainedOutput, chainedError, null);
			p.WaitForExit ();
			return p.ExitCode == 0;
        }

		void CompileToAssembly (IProgressMonitor monitor, DotNetProjectConfiguration configuration, JavaCompilerParameters compilerparameters, ProjectReferenceCollection references, TextWriter output, TextWriter error)
		{
			monitor.Log.WriteLine ("Generating assembly ...");
			
			LogTextWriter chainedError = new LogTextWriter ();
			chainedError.ChainWriter (monitor.Log);
			chainedError.ChainWriter (error);
			
			LogTextWriter chainedOutput = new LogTextWriter ();
			chainedOutput.ChainWriter (monitor.Log);
			chainedOutput.ChainWriter (output);
			
			string outdir = configuration.OutputDirectory;
			string outclass = Path.Combine (outdir, configuration.OutputAssembly + ".class");
			string asm = Path.GetFileNameWithoutExtension (outclass);
			
			string opts = "-assembly:" + asm;
			
			switch (configuration.CompileTarget) {
				case CompileTarget.Exe:
					opts += " -target:exe";
					break;
				case CompileTarget.WinExe:
					opts += " -target:winexe";
					break;
				case CompileTarget.Library:
					opts += " -target:library";
					break;
			}
			
			if (configuration.DebugMode)
				opts += " -debug";

			opts += " -srcpath:" + configuration.SourceDirectory;
			
			if (references != null) {
				foreach (ProjectReference lib in references)
					foreach (string a in lib.GetReferencedFileNames ())
						opts += " -r:" + a;
			}
			
			string ikvmc = Path.Combine (ikvmPath, "ikvmc.exe");
		
			string args = String.Format ("-c \"mono {0} {1} {2}\"", ikvmc, "*.class", opts);
			Process p = Runtime.ProcessService.StartProcess ("/bin/sh", args, configuration.OutputDirectory, chainedOutput, chainedError, null);
			p.WaitForExit ();
		}
		
		void ParseJavaOutput (JavaCompiler jc, string errorStr, CompilerResults cr)
		{
			TextReader sr = new StringReader (errorStr);
			string next = sr.ReadLine ();
			while (next != null) 
			{
				CompilerError error = CreateJavaErrorFromString (jc, next);
				if (error != null) cr.Errors.Add (error);
				next = sr.ReadLine ();
			}
			sr.Close ();
		}
		
		// FIXME: the various java compilers will probably need to be parse on
		// their own and then ikvmc would need one as well
		private static CompilerError CreateJavaErrorFromString (JavaCompiler jc, string next)
		{
			CompilerError error = new CompilerError ();

			int errorCol = 0;
			string col = next.Trim ();
			if (col.Length == 1 && col == "^")
				errorCol = next.IndexOf ("^");

			int index1 = next.IndexOf (".java:");
			if (index1 < 0)
				return null;
		
			//string s1 = next.Substring (0, index1);
			string s2 = next.Substring (index1 + 6);									
			int index2  = s2.IndexOf (":");				
			int line = Int32.Parse (next.Substring (index1 + 6, index2));
			//error.IsWarning   = what[0] == "warning";
			//error.ErrorNumber = what[what.Length - 1];
						
			error.Column = errorCol;
			error.Line = line;
			error.ErrorText = next.Substring (index1 + index2 + 7);
			error.FileName = Path.GetFullPath (next.Substring (0, index1) + ".java"); //Path.GetFileName(filename);
			return error;
		}
		
		void ParseIkvmOutput (JavaCompiler jc, string errorStr, CompilerResults cr)
		{
			TextReader sr = new StringReader (errorStr);
			string next = sr.ReadLine ();
			while (next != null) 
			{
				CompilerError error = CreateIkvmErrorFromString (next);
				if (error != null) cr.Errors.Add (error);
				next = sr.ReadLine ();
			}
			sr.Close ();
		}
		
		private static CompilerError CreateIkvmErrorFromString (string error)
		{
			if (error.StartsWith ("Note") || error.StartsWith ("Warning"))
				return null;
			string trimmed = error.Trim ();
			if (trimmed.StartsWith ("(to avoid this warning add"))
				return null;

			CompilerError cerror = new CompilerError ();
			cerror.ErrorText = error;
			return cerror;
		}
	}
}
