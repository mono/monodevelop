// 
// CSharpBindingCompilerManager.cs
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
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Assemblies;
using MonoDevelop.CSharp.Project;
using System.Threading;
using MonoDevelop.Ide;
using MonoDevelop.Core.ProgressMonitoring;


namespace MonoDevelop.CSharp
{
	static class CSharpBindingCompilerManager
	{	
		static void AppendQuoted (StringBuilder sb, string option, string val)
		{
			sb.Append ('"');
			sb.Append (option);
			sb.Append (val);
			sb.Append ('"');
			sb.AppendLine ();
		}

		public static BuildResult Compile (ProjectItemCollection projectItems, DotNetProjectConfiguration configuration, ConfigurationSelector configSelector, IProgressMonitor monitor)
		{
			var compilerParameters = (CSharpCompilerParameters)configuration.CompilationParameters ?? new CSharpCompilerParameters ();
			var projectParameters = (CSharpProjectParameters)configuration.ProjectParameters ?? new CSharpProjectParameters ();
			
			FilePath outputName = configuration.CompiledOutputName;
			string responseFileName = Path.GetTempFileName ();

			//make sure that the output file is writable
			if (File.Exists (outputName)) {
				bool isWriteable = false;
				int count = 0;
				do {
					try {
						outputName.MakeWritable ();
						using (var stream = File.OpenWrite (outputName)) {
							isWriteable = true;
						}
					} catch (Exception) {
						Thread.Sleep (20);
					}
				} while (count++ < 5 && !isWriteable);
				if (!isWriteable) {
					MessageService.ShowError (string.Format (GettextCatalog.GetString ("Can't lock file: {0}."), outputName));
					return null;
				}
			}

			//get the runtime
			TargetRuntime runtime = MonoDevelop.Core.Runtime.SystemAssemblyService.DefaultRuntime;
			DotNetProject project = configuration.ParentItem as DotNetProject;
			if (project != null)
				runtime = project.TargetRuntime;

			//get the compiler name
			string compilerName;
			try {
				compilerName = GetCompilerName (runtime, configuration.TargetFramework);
			} catch (Exception e) {
				string message = "Could not obtain a C# compiler";
				monitor.ReportError (message, e);
				return null;
			}

			var sb = new StringBuilder ();

			HashSet<string> alreadyAddedReference = new HashSet<string> ();

			var monoRuntime = runtime as MonoTargetRuntime;
			if (!compilerParameters.NoStdLib && (monoRuntime == null || monoRuntime.HasMultitargetingMcs)) {
				string corlib = project.AssemblyContext.GetAssemblyFullName ("mscorlib", project.TargetFramework);
				if (corlib != null) {
					corlib = project.AssemblyContext.GetAssemblyLocation (corlib, project.TargetFramework);
				}
				if (corlib == null) {
					var br = new BuildResult ();
					br.AddError (string.Format ("Could not find mscorlib for framework {0}", project.TargetFramework.Id));
					return br;
				}
				AppendQuoted (sb, "/r:", corlib);
				sb.AppendLine ("-nostdlib");
			}

			List<string> gacRoots = new List<string> ();
			sb.AppendFormat ("\"/out:{0}\"", outputName);
			sb.AppendLine ();
			
			foreach (ProjectReference lib in projectItems.GetAll <ProjectReference> ()) {
				if (lib.ReferenceType == ReferenceType.Project && !(lib.OwnerProject.ParentSolution.FindProjectByName (lib.Reference) is DotNetProject))
					continue;
				string refPrefix = string.IsNullOrEmpty (lib.Aliases) ? "" : lib.Aliases + "=";
				foreach (string fileName in lib.GetReferencedFileNames (configSelector)) {
					switch (lib.ReferenceType) {
					case ReferenceType.Package:
						SystemPackage pkg = lib.Package;
						if (pkg == null) {
							string msg = string.Format (GettextCatalog.GetString ("{0} could not be found or is invalid."), lib.Reference);
							monitor.ReportWarning (msg);
							continue;
						}

						if (alreadyAddedReference.Add (fileName))
							AppendQuoted (sb, "/r:", refPrefix + fileName);
						
						if (pkg.GacRoot != null && !gacRoots.Contains (pkg.GacRoot))
							gacRoots.Add (pkg.GacRoot);
						if (!string.IsNullOrEmpty (pkg.Requires)) {
							foreach (string requiredPackage in pkg.Requires.Split(' ')) {
								SystemPackage rpkg = runtime.AssemblyContext.GetPackage (requiredPackage);
								if (rpkg == null)
									continue;
								foreach (SystemAssembly assembly in rpkg.Assemblies) {
									if (alreadyAddedReference.Add (assembly.Location))
										AppendQuoted (sb, "/r:", assembly.Location);
								}
							}
						}
						break;
					default:
						if (alreadyAddedReference.Add (fileName))
							AppendQuoted (sb, "/r:", refPrefix + fileName);
						break;
					}
				}
			}
			
			string sysCore = project.AssemblyContext.GetAssemblyFullName ("System.Core", project.TargetFramework);
			if (sysCore != null) {
				sysCore = project.AssemblyContext.GetAssemblyLocation (sysCore, project.TargetFramework);
				if (sysCore != null && !alreadyAddedReference.Contains (sysCore))
					AppendQuoted (sb, "/r:", sysCore);
			}
			
			sb.AppendLine ("/nologo");
			sb.Append ("/warn:");sb.Append (compilerParameters.WarningLevel.ToString ());
			sb.AppendLine ();
			
			if (configuration.SignAssembly) {
				if (File.Exists (configuration.AssemblyKeyFile))
					AppendQuoted (sb, "/keyfile:", configuration.AssemblyKeyFile);
			}
			
			if (configuration.DebugMode) {
//				sb.AppendLine ("/debug:+");
				sb.AppendLine ("/debug:full");
			}

			if (compilerParameters.LangVersion != LangVersion.Default) {
				var langVersionString = CSharpCompilerParameters.TryLangVersionToString (compilerParameters.LangVersion);
				if (langVersionString == null) {
					string message = "Invalid LangVersion enum value '" + compilerParameters.LangVersion.ToString () + "'";
					monitor.ReportError (message, null);
					LoggingService.LogError (message);
					return null;
				}
				sb.AppendLine ("/langversion:" + langVersionString);
			}
			
			// mcs default is + but others might not be
			if (compilerParameters.Optimize)
				sb.AppendLine ("/optimize+");
			else
				sb.AppendLine ("/optimize-");
			
			bool hasWin32Res = !string.IsNullOrEmpty (projectParameters.Win32Resource) && File.Exists (projectParameters.Win32Resource);
			if (hasWin32Res) 
				AppendQuoted (sb, "/win32res:", projectParameters.Win32Resource);
			
			if (!string.IsNullOrEmpty (projectParameters.Win32Icon) && File.Exists (projectParameters.Win32Icon)) {
				if (hasWin32Res) {
					monitor.ReportWarning ("Both Win32 icon and Win32 resource cannot be specified. Ignoring the icon.");
				} else {
					AppendQuoted (sb, "/win32icon:", projectParameters.Win32Icon);
				}
			}
			
			if (projectParameters.CodePage != 0)
				sb.AppendLine ("/codepage:" + projectParameters.CodePage);
			else if (runtime is MonoTargetRuntime)
				sb.AppendLine ("/codepage:utf8");
			
			if (compilerParameters.UnsafeCode) 
				sb.AppendLine ("-unsafe");
			if (compilerParameters.NoStdLib) 
				sb.AppendLine ("-nostdlib");
			
			if (!string.IsNullOrEmpty (compilerParameters.PlatformTarget) && compilerParameters.PlatformTarget.ToLower () != "anycpu") {
				//HACK: to ignore the platform flag for Mono <= 2.4, because gmcs didn't support it
				if (runtime.RuntimeId == "Mono" && runtime.AssemblyContext.GetAssemblyLocation ("Mono.Debugger.Soft", null) == null) {
					LoggingService.LogWarning ("Mono runtime '" + runtime.DisplayName + 
					                           "' appears to be too old to support the 'platform' C# compiler flag.");
				} else {
					sb.AppendLine ("/platform:" + compilerParameters.PlatformTarget);
				}
			}

			if (compilerParameters.TreatWarningsAsErrors) {
				sb.AppendLine ("-warnaserror");
				if (!string.IsNullOrEmpty (compilerParameters.WarningsNotAsErrors))
					sb.AppendLine ("-warnaserror-:" + compilerParameters.WarningsNotAsErrors);
			}
			
			if (compilerParameters.DefineSymbols.Length > 0) {
				string define_str = string.Join (";", compilerParameters.DefineSymbols.Split (new char [] {',', ' ', ';'}, StringSplitOptions.RemoveEmptyEntries));
				if (define_str.Length > 0) {
					AppendQuoted (sb, "/define:", define_str);
					sb.AppendLine ();
				}
			}

			CompileTarget ctarget = configuration.CompileTarget;
			
			if (!string.IsNullOrEmpty (projectParameters.MainClass)) {
				sb.AppendLine ("/main:" + projectParameters.MainClass);
				// mcs does not allow providing a Main class when compiling a dll
				// As a workaround, we compile as WinExe (although the output will still
				// have a .dll extension).
				if (ctarget == CompileTarget.Library)
					ctarget = CompileTarget.WinExe;
			}
			
			switch (ctarget) {
				case CompileTarget.Exe:
					sb.AppendLine ("/t:exe");
					break;
				case CompileTarget.WinExe:
					sb.AppendLine ("/t:winexe");
					break;
				case CompileTarget.Library:
					sb.AppendLine ("/t:library");
					break;
			}
			
			foreach (ProjectFile finfo in projectItems.GetAll<ProjectFile> ()) {
				if (finfo.Subtype == Subtype.Directory)
					continue;

				switch (finfo.BuildAction) {
					case "Compile":
						AppendQuoted (sb, "", finfo.Name);
						break;
					case "EmbeddedResource":
						string fname = finfo.Name;
						if (string.Compare (Path.GetExtension (fname), ".resx", true) == 0)
							fname = Path.ChangeExtension (fname, ".resources");
						sb.Append ('"');sb.Append ("/res:");
						sb.Append (fname);sb.Append (',');sb.Append (finfo.ResourceId);
						sb.Append ('"');sb.AppendLine ();
						break;
					default:
						continue;
				}
			}
			if (compilerParameters.GenerateXmlDocumentation) 
				AppendQuoted (sb, "/doc:", Path.ChangeExtension (outputName, ".xml"));
			
			if (!string.IsNullOrEmpty (compilerParameters.AdditionalArguments)) 
				sb.AppendLine (compilerParameters.AdditionalArguments);
			
			if (!string.IsNullOrEmpty (compilerParameters.NoWarnings)) 
				AppendQuoted (sb, "/nowarn:", compilerParameters.NoWarnings);

			if (runtime.RuntimeId == "MS.NET") {
				sb.AppendLine("/fullpaths");
				sb.AppendLine("/utf8output");
			}

			string output = "";
			string error  = "";
			
			File.WriteAllText (responseFileName, sb.ToString ());
			

			
			monitor.Log.WriteLine (compilerName + " /noconfig " + sb.ToString ().Replace ('\n',' '));
			
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

			LoggingService.LogInfo (compilerName + " " + sb.ToString ());
			
			ExecutionEnvironment envVars = runtime.GetToolsExecutionEnvironment (project.TargetFramework);
			string cargs = "/noconfig @\"" + responseFileName + "\"";

			int exitCode = DoCompilation (monitor, compilerName, cargs, workingDir, envVars, gacRoots, ref output, ref error);
			
			BuildResult result = ParseOutput (output, error);
			if (result.CompilerOutput.Trim ().Length != 0)
				monitor.Log.WriteLine (result.CompilerOutput);
			
			//if compiler crashes, output entire error string
			if (result.ErrorCount == 0 && exitCode != 0) {
				try {
					monitor.Log.Write (File.ReadAllText (error));
				} catch (IOException) {
				}
				result.AddError ("The compiler appears to have crashed. Check the build output pad for details.");
				LoggingService.LogError ("C# compiler crashed. Response file '{0}', stdout file '{1}', stderr file '{2}'",
				                         responseFileName, output, error);
			} else {
				FileService.DeleteFile (responseFileName);
				FileService.DeleteFile (output);
				FileService.DeleteFile (error);
			}
			return result;
		}
		
