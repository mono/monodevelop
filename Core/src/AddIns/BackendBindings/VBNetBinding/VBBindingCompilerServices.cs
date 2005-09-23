// <file>
//     <copyright see="prj:///doc/copyright.txt"/>
//     <license see="prj:///doc/license.txt"/>
//     <owner name="Markus Palme" email="MarkusPalme@gmx.de"/>
//     <version value="$version"/>
// </file>

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections;
using System.IO;
using System.Diagnostics;
using System.CodeDom.Compiler;
using MonoDevelop.Core.Services;

using MonoDevelop.Services;

using MonoDevelop.Internal.Project;
using MonoDevelop.Gui;
using MonoDevelop.Gui.Components;

namespace VBBinding {
	
	/// <summary>
	/// This class controls the compilation of VB.net files and VB.net projects
	/// </summary>
	public class VBBindingCompilerServices
	{	
	
		static Regex regexError = new Regex (@"^(\s*(?<file>.*)\((?<line>\d*)(,(?<column>\d*))?\)\s+)*(?<level>\w+)\s*(?<number>.*):\s(?<message>.*)",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture);

	
		FileUtilityService fileUtilityService = (FileUtilityService)ServiceManager.GetService(typeof(FileUtilityService));
		PropertyService propertyService       = (PropertyService)ServiceManager.GetService(typeof(PropertyService));
		
		public bool CanCompile(string fileName)
		{
			return Path.GetExtension(fileName) == ".vb";
		}
		
		string GetCompilerName(string compilerVersion)
		{
			//string runtimeDirectory = Path.Combine(fileUtilityService.NETFrameworkInstallRoot, compilerVersion);
			//if (compilerVersion.Length == 0 || compilerVersion == "Standard" || !Directory.Exists(runtimeDirectory)) {
			//	runtimeDirectory = System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory();
			//}
			//return String.Concat('"', Path.Combine(runtimeDirectory, "vbc.exe"), '"');
			return "mbas";
		}
		
		string GenerateOptions (DotNetProjectConfiguration configuration, VBCompilerParameters compilerparameters, string outputFileName)
		{
			StringBuilder sb = new StringBuilder();
			
			sb.Append("-out:");sb.Append(outputFileName);/*sb.Append('"');*/sb.Append(Environment.NewLine);
			
			sb.Append("-nologo");sb.Append(Environment.NewLine);
			sb.Append("-utf8output");sb.Append(Environment.NewLine);
			
//			if (compilerparameters.DebugMode) {
//				sb.Append("--debug+");sb.Append(Environment.NewLine);
//				sb.Append("--debug:full");sb.Append(Environment.NewLine);
//			}
			
			//if (compilerparameters.Optimize) {
			//	sb.Append("-optimize");sb.Append(Environment.NewLine);
			//}
			
			//if (compilerparameters.OptionStrict) {
			//	sb.Append("-optionstrict");sb.Append(Environment.NewLine);
			//}
			
			//if (compilerparameters.OptionExplicit) {
			//	sb.Append("-optionexplicit");sb.Append(Environment.NewLine);
			//}// else {
			//	sb.Append("--optionexplicit-");sb.Append(Environment.NewLine);
			//}
			
			if (compilerparameters.Win32Icon != null && compilerparameters.Win32Icon.Length > 0 && File.Exists(compilerparameters.Win32Icon)) {
				sb.Append("-win32icon:");sb.Append('"');sb.Append(compilerparameters.Win32Icon);sb.Append('"');sb.Append(Environment.NewLine);
			}
			
			if (compilerparameters.RootNamespace!= null && compilerparameters.RootNamespace.Length > 0) {
				sb.Append("-rootnamespace:");sb.Append('"');sb.Append(compilerparameters.RootNamespace);sb.Append('"');sb.Append(Environment.NewLine);
			}
			
			if (compilerparameters.DefineSymbols.Length > 0) {
				sb.Append("-define:");sb.Append('"');sb.Append(compilerparameters.DefineSymbols);sb.Append('"');sb.Append(Environment.NewLine);
			}
			
			if (compilerparameters.MainClass != null && compilerparameters.MainClass.Length > 0) {
				sb.Append("-main:");sb.Append(compilerparameters.MainClass);sb.Append(Environment.NewLine);
			}
			
			if(compilerparameters.Imports.Length > 0) {
				sb.Append("-imports:");sb.Append(compilerparameters.Imports);sb.Append(Environment.NewLine);
			}
			
			switch (configuration.CompileTarget) {
				case CompileTarget.Exe:
					sb.Append("-target:exe");
					break;
				case CompileTarget.WinExe:
					sb.Append("-target:winexe");
					break;
				case CompileTarget.Library:
					sb.Append("-target:library");
					break;
				case CompileTarget.Module:
					sb.Append("-target:module");
					break;
				default:
					throw new NotSupportedException("unknown compile target:" + configuration.CompileTarget);
			}
			sb.Append(Environment.NewLine);
			return sb.ToString();
		}
		
