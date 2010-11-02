//  VBBindingCompilerServices.cs
//
//  This file was derived from a file from #Develop, and relicensed
//  by Markus Palme to MIT/X11
//
//  Authors:
//    Markus Palme <MarkusPalme@gmx.de>
//    Rolf Bjarne Kvinge <RKvinge@novell.com>
//
//  Copyright (C) 2001-2007 Markus Palme <MarkusPalme@gmx.de>
//  Copyright (C) 2008 Novell, Inc (http://www.novell.com)
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

using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.CodeDom.Compiler;

using MonoDevelop.Core;
using MonoDevelop.Core.Execution;
using MonoDevelop.Projects;
using MonoDevelop.Projects.Text;

namespace MonoDevelop.VBNetBinding {
	
	/// <summary>
	/// This class controls the compilation of VB.net files and VB.net projects
	/// </summary>
	public class VBBindingCompilerServices
	{
		//matches "/home/path/Default.aspx.vb (40,31) : Error VBNC30205: Expected end of statement."
		//and "Error : VBNC99999: vbnc crashed nearby this location in the source code."
		//and "Error : VBNC99999: Unexpected error: Object reference not set to an instance of an object" 
		static Regex regexError = new Regex (@"^\s*((?<file>.*)\s?\((?<line>\d*)(,(?<column>\d*))?\) : )?(?<level>\w+) :? ?(?<number>[^:]*): (?<message>.*)$",
		                                     RegexOptions.Compiled | RegexOptions.ExplicitCapture);
		
