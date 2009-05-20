// 
// ILAsmCompilerManager.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2009 Novell, Inc (http://www.novell.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Text;

using MonoDevelop.Projects;
using MonoDevelop.Core;
using MonoDevelop.Core.Assemblies;

namespace ILAsmBinding
{
	static class ILAsmCompilerManager
	{
		static void AppendQuoted (StringBuilder sb, string option, string val)
		{
			sb.Append ('"');
			sb.Append (option);
			sb.Append (val);
			sb.Append ("\" ");
		}
		
		public static BuildResult Compile (ProjectItemCollection projectItems, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			ILAsmCompilerParameters compilerParameters = (ILAsmCompilerParameters)configuration.CompilationParameters ?? new ILAsmCompilerParameters ();
			string outputName       = configuration.CompiledOutputName;
			
			StringBuilder sb = new StringBuilder ();
			sb.AppendFormat ("\"/output:{0}\" ", outputName);
			
			List<string> gacRoots = new List<string> ();
			
			
			switch (configuration.CompileTarget) {
				case CompileTarget.WinExe:
				case CompileTarget.Exe:
					sb.Append ("/exe ");
					break;
				case CompileTarget.Library:
					sb.Append ("/dll ");
					break;
			}
			
			if (configuration.DebugMode)
				sb.Append ("/debug ");
			
			foreach (ProjectFile finfo in projectItems.GetAll<ProjectFile> ()) {
				if (finfo.Subtype == Subtype.Directory)
					continue;

				switch (finfo.BuildAction) {
					case "Compile":
						AppendQuoted (sb, "", finfo.Name);
						break;
					default:
						continue;
				}
			}
			
			string output = "";
			string error  = "";
			
			
			string outstr = "ilasm " + sb.ToString ();
			monitor.Log.WriteLine (outstr);
			
			string workingDir = ".";
			if (configuration.ParentItem != null) {
				workingDir = configuration.ParentItem.BaseDirectory;
				if (workingDir == null)
					// Dummy projects created for single files have no filename
					// and so no BaseDirectory.
					// This is a workaround for a bug in 
					// ProcessStartInfo.WorkingDirectory - not able to handle null
					workingDir = ".";
			}

			LoggingService.LogInfo ("ilasm " + sb.ToString ());
			
			//FIXME: is Current the real target framework?
			Dictionary<string,string> envVars = MonoDevelop.Core.Runtime.SystemAssemblyService.CurrentRuntime.GetToolsEnvironmentVariables (configuration.TargetFramework);
			int exitCode = DoCompilation (outstr, workingDir, envVars, gacRoots, ref output, ref error);
			
			BuildResult result = ParseOutput (output, error);
			if (result.CompilerOutput.Trim ().Length != 0)
				monitor.Log.WriteLine (result.CompilerOutput);
			
			//if compiler crashes, output entire error string
			if (result.ErrorCount == 0 && exitCode != 0) {
				if (!string.IsNullOrEmpty (error))
					result.AddError (error);
				else
					result.AddError ("The compiler appears to have crashed without any error output.");
			}
			
			FileService.DeleteFile (output);
			FileService.DeleteFile (error);
			return result;
		}
		
		static BuildResult ParseOutput (string stdout, string stderr)
		{
			BuildResult result = new BuildResult ();
			
			StringBuilder compilerOutput = new StringBuilder ();
			bool typeLoadException = false;
			foreach (string s in new string[] { stdout, stderr }) {
				StreamReader sr = File.OpenText (s);
				while (true) {
					if (typeLoadException) {
						compilerOutput.Append (sr.ReadToEnd ());
						break;
					}
					string curLine = sr.ReadLine();
					compilerOutput.AppendLine (curLine);
					
					if (curLine == null) 
						break;
					
					curLine = curLine.Trim();
					if (curLine.Length == 0) 
						continue;
					
					if (curLine.StartsWith ("Unhandled Exception: System.TypeLoadException") || 
					    curLine.StartsWith ("Unhandled Exception: System.IO.FileNotFoundException")) {
						result.ClearErrors ();
						typeLoadException = true;
					}
					
					BuildError error = CreateErrorFromString (curLine);
					
					if (error != null)
						result.Append (error);
				}
				sr.Close();
			}
			if (typeLoadException) {
				Regex reg  = new Regex (@".*WARNING.*used in (mscorlib|System),.*", RegexOptions.Multiline);
				if (reg.Match (compilerOutput.ToString ()).Success)
					result.AddError ("", 0, 0, "", "Error: A referenced assembly may be built with an incompatible CLR version. See the compilation output for more details.");
				else
					result.AddError ("", 0, 0, "", "Error: A dependency of a referenced assembly may be missing, or you may be referencing an assembly created with a newer CLR version. See the compilation output for more details.");
			}
			result.CompilerOutput = compilerOutput.ToString ();
			return result;
		}
		