		public ICompilerResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			VBCompilerParameters compilerparameters = (VBCompilerParameters) configuration.CompilationParameters;
			if (compilerparameters == null) compilerparameters = new VBCompilerParameters ();
			
			string exe = configuration.CompiledOutputName;
			string responseFileName = Path.GetTempFileName();
			//string stdResponseFileName = String.Concat(propertyService.DataDirectory, Path.DirectorySeparatorChar, "vb.rsp");
			StreamWriter writer = new StreamWriter(responseFileName);
			
			//Console.WriteLine(GenerateOptions(compilerparameters,exe));	
			writer.WriteLine(GenerateOptions (configuration, compilerparameters, exe));
			
			foreach (ProjectReference lib in references) {
				string fileName = lib.GetReferencedFileName();
				//Console.WriteLine(String.Concat("-r:",fileName));
				writer.WriteLine(String.Concat("-r:", fileName));
			}
			
			// write source files and embedded resources
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
						case BuildAction.Compile:
							//Console.WriteLine(finfo.Name);
							writer.WriteLine(finfo.Name);
						break;
						
						case BuildAction.EmbedAsResource:
							//Console.WriteLine(String.Concat("-resource:", finfo.Name));
							writer.WriteLine(String.Concat("-resource:", finfo.Name));
						break;
					}
				}
			}
			
			TempFileCollection tf = new TempFileCollection ();
			writer.Close();
			
			string output = "";
			string error  = "";
			string compilerName = GetCompilerName(compilerparameters.VBCompilerVersion);
			string outstr = String.Concat(compilerName, " @", responseFileName); //, " @", stdResponseFileName);
			
			//Console.WriteLine("Attempting to run: "+outstr);
			
			//Executor.ExecWaitWithCapture(outstr, tf, ref output, ref error);
			DoCompilation(outstr,tf,ref output,ref error);
			
			//Console.WriteLine("Output: "+output);
			//Console.WriteLine("Error: "+error);
			
			
			ICompilerResult result = ParseOutput(tf, output);
			ParseOutput(tf,error);
			
			File.Delete(responseFileName);
			File.Delete(output);
			File.Delete(error);
			if (configuration.CompileTarget != CompileTarget.Library) {
				WriteManifestFile(exe);
			}
			return result;
		}
		
		// code duplication: see C# backend : CSharpBindingCompilerManager
		void WriteManifestFile(string fileName)
		{
			string manifestFile = String.Concat(fileName, ".manifest");
			if (File.Exists(manifestFile)) {
				return;
			}
			StreamWriter sw = new StreamWriter(manifestFile);
			sw.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
			sw.WriteLine("");
			sw.WriteLine("<assembly xmlns=\"urn:schemas-microsoft-com:asm.v1\" manifestVersion=\"1.0\">");
			sw.WriteLine("	<dependency>");
			sw.WriteLine("		<dependentAssembly>");
			sw.WriteLine("			<assemblyIdentity");
			sw.WriteLine("				type=\"win32\"");
			sw.WriteLine("				name=\"Microsoft.Windows.Common-Controls\"");
			sw.WriteLine("				version=\"6.0.0.0\"");
			sw.WriteLine("				processorArchitecture=\"X86\"");
			sw.WriteLine("				publicKeyToken=\"6595b64144ccf1df\"");
			sw.WriteLine("				language=\"*\"");
			sw.WriteLine("			/>");
			sw.WriteLine("		</dependentAssembly>");
			sw.WriteLine("	</dependency>");
			sw.WriteLine("</assembly>");
			sw.Close();
		}
		
