//
// IKVMCompilerManager.cs
//
// Author:
//   Mike Krüger <mkrueger@novell.com>
//
// Copyright (C) 2007 Novell, Inc (http://www.novell.com)
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
using System.CodeDom.Compiler;
using System.Diagnostics;
using System.IO;
using System.Text;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.ProgressMonitoring;
using MonoDevelop.Projects;

namespace JavaBinding
{
	public static class IKVMCompilerManager
	{
		static string GenerateOptionString (DotNetProjectConfiguration configuration)
		{
			JavaCompilerParameters parameters = (configuration.CompilationParameters as JavaCompilerParameters) ?? new JavaCompilerParameters ();
			StringBuilder result = new StringBuilder ();
			result.Append (configuration.DebugMode ? " -g " : " -g:none ");
			if (parameters.Optimize)
				result.Append (" -O ");
			if (parameters.Deprecation)
				result.Append (" -deprecation ");
			if (parameters.GenWarnings)
				result.Append (" -nowarn ");
			result.Append (" -encoding utf8 ");
			return result.ToString ();
		}
		
		public static BuildResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			JavaCompilerParameters parameters = (configuration.CompilationParameters as JavaCompilerParameters) ?? new JavaCompilerParameters ();
			string outdir   = configuration.OutputDirectory;
			string options  = GenerateOptionString (configuration);
			string compiler = parameters.CompilerPath;
			if (String.IsNullOrEmpty (compiler))
				compiler = "javac";
			