		static string GetCompilerName (TargetRuntime runtime, TargetFramework fx)
		{
			string csc = runtime.GetToolPath (fx, "csc");
			if (csc != null)
				return csc;
			else {
				string message = GettextCatalog.GetString ("C# compiler not found for {0}.", fx.Name);
				LoggingService.LogError (message);
				throw new Exception (message);
			}
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
		
		static int DoCompilation (IProgressMonitor monitor, string compilerName, string compilerArgs, string working_dir, ExecutionEnvironment envVars, List<string> gacRoots, ref string output, ref string error)
		{
			output = Path.GetTempFileName ();
			error = Path.GetTempFileName ();
			
			StreamWriter outwr = new StreamWriter (output);
			StreamWriter errwr = new StreamWriter (error);
			
			ProcessStartInfo pinfo = new ProcessStartInfo (compilerName, compilerArgs);
			pinfo.StandardErrorEncoding = Encoding.UTF8;
			pinfo.StandardOutputEncoding = Encoding.UTF8;
			pinfo.WorkingDirectory = working_dir;
			
			if (gacRoots.Count > 0) {
				// Create the gac prefix string
				string gacPrefix = string.Join ("" + Path.PathSeparator, gacRoots.ToArray ());
				string oldGacVar = Environment.GetEnvironmentVariable ("MONO_GAC_PREFIX");
				if (!string.IsNullOrEmpty (oldGacVar))
					gacPrefix += Path.PathSeparator + oldGacVar;
				pinfo.EnvironmentVariables ["MONO_GAC_PREFIX"] = gacPrefix;
			}
			
			envVars.MergeTo (pinfo);
			
			pinfo.UseShellExecute = false;
			pinfo.RedirectStandardOutput = true;
			pinfo.RedirectStandardError = true;

			MonoDevelop.Core.Execution.ProcessWrapper pw = Runtime.ProcessService.StartProcess (pinfo, outwr, errwr, null);
			using (var mon = new AggregatedOperationMonitor (monitor, pw)) {
				pw.WaitForOutput ();
			}
			int exitCode = pw.ExitCode;
			outwr.Close ();
			errwr.Close ();
			pw.Dispose ();
			return exitCode;
		}
		
		// Snatched from our codedom code, with some changes to make it compatible with csc
		// (the line+column group is optional is csc)
		static Regex regexError = new Regex (@"^(\s*(?<file>.+[^)])(\((?<line>\d*)(,(?<column>\d*[\+]*))?\))?:\s+)*(?<level>\w+)\s+(?<number>..\d+):\s*(?<message>.*)", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		static BuildError CreateErrorFromString (string error_string)
		{
			// When IncludeDebugInformation is true, prevents the debug symbols stats from braeking this.
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
