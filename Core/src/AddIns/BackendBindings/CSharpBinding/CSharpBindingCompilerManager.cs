// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Mike KrÃ¼ger" email="mike@icsharpcode.net"/>
//     <version value="$version"/>
// </file>

using System;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.CodeDom.Compiler;

using MonoDevelop.Core.Services;

using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Components;
using MonoDevelop.Services;

namespace CSharpBinding
{
	/// <summary>
	/// This class controls the compilation of C Sharp files and C Sharp projects
	/// </summary>
	public class CSharpBindingCompilerManager
	{	
		FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
		
		public bool CanCompile(string fileName)
		{
			return Path.GetExtension(fileName).ToUpper() == ".CS";
		}

		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			CSharpCompilerParameters compilerparameters = (CSharpCompilerParameters) configuration.CompilationParameters;
			if (compilerparameters == null) compilerparameters = new CSharpCompilerParameters ();
			
			string exe = configuration.CompiledOutputName;
			string responseFileName = Path.GetTempFileName();
			StreamWriter writer = new StreamWriter(responseFileName);
			
			if (compilerparameters.CsharpCompiler == CsharpCompiler.Csc) {
				writer.WriteLine("\"/out:" + exe + '"');
				
				ArrayList pkg_references = new ArrayList ();
				
				if (references != null) {
					foreach (ProjectReference lib in references) {
						string fileName = lib.GetReferencedFileName ();
						switch (lib.ReferenceType) {
						case ReferenceType.Gac:
							string pkg = Runtime.SystemAssemblyService.GetPackageFromFullName (lib.Reference);
							if (pkg.Trim () == String.Empty) {
								string msg = String.Format (GettextCatalog.GetString ("{0} could not be found or is invalid."), lib.Reference);
								Runtime.MessageService.ShowWarning (msg);
								continue;
							}
							if (pkg == "MONO-SYSTEM") {
								writer.WriteLine ("\"/r:" + Path.GetFileName (fileName) + "\"");
							} else if (!pkg_references.Contains (pkg)) {
								pkg_references.Add (pkg);
								writer.WriteLine ("\"-pkg:" + pkg + "\"");
							}
							break;
						case ReferenceType.Assembly:
						case ReferenceType.Project:
							writer.WriteLine ("\"/r:" + fileName + "\"");
							break;
						}
					}
				}
				
				writer.WriteLine("/noconfig");
				writer.WriteLine("/nologo");
				writer.WriteLine("/codepage:utf8");
//				writer.WriteLine("/utf8output");
//				writer.WriteLine("/w:" + compilerparameters.WarningLevel);;
				
				if (configuration.DebugMode) {
					writer.WriteLine("/debug:+");
					writer.WriteLine("/debug:full");
					writer.WriteLine("/d:DEBUG");
				}
				
				// mcs default is + but others might not be
				if (compilerparameters.Optimize)
					writer.WriteLine("/optimize+");
				else
					writer.WriteLine("/optimize-");
				
				if (compilerparameters.Win32Icon != null && compilerparameters.Win32Icon.Length > 0 && File.Exists (compilerparameters.Win32Icon)) {
					writer.WriteLine("\"/win32icon:" + compilerparameters.Win32Icon + "\"");
				}
				
				if (compilerparameters.UnsafeCode) {
					writer.WriteLine("/unsafe");
				}
				
				if (compilerparameters.DefineSymbols.Length > 0) {
					writer.WriteLine("/define:" + '"' + compilerparameters.DefineSymbols + '"');
				}
				
				if (compilerparameters.MainClass != null && compilerparameters.MainClass.Length > 0) {
					writer.WriteLine("/main:" + compilerparameters.MainClass);
				}
				
				switch (configuration.CompileTarget) {
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
				
				foreach (ProjectFile finfo in projectFiles) {
					if (finfo.Subtype != Subtype.Directory) {
						switch (finfo.BuildAction) {
							case BuildAction.Compile:
								if (CanCompile (finfo.Name))
									writer.WriteLine('"' + finfo.Name + '"');
								break;
							case BuildAction.EmbedAsResource:
								// FIXME: workaround 60990
								writer.WriteLine(@"""/res:{0},{1}""", finfo.Name, Path.GetFileName (finfo.Name));
								break;
						}
					}
				}
				if (compilerparameters.GenerateXmlDocumentation) {
					writer.WriteLine("\"/doc:" + Path.ChangeExtension(exe, ".xml") + '"');
				}
			} 
			else {
				writer.WriteLine("-o " + exe);
				
				if (compilerparameters.UnsafeCode) {
					writer.WriteLine("--unsafe");
				}
				
				writer.WriteLine("--wlevel " + compilerparameters.WarningLevel);
		
				if (references != null) {		
					foreach (ProjectReference lib in references) {
						string fileName = lib.GetReferencedFileName ();
						writer.WriteLine("-r:" + fileName );
					}
				}
				
				switch (configuration.CompileTarget) {
					case CompileTarget.Exe:
						writer.WriteLine("--target exe");
						break;
					case CompileTarget.WinExe:
						writer.WriteLine("--target winexe");
						break;
					case CompileTarget.Library:
						writer.WriteLine("--target library");
						break;
				}
				foreach (ProjectFile finfo in projectFiles) {
					if (finfo.Subtype != Subtype.Directory) {
						switch (finfo.BuildAction) {
							case BuildAction.Compile:
								writer.WriteLine('"' + finfo.Name + '"');
								break;
							
							case BuildAction.EmbedAsResource:
								writer.WriteLine("--linkres " + finfo.Name);
								break;
						}
					}
				}			
			}
			writer.Close();
			
			string output = String.Empty;
			string error  = String.Empty;
			
			string compilerName = compilerparameters.CsharpCompiler == CsharpCompiler.Csc ? GetCompilerName() : System.Environment.GetEnvironmentVariable("ComSpec") + " /c mcs";
			string outstr = compilerName + " @" + responseFileName;
			TempFileCollection tf = new TempFileCollection();
			
			
			//StreamReader t = File.OpenText(responseFileName);
			
			//Executor.ExecWaitWithCapture(outstr,  tf, ref output, ref error);
			DoCompilation(outstr, tf, ref output, ref error);
			
			ICompilerResult result = ParseOutput(tf, output, error);
			if (result.CompilerOutput.Trim () != "")
				monitor.Log.WriteLine (result.CompilerOutput);
			
			File.Delete(responseFileName);
			File.Delete(output);
			File.Delete(error);
			return result;
		}
		
