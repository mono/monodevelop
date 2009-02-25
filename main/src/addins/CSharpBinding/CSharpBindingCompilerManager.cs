//  CSharpBindingCompilerManager.cs
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
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;

namespace CSharpBinding
{
	/// <summary>
	/// This class controls the compilation of C Sharp files and C Sharp projects
	/// </summary>
	public class CSharpBindingCompilerManager
	{	
		public bool CanCompile(string fileName)
		{
			return Path.GetExtension(fileName).ToUpper() == ".CS";
		}

		public BuildResult Compile (ProjectItemCollection projectItems, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			CSharpCompilerParameters compilerparameters = (CSharpCompilerParameters) configuration.CompilationParameters;
			if (compilerparameters == null) compilerparameters = new CSharpCompilerParameters ();
			
			CSharpProjectParameters projectParameters = (CSharpProjectParameters) configuration.ProjectParameters;
			if (projectParameters == null) projectParameters = new CSharpProjectParameters ();
			
			string exe = configuration.CompiledOutputName;
			string responseFileName = Path.GetTempFileName();
			StringWriter writer = new StringWriter ();
			bool hasWin32Res = false;
			ArrayList gacRoots = new ArrayList ();

			writer.WriteLine("\"/out:" + exe + '"');
			
			ArrayList pkg_references = new ArrayList ();
			
			foreach (ProjectReference lib in projectItems.GetAll <ProjectReference> ()) {
				if ((lib.ReferenceType == ReferenceType.Project) &&
				    (!(lib.OwnerProject.ParentSolution.FindProjectByName (lib.Reference) is DotNetProject)))
					continue;
				foreach (string fileName in lib.GetReferencedFileNames (configuration.Id)) {
					switch (lib.ReferenceType) {
					case ReferenceType.Gac:
						SystemPackage pkg = lib.Package;
						if (pkg == null) {
							string msg = String.Format (GettextCatalog.GetString ("{0} could not be found or is invalid."), lib.Reference);
							monitor.ReportWarning (msg);
							continue;
						}
						if (pkg.IsCorePackage) {
							writer.WriteLine ("\"/r:" + Path.GetFileName (fileName) + "\"");
						} else if (pkg.IsInternalPackage) {
							writer.WriteLine ("\"/r:" + fileName + "\"");
						} else if (!pkg_references.Contains (pkg.Name)) {
							pkg_references.Add (pkg.Name);
							writer.WriteLine ("\"-pkg:" + pkg.Name + "\"");
						}
						if (pkg.GacRoot != null && !gacRoots.Contains (pkg.GacRoot))
							gacRoots.Add (pkg.GacRoot);
						break;
					default:
						writer.WriteLine ("\"/r:" + fileName + "\"");
						break;
					}
				}
			}
			
			writer.WriteLine("/noconfig");
			writer.WriteLine("/nologo");
//			writer.WriteLine("/utf8output");
			writer.WriteLine("/warn:" + compilerparameters.WarningLevel);
				
			if (configuration.SignAssembly) {
				if (File.Exists (configuration.AssemblyKeyFile))
					writer.WriteLine("\"/keyfile:" + configuration.AssemblyKeyFile + '"');
			}
			
			if (configuration.DebugMode) {
				writer.WriteLine("/debug:+");
				writer.WriteLine("/debug:full");
			}
			
			switch (compilerparameters.LangVersion) {
			case LangVersion.Default:
				break;
			case LangVersion.ISO_1:
				writer.WriteLine ("/langversion:ISO-1");
				break;
			case LangVersion.ISO_2:
				writer.WriteLine ("/langversion:ISO-2");
				break;
			default:
				string message = "Invalid LangVersion enum value '" + compilerparameters.LangVersion.ToString () + "'";
				monitor.ReportError (message, null);
				LoggingService.LogError (message);
				return null;
			}
			
			// mcs default is + but others might not be
			if (compilerparameters.Optimize)
				writer.WriteLine("/optimize+");
			else
				writer.WriteLine("/optimize-");

			if (projectParameters.Win32Resource != null && projectParameters.Win32Resource.Length > 0 && File.Exists (projectParameters.Win32Resource)) {
				writer.WriteLine("\"/win32res:" + projectParameters.Win32Resource + "\"");
				hasWin32Res = true;
			}
		
			if (projectParameters.Win32Icon != null && projectParameters.Win32Icon.Length > 0 && File.Exists (projectParameters.Win32Icon)) {
				if (hasWin32Res)
					monitor.ReportWarning ("Both Win32 icon and Win32 resource cannot be specified. Ignoring the icon.");
				else
					writer.WriteLine("\"/win32icon:" + projectParameters.Win32Icon + "\"");
			}
			
			if (projectParameters.CodePage != 0)
				writer.WriteLine ("/codepage:" + projectParameters.CodePage);
			else
				writer.WriteLine("/codepage:utf8");
			
			if (compilerparameters.UnsafeCode) {
				writer.WriteLine("-unsafe");
			}
			
			if (compilerparameters.NoStdLib) {
				writer.WriteLine("-nostdlib");
			}
			
			if (compilerparameters.TreatWarningsAsErrors) {
				writer.WriteLine("-warnaserror");
			}
			
			if (compilerparameters.DefineSymbols.Length > 0) {
				string define_str = String.Join (";",
							compilerparameters.DefineSymbols.Split (
								new char [] {',', ' ', ';'},
								StringSplitOptions.RemoveEmptyEntries));

				if (define_str.Length > 0)
					writer.WriteLine("/define:\"" + define_str + '"');
			}

			CompileTarget ctarget = configuration.CompileTarget;
			
			if (projectParameters.MainClass != null && projectParameters.MainClass.Length > 0) {
				writer.WriteLine("/main:" + projectParameters.MainClass);
				// mcs does not allow providing a Main class when compiling a dll
				// As a workaround, we compile as WinExe (although the output will still
				// have a .dll extension).
				if (ctarget == CompileTarget.Library)
					ctarget = CompileTarget.WinExe;
			}
			
			switch (ctarget) {
				case CompileTarget.Exe:
					writer.WriteLine("/t:exe");
					break;
				case CompileTarget.WinExe:
					writer.WriteLine("/t:winexe");
					break;
				case CompileTarget.Library:
					writer.WriteLine("/t:library");
					break;
			}
			
			foreach (ProjectFile finfo in projectItems.GetAll<ProjectFile> ()) {
				if (finfo.Subtype == Subtype.Directory)
					continue;

				switch (finfo.BuildAction) {
					case "Compile":
						writer.WriteLine('"' + finfo.Name + '"');
						break;
					case "EmbeddedResource":
						string fname = finfo.Name;
						if (String.Compare (Path.GetExtension (fname), ".resx", true) == 0)
							fname = Path.ChangeExtension (fname, ".resources");

						writer.WriteLine(@"""/res:{0},{1}""", fname, finfo.ResourceId);
						break;
					default:
						continue;
				}
			}
			if (compilerparameters.GenerateXmlDocumentation) {
				writer.WriteLine("\"/doc:" + Path.ChangeExtension(exe, ".xml") + '"');
			}
			
			if (!string.IsNullOrEmpty (compilerparameters.AdditionalArguments)) {
				writer.WriteLine (compilerparameters.AdditionalArguments);
			}
			
			if (!string.IsNullOrEmpty (compilerparameters.NoWarnings)) {
				writer.WriteLine ("/nowarn:\"{0}\"", compilerparameters.NoWarnings);
			}

			writer.Close();

			string output = String.Empty;
			string error  = String.Empty;

			File.WriteAllText (responseFileName, writer.ToString ());
			
			string compilerName;
			try {
				compilerName = GetCompilerName (configuration.TargetFramework.ClrVersion);
			} catch (Exception e) {
				string message = "Could not obtain a C# compiler";
				monitor.ReportError (message, e);
				return null;
			}
			
			monitor.Log.WriteLine (compilerName + " " + writer.ToString ().Replace ('\n',' '));
			
			string outstr = compilerName + " @" + responseFileName;
			TempFileCollection tf = new TempFileCollection();

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

			LoggingService.LogInfo (compilerName + " " + writer.ToString ());
			
			int exitCode = DoCompilation (outstr, tf, workingDir, gacRoots, ref output, ref error);
			
			BuildResult result = ParseOutput(tf, output, error);
			if (result.CompilerOutput.Trim ().Length != 0)
				monitor.Log.WriteLine (result.CompilerOutput);
			
			//if compiler crashes, output entire error string
			if (result.ErrorCount == 0 && exitCode != 0) {
				if (!string.IsNullOrEmpty (error))
					result.AddError (error);
				else
					result.AddError ("The compiler appears to have crashed without any error output.");
			}
			
			FileService.DeleteFile (responseFileName);
			FileService.DeleteFile (output);
			FileService.DeleteFile (error);
			return result;
		}

