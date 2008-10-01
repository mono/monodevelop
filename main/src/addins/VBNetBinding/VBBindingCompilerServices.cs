//  VBBindingCompilerServices.cs
//
//  This file was derived from a file from #Develop. 
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
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
		//matches "/home/path/Default.aspx.vb (40,31) : Error VBNC30205: Expected end of statement."
		//and "Error : VBNC99999: vbnc crashed nearby this location in the source code."
		//and "Error : VBNC99999: Unexpected error: Object reference not set to an instance of an object" 
		static Regex regexError = new Regex (@"^\s*((?<file>.*)\((?<line>\d*),(?<column>\d*)\) : )?(?<level>\w+) :? ?(?<number>[^:]*): (?<message>.*)$",
		                                     RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		
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
			
			if (configuration.DebugMode) {
//				sb.Append("--debug+");sb.Append(Environment.NewLine);
				sb.Append("-debug:full");sb.Append(Environment.NewLine);
			}
			
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
			
			DotNetProject dp = configuration.ParentItem as DotNetProject;
			if (dp != null && !string.IsNullOrEmpty (dp.DefaultNamespace)) {
				sb.Append("-rootnamespace:").Append('"').Append(dp.DefaultNamespace).Append('"').Append(Environment.NewLine);
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
			
			if (!String.IsNullOrEmpty (compilerparameters.AdditionalParameters)) {
				sb.Append(compilerparameters.AdditionalParameters);sb.Append(Environment.NewLine);
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
		
		public BuildResult Compile (ProjectFileCollection projectFiles, ProjectReferenceCollection references, DotNetProjectConfiguration configuration, IProgressMonitor monitor)
		{
			VBCompilerParameters compilerparameters = (VBCompilerParameters) configuration.CompilationParameters;
			if (compilerparameters == null) compilerparameters = new VBCompilerParameters ();
			
			string exe = configuration.CompiledOutputName;
			string responseFileName = Path.GetTempFileName();
			StreamWriter writer = new StreamWriter(responseFileName);
			
			writer.WriteLine(GenerateOptions (configuration, compilerparameters, exe));
			
			foreach (ProjectReference lib in references) {
				foreach (string fileName in lib.GetReferencedFileNames(configuration.Id))
					writer.WriteLine(String.Concat("-r:", fileName));
			}
			
			// write source files and embedded resources
			foreach (ProjectFile finfo in projectFiles) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
						case "Compile":
							writer.WriteLine("\"" + finfo.Name + "\"");
						break;
						
						case "EmbeddedResource":
							string fname = finfo.Name;
							if (String.Compare (Path.GetExtension (fname), ".resx", true) == 0)
								fname = Path.ChangeExtension (fname, ".resources");

							writer.WriteLine(@"""-resource:{0},{1}""", fname, finfo.ResourceId);
							break;
						default:
							continue;
					}
				}
			}
			
			TempFileCollection tf = new TempFileCollection ();
			writer.Close();
			
			string output = "";
			string compilerName = GetCompilerName(compilerparameters.VBCompilerVersion);
			string outstr = String.Concat(compilerName, " @", responseFileName);
			
			
			string workingDir = ".";
			if (projectFiles != null && projectFiles.Count > 0)
				workingDir = projectFiles [0].Project.BaseDirectory;
			int exitCode;
			
			monitor.Log.WriteLine (compilerName + " " + string.Join (" ", File.ReadAllLines (responseFileName)));
			exitCode = DoCompilation (outstr, tf, workingDir, ref output);
			
			monitor.Log.WriteLine (output);			                                                          
			BuildResult result = ParseOutput (tf, output);
			if (result.Errors.Count == 0 && exitCode != 0) {
				// Compilation failed, but no errors?
				// Show everything the compiler said.
				result.AddError (output);
			}
			
			FileService.DeleteFile (responseFileName);
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
		
		BuildResult ParseOutput(TempFileCollection tf, string output)
		{
			CompilerResults results = new CompilerResults (tf);

			using (StringReader sr = new StringReader (output)) {			
				while (true) {
					string curLine = sr.ReadLine();

					if (curLine == null) {
						break;
					}
					
					curLine = curLine.Trim();
					if (curLine.Length == 0) {
						continue;
					}
					
					CompilerError error = CreateErrorFromString (curLine);
					
					if (error != null)
						results.Errors.Add (error);
				}
			}
			return new BuildResult (results, output);
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
			if (!match.Success) {
				return null;
			}
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
		
		private int DoCompilation (string outstr, TempFileCollection tf, string working_dir, ref string output)
		{
			StringWriter outwr = new StringWriter ();
			string[] tokens = outstr.Split (' ');			
			try {
				outstr = outstr.Substring (tokens[0].Length+1);
				ProcessWrapper pw = Runtime.ProcessService.StartProcess (tokens[0], "\"" + outstr + "\"", working_dir, outwr, outwr, null);
				pw.WaitForOutput ();
				output = outwr.ToString ();
				
				return pw.ExitCode;
			} finally {
				if (outwr != null)
					outwr.Dispose ();
			}
		}
	}
}