/***** Legacy #D code, will remove if replacement code tests OK *****

		CompilerResult ParseOutput(TempFileCollection tf, string file)
		{
			StringBuilder compilerOutput = new StringBuilder();
			
			StreamReader sr = File.OpenText(file);
			
			// skip fist whitespace line
			sr.ReadLine();
			
			CompilerResults cr = new CompilerResults(tf);
			
			while (true) {
				string next = sr.ReadLine();
				compilerOutput.Append(next);compilerOutput.Append(Environment.NewLine);
				if (next == null) {
					break;
				}
				CompilerError error = new CompilerError();
				
				int index           = next.IndexOf(": ");
				if (index < 0) {
					continue;
				}
				
				string description  = null;
				string errorwarning = null;
				string location     = null;
				
				string s1 = next.Substring(0, index);
				string s2 = next.Substring(index + 2);
				index  = s2.IndexOf(": ");
				
				if (index == -1) {
					errorwarning = s1;
					description = s2;
				} else {
					location = s1;
					s1 = s2.Substring(0, index);
					s2 = s2.Substring(index + 2);
					errorwarning = s1;
					description = s2;
				}
				
				if (location != null) {
					int idx1 = location.LastIndexOf('(');
					int idx2 = location.LastIndexOf(')');
					if (idx1 >= 0 &&  idx2 >= 0) {
						string filename = location.Substring(0, idx1);
						error.Line = Int32.Parse(location.Substring(idx1 + 1, idx2 - idx1 - 1));
						error.FileName = Path.GetFullPath(filename.Trim()); // + "\\" + Path.GetFileName(filename);
					}
				}
				
				string[] what = errorwarning.Split(' ');
				Console.WriteLine("Error is: "+what[0]);
				error.IsWarning   = (what[0] == "warning" || what[0]=="MonoBASIC");
				error.ErrorNumber = what[what.Length - 1];
				
				error.ErrorText = description;
				
				cr.Errors.Add(error);
			}
			sr.Close();
			Console.WriteLine(compilerOutput.ToString());
			return new DefaultCompilerResult(cr, compilerOutput.ToString());
		}
*/
		
		ICompilerResult ParseOutput(TempFileCollection tf, string file)
		{
			StringBuilder compilerOutput = new StringBuilder();
			
			StreamReader sr = File.OpenText(file);
			
			// skip fist whitespace line
			//sr.ReadLine();
			
			CompilerResults cr = new CompilerResults(tf);
			
			// we have 2 formats for the error output the csc gives :
			//Regex normalError  = new Regex(@"(?<file>.*)\((?<line>\d+),(?<column>\d+)\):\s+(?<error>\w+)\s+(?<number>[\d\w]+):\s+(?<message>.*)", RegexOptions.Compiled);
			//Regex generalError = new Regex(@"(?<error>.+)\s+(?<number>[\d\w]+):\s+(?<message>.*)", RegexOptions.Compiled);
			
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
			return new DefaultCompilerResult(cr, compilerOutput.ToString());
		}
		
		
		private static CompilerError CreateErrorFromString(string error_string)
		{
			// When IncludeDebugInformation is true, prevents the debug symbols stats from braeking this.
			if (error_string.StartsWith ("WROTE SYMFILE") ||
			    error_string.StartsWith ("OffsetTable") ||
			    error_string.StartsWith ("Compilation succeeded") ||
			    error_string.StartsWith ("Compilation failed") || 
			    error_string.StartsWith("MonoBASIC") || 
			    error_string.StartsWith("Type:"))
				return null;

			CompilerError error = new CompilerError();

			Match match=regexError.Match(error_string);
			if (!match.Success) return null;
			if (String.Empty != match.Result("${file}"))
				error.FileName=match.Result("${file}");
			if (String.Empty != match.Result("${line}"))
				error.Line=Int32.Parse(match.Result("${line}"));
			if (String.Empty != match.Result("${column}"))
				error.Column=Int32.Parse(match.Result("${column}"));
			if (match.Result("${level}")=="warning")
				error.IsWarning=true;
			error.ErrorNumber=match.Result("${number}");
			error.ErrorText=match.Result("${message}");
			return error;
		}
		
		public void GenerateMakefile (Project project, Combine parentCombine)
		{
			StreamWriter stream = new StreamWriter (Path.Combine (project.BaseDirectory, "Makefile." + project.Name.Replace (" ", "")));

			DotNetProjectConfiguration configuration = (DotNetProjectConfiguration) project.ActiveConfiguration;
			VBCompilerParameters compilerparameters = (VBCompilerParameters) configuration.CompilationParameters;
			
			//special case for module?
			string outputName = configuration.CompiledOutputName;

			string target = "";
			string relativeOutputDir = fileUtilityService.AbsoluteToRelativePath (project.BaseDirectory, parentCombine.OutputDirectory);

			switch (configuration.CompileTarget) {
			case CompileTarget.Exe:
				target = "exe";
				break;
			case CompileTarget.WinExe:
				target = "winexe";
				break;
			case CompileTarget.Library:
				target = "library";
				break;
			//no module yet
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

			SystemAssemblyService sas = (SystemAssemblyService)ServiceManager.GetService (typeof (SystemAssemblyService));
			foreach (ProjectReference lib in project.ProjectReferences) {
				switch (lib.ReferenceType) {
				case ReferenceType.Gac:
					string pkg = sas.GetPackageFromFullName (lib.Reference);
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

			stream.Write ("MBAS_OPTIONS = ");
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
			
			stream.Write ("\tmbas $(MBAS_OPTIONS) -target:{0} -out:\"{1}\"", target, outputName);
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
	}
}