		static int DoCompilation (string outstr, string working_dir, Dictionary<string, string> envVars, List<string> gacRoots, ref string output, ref string error) 
		{
			output = Path.GetTempFileName();
			error = Path.GetTempFileName();
			
			StreamWriter outwr = new StreamWriter (output);
			StreamWriter errwr = new StreamWriter (error);
			string[] tokens = outstr.Split (' ');
			
			outstr = outstr.Substring (tokens[0].Length+1);

			ProcessStartInfo pinfo = new ProcessStartInfo (tokens[0], outstr);
			pinfo.WorkingDirectory = working_dir;
			
			if (gacRoots.Count > 0) {
				// Create the gac prefix string
				string gacPrefix = string.Join ("" + Path.PathSeparator, gacRoots.ToArray ());
				string oldGacVar = Environment.GetEnvironmentVariable ("MONO_GAC_PREFIX");
				if (!string.IsNullOrEmpty (oldGacVar))
					gacPrefix += Path.PathSeparator + oldGacVar;
				pinfo.EnvironmentVariables ["MONO_GAC_PREFIX"] = gacPrefix;
			}
			
			foreach (KeyValuePair<string,string> ev in envVars) {
				if (ev.Value == null)
					pinfo.EnvironmentVariables.Remove (ev.Key);
				else
					pinfo.EnvironmentVariables [ev.Key] = ev.Value;
			}
			
			pinfo.UseShellExecute = false;
			pinfo.RedirectStandardOutput = true;
			pinfo.RedirectStandardError = true;
			
			MonoDevelop.Core.Execution.ProcessWrapper pw = Runtime.ProcessService.StartProcess (pinfo, outwr, errwr, null);
			pw.WaitForOutput();
			int exitCode = pw.ExitCode;
			outwr.Close();
			errwr.Close();
			pw.Dispose ();
			return exitCode;
		}

		static Regex regexError = new Regex (@"^(\s*(?<file>.*)\s\((?<line>\d*)(,\s(?<column>\d*[\+]*))?\)\s(:|)\s+)*(?<level>\w+)\s*:\s*(?<message>.*)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static BuildError CreateErrorFromString (string error_string)
		{
			// When IncludeDebugInformation is true, prevents the debug symbols stats from breaking this.
			if (error_string.StartsWith ("WROTE SYMFILE") ||
			    error_string.StartsWith ("OffsetTable") ||
			    error_string.StartsWith ("Compilation succeeded") ||
			    error_string.StartsWith ("Compilation failed"))
				return null;
			
			Match match = regexError.Match(error_string);
			if (!match.Success) 
				return null;
			
			BuildError error = new BuildError ();
			error.FileName = match.Result ("${file}") ?? "";
			
			string line = match.Result ("${line}");
			error.Line = !string.IsNullOrEmpty (line) ? Int32.Parse (line) : 0;
			
			string col = match.Result ("${column}");
			if (!string.IsNullOrEmpty (col)) 
				error.Column = col == "255+" ? -1 : Int32.Parse (col);
			
			error.IsWarning   = match.Result ("${level}") == "warning";
			error.ErrorNumber = match.Result ("${number}");
			error.ErrorText   = match.Result ("${message}");
			return error;
		}
	}
}