		string GenerateOptions (DotNetProjectConfiguration configuration, VBCompilerParameters compilerparameters, VBProjectParameters projectparameters, string outputFileName)
		{
			DotNetProject project = (DotNetProject) configuration.ParentItem;
			StringBuilder sb = new StringBuilder ();
			
			sb.AppendFormat ("\"-out:{0}\"", outputFileName);
			sb.AppendLine ();
			
			sb.AppendLine ("-nologo");
			sb.AppendLine ("-utf8output");
			sb.AppendLine ("-quiet");

			sb.AppendFormat ("-debug:{0}", compilerparameters.DebugType);
			sb.AppendLine ();

			if (compilerparameters.Optimize)
				sb.AppendLine ("-optimize+");

			
			if (projectparameters.OptionStrict)
				sb.AppendLine ("-optionstrict+");
			else
				sb.AppendLine ("-optionstrict-");
			
			if (projectparameters.OptionExplicit)
				sb.AppendLine ("-optionexplicit+");
			else
				sb.AppendLine ("-optionexplicit-");

			if (projectparameters.BinaryOptionCompare)
				sb.AppendLine ("-optioncompare:binary");
			else
				sb.AppendLine ("-optioncompare:text");

			if (projectparameters.OptionInfer)
				sb.AppendLine ("-optioninfer+");
			else
				sb.AppendLine ("-optioninfer-");

			string mytype = projectparameters.MyType;
			if (!string.IsNullOrEmpty (mytype)) {
				sb.AppendFormat ("-define:_MYTYPE=\\\"{0}\\\"", mytype);
				sb.AppendLine ();
			}
			
			string win32IconPath = projectparameters.ApplicationIcon;
			if (!string.IsNullOrEmpty (win32IconPath) && File.Exists (win32IconPath)) {
				sb.AppendFormat ("\"-win32icon:{0}\"", win32IconPath);
				sb.AppendLine ();
			}

			if (!string.IsNullOrEmpty (projectparameters.CodePage)) {
				TextEncoding enc = TextEncoding.GetEncoding (projectparameters.CodePage);
				sb.AppendFormat ("-codepage:{0}", enc.CodePage);
				sb.AppendLine ();
			}
			
			if (!string.IsNullOrEmpty (project.DefaultNamespace)) {
				sb.AppendFormat ("-rootnamespace:{0}", project.DefaultNamespace);
				sb.AppendLine ();
			}
			
			if (!string.IsNullOrEmpty (compilerparameters.DefineConstants)) {
				sb.AppendFormat ("\"-define:{0}\"", compilerparameters.DefineConstants);
				sb.AppendLine ();
			}

			if (compilerparameters.DefineDebug)
				sb.AppendLine ("-define:DEBUG=-1");

			if (compilerparameters.DefineTrace)
				sb.AppendLine ("-define:TRACE=-1");

			if (compilerparameters.WarningsDisabled) {
				sb.AppendLine ("-nowarn");
			} else if (!string.IsNullOrEmpty (compilerparameters.NoWarn)) {
				sb.AppendFormat ("-nowarn:{0}", compilerparameters.NoWarn);
				sb.AppendLine ();
			}

			if (!string.IsNullOrEmpty (compilerparameters.WarningsAsErrors)) {
				sb.AppendFormat ("-warnaserror+:{0}", compilerparameters.WarningsAsErrors);
				sb.AppendLine ();
			}
			
			if (configuration.SignAssembly) {
				if (File.Exists (configuration.AssemblyKeyFile)) {
					sb.AppendFormat ("\"-keyfile:{0}\"", configuration.AssemblyKeyFile);
					sb.AppendLine ();
				}
			}

			if (!string.IsNullOrEmpty (compilerparameters.DocumentationFile)) {
				sb.AppendFormat ("\"-doc:{0}\"", compilerparameters.DocumentationFile);
				sb.AppendLine ();
			}

			if (!string.IsNullOrEmpty (projectparameters.StartupObject) && projectparameters.StartupObject != "Sub Main") {
				sb.Append ("-main:");
				sb.Append (projectparameters.StartupObject);
				sb.AppendLine ();
			}

			if (compilerparameters.RemoveIntegerChecks)
				sb.AppendLine ("-removeintchecks+");
			
			if (!string.IsNullOrEmpty (compilerparameters.AdditionalParameters)) {
				sb.Append (compilerparameters.AdditionalParameters);
				sb.AppendLine ();
			}
						
			switch (configuration.CompileTarget) {
				case CompileTarget.Exe:
					sb.AppendLine ("-target:exe");
					break;
				case CompileTarget.WinExe:
					sb.AppendLine ("-target:winexe");
					break;
				case CompileTarget.Library:
					sb.AppendLine ("-target:library");
					break;
				case CompileTarget.Module:
					sb.AppendLine ("-target:module");
					break;
				default:
					throw new NotSupportedException("unknown compile target:" + configuration.CompileTarget);
			}
			
			return sb.ToString();
		}
		