		string GetCompilerName (ClrVersion version)
		{
			string runtimeDir = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
			// The following regex foo gets the index of the
			// last match of lib/lib32/lib64 and uses
			// the text before that as the 'prefix' in order
			// to find the right mcs to use.
			Regex regex = new Regex ("lib[32 64]?");
			MatchCollection matches = regex.Matches(runtimeDir);
			Match match = matches[matches.Count - 1];
			string mcs;
			switch (version) {
			case ClrVersion.Net_1_1:
				mcs = "mcs";
				break;
			case ClrVersion.Net_2_0:
				mcs = "gmcs";
				break;
			case ClrVersion.Clr_2_1:
				mcs = "smcs";
				break;
			default:
				string message = "Cannot handle unknown runtime version ClrVersion.'" + version.ToString () + "'.";
				LoggingService.LogError (message);
				throw new Exception (message);
				
			}
			
			string compilerName = Path.Combine (runtimeDir.Substring(0, match.Index), Path.Combine("bin", mcs));
			return compilerName;
		}
		
		BuildResult ParseOutput(TempFileCollection tf, string stdout, string stderr)
		{
			StringBuilder compilerOutput = new StringBuilder();
			
			CompilerResults cr = new CompilerResults(tf);
			
			// we have 2 formats for the error output the csc gives :
			//Regex normalError  = new Regex(@"(?<file>.*)\((?<line>\d+),(?<column>\d+)\):\s+(?<error>\w+)\s+(?<number>[\d\w]+):\s+(?<message>.*)", RegexOptions.Compiled);
			//Regex generalError = new Regex(@"(?<error>.+)\s+(?<number>[\d\w]+):\s+(?<message>.*)", RegexOptions.Compiled);
			
			bool typeLoadException = false;
			
			foreach (string s in new string[] { stdout, stderr }) {
				StreamReader sr = File.OpenText (s);
				while (true) {
					if (typeLoadException) {
						compilerOutput.Append (sr.ReadToEnd ());
						break;
					}
					string curLine = sr.ReadLine();
					compilerOutput.Append(curLine);
					compilerOutput.Append('\n');
					if (curLine == null) {
						break;
					}
					curLine = curLine.Trim();
					if (curLine.Length == 0) {
						continue;
					}

					if (curLine.StartsWith ("Unhandled Exception: System.TypeLoadException") || 
					    curLine.StartsWith ("Unhandled Exception: System.IO.FileNotFoundException")) {
						cr.Errors.Clear ();
						typeLoadException = true;
					}
					
					CompilerError error = CreateErrorFromString (curLine);
					
					if (error != null)
						cr.Errors.Add (error);
				}
				sr.Close();
			}
			if (typeLoadException) {
				Regex reg  = new Regex(@".*WARNING.*used in (mscorlib|System),.*", RegexOptions.Multiline);
				if (reg.Match (compilerOutput.ToString ()).Success)
					cr.Errors.Add (new CompilerError (String.Empty, 0, 0, String.Empty, "Error: A referenced assembly may be built with an incompatible CLR version. See the compilation output for more details."));
				else
					cr.Errors.Add (new CompilerError (String.Empty, 0, 0, String.Empty, "Error: A dependency of a referenced assembly may be missing, or you may be referencing an assembly created with a newer CLR version. See the compilation output for more details."));
			}
			
			return new BuildResult(cr, compilerOutput.ToString());
		}
		