		public void GenerateMakefile (Project project, Combine parentCombine)
		{
			StreamWriter stream = new StreamWriter (Path.Combine (project.BaseDirectory, "Makefile." + project.Name.Replace (" ", "")));

			DotNetProjectConfiguration conf = (DotNetProjectConfiguration) project.ActiveConfiguration;
			CSharpCompilerParameters compilerparameters = (CSharpCompilerParameters) conf.CompilationParameters;
			
			string outputName = conf.CompiledOutputName;

			string target = "";
			string relativeOutputDir = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, parentCombine.OutputDirectory);

			switch (conf.CompileTarget) {
			case CompileTarget.Exe:
				target = "exe";
				break;
			case CompileTarget.WinExe:
				target = "winexe";
				break;
			case CompileTarget.Library:
				target = "library";
				break;
			}			
			
			ArrayList compile_files = new ArrayList ();
			ArrayList pkg_references = new ArrayList ();
			ArrayList assembly_references = new ArrayList ();
			ArrayList project_references = new ArrayList ();
			ArrayList system_references = new ArrayList ();
			ArrayList resources = new ArrayList ();
			
			foreach (ProjectFile finfo in project.ProjectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
					case BuildAction.Compile:
						string rel_path = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, Path.GetDirectoryName (finfo.Name));
						if (CanCompile (finfo.Name));
						compile_files.Add (Path.Combine (rel_path, Path.GetFileName (finfo.Name)));
						break;
						