			StringBuilder files = new StringBuilder ();
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype != Subtype.Directory && finfo.BuildAction == BuildAction.Compile) {
					files.Append (" \"");
					files.Append (finfo.Name);
					files.Append ("\"");
				}
			}

			StringBuilder classpath = new StringBuilder (parameters.ClassPath);
			AppendClasspath (classpath, GenerateReferenceStubs (monitor, configuration, parameters, references));
			AppendClasspath (classpath, GenerateReferenceStub (monitor, configuration, new ProjectReference(ReferenceType.Gac, "mscorlib")));
			
			StringBuilder args = new StringBuilder ();
			args.Append (options.ToString ());
			if (parameters.Compiler == JavaCompiler.Gcj)
				args.Append ("-C ");
			if (classpath.Length != 0) {
				args.Append (" -classpath ");
				args.Append (classpath.ToString ());
			}
			args.Append (files.ToString ());
			args.Append (" -d ");
			args.Append (outdir);
			
			CompilerResults results = new CompilerResults (new TempFileCollection ());
			StringWriter output = new StringWriter ();
			StringWriter error = new StringWriter ();
			
			bool success = Compile (monitor, compiler, args.ToString (), configuration, parameters, output, error);
			ParseJavaOutput (parameters.Compiler, error.ToString(), results);
			
			if (success) {
				output = new StringWriter ();
				error = new StringWriter ();
				CompileToAssembly (monitor, configuration, parameters, references, output, error);
				ParseIkvmOutput (parameters.Compiler, error.ToString(), results);
			}
			
			return new BuildResult (results, "");
		}
		
		static void AppendClasspath (StringBuilder path, string jar)
		{
			if (path.Length > 0)
				path.Append (":");
			path.Append (jar);
		}
		
		static string GenerateReferenceStubs (IProgressMonitor monitor, DotNetProjectConfiguration configuration, JavaCompilerParameters compilerparameters, ProjectReferenceCollection references)
		{
			StringBuilder result = new StringBuilder ();
			if (references != null) {
				foreach (ProjectReference reference in references) {
					AppendClasspath (result, GenerateReferenceStub (monitor, configuration, reference));
				}
			}
			return result.ToString ();
		}
		
		static string GenerateReferenceStub (IProgressMonitor monitor,DotNetProjectConfiguration configuration, ProjectReference reference)
		{
			StringBuilder result = new StringBuilder ();
			foreach (string fileName in reference.GetReferencedFileNames (configuration.Id)) {
				string name = Path.GetFileNameWithoutExtension (Path.GetFileName (fileName));
				string outputName = Path.Combine (configuration.OutputDirectory, name + ".jar");
				if (!System.IO.File.Exists (outputName)) {
					monitor.Log.WriteLine (String.Format (GettextCatalog.GetString ("Generating {0} reference stub ..."), name));
					ProcessWrapper p = Runtime.ProcessService.StartProcess ("ikvmstub", "\"" + fileName + "\"", configuration.OutputDirectory, monitor.Log, monitor.Log, null);
					p.WaitForExit ();
					if (p.ExitCode != 0) {
						monitor.ReportError ("Stub generation failed.", null);
						if (File.Exists (outputName)) {
							try {
								File.Delete (outputName);
							} catch {
								// Ignore
							}
						}
					}
				}
				AppendClasspath (result, outputName);
			}
			return result.ToString ();
		}
		static string TargetToString (CompileTarget target)
		{
			switch (target) {
				case CompileTarget.WinExe:
					return "winexe";
				case CompileTarget.Library:
					return "library";
			}
			return "exe";
		}
		
		static void CompileToAssembly (IProgressMonitor monitor, DotNetProjectConfiguration configuration, JavaCompilerParameters compilerparameters, ProjectReferenceCollection references, TextWriter output, TextWriter error)
		{
			monitor.Log.WriteLine (GettextCatalog.GetString ("Generating assembly ..."));
			
			LogTextWriter chainedError = new LogTextWriter ();
			chainedError.ChainWriter (monitor.Log);
			chainedError.ChainWriter (error);
			
			LogTextWriter chainedOutput = new LogTextWriter ();
			chainedOutput.ChainWriter (monitor.Log);
			chainedOutput.ChainWriter (output);
			
			string outdir = configuration.OutputDirectory;
			string outclass = Path.Combine (outdir, configuration.OutputAssembly + ".class");
			string asm = Path.GetFileNameWithoutExtension (outclass);
			
			StringBuilder args = new StringBuilder ("*.class ");
			
			args.Append ("-assembly:"); args.Append (asm);
			args.Append (" -target:"); args.Append (TargetToString (configuration.CompileTarget));
			if (configuration.DebugMode)
				args.Append (" -debug");
			args.Append (" -srcpath:"); args.Append (configuration.SourceDirectory);
			
			if (references != null) {
				foreach (ProjectReference lib in references) {
					foreach (string fileName in lib.GetReferencedFileNames (configuration.Id)) {
						args.Append (" -r:"); args.Append (fileName);
					}
				}
			}
			
			foreach (string fileName in new ProjectReference(ReferenceType.Gac, "mscorlib").GetReferencedFileNames (configuration.Id)) {
				args.Append (" -r:"); args.Append (fileName);
			}
			
			Process process = Runtime.ProcessService.StartProcess ("ikvmc", args.ToString (), configuration.OutputDirectory, chainedOutput, chainedError, null);
			process.WaitForExit ();
		}
		
		static bool Compile (IProgressMonitor monitor, string compiler, string args, DotNetProjectConfiguration configuration, JavaCompilerParameters compilerparameters, TextWriter output, TextWriter error)
		{
			LogTextWriter chainedError = new LogTextWriter ();
			chainedError.ChainWriter (monitor.Log);
			chainedError.ChainWriter (error);
			
			LogTextWriter chainedOutput = new LogTextWriter ();
			chainedOutput.ChainWriter (monitor.Log);
			chainedOutput.ChainWriter (output);
			
			monitor.Log.WriteLine (GettextCatalog.GetString ("Compiling Java source code ..."));
			
			Process process = Runtime.ProcessService.StartProcess (compiler, args, null, chainedOutput, chainedError, null);
			process.WaitForExit ();
			return process.ExitCode == 0;
        }
		
		static void ParseJavaOutput (JavaCompiler compiler, string errorStr, CompilerResults cr)
		{
			TextReader reader = new StringReader (errorStr);
			string line;
			while ((line = reader.ReadLine ()) != null) {
				CompilerError error = CreateJavaErrorFromString (compiler, line);
				if (error != null) 
					cr.Errors.Add (error);
			}
			reader.Close ();
		}
		
		private static CompilerError CreateJavaErrorFromString (JavaCompiler compiler, string next)
		{
			CompilerError result = new CompilerError ();

			int errorCol = 0;
			string col = next.Trim ();
			if (col.Length == 1 && col == "^")
				errorCol = next.IndexOf ("^");

			int index1 = next.IndexOf (".java:");
			if (index1 < 0)
				return null;
		
			string s2 = next.Substring (index1 + 6);									
			int index2  = s2.IndexOf (":");				
			int line = Int32.Parse (next.Substring (index1 + 6, index2));
						
			result.Column = errorCol;
			result.Line = line;
			result.ErrorText = next.Substring (index1 + index2 + 7);
			result.FileName = Path.GetFullPath (next.Substring (0, index1) + ".java");
			return result;
		}
		
		static void ParseIkvmOutput (JavaCompiler compiler, string errorStr, CompilerResults cr)
		{
			TextReader reader = new StringReader (errorStr);
			string line;
			while ((line = reader.ReadLine ()) != null) {
				CompilerError error = CreateIkvmErrorFromString (line);
				if (error != null) 
					cr.Errors.Add (error);
			}
			reader.Close ();
		}
		
		static CompilerError CreateIkvmErrorFromString (string error)
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
		