		private int DoCompilation (string outstr, TempFileCollection tf, string working_dir, ArrayList gacRoots, ref string output, ref string error) {
			output = Path.GetTempFileName();
			error = Path.GetTempFileName();
			
			StreamWriter outwr = new StreamWriter(output);
			StreamWriter errwr = new StreamWriter(error);
			string[] tokens = outstr.Split(' ');
			
			outstr = outstr.Substring(tokens[0].Length+1);

			ProcessStartInfo pinfo = new ProcessStartInfo (tokens[0], "\"" + outstr + "\"");
			pinfo.WorkingDirectory = working_dir;
			
			if (gacRoots.Count > 0) {
				// Create the gac prefix string
				string gacPrefix = string.Join ("" + Path.PathSeparator, (string[])gacRoots.ToArray (typeof(string)));
				string oldGacVar = Environment.GetEnvironmentVariable ("MONO_GAC_PREFIX");
				if (!string.IsNullOrEmpty (oldGacVar))
					gacPrefix += Path.PathSeparator + oldGacVar;
				pinfo.EnvironmentVariables ["MONO_GAC_PREFIX"] = gacPrefix;
			}
			pinfo.UseShellExecute = false;
			pinfo.RedirectStandardOutput = true;
			pinfo.RedirectStandardError = true;
			
			ProcessWrapper pw = Runtime.ProcessService.StartProcess (pinfo, outwr, errwr, null);
			pw.WaitForOutput();
			int exitCode = pw.ExitCode;
			outwr.Close();
			errwr.Close();
			pw.Dispose ();
			return exitCode;
		}


		// Snatched from our codedom code :-).
		static Regex regexError = new Regex (@"^(\s*(?<file>.*)\((?<line>\d*)(,(?<column>\d*[\+]*))?\)(:|)\s+)*(?<level>\w+)\s*(?<number>.*\d):\s*(?<message>.*)",
			RegexOptions.Compiled | RegexOptions.ExplicitCapture);

		private static CompilerError CreateErrorFromString(string error_string)
		{
			// When IncludeDebugInformation is true, prevents the debug symbols stats from braeking this.
			if (error_string.StartsWith ("WROTE SYMFILE") ||
			    error_string.StartsWith ("OffsetTable") ||
			    error_string.StartsWith ("Compilation succeeded") ||
			    error_string.StartsWith ("Compilation failed"))
				return null;

			CompilerError error = new CompilerError();

			Match match=regexError.Match(error_string);
			if (!match.Success) return null;
			if (String.Empty != match.Result("${file}"))
				error.FileName=match.Result("${file}");
			if (String.Empty != match.Result("${line}"))
				error.Line=Int32.Parse(match.Result("${line}"));
			if (String.Empty != match.Result("${column}")) {
				if (match.Result("${column}") == "255+")
					error.Column = -1;
				else
					error.Column=Int32.Parse(match.Result("${column}"));
			}
			if (match.Result("${level}")=="warning")
				error.IsWarning=true;
			error.ErrorNumber=match.Result("${number}");
			error.ErrorText=match.Result("${message}");
			return error;
		}

	}
}