					case BuildAction.EmbedAsResource:
						string resource_rel_path = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, Path.GetDirectoryName (finfo.Name));
						resources.Add (Path.Combine (resource_rel_path, Path.GetFileName (finfo.Name)));
						break;
					}
				}
			}

			foreach (ProjectReference lib in project.ProjectReferences) {
				switch (lib.ReferenceType) {
				case ReferenceType.Gac:
					string pkg = Runtime.SystemAssemblyService.GetPackageFromFullName (lib.Reference);
					if (pkg.Trim () == String.Empty)
						continue;
					if (pkg == "MONO-SYSTEM") {
						system_references.Add (Path.GetFileName (lib.GetReferencedFileName ()));
					} else if (!pkg_references.Contains (pkg)) {
						pkg_references.Add (pkg);
					}
					break;
				case ReferenceType.Assembly:
					string assembly_fileName = lib.GetReferencedFileName ();
					string rel_path_to = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, Path.GetDirectoryName (assembly_fileName));
					assembly_references.Add (Path.Combine (rel_path_to, Path.GetFileName (assembly_fileName)));
					break;
				case ReferenceType.Project:
					//string project_fileName = lib.GetReferencedFileName ();
					IProjectService prjService = (IProjectService)ServiceManager.GetService (typeof (IProjectService));
					CombineEntryCollection allProjects = prjService.CurrentOpenCombine.GetAllProjects();
					
					foreach (Project projectEntry in allProjects) {
						if (projectEntry.Name == lib.Reference) {
							string project_base_dir = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, projectEntry.BaseDirectory);
							
							string project_output_fileName = projectEntry.GetOutputFileName ();
							project_references.Add (Path.Combine (project_base_dir, Path.GetFileName (project_output_fileName)));
						}
					}
					break;
				}
			}

			stream.WriteLine ("# This makefile is autogenerated by MonoDevelop");
			stream.WriteLine ("# Do not modify this file");
			stream.WriteLine ();
			stream.WriteLine ("SOURCES = \\");
			for (int i = 0; i < compile_files.Count; i++) {
				stream.Write (((string)compile_files[i]).Replace (" ", "\\ "));
				if (i != compile_files.Count - 1)
					stream.WriteLine (" \\");
				else
					stream.WriteLine ();
			}
			stream.WriteLine ();

			if (resources.Count > 0) {
				stream.WriteLine ("RESOURCES = \\");
				for (int i = 0; i < resources.Count; i++) {
					stream.Write (((string)resources[i]).Replace (" ", "\\ "));
					if (i != resources.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				stream.WriteLine ();
				stream.WriteLine ("RESOURCES_BUILD = $(foreach res,$(RESOURCES), $(addprefix -resource:,$(res)),$(notdir $(res)))");
				stream.WriteLine ();
			}

			if (pkg_references.Count > 0) {
				stream.WriteLine ("PKG_REFERENCES = \\");
				for (int i = 0; i < pkg_references.Count; i++) {
					stream.Write (pkg_references[i]);
					if (i != pkg_references.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				
				stream.WriteLine ();
				stream.WriteLine ("PKG_REFERENCES_BUILD = $(addprefix -pkg:, $(PKG_REFERENCES))");
				stream.WriteLine ();
				stream.WriteLine ("PKG_REFERENCES_CHECK = $(addsuffix .pkgcheck, $(PKG_REFERENCES))");
				stream.WriteLine ();
			}
			
			if (system_references.Count > 0) {
				stream.WriteLine ("SYSTEM_REFERENCES = \\");
				for (int i = 0; i < system_references.Count; i++) {
					stream.Write (system_references[i]);
					if (i != system_references.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				stream.WriteLine ();
				stream.WriteLine ("SYSTEM_REFERENCES_BUILD = $(addprefix -r:, $(SYSTEM_REFERENCES))");
				stream.WriteLine ();
				stream.WriteLine ("SYSTEM_REFERENCES_CHECK = $(addsuffix .check, $(SYSTEM_REFERENCES))");
				stream.WriteLine ();
			}

			if (assembly_references.Count > 0) {
				stream.WriteLine ("ASSEMBLY_REFERENCES = \\");
				for (int i = 0; i < assembly_references.Count; i++) {
					stream.Write ("\"" + assembly_references[i] + "\"");
					if (i != assembly_references.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				
				stream.WriteLine ();
				stream.WriteLine ("ASSEMBLY_REFERENCES_BUILD = $(addprefix -r:, $(ASSEMBLY_REFERENCES))");
				stream.WriteLine ();
			}

			if (project_references.Count > 0) {
				stream.WriteLine ("PROJECT_REFERENCES = \\");
				for (int i = 0; i < project_references.Count; i++) {
					stream.Write ("\"" + project_references[i] + "\"");
					if (i != project_references.Count - 1)
						stream.WriteLine (" \\");
					else
						stream.WriteLine ();
				}
				
				stream.WriteLine ();
				stream.WriteLine ("PROJECT_REFERENCES_BUILD = $(addprefix -r:, $(PROJECT_REFERENCES))");
				stream.WriteLine ();
			}

			stream.WriteLine ("COMPILER = mcs");
			stream.Write ("COMPILER_OPTIONS = ");
			if (compilerparameters.UnsafeCode) {
				stream.Write ("-unsafe ");
			}
			if (compilerparameters.DefineSymbols != null && compilerparameters.DefineSymbols.Length > 0) {
				stream.Write ("-define:" + '"' + compilerparameters.DefineSymbols + '"' + " ");
			}
			if (compilerparameters.MainClass != null && compilerparameters.MainClass.Length > 0) {
				stream.Write ("-main:" + compilerparameters.MainClass + " ");
			}
			stream.WriteLine ();
			stream.WriteLine ();

			stream.WriteLine ("all: " + outputName);
			stream.WriteLine ();
			
			stream.Write (outputName + ": $(SOURCES)");
			if (resources.Count > 0) {
				stream.WriteLine (" $(RESOURCES)");
			} else {
				stream.WriteLine ();
			}
			
			stream.Write ("\t$(COMPILER) $(COMPILER_OPTIONS) -target:{0} -out:\"{1}\"", target, outputName);
			if (resources.Count > 0) {
				stream.Write (" $(RESOURCES_BUILD)");
			}
			if (pkg_references.Count > 0) {
				stream.Write (" $(PKG_REFERENCES_BUILD)");
			}
			if (assembly_references.Count > 0) {
				stream.Write (" $(ASSEMBLY_REFERENCES_BUILD)");
			}
			if (project_references.Count > 0) {
				stream.Write (" $(PROJECT_REFERENCES_BUILD)");
			}
			if (system_references.Count > 0) {
				stream.Write (" $(SYSTEM_REFERENCES_BUILD)");
			}
			stream.WriteLine (" $(SOURCES) \\");
			stream.WriteLine ("\t&& cp \"{0}\" {1}/.", outputName, relativeOutputDir);
			
			stream.WriteLine ();
			stream.WriteLine ("clean:");
			stream.WriteLine ("\trm -f {0}", outputName);
			stream.WriteLine ();
			
			stream.Write ("depcheck: ");
			if (pkg_references.Count > 0) {
				stream.Write ("PKG_depcheck ");
			}
			if (system_references.Count > 0) {
				stream.Write ("SYSTEM_depcheck");
			}
			stream.WriteLine ();
			stream.WriteLine ();
			if (pkg_references.Count > 0) {
				stream.WriteLine ("PKG_depcheck: $(PKG_REFERENCES_CHECK)");
				stream.WriteLine ();
				stream.WriteLine ("%.pkgcheck:");
				stream.WriteLine ("\t@echo -n Checking for package $(subst .pkgcheck,,$@)...");
				stream.WriteLine ("\t@if pkg-config --libs $(subst .pkgcheck,,$@) &> /dev/null; then \\");
				stream.WriteLine ("\t\techo yes; \\");
				stream.WriteLine ("\telse \\");
				stream.WriteLine ("\t\techo no; \\");
				stream.WriteLine ("\t\texit 1; \\");
				stream.WriteLine ("\tfi");
				stream.WriteLine ();
			}

			if (system_references.Count > 0) {
				stream.WriteLine ("SYSTEM_depcheck: $(SYSTEM_REFERENCES_CHECK)");
				stream.WriteLine ();
				stream.WriteLine ("%.check:");
				stream.WriteLine ("\t@echo -n Checking for $(subst .check,,$@)...");
				stream.WriteLine ("\t@if [ ! -e `pkg-config --variable=libdir mono`/mono/1.0/$(subst .check,,$@) ]; then \\");
				stream.WriteLine ("\t\techo no; \\");
				stream.WriteLine ("\t\texit 1; \\");
				stream.WriteLine ("\telse \\");
				stream.WriteLine ("\t\techo yes; \\");
				stream.WriteLine ("\tfi");
			}
			
			stream.Flush ();
			stream.Close ();
		}
		
		string compilerName = String.Empty;
		string GetCompilerName()
		{
			if (compilerName == String.Empty)
			{
				string runtimeDir = fileUtilityService.GetDirectoryNameWithSeparator(System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory());
				// The following regex foo gets the index of the
				// last match of lib/lib32/lib64 and uses
				// the text before that as the 'prefix' in order
				// to find the right mcs to use.
				Regex regex = new Regex ("lib[32 64]?");
				MatchCollection matches = regex.Matches(runtimeDir);
				Match match = matches[matches.Count - 1];
				compilerName = runtimeDir.Substring(0, match.Index) + Path.Combine("bin", "mcs");
			}

			return compilerName;
		}
		
		ICompilerResult ParseOutput(TempFileCollection tf, string stdout, string stderr)
		{
			StringBuilder compilerOutput = new StringBuilder();
			
			CompilerResults cr = new CompilerResults(tf);
			
			// we have 2 formats for the error output the csc gives :
			//Regex normalError  = new Regex(@"(?<file>.*)\((?<line>\d+),(?<column>\d+)\):\s+(?<error>\w+)\s+(?<number>[\d\w]+):\s+(?<message>.*)", RegexOptions.Compiled);
			//Regex generalError = new Regex(@"(?<error>.+)\s+(?<number>[\d\w]+):\s+(?<message>.*)", RegexOptions.Compiled);
			
			foreach (string s in new string[] { stdout, stderr }) {
				StreamReader sr = File.OpenText (s);
				while (true) {
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
				
					CompilerError error = CreateErrorFromString (curLine);
					
					if (error != null)
						cr.Errors.Add (error);
				}
				sr.Close();
			}
			return new DefaultCompilerResult(cr, compilerOutput.ToString());
		}
		
		private void DoCompilation(string outstr, TempFileCollection tf, ref string output, ref string error) {
			output = Path.GetTempFileName();
			error = Path.GetTempFileName();
			
			string arguments = outstr + " > " + output + " 2> " + error;
			string command = arguments;
			ProcessStartInfo si = new ProcessStartInfo("/bin/sh","-c \"" + command + "\"");
			si.RedirectStandardOutput = true;
			si.RedirectStandardError = true;
			si.UseShellExecute = false;
			Process p = new Process();
			p.StartInfo = si;
			p.Start();
			p.WaitForExit ();
		}

		// Snatched from our codedom code :-).
		static Regex regexError = new Regex (@"^(\s*(?<file>.*)\((?<line>\d*)(,(?<column>\d*[\+]*))?\)(:|)\s+)*(?<level>\w+)\s*(?<number>.*):\s(?<message>.*)",
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