		public BuildResult Compile (ProjectItemCollection items, DotNetProjectConfiguration configuration, ConfigurationSelector configSelector, IProgressMonitor monitor)
		{
			VBCompilerParameters compilerparameters = (VBCompilerParameters) configuration.CompilationParameters;
			if (compilerparameters == null)
				compilerparameters = new VBCompilerParameters ();
			
			VBProjectParameters projectparameters = (VBProjectParameters) configuration.ProjectParameters;
			if (projectparameters == null)
				projectparameters = new VBProjectParameters ();
			
			string exe = configuration.CompiledOutputName;
			string responseFileName = Path.GetTempFileName();
			StreamWriter writer = new StreamWriter (responseFileName);
			
			writer.WriteLine (GenerateOptions (configuration, compilerparameters, projectparameters, exe));

			// Write references
			foreach (ProjectReference lib in items.GetAll<ProjectReference> ()) {
				foreach (string fileName in lib.GetReferencedFileNames (configSelector)) {
					writer.Write ("\"-r:");
					writer.Write (fileName);
					writer.WriteLine ("\"");
				}
			}
			
			// Write source files and embedded resources
			foreach (ProjectFile finfo in items.GetAll<ProjectFile> ()) {
				if (finfo.Subtype != Subtype.Directory) {
					switch (finfo.BuildAction) {
						case "Compile":
							writer.WriteLine("\"" + finfo.Name + "\"");
						break;
						
						case "EmbeddedResource":
							string fname = finfo.Name;
							if (String.Compare (Path.GetExtension (fname), ".resx", true) == 0)
								fname = Path.ChangeExtension (fname, ".resources");

							writer.WriteLine("\"-resource:{0},{1}\"", fname, finfo.ResourceId);
							break;
						
						default:
							continue;
					}
				}
			}
			
			// Write source files and embedded resources
			foreach (Import import in items.GetAll<Import> ()) {
				writer.WriteLine ("-imports:{0}", import.Include);
			}
			
			TempFileCollection tf = new TempFileCollection ();
			writer.Close();
			
			string output = "";
			string compilerName = configuration.TargetRuntime.GetToolPath (configuration.TargetFramework, "vbc");
			if (compilerName == null) {
				BuildResult res = new BuildResult ();
				res.AddError (string.Format ("Visual Basic .NET compiler not found ({0})", configuration.TargetRuntime.DisplayName));
				return res;
			}
			
			string outstr = String.Concat (compilerName, " @", responseFileName);
			
			
			string workingDir = ".";
			if (configuration.ParentItem != null)
				workingDir = configuration.ParentItem.BaseDirectory;
			int exitCode;
			
			var envVars = configuration.TargetRuntime.GetToolsExecutionEnvironment (configuration.TargetFramework);
			
			monitor.Log.WriteLine (Path.GetFileName (compilerName) + " " + string.Join (" ", File.ReadAllLines (responseFileName)));
			exitCode = DoCompilation (outstr, tf, workingDir, envVars, ref output);
			
			monitor.Log.WriteLine (output);			                                                          
			BuildResult result = ParseOutput (tf, output);
			if (result.Errors.Count == 0 && exitCode != 0) {
				// Compilation failed, but no errors?
				// Show everything the compiler said.
				result.AddError (output);
			}
			
			FileService.DeleteFile (responseFileName);
			if (configuration.CompileTarget != CompileTarget.Library) {
				WriteManifestFile (exe);
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
		
		
		private static CompilerError CreateErrorFromString (string error_string)
		{
			Match match;
			int i;
			
			match = regexError.Match (error_string);
			    
			if (match.Success) {
				CompilerError error = new CompilerError ();

				error.IsWarning = match.Result ("${level}").ToLowerInvariant () == "warning";
				error.ErrorNumber = match.Result("${number}");
				error.ErrorText = match.Result("${message}");
				error.FileName = match.Result ("${file}").Trim ();
				if (int.TryParse (match.Result ("${line}"), out i))
					error.Line = i;
				if (int.TryParse (match.Result ("${column}"), out i))
					error.Column = i;
				
				// Workaround for bug #484351. Vbnc incorrectly emits this warning.
				if (error.ErrorNumber == "VBNC2009" && error.ErrorText != null && error.ErrorText.IndexOf ("optioninfer") != -1)
					return null;
				
				return error;
			}

			return null;
		}
		
		private int DoCompilation (string outstr, TempFileCollection tf, string working_dir, ExecutionEnvironment envVars, ref string output)
		{
			StringWriter outwr = new StringWriter ();
			string[] tokens = outstr.Split (' ');			
			try {
				outstr = outstr.Substring (tokens[0].Length+1);
				
				ProcessStartInfo pinfo = new ProcessStartInfo (tokens[0], "\"" + outstr + "\"");
				pinfo.WorkingDirectory = working_dir;
				envVars.MergeTo (pinfo);
			
				pinfo.UseShellExecute = false;
				pinfo.RedirectStandardOutput = true;
				pinfo.RedirectStandardError = true;
				
				ProcessWrapper pw = Runtime.ProcessService.StartProcess (pinfo, outwr, outwr, null);
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
