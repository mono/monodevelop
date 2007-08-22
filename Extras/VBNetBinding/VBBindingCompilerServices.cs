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
using System.Threading;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Core.Gui;
using MonoDevelop.Core.Gui.Components;
using MonoDevelop.Projects;

namespace VBBinding {
	
	/// <summary>
	/// This class controls the compilation of VB.net files and VB.net projects
	/// </summary>
	public class VBBindingCompilerServices
	{	
		static Regex regexError = new Regex (@"^(\s*(?<file>.*)\((?<line>\d*)(,(?<column>\d*))?\)\s+)*(?<level>\w+)\s*(?<number>.*):\s(?<message>.*)",
		RegexOptions.Compiled | RegexOptions.ExplicitCapture);

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
			return "vbnc";
		}
		
		string GenerateOptions (DotNetProjectConfiguration configuration, VBCompilerParameters compilerparameters, string outputFileName)
		{
			StringBuilder sb = new StringBuilder();
			bool hasWin32Resource = false;
			
			sb.Append("-out:");sb.Append("\"" + outputFileName + "\"");/*sb.Append('"');*/sb.Append(Environment.NewLine);
			
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
			
			if (compilerparameters.Win32Resource != null && compilerparameters.Win32Resource.Length > 0 && File.Exists(compilerparameters.Win32Resource)) {
				sb.Append("-win32resource:");sb.Append('"');sb.Append(compilerparameters.Win32Resource);sb.Append('"');sb.Append(Environment.NewLine);
				hasWin32Resource = true;
			}
			if (compilerparameters.Win32Icon != null && compilerparameters.Win32Icon.Length > 0 && File.Exists(compilerparameters.Win32Icon)) {
				if (hasWin32Resource)
					Console.WriteLine ("Warning: Both Win32 icon and Win32 resource cannot be specified. Ignoring the icon.");
				else
					sb.Append("-win32icon:");sb.Append('"');sb.Append(compilerparameters.Win32Icon);sb.Append('"');sb.Append(Environment.NewLine);
			}
			
			if (compilerparameters.RootNamespace!= null && compilerparameters.RootNamespace.Length > 0) {
				sb.Append("-rootnamespace:");sb.Append('"');sb.Append(compilerparameters.RootNamespace);sb.Append('"');sb.Append(Environment.NewLine);
			}
			
			if (compilerparameters.DefineSymbols.Length > 0) {
				sb.Append("-define:");sb.Append('"');sb.Append(compilerparameters.DefineSymbols);sb.Append('"');sb.Append(Environment.NewLine);
			}
			if (configuration.SignAssembly) {
				if (File.Exists (configuration.AssemblyKeyFile)) {
					sb.Append("-keyfile:");sb.Append('"');sb.Append(configuration.AssemblyKeyFile);sb.Append('"');sb.Append(Environment.NewLine);
				}
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
			StreamWriter writer = new StreamWriter(responseFileName);
			
			writer.WriteLine(GenerateOptions (configuration, compilerparameters, exe));
			
			foreach (ProjectReference lib in references) {
				foreach (string fileName in lib.GetReferencedFileNames())
					writer.WriteLine(String.Concat("-r:", fileName));
			}
			
			string resgen = "resgen";
			//ClrVersion.Default ?
			if (configuration.ClrVersion == ClrVersion.Net_2_0)
				resgen = "resgen2";

			// write source files and embedded resources
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
						case BuildAction.Compile:
							writer.WriteLine("\"" + finfo.Name + "\"");
						break;
						
						case BuildAction.EmbedAsResource:
							//FIXME: Duplicate of code from CSharpBindingCompilerManager
							string fname = finfo.Name;
							string resourceId = GetResourceId (finfo, ref fname, resgen, monitor);
							if (resourceId == null)
								continue;

							writer.WriteLine(@"""-resource:{0},{1}""", fname, resourceId);
							break;
						default:
							continue;
					}
				}
			}
			
			TempFileCollection tf = new TempFileCollection ();
			writer.Close();
			
			string output = "";
			string error  = "";
			string compilerName = GetCompilerName(compilerparameters.VBCompilerVersion);
			string outstr = String.Concat(compilerName, " @", responseFileName);
			
			
			string workingDir = ".";
			if (projectFiles != null && projectFiles.Count > 0)
				workingDir = projectFiles [0].Project.BaseDirectory;

			DoCompilation (outstr, tf, workingDir, ref output, ref error);
			
			ICompilerResult result = ParseOutput(tf, output);
			ParseOutput(tf,error);
			
			Runtime.FileService.DeleteFile (responseFileName);
			Runtime.FileService.DeleteFile (output);
			Runtime.FileService.DeleteFile (error);
			if (configuration.CompileTarget != CompileTarget.Library) {
				WriteManifestFile(exe);
			}
			return result;
		}
		
		string GetResourceId (ProjectFile finfo, ref string fname, string resgen, IProgressMonitor monitor)
		{
			string resourceId = finfo.ResourceId;
			if (resourceId == null) {
				Console.WriteLine ("Warning: Unable to build ResourceId for {0}. Ignoring.", fname);
				monitor.Log.WriteLine ("Warning: Unable to build ResourceId for {0}. Ignoring.", fname);
				return null;
			}

			if (String.Compare (Path.GetExtension (fname), ".resx", true) != 0)
				return resourceId;

			//Check whether resgen required
			FileInfo finfo_resx = new FileInfo (fname);
			FileInfo finfo_resources = new FileInfo (Path.ChangeExtension (fname, ".resources"));
			if (finfo_resx.LastWriteTime < finfo_resources.LastWriteTime) {
				fname = Path.ChangeExtension (fname, ".resources");
				return null;
			}

			using (StringWriter sw = new StringWriter ()) {
				Console.WriteLine ("Compiling resources\n{0}$ {1} /compile {2}", Path.GetDirectoryName (fname), resgen, fname);
				monitor.Log.WriteLine (GettextCatalog.GetString (
					"Compiling resource {0} with {1}", fname, resgen));
				ProcessWrapper pw = null;
				try {
					ProcessStartInfo info = Runtime.ProcessService.CreateProcessStartInfo (
									resgen, String.Format ("/compile \"{0}\"", fname),
									Path.GetDirectoryName (fname), false);
					if (PlatformID.Unix == Environment.OSVersion.Platform)
						info.EnvironmentVariables ["MONO_IOMAP"] = "drive";
					pw = Runtime.ProcessService.StartProcess (info, sw, sw, null);
				} catch (System.ComponentModel.Win32Exception ex) {
					Console.WriteLine (GettextCatalog.GetString (
						"Error while trying to invoke '{0}' to compile resource '{1}' :\n {2}", resgen, fname, ex.ToString ()));
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Error while trying to invoke '{0}' to compile resource '{1}' :\n {2}", resgen, fname, ex.Message));

					return null;
				}

				//FIXME: Handle exceptions
				pw.WaitForOutput ();

				if (pw.ExitCode == 0) {
					fname = Path.ChangeExtension (fname, ".resources");
				} else {
					Console.WriteLine (GettextCatalog.GetString (
						"Unable to compile ({0}) {1} to .resources. Ignoring. \nReason: \n{2}\n",
						resgen, fname, sw.ToString ()));
					monitor.Log.WriteLine (GettextCatalog.GetString (
						"Unable to compile ({0}) {1} to .resources. Ignoring. \nReason: \n{2}\n",
						resgen, fname, sw.ToString ()));

					return null;
				}
			}

			return resourceId;
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
		
		ICompilerResult ParseOutput(TempFileCollection tf, string file)
		{
			StringBuilder compilerOutput = new StringBuilder();
			
			StreamReader sr = File.OpenText(file);
			
			CompilerResults cr = new CompilerResults(tf);
			
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
		
		private void DoCompilation(string outstr, TempFileCollection tf, string working_dir, ref string output, ref string error) {
			output = Path.GetTempFileName();
			error = Path.GetTempFileName();
			
			StreamWriter outwr = new StreamWriter(output);
			StreamWriter errwr = new StreamWriter(error);
			string[] tokens = outstr.Split(' ');

			outstr = outstr.Substring(tokens[0].Length+1);

			ProcessService ps = (ProcessService) ServiceManager.GetService (typeof(ProcessService));
			ProcessWrapper pw = ps.StartProcess(tokens[0], "\"" + outstr + "\"", working_dir, outwr, errwr, delegate{});
			pw.WaitForOutput();
			outwr.Close();
			errwr.Close();
		}
	}
}
